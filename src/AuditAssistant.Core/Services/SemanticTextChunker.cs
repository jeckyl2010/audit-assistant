using AuditAssistant.Core.Interfaces;
using AuditAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;
using TiktokenSharp;

namespace AuditAssistant.Core.Services;

/// <summary>
/// Implements token-aware semantic chunking strategy following December 2025 best practices:
/// - Token-based chunking (uses tiktoken for accurate token counting)
/// - Respects document structure (headers, paragraphs, lists)
/// - Maintains semantic boundaries
/// - Includes overlapping context between chunks
/// - Preserves metadata and hierarchical context
/// - Quality metrics for each chunk
/// </summary>
public class SemanticTextChunker : ITextChunker
{
    private readonly ChunkingOptions _defaultOptions;
    private readonly TikToken _tokenizer;
    private readonly ChunkQualityAnalyzer _qualityAnalyzer;
    private readonly ILogger<SemanticTextChunker>? _logger;

    public SemanticTextChunker(
        ChunkingOptions? defaultOptions = null,
        ILogger<SemanticTextChunker>? logger = null)
    {
        _defaultOptions = defaultOptions ?? new ChunkingOptions();
        _tokenizer = TikToken.EncodingForModel("text-embedding-3-small");
        _qualityAnalyzer = new ChunkQualityAnalyzer();
        _logger = logger;
    }

    public List<DocumentChunk> ChunkDocument(
        AuditDocument document, 
        ChunkingOptions? options = null)
    {
        var effectiveOptions = options ?? _defaultOptions;
        var chunks = new List<DocumentChunk>();
        var content = document.Content;

        // Split by markdown sections (preserving hierarchy)
        var sections = ParseMarkdownSections(content, effectiveOptions);

        var currentTokens = new List<int>();
        var currentChunk = new StringBuilder();
        var currentSectionTitle = string.Empty;
        var chunkIndex = 0;
        var startPosition = 0;

        _logger?.LogDebug("Starting token-aware chunking for document {FileName}", document.FileName);

        foreach (var section in sections)
        {
            // Tokenize the section to get accurate token count
            var sectionTokens = _tokenizer.Encode(section.Content);
            
            // Check if adding this section would exceed token limit
            if (currentTokens.Count > 0 && 
                currentTokens.Count + sectionTokens.Count > effectiveOptions.MaxChunkSize)
            {
                // Finalize current chunk
                var chunkText = currentChunk.ToString();
                var chunk = CreateChunk(
                    document.Id,
                    chunkIndex++,
                    chunkText,
                    startPosition,
                    startPosition + chunkText.Length,
                    currentSectionTitle,
                    document.Metadata
                );
                
                // Add quality metrics
                AddQualityMetrics(chunk, currentTokens.Count);
                chunks.Add(chunk);

                _logger?.LogDebug(
                    "Created chunk {Index} with {TokenCount} tokens (Quality: {Quality})",
                    chunkIndex - 1,
                    currentTokens.Count,
                    chunk.Metadata.GetValueOrDefault("QualityScore", "N/A"));

                // Create overlap in TOKENS (not characters)
                var overlapTokenCount = Math.Min(effectiveOptions.OverlapSize, currentTokens.Count);
                var overlapTokens = currentTokens.TakeLast(overlapTokenCount).ToList();
                var overlapText = _tokenizer.Decode(overlapTokens);
                
                currentTokens.Clear();
                currentTokens.AddRange(overlapTokens);
                currentChunk.Clear();
                currentChunk.Append(overlapText);
                startPosition += chunkText.Length - overlapText.Length;
            }

            // Add section to current chunk
            if (currentChunk.Length == 0 || string.IsNullOrEmpty(currentSectionTitle))
            {
                currentSectionTitle = section.Title;
            }

            currentTokens.AddRange(sectionTokens);
            currentChunk.AppendLine(section.Content);
        }

        // Add final chunk
        if (currentTokens.Count > 0)
        {
            var chunkText = currentChunk.ToString();
            var chunk = CreateChunk(
                document.Id,
                chunkIndex,
                chunkText,
                startPosition,
                startPosition + chunkText.Length,
                currentSectionTitle,
                document.Metadata
            );
            
            // Add quality metrics
            AddQualityMetrics(chunk, currentTokens.Count);
            chunks.Add(chunk);

            _logger?.LogDebug(
                "Created final chunk {Index} with {TokenCount} tokens (Quality: {Quality})",
                chunkIndex,
                currentTokens.Count,
                chunk.Metadata.GetValueOrDefault("QualityScore", "N/A"));
        }

        _logger?.LogInformation(
            "Document {FileName} chunked into {ChunkCount} token-aware chunks. Avg tokens: {AvgTokens:F0}",
            document.FileName,
            chunks.Count,
            chunks.Average(c => int.Parse(c.Metadata.GetValueOrDefault("TokenCount", "0"))));

        return chunks;
    }

    /// <summary>
    /// Adds quality metrics to a chunk
    /// </summary>
    private void AddQualityMetrics(DocumentChunk chunk, int tokenCount)
    {
        try
        {
            var quality = _qualityAnalyzer.AnalyzeChunk(chunk);
            
            chunk.Metadata["TokenCount"] = tokenCount.ToString();
            chunk.Metadata["QualityScore"] = quality.OverallScore.ToString("F3");
            chunk.Metadata["QualityLevel"] = quality.QualityLevel;
            chunk.Metadata["Completeness"] = quality.Completeness.ToString("F3");
            chunk.Metadata["Coherence"] = quality.Coherence.ToString("F3");
            chunk.Metadata["ContextRichness"] = quality.ContextRichness.ToString("F3");
            chunk.Metadata["LengthScore"] = quality.LengthScore.ToString("F3");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to calculate quality metrics for chunk {ChunkId}", chunk.Id);
            chunk.Metadata["TokenCount"] = tokenCount.ToString();
            chunk.Metadata["QualityScore"] = "0.5";
            chunk.Metadata["QualityLevel"] = "Unknown";
        }
    }

    private List<MarkdownSection> ParseMarkdownSections(string content, ChunkingOptions options)
    {
        var sections = new List<MarkdownSection>();
        var lines = content.Split('\n');
        
        var currentSection = new StringBuilder();
        var currentTitle = string.Empty;
        var inCodeBlock = false;

        foreach (var line in lines)
        {
            // Track code blocks to avoid splitting them (if enabled)
            if (options.PreserveCodeBlocks && line.TrimStart().StartsWith("```"))
            {
                inCodeBlock = !inCodeBlock;
                currentSection.AppendLine(line);
                continue;
            }

            // Detect headers (semantic boundaries)
            if (!inCodeBlock && Regex.IsMatch(line, @"^#{1,6}\s+"))
            {
                // Save previous section
                if (currentSection.Length > 0)
                {
                    sections.Add(new MarkdownSection
                    {
                        Title = currentTitle,
                        Content = currentSection.ToString().Trim()
                    });
                    currentSection.Clear();
                }

                currentTitle = Regex.Replace(line, @"^#{1,6}\s+", "").Trim();
                currentSection.AppendLine(line);
            }
            // Detect paragraph boundaries (double newline)
            else if (string.IsNullOrWhiteSpace(line) && currentSection.Length > 0)
            {
                currentSection.AppendLine(line);
                
                // If current section is getting large, create a boundary
                if (!inCodeBlock && currentSection.Length > options.MaxChunkSize / 2)
                {
                    sections.Add(new MarkdownSection
                    {
                        Title = currentTitle,
                        Content = currentSection.ToString().Trim()
                    });
                    currentSection.Clear();
                }
            }
            else
            {
                currentSection.AppendLine(line);
            }
        }

        // Add final section
        if (currentSection.Length > 0)
        {
            sections.Add(new MarkdownSection
            {
                Title = currentTitle,
                Content = currentSection.ToString().Trim()
            });
        }

        return sections;
    }

    private string GetOverlapText(string text, int overlapSize)
    {
        if (text.Length <= overlapSize)
            return text;

        // Try to find sentence boundary for natural overlap
        var overlapStart = text.Length - overlapSize;
        var sentenceBoundary = text.LastIndexOfAny(new[] { '.', '!', '?' }, text.Length - 1, overlapSize);
        
        if (sentenceBoundary > overlapStart)
        {
            overlapStart = sentenceBoundary + 1;
        }

        return text[overlapStart..].TrimStart();
    }

    private DocumentChunk CreateChunk(
        Guid documentId,
        int chunkIndex,
        string content,
        int startPosition,
        int endPosition,
        string sectionTitle,
        Dictionary<string, string> documentMetadata)
    {
        var chunkMetadata = new Dictionary<string, string>(documentMetadata)
        {
            ["ChunkIndex"] = chunkIndex.ToString(),
            ["SectionTitle"] = sectionTitle,
            ["StartPosition"] = startPosition.ToString(),
            ["EndPosition"] = endPosition.ToString()
        };

        return new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            ChunkIndex = chunkIndex,
            Content = content.Trim(),
            StartPosition = startPosition,
            EndPosition = endPosition,
            SectionTitle = sectionTitle,
            Metadata = chunkMetadata
        };
    }

    private class MarkdownSection
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
