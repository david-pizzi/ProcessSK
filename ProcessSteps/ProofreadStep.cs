using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ProcessSK.Data;

namespace ProcessSK.ProcessSteps;

// A process step to proofread documentation
public class ProofreadStep : KernelProcessStep
{
    [KernelFunction]
    public async Task ProofreadDocumentationAsync(Kernel kernel, KernelProcessStepContext context, DocumentInfo documentation)
    {
        Console.WriteLine($"{nameof(ProofreadDocumentationAsync)}:\n\tProofreading documentation...");

        var systemPrompt =
            """
            Your job is to proofread customer-facing documentation for a new product from Contoso. You will be provided with proposed documentation
            and must do the following:

            1. Determine if the documentation meets these criteria:
               - Uses a professional tone.
               - Free of spelling or grammar mistakes.
               - Free of offensive or inappropriate language.
               - Technically accurate.

            2. If it does NOT meet the criteria, provide:
               - A brief explanation.
               - A list of detailed feedback of the changes that are needed to improve the documentation.

            Respond in JSON using this schema:
            {
              "Explanation": "...",
              "Suggestions": ["...", "..."]
            }
            """;

        var chatHistory = new ChatHistory(systemPrompt);
        chatHistory.AddUserMessage(documentation.Content);

        var settings = new OpenAIPromptExecutionSettings
        {
            ResponseFormat = typeof(ProofreadingResponse)
        };

        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var result = await chatService.GetChatMessageContentAsync(chatHistory, executionSettings: settings);

        var feedback = JsonSerializer.Deserialize<ProofreadingResponse>(result.Content!.ToString(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (feedback is null || feedback.Suggestions.Count == 0)
        {
            Console.WriteLine($"\n\tDocumentation PASSED.\n");
            await context.EmitEventAsync("DocumentationApproved", documentation);
        }
        else
        {
            Console.WriteLine($"\n\tDocumentation FAILED.\n\tExplanation: {feedback.Explanation}\n\tSuggestions: {string.Join("\n\t\t", feedback.Suggestions)}");

            await context.EmitEventAsync("DocumentationRejected", feedback);
        }
    }
}
