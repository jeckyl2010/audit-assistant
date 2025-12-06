namespace AuditAssistant.Core.Models;

public class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
    public string SectionTitle { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
