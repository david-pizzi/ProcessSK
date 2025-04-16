using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using ProcessSK.Data;

namespace ProcessSK.ProcessSteps;

public class GenerateDocumentationStep : KernelProcessStep<GeneratedDocumentationState>
{
    private GeneratedDocumentationState _state = new();
    private ChatHistory? _chatHistory;

    private readonly string systemPrompt =
        """
        Your job is to write high quality and engaging customer facing documentation for a new product from Contoso. You will be provide with information
        about the product in the form of internal documentation, specs, and troubleshooting guides and you must use this information and
        nothing else to generate the documentation. If suggestions are provided on the documentation you create, take the suggestions into account and
        rewrite the documentation. Make sure the product sounds amazing.
        """;

    public override ValueTask ActivateAsync(KernelProcessStepState<GeneratedDocumentationState> state)
    {
        _state = state.State!;
        _chatHistory = new ChatHistory(systemPrompt);

        // Rebuild chat history from stored messages
        foreach (var message in _state.ChatLog)
        {
            _chatHistory.AddMessage(message.Role, message.Content!);
        }

        return base.ActivateAsync(state);
    }

    [KernelFunction("GenerateDocumentationAsync")]
    public async Task GenerateDocumentationAsync(Kernel kernel, KernelProcessStepContext context, DocumentInfo productInfo)
    {
        Console.WriteLine($"[{nameof(GenerateDocumentationAsync)}]:\n\tGenerating documentation for: {productInfo.Title}");

        _chatHistory!.AddUserMessage($"Product Info:\n{productInfo.Content}");

        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var result = await chatService.GetChatMessageContentAsync(_chatHistory);

        var document = new DocumentInfo
        {
            Id = Guid.NewGuid().ToString(),
            Title = $"Generated Documentation - {productInfo.Title}",
            Content = result.Content!
        };

        _state.LastGeneratedDocument = document;
        _state.ChatLog = _chatHistory.ToList(); // persist new chat state

        await context.EmitEventAsync("DocumentationGenerated", document);
    }

    [KernelFunction("ApplySuggestionsAsync")]
    public async Task ApplySuggestionsAsync(Kernel kernel, KernelProcessStepContext context, ProofreadingResponse feedback)
    {
        _state.RevisionsAttempted++;

        Console.WriteLine($"Revision attempt #{_state.RevisionsAttempted}");

        Console.WriteLine($"[{nameof(ApplySuggestionsAsync)}]:\n\tApplying suggestions to improve documentation...");

        var lastDoc = _state.LastGeneratedDocument;
        var suggestions = string.Join("\n", feedback.Suggestions);

        _chatHistory!.AddUserMessage(
            $"Rewrite the following documentation using these suggestions:\n\n" +
            $"Documentation:\n{lastDoc.Content}\n\n" +
            $"Suggestions:\n{suggestions}"
        );

        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var result = await chatService.GetChatMessageContentAsync(_chatHistory);

        var revised = new DocumentInfo
        {
            Id = Guid.NewGuid().ToString(),
            Title = $"Revised Documentation - {lastDoc.Title}",
            Content = result.Content!
        };

        _state.LastGeneratedDocument = revised;
        _state.ChatLog = _chatHistory.ToList(); // update persisted chat

        if (_state.RevisionsAttempted >= 3)
        {
            Console.WriteLine("Max revisions reached — sending to human for final approval.");

            await context.EmitEventAsync("NeedsFinalApproval", revised);
        }
        else
        {
            Console.WriteLine("Revisions applied. Sending revised documentation to prrof reading.");
            await context.EmitEventAsync("DocumentationGenerated", revised);
        }
    }
}
