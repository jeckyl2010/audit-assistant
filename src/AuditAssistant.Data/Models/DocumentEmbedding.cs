using Pgvector;

namespace AuditAssistant.Data.Models;

public class DocumentEmbedding
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid? ChunkId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public Vector Embedding { get; set; } = null!;
    public string MetadataJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
