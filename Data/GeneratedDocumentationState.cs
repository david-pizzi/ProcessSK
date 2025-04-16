using Microsoft.SemanticKernel;

namespace ProcessSK.Data;

public class GeneratedDocumentationState
{
    // The latest draft of the document
    public DocumentInfo LastGeneratedDocument { get; set; } = new();

    // Serializable form of chat memory
    public List<ChatMessageContent> ChatLog { get; set; } = new();

    // The number of revisions attempted
    public int RevisionsAttempted { get; set; } = 0;
}
