namespace AuditAssistant.Core.Models;

/// <summary>
/// Quality metrics for a document chunk
/// </summary>
public class ChunkQualityMetrics
{
    public Guid ChunkId { get; set; }
    
    /// <summary>
    /// Score for sentence/paragraph completeness (0.0 - 1.0)
    /// Higher = chunk ends at natural boundaries
    /// </summary>
    public double Completeness { get; set; }
    
    /// <summary>
    /// Score for semantic coherence between sentences (0.0 - 1.0)
    /// Higher = sentences flow logically
    /// </summary>
    public double Coherence { get; set; }
    
    /// <summary>
    /// Score for context richness (metadata, headers, etc.) (0.0 - 1.0)
    /// Higher = more contextual information preserved
    /// </summary>
    public double ContextRichness { get; set; }
    
    /// <summary>
    /// Score for optimal token length (0.0 - 1.0)
    /// Higher = closer to ideal token count
    /// </summary>
    public double LengthScore { get; set; }
    
    /// <summary>
    /// Actual token count in this chunk
    /// </summary>
    public int TokenCount { get; set; }
    
    /// <summary>
    /// Overall weighted quality score (0.0 - 1.0)
    /// </summary>
    public double OverallScore { get; set; }
    
    /// <summary>
    /// Human-readable quality classification
    /// </summary>
    public string QualityLevel => OverallScore switch
    {
        >= 0.8 => "Excellent",
        >= 0.6 => "Good",
        >= 0.4 => "Acceptable",
        >= 0.2 => "Poor",
        _ => "VeryPoor"
    };
}
