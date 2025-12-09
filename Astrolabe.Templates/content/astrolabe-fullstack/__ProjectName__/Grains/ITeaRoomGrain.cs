using Astrolabe.Annotation;
using AstrolabeApp.Models;

namespace AstrolabeApp.Grains;

/// <summary>
/// Represents a tea room that manages multiple kettles and coordinates tea service.
/// There is typically one tea room per location (e.g., "main-office", "break-room").
/// </summary>
public interface ITeaRoomGrain : IGrainWithStringKey
{
    /// <summary>
    /// Request a cup of tea. The tea room will find an available kettle and start brewing.
    /// </summary>
    Task<TeaOrder> OrderTea(TeaType teaType, string customerName);

    /// <summary>
    /// Get the status of all kettles in this tea room.
    /// </summary>
    Task<TeaRoomStatus> GetStatus();

    /// <summary>
    /// Get pending orders waiting for an available kettle.
    /// </summary>
    Task<List<TeaOrder>> GetPendingOrders();

    /// <summary>
    /// Get the order history for this tea room.
    /// </summary>
    Task<List<TeaOrder>> GetOrderHistory(int limit = 50);

    /// <summary>
    /// Check if a specific order is ready for pickup.
    /// </summary>
    Task<TeaOrder?> CheckOrder(Guid orderId);

    /// <summary>
    /// Collect a ready tea order.
    /// </summary>
    Task<BrewedTea?> CollectOrder(Guid orderId);

    /// <summary>
    /// Add a kettle to this tea room.
    /// </summary>
    Task AddKettle(string kettleId);

    /// <summary>
    /// Remove a kettle from this tea room.
    /// </summary>
    Task RemoveKettle(string kettleId);
}

[GenerateSerializer]
public class TeaOrder
{
    [Id(0)]
    public Guid Id { get; set; }

    [Id(1)]
    public TeaType TeaType { get; set; }

    [Id(2)]
    public string CustomerName { get; set; } = "";

    [Id(3)]
    public DateTime OrderedAt { get; set; }

    [Id(4)]
    public DateTime? CompletedAt { get; set; }

    [Id(5)]
    public OrderStatus Status { get; set; }

    [Id(6)]
    public string? AssignedKettleId { get; set; }

    [Id(7)]
    public Guid? BrewingSessionId { get; set; }

    [Id(8)]
    public int QueuePosition { get; set; }
}

[GenerateSerializer]
public class TeaRoomStatus
{
    [Id(0)]
    public string RoomId { get; set; } = "";

    [Id(1)]
    public List<KettleStatus> Kettles { get; set; } = new();

    [Id(2)]
    public int PendingOrderCount { get; set; }

    [Id(3)]
    public int CompletedOrdersToday { get; set; }

    [Id(4)]
    public int AvailableKettles { get; set; }

    [Id(5)]
    public TeaType? MostPopularTeaToday { get; set; }
}

[JsonString]
public enum OrderStatus
{
    Pending,
    Brewing,
    Ready,
    Collected,
    Cancelled,
}
