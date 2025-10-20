using System.Runtime.CompilerServices;
using System.Text.Json;
using Astrolabe.TestTemplate.Models;
using Microsoft.Extensions.AI;

namespace Astrolabe.TestTemplate.Service;

/// <summary>
/// Service that uses IChatClient (from Anthropic SDK) to provide form-building assistance.
/// Uses Microsoft.Extensions.AI abstractions via Anthropic SDK's built-in support.
/// </summary>
public class FormAssistantService
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<FormAssistantService> _logger;
    private readonly string _modelId;

    public FormAssistantService(
        IChatClient chatClient,
        ILogger<FormAssistantService> logger,
        IConfiguration configuration)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _modelId = configuration["Anthropic:ModelId"]
            ?? throw new InvalidOperationException("Anthropic:ModelId is not configured");
    }

    /// <summary>
    /// Process a form modification command and return a structured response
    /// </summary>
    public async Task<ProcessCommandResponse> ProcessCommand(ProcessCommandRequest request)
    {
        try
        {
            var systemPrompt = BuildSystemPrompt(
                request.Schema,
                request.CurrentFormDefinition,
                request.SelectedControl
            );
            var chatMessages = BuildChatHistory(
                systemPrompt,
                request.ConversationHistory,
                request.Command
            );

            var chatOptions = new ChatOptions
            {
                ModelId = _modelId,
                MaxOutputTokens = 8192,
                Temperature = 1.0f,
                Tools = GetFormTools(),
            };

            _logger.LogInformation(
                "Processing command with {MessageCount} messages",
                chatMessages.Count
            );

            var response = await _chatClient.GetResponseAsync(chatMessages, chatOptions);

            return ProcessChatResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing command: {Command}", request.Command);
            return new ProcessCommandResponse
            {
                Response = $"Error processing command: {ex.Message}",
                Success = false,
            };
        }
    }

    /// <summary>
    /// Stream a form modification command with real-time updates using Server-Sent Events
    /// Cannot use try-catch with yield return, so errors are returned as error chunks
    /// </summary>
    public async IAsyncEnumerable<StreamChunk> StreamCommand(
        ProcessCommandRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var systemPrompt = BuildSystemPrompt(
            request.Schema,
            request.CurrentFormDefinition,
            request.SelectedControl
        );
        var chatMessages = BuildChatHistory(
            systemPrompt,
            request.ConversationHistory,
            request.Command
        );

        var chatOptions = new ChatOptions
        {
            ModelId = _modelId,
            MaxOutputTokens = 8192,
            Temperature = 1.0f,
            Tools = GetFormTools(),
        };

        _logger.LogInformation(
            "Starting streaming command with {MessageCount} messages",
            chatMessages.Count
        );

        var toolCalls = new List<FunctionCallContent>();

        await foreach (
            var update in _chatClient.GetStreamingResponseAsync(
                chatMessages,
                chatOptions,
                cancellationToken
            )
        )
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Streaming cancelled by client");
                yield break;
            }

            // Handle text content streaming
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return new StreamChunk { Type = "text", Content = update.Text };
            }

            // Handle function/tool calls
            if (update.Contents != null)
            {
                foreach (var content in update.Contents)
                {
                    if (content is FunctionCallContent functionCall)
                    {
                        toolCalls.Add(functionCall);
                    }
                }
            }

            // Handle completion with tool calls
            if (update.FinishReason == ChatFinishReason.ToolCalls && toolCalls.Count > 0)
            {
                foreach (var toolCall in toolCalls)
                {
                    if (toolCall.Name == "update_form_definition")
                    {
                        var args = toolCall.Arguments;
                        if (
                            args != null
                            && args.TryGetValue("explanation", out var explanation)
                            && args.TryGetValue("form_definition", out var formDef)
                        )
                        {
                            yield return new StreamChunk
                            {
                                Type = "tool_result",
                                Content = explanation?.ToString() ?? "",
                                ToolCall = new
                                {
                                    name = "update_form_definition",
                                    form_definition = formDef,
                                },
                            };
                        }
                    }
                }
            }
        }

        yield return new StreamChunk { Type = "done", Content = string.Empty };
        _logger.LogInformation("Streaming completed successfully");
    }

    #region Private Helper Methods

    private string BuildSystemPrompt(
        JsonElement[] schema,
        JsonElement[] currentFormDefinition,
        JsonElement? selectedControl)
    {
        var formInfo = ExtractFormInfo(currentFormDefinition);
        var schemaInfo = JsonSerializer.Serialize(
            schema,
            new JsonSerializerOptions { WriteIndented = true }
        );

        var selectedControlInfo = string.Empty;
        if (selectedControl.HasValue)
        {
            var selectedJson = JsonSerializer.Serialize(
                selectedControl.Value,
                new JsonSerializerOptions { WriteIndented = true }
            );
            selectedControlInfo = $@"

Currently Selected/Highlighted Control:
{selectedJson}

NOTE: When the user says 'this control', 'the selected control', 'here', or similar context-dependent references, they are referring to the control shown above.";
        }

        return $@"You are an AI assistant that helps users modify form schemas using structured tools. You have deep knowledge of the Astrolabe control definition system.

Current Form:
- Fields: {string.Join(", ", formInfo.Fields)}

Schema:
{schemaInfo}{selectedControlInfo}

## Schema-First Approach

**ALWAYS reference the provided schema to:**
- Verify field existence before creating Data controls
- Use correct field types and validation
- Respect enumValues for dropdown/selection controls
- Apply schema-defined constraints
- Understand field relationships and dependencies

## Response Requirements

**Always explain your reasoning:**
- Why you chose specific control types
- How you organized the form structure
- What schema constraints influenced decisions
- Any assumptions made about user intent

IMPORTANT: Always use the update_form_definition tool to make changes. Include the complete updated form definition, not just the changes.";
    }

    private List<ChatMessage> BuildChatHistory(
        string systemPrompt,
        ConversationMessage[] conversationHistory,
        string currentCommand
    )
    {
        var messages = new List<ChatMessage> { new(ChatRole.System, systemPrompt) };

        foreach (var msg in conversationHistory)
        {
            var role = msg.Role switch
            {
                "user" => ChatRole.User,
                "assistant" => ChatRole.Assistant,
                _ => ChatRole.User,
            };

            messages.Add(new ChatMessage(role, msg.Content));
        }

        messages.Add(new ChatMessage(ChatRole.User, currentCommand));

        return messages;
    }

    private List<AITool> GetFormTools()
    {
        return
        [
            AIFunctionFactory.Create(
                UpdateFormDefinition,
                options: new()
                {
                    Name = "update_form_definition",
                    Description =
                        "Update the form definition with new structure. Returns the complete updated form definition as an array of control definitions.",
                }
            ),
        ];
    }

    [System.ComponentModel.Description("Update the form definition with new structure")]
    private static string UpdateFormDefinition(
        [System.ComponentModel.Description("Brief explanation of what changes were made")]
            string explanation,
        [System.ComponentModel.Description("The complete updated form definition as JSON array")]
            JsonElement[] formDefinition
    )
    {
        return JsonSerializer.Serialize(new { explanation, formDefinition });
    }

    private static ProcessCommandResponse ProcessChatResponse(ChatResponse response)
    {
        // ChatResponse from Anthropic SDK via Microsoft.Extensions.AI
        // For the non-streaming API, we primarily use the streaming endpoint in production
        // This method returns the basic text response
        // Tool calls are properly handled in the StreamCommand method
        var textContent = response.Text ?? string.Empty;

        return new ProcessCommandResponse
        {
            Response = textContent,
            Success = !string.IsNullOrEmpty(textContent),
            UpdatedFormDefinition = null,
        };
    }

    private FormInfo ExtractFormInfo(JsonElement[] currentFormDefinition)
    {
        var fields = new List<string>();

        foreach (var definition in currentFormDefinition)
        {
            ExtractFieldsFromDefinition(definition, fields);
        }

        return new FormInfo { Fields = fields.ToArray() };
    }

    private void ExtractFieldsFromDefinition(JsonElement definition, List<string> fields)
    {
        if (definition.TryGetProperty("field", out var fieldProperty))
        {
            var fieldName = fieldProperty.GetString();
            if (!string.IsNullOrEmpty(fieldName))
            {
                fields.Add(fieldName);
            }
        }

        if (
            definition.TryGetProperty("children", out var childrenProperty)
            && childrenProperty.ValueKind == JsonValueKind.Array
        )
        {
            foreach (var child in childrenProperty.EnumerateArray())
            {
                ExtractFieldsFromDefinition(child, fields);
            }
        }
    }

    #endregion

    #region Helper Classes

    private class FormInfo
    {
        public string[] Fields { get; set; } = Array.Empty<string>();
    }

    #endregion
}
