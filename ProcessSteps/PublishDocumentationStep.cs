using Microsoft.SemanticKernel;
using ProcessSK.Data;

namespace ProcessSK.ProcessSteps;

// A process step to publish documentation
public class PublishDocumentationStep : KernelProcessStep
{
    [KernelFunction]
    public DocumentInfo PublishDocumentation(DocumentInfo document)
    {
        // For example purposes we just write the generated docs to the console
        Console.WriteLine($"[{nameof(PublishDocumentationStep)}]:\n\tPublishing product documentation approved by user: \n{document.Title}\n{document.Content}");
        return document;
    }
}