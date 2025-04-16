using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

// A process step to gather information about a product
public class GatherProductInfoStep : KernelProcessStep
{
    [KernelFunction]
    public async Task GatherProductInformationAsync(string productName, KernelProcessStepContext context)
    {
        Console.WriteLine($"{nameof(GatherProductInfoStep)}:\n\tGathering product information for product named {productName}");

        // Create a DocumentInfo object
        var productInfo = new DocumentInfo
        {
            Id = Guid.NewGuid().ToString(),
            Title = productName,
            Content = """
            Product Description:
            GlowBrew is a revolutionary AI driven coffee machine with industry leading number of LEDs and programmable light shows. The machine is also capable of brewing coffee and has a built in grinder.

            Product Features:
            1. **Luminous Brew Technology**: Customize your morning ambiance with programmable LED lights that sync with your brewing process.
            2. **AI Taste Assistant**: Learns your taste preferences over time and suggests new brew combinations to explore.
            3. **Gourmet Aroma Diffusion**: Built-in aroma diffusers enhance your coffee's scent profile, energizing your senses before the first sip.

            Troubleshooting:
            - **Issue**: LED Lights Malfunctioning
                - **Solution**: Reset the lighting settings via the app. Ensure the LED connections inside the GlowBrew are secure. Perform a factory reset if necessary.
            """
        };

        // Emit the event to pass productInfo to the next step
        await context.EmitEventAsync("ProductInfoGathered", productInfo);
    }
}

// A process step to generate documentation for a product
public class GenerateDocumentationStep : KernelProcessStep<GeneratedDocumentationState>
{
    private GeneratedDocumentationState _state = new();

    private string systemPrompt =
            """
            Your job is to write high quality and engaging customer facing documentation for a new product from Contoso. You will be provide with information
            about the product in the form of internal documentation, specs, and troubleshooting guides and you must use this information and
            nothing else to generate the documentation. If suggestions are provided on the documentation you create, take the suggestions into account and
            rewrite the documentation. Make sure the product sounds amazing.
            """;

    // Called by the process runtime when the step instance is activated. Use this to load state that may be persisted from previous activations.
    override public ValueTask ActivateAsync(KernelProcessStepState<GeneratedDocumentationState> state)
    {
        this._state = state.State!;
        this._state.ChatHistory ??= new ChatHistory(systemPrompt);

        return base.ActivateAsync(state);
    }

    [KernelFunction]
    public async Task GenerateDocumentationAsync(Kernel kernel, KernelProcessStepContext context, DocumentInfo productInfo)
    {
        Console.WriteLine($"[{nameof(GenerateDocumentationStep)}]:\tGenerating documentation for provided productInfo...");

        // Add the new product info to the chat history
        this._state.ChatHistory!.AddUserMessage($"Product Info:\n{productInfo.Title} - {productInfo.Content}");

        // Get a response from the LLM
        IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var generatedDocumentationResponse = await chatCompletionService.GetChatMessageContentAsync(this._state.ChatHistory!);

        DocumentInfo generatedContent = new()
        {
            Id = Guid.NewGuid().ToString(),
            Title = $"Generated document - {productInfo.Title}",
            Content = generatedDocumentationResponse.Content!,
        };

        this._state!.LastGeneratedDocument = generatedContent;

        await context.EmitEventAsync("DocumentationGenerated", generatedContent);
    }

}

// A process step to publish documentation
public class PublishDocumentationStep : KernelProcessStep
{
    [KernelFunction]
    public DocumentInfo PublishDocumentation(DocumentInfo document)
    {
        // For example purposes we just write the generated docs to the console
        Console.WriteLine($"[{nameof(PublishDocumentationStep)}]:\tPublishing product documentation approved by user: \n{document.Title}\n{document.Content}");
        return document;
    }
}

// Custom classes must be serializable
public class DocumentInfo
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class GeneratedDocumentationState
{
    public DocumentInfo LastGeneratedDocument { get; set; } = new();
    public ChatHistory? ChatHistory { get; set; }
}