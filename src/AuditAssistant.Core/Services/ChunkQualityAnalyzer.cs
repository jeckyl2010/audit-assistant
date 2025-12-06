using AuditAssistant.Core.Models;
using System.Text.RegularExpressions;
using TiktokenSharp;

namespace AuditAssistant.Core.Services;

/// <summary>
/// Analyzes and scores the quality of document chunks
/// </summary>
public class ChunkQualityAnalyzer
{
    private readonly TikToken _tokenizer;

    public ChunkQualityAnalyzer()
    {
        _tokenizer = TikToken.EncodingForModel("text-embedding-3-small");
    }

    /// <summary>
    /// Analyzes a chunk and returns quality metrics
    /// </summary>
    public ChunkQualityMetrics AnalyzeChunk(DocumentChunk chunk)
    {
        var tokenCount = _tokenizer.Encode(chunk.Content).Count;
        
        var metrics = new ChunkQualityMetrics
        {
            ChunkId = chunk.Id,
            TokenCount = tokenCount,
            Completeness = CalculateCompleteness(chunk),
            Coherence = CalculateCoherence(chunk),
            ContextRichness = CalculateContextRichness(chunk),
            LengthScore = CalculateLengthScore(tokenCount)
        };
        
        // Calculate weighted average
        metrics.OverallScore = 
            metrics.Completeness * 0.30 +
            metrics.Coherence * 0.30 +
            metrics.ContextRichness * 0.20 +
            metrics.LengthScore * 0.20;
        
        return metrics;
    }

    /// <summary>
    /// Calculates how complete the chunk is (proper boundaries)
    /// </summary>
    private double CalculateCompleteness(DocumentChunk chunk)
    {
        var content = chunk.Content.Trim();
        if (string.IsNullOrEmpty(content)) return 0.0;
        
        var score = 1.0;
        
        // Check if ends with sentence terminator
        if (!EndsWithSentenceTerminator(content))
            score -= 0.3;
        
        // Check if starts properly (not mid-word or mid-sentence)
        if (StartsWithLowerCase(content) && !StartsWithListMarker(content))
            score -= 0.2;
        
        // Bonus for complete paragraphs (ends with double newline or single newline + new header)
        if (HasCompleteParagraphs(content))
            score += 0.1;
        
        // Deduct if chunk is just a fragment (very short and incomplete)
        if (content.Length < 50 && !EndsWithSentenceTerminator(content))
            score -= 0.3;
        
        return Math.Clamp(score, 0.0, 1.0);
    }

    /// <summary>
    /// Calculates semantic coherence (how well sentences flow)
    /// </summary>
    private double CalculateCoherence(DocumentChunk chunk)
    {
        var sentences = SplitIntoSentences(chunk.Content);
        
        if (sentences.Length < 2)
            return 0.5; // Neutral score for single sentence
        
        var coherenceIndicators = 0;
        var totalTransitions = sentences.Length - 1;
        
        for (int i = 0; i < sentences.Length - 1; i++)
        {
            var current = sentences[i].Trim();
            var next = sentences[i + 1].Trim();
            
            // Check for transition words
            if (HasTransitionWord(next))
                coherenceIndicators++;
            
            // Check for pronoun references (it, this, these, those)
            if (HasPronounReference(next))
                coherenceIndicators++;
            
            // Check for repeated key terms
            if (SharesKeyTerms(current, next))
                coherenceIndicators++;
        }
        
        // Score based on percentage of coherent transitions
        var coherenceScore = (coherenceIndicators / (double)(totalTransitions * 2)) * 0.8 + 0.2;
        return Math.Clamp(coherenceScore, 0.0, 1.0);
    }

    /// <summary>
    /// Calculates how context-rich the chunk is (metadata, headers, etc.)
    /// </summary>
    private double CalculateContextRichness(DocumentChunk chunk)
    {
        var score = 0.0;
        
        // Has section title
        if (chunk.Metadata.ContainsKey("SectionTitle") && 
            !string.IsNullOrEmpty(chunk.Metadata["SectionTitle"]))
            score += 0.25;
        
        // Has parent section context
        if (chunk.Metadata.ContainsKey("ParentSection"))
            score += 0.15;
        
        // Has hierarchical breadcrumb
        if (chunk.Metadata.ContainsKey("Breadcrumb"))
            score += 0.10;
        
        // Contains headers in the content itself
        if (ContainsHeaders(chunk.Content))
            score += 0.20;
        
        // Contains key terms or technical vocabulary
        var keyTermCount = CountKeyTerms(chunk.Content);
        score += Math.Min(keyTermCount / 20.0, 0.15);
        
        // Has references, links, or citations
        if (ContainsReferences(chunk.Content))
            score += 0.15;
        
        return Math.Clamp(score, 0.0, 1.0);
    }

    /// <summary>
    /// Scores based on optimal token length
    /// </summary>
    private double CalculateLengthScore(int tokenCount)
    {
        // Ideal range: 256-512 tokens (based on research)
        // Acceptable: 128-768 tokens
        // Poor: < 100 or > 800 tokens
        
        if (tokenCount >= 256 && tokenCount <= 512)
            return 1.0; // Perfect
        
        if (tokenCount >= 128 && tokenCount < 256)
        {
            // Gradually increase from 0.7 to 1.0
            return 0.7 + ((tokenCount - 128) / 128.0) * 0.3;
        }
        
        if (tokenCount > 512 && tokenCount <= 768)
        {
            // Gradually decrease from 1.0 to 0.7
            return 1.0 - ((tokenCount - 512) / 256.0) * 0.3;
        }
        
        if (tokenCount >= 100 && tokenCount < 128)
        {
            // Very short but acceptable
            return 0.5 + ((tokenCount - 100) / 28.0) * 0.2;
        }
        
        if (tokenCount > 768 && tokenCount <= 1000)
        {
            // Too long but still usable
            return 0.7 - ((tokenCount - 768) / 232.0) * 0.3;
        }
        
        if (tokenCount < 100)
        {
            // Too short
            return Math.Max(tokenCount / 100.0 * 0.5, 0.1);
        }
        
        // Way too long (> 1000 tokens)
        return Math.Max(0.4 - ((tokenCount - 1000) / 1000.0), 0.1);
    }

    // Helper methods
    
    private bool EndsWithSentenceTerminator(string text)
    {
        var trimmed = text.TrimEnd();
        return trimmed.EndsWith('.') || trimmed.EndsWith('!') || 
               trimmed.EndsWith('?') || trimmed.EndsWith(':');
    }

    private bool StartsWithLowerCase(string text)
    {
        var trimmed = text.TrimStart();
        return trimmed.Length > 0 && char.IsLower(trimmed[0]);
    }

    private bool StartsWithListMarker(string text)
    {
        var trimmed = text.TrimStart();
        return trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || 
               trimmed.StartsWith("+ ") || Regex.IsMatch(trimmed, @"^\d+\.\s");
    }

    private bool HasCompleteParagraphs(string text)
    {
        return text.Contains("\n\n") || 
               (text.TrimEnd().EndsWith('\n') && text.Contains('#'));
    }

    private string[] SplitIntoSentences(string text)
    {
        // Simple sentence splitting (could be improved with NLP)
        return Regex.Split(text, @"(?<=[.!?])\s+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();
    }

    private bool HasTransitionWord(string sentence)
    {
        var transitionWords = new[]
        {
            "however", "therefore", "furthermore", "moreover", "additionally",
            "consequently", "thus", "hence", "nevertheless", "meanwhile",
            "subsequently", "accordingly", "similarly", "likewise", "conversely"
        };
        
        var lowerSentence = sentence.ToLower();
        return transitionWords.Any(word => lowerSentence.StartsWith(word));
    }

    private bool HasPronounReference(string sentence)
    {
        var pronouns = new[] { "it ", "this ", "these ", "those ", "they ", "them " };
        var lowerSentence = sentence.ToLower();
        return pronouns.Any(pronoun => lowerSentence.StartsWith(pronoun));
    }

    private bool SharesKeyTerms(string sentence1, string sentence2)
    {
        var words1 = Regex.Matches(sentence1, @"\b\w{4,}\b")
            .Select(m => m.Value.ToLower())
            .ToHashSet();
        
        var words2 = Regex.Matches(sentence2, @"\b\w{4,}\b")
            .Select(m => m.Value.ToLower())
            .ToHashSet();
        
        return words1.Intersect(words2).Any();
    }

    private bool ContainsHeaders(string text)
    {
        return Regex.IsMatch(text, @"^#{1,6}\s+.+", RegexOptions.Multiline);
    }

    private int CountKeyTerms(string text)
    {
        var count = 0;
        
        // Acronyms (HTTP, API, SQL)
        count += Regex.Matches(text, @"\b[A-Z]{2,}\b").Count;
        
        // CamelCase identifiers
        count += Regex.Matches(text, @"\b[A-Z][a-z]+[A-Z]\w+\b").Count;
        
        // Technical function calls
        count += Regex.Matches(text, @"\b\w+\(\)").Count;
        
        // Hex numbers
        count += Regex.Matches(text, @"\b0x[0-9A-Fa-f]+\b").Count;
        
        return count;
    }

    private bool ContainsReferences(string text)
    {
        // Check for URLs, citations, or reference markers
        return Regex.IsMatch(text, @"https?://") ||
               Regex.IsMatch(text, @"\[[\d,\s]+\]") ||
               Regex.IsMatch(text, @"\[\^[\w]+\]");
    }
}
