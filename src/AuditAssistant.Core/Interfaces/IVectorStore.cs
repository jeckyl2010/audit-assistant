namespace AuditAssistant.Core.Interfaces;

public interface IVectorStore
{
    Task StoreDocumentAsync(
        Guid documentId, 
        string content, 
        float[] embedding, 
        Dictionary<string, string> metadata, 
        CancellationToken cancellationToken = default);

    Task StoreChunkAsync(
        Guid documentId,
        Guid chunkId,
        int chunkIndex,
        string content,
        float[] embedding,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default);

    Task StoreChunksBatchAsync(
        IEnumerable<(Guid DocumentId, Guid ChunkId, int ChunkIndex, string Content, float[] Embedding, Dictionary<string, string> Metadata)> chunks,
        CancellationToken cancellationToken = default);

    Task<bool> ChunkExistsAsync(Guid chunkId, CancellationToken cancellationToken = default);

    Task<List<(Guid DocumentId, string Content, double Similarity)>> SearchSimilarAsync(
        float[] queryEmbedding, 
        int topK = 10, 
        CancellationToken cancellationToken = default);

    Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
}
