using AstrolabeApp.Models;
using Orleans.Runtime;

namespace AstrolabeApp.Grains;

/// <summary>
/// A tea kettle grain that simulates brewing tea with realistic timing.
/// Uses Orleans timers to simulate the brewing process.
/// </summary>
public class TeaKettleGrain : Grain, ITeaKettleGrain
{
    private readonly IPersistentState<TeaKettleState> _state;
    private IGrainTimer? _brewingTimer;

    public TeaKettleGrain(
        [PersistentState("kettleState", "teaRoomStore")] IPersistentState<TeaKettleState> state
    )
    {
        _state = state;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // If there was a brewing session in progress when the grain was deactivated,
        // check if it should have completed by now
        if (_state.State.CurrentSession is { State: BrewingState.Heating or BrewingState.Steeping })
        {
            var elapsed = DateTime.UtcNow - _state.State.CurrentSession.StartedAt;
            if (elapsed.TotalSeconds >= _state.State.CurrentSession.BrewTimeSeconds)
            {
                _state.State.CurrentSession.State = BrewingState.Ready;
                _state.State.CurrentSession.CompletedAt =
                    _state.State.CurrentSession.StartedAt.AddSeconds(
                        _state.State.CurrentSession.BrewTimeSeconds
                    );
            }
            else
            {
                // Resume the brewing timer
                var remaining =
                    TimeSpan.FromSeconds(_state.State.CurrentSession.BrewTimeSeconds) - elapsed;
                ScheduleBrewingCompletion(remaining);
            }
        }

        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(
        DeactivationReason reason,
        CancellationToken cancellationToken
    )
    {
        _brewingTimer?.Dispose();
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task<BrewingSession> StartBrewing(
        TeaType teaType,
        string orderedBy,
        string? flavorNotes = null,
        int? brewTimeSeconds = null
    )
    {
        if (
            _state.State.CurrentSession is
            { State: BrewingState.Heating or BrewingState.Steeping or BrewingState.Ready }
        )
        {
            throw new InvalidOperationException(
                "Kettle is currently in use. Please wait or use another kettle."
            );
        }

        // Use provided values or fall back to defaults
        var brewTime = brewTimeSeconds ?? GetDefaultBrewTimeForTeaType(teaType);
        var notes = flavorNotes ?? GetDefaultFlavorNotes(teaType);

        var session = new BrewingSession
        {
            Id = Guid.NewGuid(),
            TeaType = teaType,
            OrderedBy = orderedBy,
            StartedAt = DateTime.UtcNow,
            State = BrewingState.Heating,
            BrewTimeSeconds = brewTime,
            FlavorNotes = notes,
        };

        _state.State.CurrentSession = session;
        _state.State.BrewingHistory.Add(session);
        _state.State.TotalBrewsToday++;

        await _state.WriteStateAsync();

        // Schedule the brewing completion
        ScheduleBrewingCompletion(TimeSpan.FromSeconds(brewTime));

        return session;
    }

    public Task<KettleStatus> GetStatus()
    {
        var secondsRemaining = 0;
        if (_state.State.CurrentSession is { State: BrewingState.Heating or BrewingState.Steeping })
        {
            var elapsed = (DateTime.UtcNow - _state.State.CurrentSession.StartedAt).TotalSeconds;
            secondsRemaining = Math.Max(
                0,
                _state.State.CurrentSession.BrewTimeSeconds - (int)elapsed
            );
        }

        return Task.FromResult(
            new KettleStatus
            {
                KettleId = this.GetPrimaryKeyString(),
                IsAvailable =
                    _state.State.CurrentSession
                        is null
                            or { State: BrewingState.Collected or BrewingState.Cancelled },
                CurrentSession = _state.State.CurrentSession,
                TotalBrewsToday = _state.State.TotalBrewsToday,
                SecondsRemaining = secondsRemaining,
            }
        );
    }

    public async Task<bool> CancelBrewing()
    {
        if (
            _state.State.CurrentSession
            is not { State: BrewingState.Heating or BrewingState.Steeping }
        )
        {
            return false;
        }

        _brewingTimer?.Dispose();
        _brewingTimer = null;

        _state.State.CurrentSession.State = BrewingState.Cancelled;
        _state.State.CurrentSession.CompletedAt = DateTime.UtcNow;

        await _state.WriteStateAsync();
        return true;
    }

    public async Task<BrewedTea?> CollectTea()
    {
        if (_state.State.CurrentSession is not { State: BrewingState.Ready })
        {
            return null;
        }

        var session = _state.State.CurrentSession;
        session.State = BrewingState.Collected;
        session.CompletedAt ??= DateTime.UtcNow;

        var brewedTea = new BrewedTea
        {
            SessionId = session.Id,
            TeaType = session.TeaType,
            OrderedBy = session.OrderedBy,
            BrewedAt = session.CompletedAt.Value,
            BrewDuration = session.CompletedAt.Value - session.StartedAt,
            FlavorNotes = session.FlavorNotes,
        };

        _state.State.CurrentSession = null;
        await _state.WriteStateAsync();

        return brewedTea;
    }

    public Task<List<BrewingSession>> GetBrewingHistory()
    {
        return Task.FromResult(_state.State.BrewingHistory.TakeLast(50).ToList());
    }

    private void ScheduleBrewingCompletion(TimeSpan delay)
    {
        _brewingTimer?.Dispose();

        // First transition to steeping after half the time
        var steepingDelay = delay / 2;
        _brewingTimer = this.RegisterGrainTimer(
            async (_, ct) =>
            {
                if (_state.State.CurrentSession?.State == BrewingState.Heating)
                {
                    _state.State.CurrentSession.State = BrewingState.Steeping;
                    await _state.WriteStateAsync();

                    // Schedule final completion
                    _brewingTimer = this.RegisterGrainTimer(
                        async (_, ct2) =>
                        {
                            if (_state.State.CurrentSession?.State == BrewingState.Steeping)
                            {
                                _state.State.CurrentSession.State = BrewingState.Ready;
                                _state.State.CurrentSession.CompletedAt = DateTime.UtcNow;
                                await _state.WriteStateAsync();
                            }
                        },
                        state: new object(),
                        new GrainTimerCreationOptions
                        {
                            DueTime = steepingDelay,
                            Period = Timeout.InfiniteTimeSpan,
                        }
                    );
                }
            },
            state: new object(),
            new GrainTimerCreationOptions
            {
                DueTime = steepingDelay,
                Period = Timeout.InfiniteTimeSpan,
            }
        );
    }

    private static int GetDefaultBrewTimeForTeaType(TeaType teaType) =>
        teaType switch
        {
            TeaType.Black => 15, // 15 seconds for demo (normally 3-5 minutes)
            TeaType.Green => 12, // 12 seconds for demo
            TeaType.Oolong => 18, // 18 seconds for demo
            TeaType.White => 10, // 10 seconds for demo
            TeaType.Herbal => 20, // 20 seconds for demo (herbal needs longer)
            TeaType.Rooibos => 20, // 20 seconds for demo
            TeaType.Purple => 15, // 15 seconds for demo
            TeaType.Peppermint => 12, // 12 seconds for demo
            _ => 15,
        };

    private static string GetDefaultFlavorNotes(TeaType teaType) =>
        teaType switch
        {
            TeaType.Black => "Bold and robust with malty undertones",
            TeaType.Green => "Fresh and grassy with a delicate sweetness",
            TeaType.Oolong => "Complex and floral with a smooth finish",
            TeaType.White => "Light and subtle with hints of honey",
            TeaType.Herbal => "Aromatic and soothing, caffeine-free",
            TeaType.Rooibos => "Naturally sweet with earthy notes",
            TeaType.Purple => "Unique and antioxidant-rich with berry hints",
            TeaType.Peppermint => "Cool and refreshing with a clean finish",
            _ => "A delightful cup of tea",
        };
}

[GenerateSerializer]
public class TeaKettleState
{
    [Id(0)]
    public BrewingSession? CurrentSession { get; set; }

    [Id(1)]
    public List<BrewingSession> BrewingHistory { get; set; } = new();

    [Id(2)]
    public int TotalBrewsToday { get; set; }
}
