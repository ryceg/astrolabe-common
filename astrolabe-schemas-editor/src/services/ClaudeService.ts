import { ViewContext, EditableForm, ConversationMessage } from "../types";
import { ControlDefinition, SchemaField } from "@react-typed-forms/schemas";
import { Control } from "@react-typed-forms/core";

export interface ClaudeResponse {
  response: string;
  success: boolean;
  updatedFormDefinition?: ControlDefinition[];
}

interface ProcessCommandRequest {
  command: string;
  currentFormDefinition: ControlDefinition[];
  schema: SchemaField[];
  conversationHistory: ConversationMessage[];
  systemPrompt?: string;
  selectedControl?: ControlDefinition;
}

interface StreamChunk {
  type: "text" | "tool_result" | "done" | "error";
  content: string;
  toolCall?: any;
  error?: string;
}


export class ClaudeService {
  private apiUrl: string;

  constructor(apiUrl: string) {
    this.apiUrl = apiUrl.endsWith("/") ? apiUrl.slice(0, -1) : apiUrl;
  }

  /**
   * Clear conversation history for a specific form
   */
  clearHistory(currentForm: Control<EditableForm>): void {
    // Use Control API to properly update the conversation history
    if (currentForm.fields.conversationHistory) {
      currentForm.fields.conversationHistory.value = [];
    }
  }

  /**
   * Get conversation history for a specific form
   */
  getHistory(currentForm: Control<EditableForm>): ConversationMessage[] {
    return currentForm.fields.conversationHistory?.value || [];
  }

  /**
   * Process an agent command using Claude - uses streaming internally by default
   */
  async processCommand(
    command: string,
    currentForm: Control<EditableForm | undefined>,
    context: ViewContext,
  ): Promise<ClaudeResponse> {
    // Use streaming internally but return a Promise
    return new Promise<ClaudeResponse>((resolve, reject) => {
      this.processCommandStream(
        command,
        currentForm,
        context,
        () => {}, // No-op for chunks since we only care about final result
        (response) => {
          if (response) {
            resolve(response);
          } else {
            reject(new Error("No response received"));
          }
        }
      ).catch(reject);
    });
  }

  /**
   * Process an agent command with streaming response
   */
  async processCommandStream(
    command: string,
    currentForm: Control<EditableForm | undefined>,
    context: ViewContext,
    onChunk: (chunk: string) => void,
    onComplete?: (response: ClaudeResponse) => void,
  ): Promise<void> {
    const editableForm = currentForm.value;
    if (!editableForm) {
      throw "No selected form";
    }

    try {
      const formControl = currentForm as Control<EditableForm>;

      // Initialize conversation history if it doesn't exist
      const conversationHistoryControl = formControl.fields.conversationHistory;
      if (!conversationHistoryControl?.value) {
        if (conversationHistoryControl) {
          conversationHistoryControl.value = [];
        }
      }

      const currentFormDefinition = editableForm.formTree.getRootDefinitions().value;
      const conversationHistory = conversationHistoryControl?.value || [];

      // Get schema information
      let schema: SchemaField[] = [];
      try {
        schema = context.getSchemaForForm(formControl).getRootFields().value;
      } catch (error) {
        console.warn("Could not get schema for form:", error);
      }

      // Get currently selected control if any
      const selectedControl = editableForm.selectedControl?.form.definition;

      const request: ProcessCommandRequest = {
        command,
        currentFormDefinition,
        schema,
        conversationHistory,
        selectedControl
      };

      // Call the streaming form assistant endpoint
      const fetchResponse = await fetch(`${this.apiUrl}/form-assistant/stream-command`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(request),
      });

      if (!fetchResponse.ok) {
        const errorText = await fetchResponse.text();
        throw new Error(`HTTP ${fetchResponse.status}: ${errorText}`);
      }

      const reader = fetchResponse.body?.getReader();
      const decoder = new TextDecoder();

      let accumulatedResponse = "";
      let toolCall: any = null;

      if (reader) {
        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          const chunk = decoder.decode(value, { stream: true });
          const lines = chunk.split('\n');

          for (const line of lines) {
            if (line.startsWith('data: ')) {
              const data = line.slice(6).trim();

              // Skip empty data
              if (!data) continue;

              try {
                const streamChunk: StreamChunk = JSON.parse(data);

                if (streamChunk.type === "text" && streamChunk.content) {
                  // Text content streaming
                  accumulatedResponse += streamChunk.content;
                  onChunk(streamChunk.content);
                } else if (streamChunk.type === "tool_result" && streamChunk.toolCall) {
                  // Tool call result with form definition
                  toolCall = streamChunk.toolCall;
                } else if (streamChunk.type === "done") {
                  // Stream completed
                  if (onComplete) {
                    const response: ClaudeResponse = {
                      response: accumulatedResponse,
                      success: toolCall != null,
                      updatedFormDefinition: toolCall?.form_definition
                    };
                    onComplete(response);
                  }
                  return;
                } else if (streamChunk.type === "error") {
                  throw new Error(streamChunk.error || "Streaming error");
                }
              } catch (parseError) {
                console.warn("Failed to parse stream chunk:", data, parseError);
              }
            }
          }
        }
      }

      // Add messages to history
      if (conversationHistoryControl) {
        const newUserMessage: ConversationMessage = {
          role: "user",
          content: command,
        };
        const newAssistantMessage: ConversationMessage = {
          role: "assistant",
          content: accumulatedResponse,
        };

        conversationHistoryControl.setValue((prev) => [
          ...(prev || []),
          newUserMessage,
          newAssistantMessage,
        ]);
      }

    } catch (error) {
      console.error("Claude streaming error:", error);
      onChunk(`Error: ${error instanceof Error ? error.message : "Unknown error"}`);
    }
  }


}
