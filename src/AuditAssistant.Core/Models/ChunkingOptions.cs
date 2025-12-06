namespace AuditAssistant.Core.Models;

public class ChunkingOptions
{
    /// <summary>
    /// Maximum size of a chunk in TOKENS (not characters)
    /// Default: 512 tokens (optimal for text-embedding-3-small)
    /// </summary>
    public int MaxChunkSize { get; set; } = 512;
    
    /// <summary>
    /// Number of TOKENS to overlap between chunks (not characters)
    /// Default: 50 tokens (~10% overlap)
    /// </summary>
    public int OverlapSize { get; set; } = 50;
    
    /// <summary>
    /// Chunking strategy
    /// </summary>
    public ChunkingStrategy Strategy { get; set; } = ChunkingStrategy.TokenAware;
    
    /// <summary>
    /// Whether to preserve code blocks as single units
    /// </summary>
    public bool PreserveCodeBlocks { get; set; } = true;
    
    /// <summary>
    /// Whether to preserve tables as single units
    /// </summary>
    public bool PreserveTables { get; set; } = true;
    
    /// <summary>
    /// Whether to calculate and store quality metrics
    /// </summary>
    public bool EnableQualityMetrics { get; set; } = true;
}

public enum ChunkingStrategy
{
    Semantic,      // Respects document structure (headers, paragraphs) - Character-based
    FixedSize,     // Fixed character count chunks
    Sentence,      // Sentence-based chunking
    TokenAware     // Token-aware semantic chunking (RECOMMENDED for December 2025)
}
