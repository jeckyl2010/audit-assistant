namespace AuditAssistant.Core.Models;

/// <summary>
/// Configuration options for RAG (Retrieval-Augmented Generation) operations
/// </summary>
public class RAGOptions
{
    /// <summary>
    /// Number of chunks to retrieve from vector search (default: 10)
    /// </summary>
    public int VectorSearchTopK { get; set; } = 10;

    /// <summary>
    /// Maximum number of chunks to send to AI for analysis (default: 5)
    /// </summary>
    public int MaxChunksForAnalysis { get; set; } = 5;

    /// <summary>
    /// Minimum similarity score threshold for relevance (0.0 to 1.0, default: 0.5)
    /// </summary>
    public double MinimumSimilarityThreshold { get; set; } = 0.5;

    /// <summary>
    /// Maximum number of requirements to analyze concurrently (default: 5)
    /// Helps respect API rate limits while maintaining performance
    /// </summary>
    public int MaxConcurrentRequirements { get; set; } = 5;

    /// <summary>
    /// Expected embedding dimension (default: 1536 for text-embedding-3-small)
    /// </summary>
    public int EmbeddingDimension { get; set; } = 1536;
}
