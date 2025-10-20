using System.Text;
using System.Text.Json;
using Astrolabe.TestTemplate.Models;
using Astrolabe.TestTemplate.Service;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.TestTemplate.Controllers;

/// <summary>
/// Controller for AI-powered form building assistance using Claude via Microsoft.Extensions.AI
/// </summary>
[ApiController]
[Route("api/form-assistant")]
public class FormAssistantController : ControllerBase
{
    private readonly FormAssistantService _assistantService;
    private readonly ILogger<FormAssistantController> _logger;

    public FormAssistantController(
        FormAssistantService assistantService,
        ILogger<FormAssistantController> logger
    )
    {
        _assistantService = assistantService;
        _logger = logger;
    }

    /// <summary>
    /// Process a command using Claude with structured response (non-streaming)
    /// </summary>
    /// <param name="request">The command processing request</param>
    /// <returns>Processed command response</returns>
    [HttpPost("process-command")]
    [ProducesResponseType(typeof(ProcessCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProcessCommandResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProcessCommandResponse>> ProcessCommand(
        [FromBody] ProcessCommandRequest request
    )
    {
        if (request == null)
        {
            return BadRequest(
                new ProcessCommandResponse
                {
                    Response = "Request body is required",
                    Success = false,
                }
            );
        }

        _logger.LogInformation("Processing command: {Command}", request.Command);

        var response = await _assistantService.ProcessCommand(request);
        return Ok(response);
    }

    /// <summary>
    /// Stream a command using Claude with real-time Server-Sent Events (SSE)
    /// This endpoint uses proper SSE streaming with IAsyncEnumerable
    /// </summary>
    /// <param name="request">The command processing request</param>
    /// <returns>Server-sent events stream</returns>
    [HttpPost("stream-command")]
    [Produces("text/event-stream")]
    public async Task StreamCommand([FromBody] ProcessCommandRequest request)
    {
        if (request == null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("Request body is required");
            return;
        }

        _logger.LogInformation("Streaming command: {Command}", request.Command);

        // Configure response for Server-Sent Events
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no"); // Disable nginx buffering

        // CORS headers if needed (configure properly in production)
        if (Request.Headers.ContainsKey("Origin"))
        {
            Response.Headers.Append("Access-Control-Allow-Origin", Request.Headers.Origin!);
            Response.Headers.Append("Access-Control-Allow-Credentials", "true");
        }

        try
        {
            await foreach (
                var chunk in _assistantService.StreamCommand(request, HttpContext.RequestAborted)
            )
            {
                await SendSseEvent(chunk);

                // Check for client disconnection
                if (HttpContext.RequestAborted.IsCancellationRequested)
                {
                    _logger.LogDebug("Client disconnected, stopping stream");
                    break;
                }

                // Stop if we received a done or error event
                if (chunk.Type is "done" or "error")
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Stream cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during streaming command");

            var errorChunk = new StreamChunk { Type = "error", Error = ex.Message };

            await SendSseEvent(errorChunk);
        }
    }

    /// <summary>
    /// Health check endpoint for the form assistant service
    /// </summary>
    /// <returns>Health status with configuration info</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> HealthCheck()
    {
        return Ok(
            new
            {
                status = "healthy",
                service = "FormAssistant",
                provider = "Anthropic Claude via Microsoft.Extensions.AI",
                timestamp = DateTime.UtcNow,
            }
        );
    }

    #region Private Helper Methods

    /// <summary>
    /// Send a Server-Sent Event following the SSE specification
    /// Format: event: [event-type]\ndata: [json-data]\n\n
    /// </summary>
    private async Task SendSseEvent(StreamChunk chunk)
    {
        var eventBuilder = new StringBuilder();

        // Optional: Add event type (useful for client-side event filtering)
        if (!string.IsNullOrEmpty(chunk.Type))
        {
            eventBuilder.AppendLine($"event: {chunk.Type}");
        }

        // Serialize the chunk to JSON
        var jsonData = JsonSerializer.Serialize(
            chunk,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System
                    .Text
                    .Json
                    .Serialization
                    .JsonIgnoreCondition
                    .WhenWritingNull,
            }
        );

        // SSE data lines (can be multiline, each prefixed with "data: ")
        eventBuilder.AppendLine($"data: {jsonData}");

        // Empty line to signal end of event
        eventBuilder.AppendLine();

        // Write to response stream
        await Response.WriteAsync(eventBuilder.ToString(), HttpContext.RequestAborted);
        await Response.Body.FlushAsync(HttpContext.RequestAborted);
    }

    #endregion
}
