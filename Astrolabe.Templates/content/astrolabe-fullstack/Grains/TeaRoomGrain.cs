using AstrolabeApp.Models;
using Orleans.Runtime;

namespace AstrolabeApp.Grains;

/// <summary>
/// A tea room grain that coordinates multiple kettles and manages tea orders.
/// Demonstrates grain-to-grain communication and state management.
/// </summary>
public class TeaRoomGrain : Grain, ITeaRoomGrain
{
    private readonly IPersistentState<TeaRoomState> _state;
    private readonly IGrainFactory _grainFactory;
    private IGrainTimer? _orderProcessingTimer;

    public TeaRoomGrain(
        [PersistentState("roomState", "teaRoomStore")] IPersistentState<TeaRoomState> state,
        IGrainFactory grainFactory
    )
    {
        _state = state;
        _grainFactory = grainFactory;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Initialize with some default kettles if this is a new tea room
        if (_state.State.KettleIds.Count == 0)
        {
            var roomId = this.GetPrimaryKeyString();
            _state.State.KettleIds.Add($"{roomId}-kettle-1");
            _state.State.KettleIds.Add($"{roomId}-kettle-2");
            await _state.WriteStateAsync();
        }

        // Start a timer to process pending orders
        _orderProcessingTimer = this.RegisterGrainTimer(
            async (_, ct) => await ProcessPendingOrders(),
            state: new object(),
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromSeconds(2),
                Period = TimeSpan.FromSeconds(2),
            }
        );

        await base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(
        DeactivationReason reason,
        CancellationToken cancellationToken
    )
    {
        _orderProcessingTimer?.Dispose();
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task<TeaOrder> OrderTea(TeaType teaType, string customerName)
    {
        var order = new TeaOrder
        {
            Id = Guid.NewGuid(),
            TeaType = teaType,
            CustomerName = customerName,
            OrderedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            QueuePosition = _state.State.PendingOrders.Count + 1,
        };

        _state.State.PendingOrders.Add(order);
        _state.State.AllOrders.Add(order);

        await _state.WriteStateAsync();

        // Try to immediately assign to an available kettle
        await TryAssignOrderToKettle(order);

        return order;
    }

    public async Task<TeaRoomStatus> GetStatus()
    {
        var kettleStatuses = new List<KettleStatus>();
        foreach (var kettleId in _state.State.KettleIds)
        {
            var kettle = _grainFactory.GetGrain<ITeaKettleGrain>(kettleId);
            kettleStatuses.Add(await kettle.GetStatus());
        }

        var completedToday = _state.State.AllOrders.Count(o =>
            o.Status == OrderStatus.Collected && o.CompletedAt?.Date == DateTime.UtcNow.Date
        );

        var teaCounts = _state
            .State.AllOrders.Where(o => o.OrderedAt.Date == DateTime.UtcNow.Date)
            .GroupBy(o => o.TeaType)
            .Select(g => new { TeaType = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefault();

        return new TeaRoomStatus
        {
            RoomId = this.GetPrimaryKeyString(),
            Kettles = kettleStatuses,
            PendingOrderCount = _state.State.PendingOrders.Count,
            CompletedOrdersToday = completedToday,
            AvailableKettles = kettleStatuses.Count(k => k.IsAvailable),
            MostPopularTeaToday = teaCounts?.TeaType,
        };
    }

    public Task<List<TeaOrder>> GetPendingOrders()
    {
        return Task.FromResult(_state.State.PendingOrders.ToList());
    }

    public Task<List<TeaOrder>> GetOrderHistory(int limit = 50)
    {
        return Task.FromResult(
            _state.State.AllOrders.OrderByDescending(o => o.OrderedAt).Take(limit).ToList()
        );
    }

    public Task<TeaOrder?> CheckOrder(Guid orderId)
    {
        var order = _state.State.AllOrders.FirstOrDefault(o => o.Id == orderId);
        return Task.FromResult(order);
    }

    public async Task<BrewedTea?> CollectOrder(Guid orderId)
    {
        var order = _state.State.AllOrders.FirstOrDefault(o => o.Id == orderId);
        if (order is null || order.Status != OrderStatus.Ready || order.AssignedKettleId is null)
        {
            return null;
        }

        var kettle = _grainFactory.GetGrain<ITeaKettleGrain>(order.AssignedKettleId);
        var brewedTea = await kettle.CollectTea();

        if (brewedTea != null)
        {
            order.Status = OrderStatus.Collected;
            order.CompletedAt = DateTime.UtcNow;
            await _state.WriteStateAsync();
        }

        return brewedTea;
    }

    public async Task AddKettle(string kettleId)
    {
        if (!_state.State.KettleIds.Contains(kettleId))
        {
            _state.State.KettleIds.Add(kettleId);
            await _state.WriteStateAsync();
        }
    }

    public async Task RemoveKettle(string kettleId)
    {
        _state.State.KettleIds.Remove(kettleId);
        await _state.WriteStateAsync();
    }

    private async Task ProcessPendingOrders()
    {
        // Update brewing orders to check if they're ready
        var brewingOrders = _state
            .State.AllOrders.Where(o =>
                o.Status == OrderStatus.Brewing && o.AssignedKettleId != null
            )
            .ToList();

        foreach (var order in brewingOrders)
        {
            var kettle = _grainFactory.GetGrain<ITeaKettleGrain>(order.AssignedKettleId!);
            var status = await kettle.GetStatus();

            if (status.CurrentSession?.State == BrewingState.Ready)
            {
                order.Status = OrderStatus.Ready;
            }
        }

        // Try to assign pending orders to available kettles
        foreach (var order in _state.State.PendingOrders.ToList())
        {
            await TryAssignOrderToKettle(order);
        }

        await _state.WriteStateAsync();
    }

    private async Task TryAssignOrderToKettle(TeaOrder order)
    {
        foreach (var kettleId in _state.State.KettleIds)
        {
            var kettle = _grainFactory.GetGrain<ITeaKettleGrain>(kettleId);
            var status = await kettle.GetStatus();

            if (status.IsAvailable)
            {
                try
                {
                    var session = await kettle.StartBrewing(order.TeaType, order.CustomerName);
                    order.Status = OrderStatus.Brewing;
                    order.AssignedKettleId = kettleId;
                    order.BrewingSessionId = session.Id;
                    _state.State.PendingOrders.Remove(order);

                    // Update queue positions for remaining pending orders
                    for (int i = 0; i < _state.State.PendingOrders.Count; i++)
                    {
                        _state.State.PendingOrders[i].QueuePosition = i + 1;
                    }

                    await _state.WriteStateAsync();
                    return;
                }
                catch
                {
                    // Kettle might have become unavailable, try next one
                }
            }
        }
    }
}

[GenerateSerializer]
public class TeaRoomState
{
    [Id(0)]
    public List<string> KettleIds { get; set; } = new();

    [Id(1)]
    public List<TeaOrder> PendingOrders { get; set; } = new();

    [Id(2)]
    public List<TeaOrder> AllOrders { get; set; } = new();
}
