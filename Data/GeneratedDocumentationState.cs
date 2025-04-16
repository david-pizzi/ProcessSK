using Microsoft.SemanticKernel.ChatCompletion;

namespace ProcessSK.Data;

public class GeneratedDocumentationState
{
    public DocumentInfo LastGeneratedDocument { get; set; } = new();
    public ChatHistory? ChatHistory { get; set; }
}
