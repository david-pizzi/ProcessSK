using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using ProcessSK.ProcessSteps;

class Program
{
    static async Task Main(string[] args)
    {
        // Load configuration from user secrets
        var config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        var deployment = config["AZURE_OPENAI_DEPLOYMENT"];
        var endpoint = config["AZURE_OPENAI_ENDPOINT"];
        var key = config["AZURE_OPENAI_KEY"];

        if (string.IsNullOrWhiteSpace(deployment) ||
            string.IsNullOrWhiteSpace(endpoint) ||
            string.IsNullOrWhiteSpace(key))
        {
            Console.WriteLine("ERROR: Missing Azure OpenAI credentials.");
            return;
        }

        // Configure the kernel with your LLM connection details
        Kernel kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(deployment, endpoint, key)
            .Build();

        // Create the process builder
        ProcessBuilder processBuilder = new("DocumentationGeneration");

        // Add the steps
        var infoGatheringStep = processBuilder.AddStepFromType<GatherProductInfoStep>();
        var docsGenerationStep = processBuilder.AddStepFromType<GenerateDocumentationStep>();
        var docsPublishStep = processBuilder.AddStepFromType<PublishDocumentationStep>();

        // Orchestrate the events
        processBuilder
            .OnInputEvent("Start")
            .SendEventTo(new(infoGatheringStep));

        infoGatheringStep
            .OnEvent("ProductInfoGathered")
            .SendEventTo(new(docsGenerationStep));

        docsGenerationStep
            .OnEvent("DocumentationGenerated")
            .SendEventTo(new(docsPublishStep));

        // Build and run the process
        var process = processBuilder.Build();
        await process.StartAsync(kernel, new KernelProcessEvent { Id = "Start", Data = "Contoso GlowBrew" });
    }
}