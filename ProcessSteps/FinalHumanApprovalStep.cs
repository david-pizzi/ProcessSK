using Microsoft.SemanticKernel;
using ProcessSK.Data;

namespace ProcessSK.ProcessSteps;

public class FinalHumanApprovalStep : KernelProcessStep
{
    [KernelFunction]
    public async Task FinalApprovalAsync(Kernel kernel, KernelProcessStepContext context, DocumentInfo document)
    {
        Console.WriteLine("[HUMAN REVIEW REQUIRED]");
        Console.WriteLine($"Document:\n\n{document.Content}");

        Console.WriteLine("\nApprove this document? (y/n): ");
        var input = Console.ReadLine()?.Trim().ToLower();

        if (input == "y")
        {
            await context.EmitEventAsync("DocumentationApproved", document);
        }
        else
        {
            Console.WriteLine("Human rejected. Ending process or escalate further.");
            // Optionally emit DocumentationRejected or stop here
        }
    }
}