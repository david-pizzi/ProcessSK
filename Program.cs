using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using ProcessSK.ProcessSteps;

class Program
{
    static async Task Main(string[] args)
    {
        // Load configuration
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

        var kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(deployment, endpoint, key)
            .Build();

        var processBuilder = new ProcessBuilder("DocumentationCycle");

        // Add steps
        var infoGatheringStep = processBuilder.AddStepFromType<GatherProductInfoStep>();
        var docsGenerationStep = processBuilder.AddStepFromType<GenerateDocumentationStep>();
        var docsProofreadStep = processBuilder.AddStepFromType<ProofreadStep>();
        var humanApprovalStep = processBuilder.AddStepFromType<FinalHumanApprovalStep>();
        var docsPublishStep = processBuilder.AddStepFromType<PublishDocumentationStep>();

        // Step orchestration (tutorial style)
        processBuilder
            .OnInputEvent("Start")
            .SendEventTo(new(infoGatheringStep));

        infoGatheringStep
            .OnEvent("ProductInfoGathered")
            .SendEventTo(new(docsGenerationStep, functionName: "GenerateDocumentationAsync"));

        docsGenerationStep
            .OnEvent("DocumentationGenerated")
            .SendEventTo(new(docsProofreadStep));

        docsGenerationStep
            .OnEvent("NeedsFinalApproval")
            .SendEventTo(new(humanApprovalStep));

        docsProofreadStep
            .OnEvent("DocumentationRejected")
            .SendEventTo(new(docsGenerationStep, functionName: "ApplySuggestionsAsync"));

        docsProofreadStep
            .OnEvent("DocumentationApproved")
            .SendEventTo(new(docsPublishStep));

        humanApprovalStep
            .OnEvent("DocumentationApproved")
            .SendEventTo(new(docsPublishStep));

        var process = processBuilder.Build();

        // Just run it — no persistence
        await process.StartAsync(kernel, new KernelProcessEvent
        {
            Id = "Start",
            Data = "Contoso GlowBrew"
        });

        Console.WriteLine("Process completed.");
    }
}
