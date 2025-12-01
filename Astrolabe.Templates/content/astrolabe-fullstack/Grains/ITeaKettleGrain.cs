using Astrolabe.Annotation;
using AstrolabeApp.Models;

namespace AstrolabeApp.Grains;

/// <summary>
/// Represents a tea kettle that can brew one cup of tea at a time.
/// Each kettle is identified by its location (e.g., "kitchen-1", "office-2").
/// </summary>
public interface ITeaKettleGrain : IGrainWithStringKey
{
    /// <summary>
    /// Start brewing a cup of tea. Returns immediately with a brewing session ID.
    /// </summary>
    /// <param name="teaType">The type of tea to brew</param>
    /// <param name="orderedBy">Name of the person who ordered</param>
    /// <param name="flavorNotes">Optional flavor notes for the tea (uses default if not provided)</param>
    /// <param name="brewTimeSeconds">Optional brew time in seconds (uses default for tea type if not provided)</param>
    Task<BrewingSession> StartBrewing(TeaType teaType, string orderedBy, string? flavorNotes = null, int? brewTimeSeconds = null);

    /// <summary>
    /// Get the current status of the kettle.
    /// </summary>
    Task<KettleStatus> GetStatus();

    /// <summary>
    /// Cancel the current brewing session if one is active.
    /// </summary>
    Task<bool> CancelBrewing();

    /// <summary>
    /// Collect the brewed tea. Returns null if tea is not ready.
    /// </summary>
    Task<BrewedTea?> CollectTea();

    /// <summary>
    /// Get the history of all brewing sessions for this kettle.
    /// </summary>
    Task<List<BrewingSession>> GetBrewingHistory();
}

[GenerateSerializer]
public class BrewingSession
{
    [Id(0)]
    public Guid Id { get; set; }

    [Id(1)]
    public TeaType TeaType { get; set; }

    [Id(2)]
    public string OrderedBy { get; set; } = "";

    [Id(3)]
    public DateTime StartedAt { get; set; }

    [Id(4)]
    public DateTime? CompletedAt { get; set; }

    [Id(5)]
    public BrewingState State { get; set; }

    [Id(6)]
    public int BrewTimeSeconds { get; set; }

    [Id(7)]
    public string FlavorNotes { get; set; } = "";
}

[GenerateSerializer]
public class KettleStatus
{
    [Id(0)]
    public string KettleId { get; set; } = "";

    [Id(1)]
    public bool IsAvailable { get; set; }

    [Id(2)]
    public BrewingSession? CurrentSession { get; set; }

    [Id(3)]
    public int TotalBrewsToday { get; set; }

    [Id(4)]
    public int SecondsRemaining { get; set; }
}

[GenerateSerializer]
public class BrewedTea
{
    [Id(0)]
    public Guid SessionId { get; set; }

    [Id(1)]
    public TeaType TeaType { get; set; }

    [Id(2)]
    public string OrderedBy { get; set; } = "";

    [Id(3)]
    public DateTime BrewedAt { get; set; }

    [Id(4)]
    public TimeSpan BrewDuration { get; set; }

    [Id(5)]
    public string FlavorNotes { get; set; } = "";
}

[JsonString]
public enum BrewingState
{
    NotStarted,
    Heating,
    Steeping,
    Ready,
    Collected,
    Cancelled,
}
