using AstrolabeApp.Grains;
using AstrolabeApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace AstrolabeApp.Controllers;

/// <summary>
/// API controller for the Tea Room - demonstrates Orleans grain interactions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TeaRoomController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;
    private const string DefaultRoomId = "main-tea-room";

    public TeaRoomController(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    /// <summary>
    /// Get all available tea types.
    /// </summary>
    [HttpGet("tea-types")]
    public TeaType[] GetTeaTypes()
    {
        return Enum.GetValues<TeaType>();
    }

    /// <summary>
    /// Get the status of a tea room including all kettles.
    /// </summary>
    [HttpGet("{roomId}/status")]
    public async Task<TeaRoomStatus> GetRoomStatus(string roomId)
    {
        var room = _grainFactory.GetGrain<ITeaRoomGrain>(roomId);
        return await room.GetStatus();
    }

    /// <summary>
    /// Get the status of the default tea room.
    /// </summary>
    [HttpGet("status")]
    public async Task<TeaRoomStatus> GetDefaultRoomStatus()
    {
        var room = _grainFactory.GetGrain<ITeaRoomGrain>(DefaultRoomId);
        return await room.GetStatus();
    }

    /// <summary>
    /// Order a cup of tea from the tea room.
    /// </summary>
    [HttpPost("{roomId}/order")]
    public async Task<TeaOrder> OrderTea(string roomId, [FromBody] TeaOrderRequest request)
    {
        var room = _grainFactory.GetGrain<ITeaRoomGrain>(roomId);
        return await room.OrderTea(request.TeaType, request.CustomerName);
    }

    /// <summary>
    /// Order a cup of tea from the default tea room.
    /// </summary>
    [HttpPost("order")]
    public async Task<TeaOrder> OrderTeaDefault([FromBody] TeaOrderRequest request)
    {
        var room = _grainFactory.GetGrain<ITeaRoomGrain>(DefaultRoomId);
        return await room.OrderTea(request.TeaType, request.CustomerName);
    }

    /// <summary>
    /// Check the status of a specific order.
    /// </summary>
    [HttpGet("{roomId}/order/{orderId:guid}")]
    public async Task<ActionResult<TeaOrder>> CheckOrder(string roomId, Guid orderId)
    {
        var room = _grainFactory.GetGrain<ITeaRoomGrain>(roomId);
        var order = await room.CheckOrder(orderId);
        if (order == null)
        {
            return NotFound(new { message = "Order not found" });
        }
        return order;
    }

    /// <summary>
    /// Collect a ready tea order.
    /// </summary>
    [HttpPost("{roomId}/order/{orderId:guid}/collect")]
    public async Task<ActionResult<BrewedTea>> CollectOrder(string roomId, Guid orderId)
    {
        var room = _grainFactory.GetGrain<ITeaRoomGrain>(roomId);
        var brewedTea = await room.CollectOrder(orderId);
        if (brewedTea == null)
        {
            return BadRequest(new { message = "Order is not ready for collection or has already been collected" });
        }
        return brewedTea;
    }

    /// <summary>
    /// Get pending orders in the tea room.
    /// </summary>
    [HttpGet("{roomId}/orders/pending")]
    public async Task<List<TeaOrder>> GetPendingOrders(string roomId)
    {
        var room = _grainFactory.GetGrain<ITeaRoomGrain>(roomId);
        return await room.GetPendingOrders();
    }

    /// <summary>
    /// Get order history for the tea room.
    /// </summary>
    [HttpGet("{roomId}/orders/history")]
    public async Task<List<TeaOrder>> GetOrderHistory(string roomId, [FromQuery] int limit = 50)
    {
        var room = _grainFactory.GetGrain<ITeaRoomGrain>(roomId);
        return await room.GetOrderHistory(limit);
    }

    /// <summary>
    /// Get the status of a specific kettle.
    /// </summary>
    [HttpGet("kettle/{kettleId}/status")]
    public async Task<KettleStatus> GetKettleStatus(string kettleId)
    {
        var kettle = _grainFactory.GetGrain<ITeaKettleGrain>(kettleId);
        return await kettle.GetStatus();
    }

    /// <summary>
    /// Cancel brewing on a specific kettle.
    /// </summary>
    [HttpPost("kettle/{kettleId}/cancel")]
    public async Task<ActionResult> CancelBrewing(string kettleId)
    {
        var kettle = _grainFactory.GetGrain<ITeaKettleGrain>(kettleId);
        var cancelled = await kettle.CancelBrewing();
        if (!cancelled)
        {
            return BadRequest(new { message = "No active brewing session to cancel" });
        }
        return Ok(new { message = "Brewing cancelled" });
    }

    /// <summary>
    /// Add a new kettle to a tea room.
    /// </summary>
    [HttpPost("{roomId}/kettles")]
    public async Task<ActionResult> AddKettle(string roomId, [FromBody] AddKettleRequest request)
    {
        var room = _grainFactory.GetGrain<ITeaRoomGrain>(roomId);
        await room.AddKettle(request.KettleId);
        return Ok(new { message = $"Kettle '{request.KettleId}' added to room '{roomId}'" });
    }

    /// <summary>
    /// Remove a kettle from a tea room.
    /// </summary>
    [HttpDelete("{roomId}/kettles/{kettleId}")]
    public async Task<ActionResult> RemoveKettle(string roomId, string kettleId)
    {
        var room = _grainFactory.GetGrain<ITeaRoomGrain>(roomId);
        await room.RemoveKettle(kettleId);
        return Ok(new { message = $"Kettle '{kettleId}' removed from room '{roomId}'" });
    }

    /// <summary>
    /// Get brewing history for a specific kettle.
    /// </summary>
    [HttpGet("kettle/{kettleId}/history")]
    public async Task<List<BrewingSession>> GetKettleHistory(string kettleId)
    {
        var kettle = _grainFactory.GetGrain<ITeaKettleGrain>(kettleId);
        return await kettle.GetBrewingHistory();
    }
}

public record TeaOrderRequest(TeaType TeaType, string CustomerName);
public record AddKettleRequest(string KettleId);
