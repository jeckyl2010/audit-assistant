using AuditAssistant.Core.Interfaces;
using AuditAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace AuditAssistant.AI.Services;

public class AuditAnalyzerService : IAuditAnalyzer
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatService;
    private readonly EmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly RAGOptions _ragOptions;
    private readonly ILogger<AuditAnalyzerService> _logger;

    public AuditAnalyzerService(
        Kernel kernel, 
        EmbeddingService embeddingService,
        IVectorStore vectorStore,
        RAGOptions ragOptions,
        ILogger<AuditAnalyzerService> logger)
    {
        _kernel = kernel;
        _chatService = kernel.GetRequiredService<IChatCompletionService>();
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _ragOptions = ragOptions;
        _logger = logger;
    }

    public async Task<List<AuditFinding>> AnalyzeDocumentsAsync(
        List<AuditDocument> documents,
        List<ComplianceStandard> standards,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var findings = new ConcurrentBag<AuditFinding>();

        // Flatten all requirements with their standards
        var allRequirements = standards
            .SelectMany(std => std.Requirements
                .Select(req => (Standard: std, Requirement: req)))
            .ToList();

        _logger.LogInformation(
            "Starting analysis of {RequirementCount} requirements across {StandardCount} standards",
            allRequirements.Count,
            standards.Count);

        // Use SemaphoreSlim for controlled concurrency
        using var semaphore = new SemaphoreSlim(_ragOptions.MaxConcurrentRequirements);
        
        var tasks = allRequirements.Select(async item =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var relevantChunks = await FindRelevantChunksForRequirementAsync(
                    item.Requirement, 
                    cancellationToken);
                
                if (relevantChunks.Any())
                {
                    var requirementFindings = await AnalyzeRequirementAsync(
                        item.Requirement, 
                        item.Standard, 
                        relevantChunks, 
                        cancellationToken);
                    
                    foreach (var finding in requirementFindings)
                    {
                        findings.Add(finding);
                    }

                    _logger.LogDebug(
                        "Requirement {Code} analysis complete. Found {FindingCount} findings",
                        item.Requirement.Code,
                        requirementFindings.Count);
                }
                else
                {
                    _logger.LogWarning(
                        "No relevant chunks found for requirement {Code}",
                        item.Requirement.Code);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error analyzing requirement {Code}",
                    item.Requirement.Code);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        
        sw.Stop();
        _logger.LogInformation(
            "Analysis complete. Processed {RequirementCount} requirements in {ElapsedSeconds:F1}s. Found {FindingCount} total findings",
            allRequirements.Count,
            sw.Elapsed.TotalSeconds,
            findings.Count);

        return findings.ToList();
    }

    private async Task<List<AuditFinding>> AnalyzeRequirementAsync(
        ComplianceRequirement requirement,
        ComplianceStandard standard,
        List<(Guid DocumentId, string Content, double Similarity)> relevantChunks,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        // Filter by similarity threshold and limit chunks
        var filteredChunks = relevantChunks
            .Where(c => c.Similarity >= _ragOptions.MinimumSimilarityThreshold)
            .Take(_ragOptions.MaxChunksForAnalysis)
            .ToList();

        if (!filteredChunks.Any())
        {
            _logger.LogWarning(
                "All chunks for requirement {Code} filtered out due to low similarity (threshold: {Threshold})",
                requirement.Code,
                _ragOptions.MinimumSimilarityThreshold);
            return new List<AuditFinding>();
        }

        var context = BuildAnalysisContext(filteredChunks, requirement);
        
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(BuildRequirementSystemPrompt(requirement));
        chatHistory.AddUserMessage(context);

        var response = await _chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
        var findings = ParseFindingsFromResponse(response.Content ?? string.Empty);
        
        // Tag findings with the affected requirement
        foreach (var finding in findings)
        {
            if (!finding.AffectedRequirements.Contains(requirement.Code))
            {
                finding.AffectedRequirements.Add(requirement.Code);
            }
        }

        sw.Stop();
        _logger.LogInformation(
            "Analyzed requirement {Code} in {ElapsedMs}ms using {ChunkCount} chunks. Generated {FindingCount} findings",
            requirement.Code,
            sw.ElapsedMilliseconds,
            filteredChunks.Count,
            findings.Count);

        return findings;
    }

    private string BuildRequirementSystemPrompt(ComplianceRequirement requirement)
    {
        return $$"""
            You are an expert audit analyst. Analyze the provided document excerpts against this compliance requirement:

            **Requirement {{requirement.Code}}: {{requirement.Title}}**
            {{requirement.Description}}

            Severity: {{requirement.Severity}}

            Identify any compliance gaps, issues, or areas of concern. For each finding:
            1. Provide a clear title
            2. Describe the specific issue
            3. Cite evidence from the documents
            4. Recommend remediation steps
            5. Assess the impact

            Return findings as a JSON array. If no issues found, return empty array [].
            """;
    }

    private string BuildAnalysisContext(
        List<(Guid DocumentId, string Content, double Similarity)> chunks,
        ComplianceRequirement requirement)
    {
        var chunkTexts = chunks.Select((c, i) => 
            $$"""
            ## Excerpt {{i + 1}} [Relevance: {{c.Similarity:F2}}]
            {{c.Content}}
            """);

        return $$"""
            Document Excerpts:

            {{string.Join("\n\n---\n\n", chunkTexts)}}
            """;
    }

    private async Task<List<(Guid DocumentId, string Content, double Similarity)>> FindRelevantChunksForRequirementAsync(
        ComplianceRequirement requirement,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        // Create a query from the requirement
        var query = $"{requirement.Code}: {requirement.Title}. {requirement.Description}";
        
        // Generate embedding for the query
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
        
        // Search for similar chunks in the vector database
        var similarChunks = await _vectorStore.SearchSimilarAsync(
            queryEmbedding, 
            topK: _ragOptions.VectorSearchTopK, 
            cancellationToken);
        
        sw.Stop();

        if (similarChunks.Any())
        {
            var avgSimilarity = similarChunks.Average(c => c.Similarity);
            var minSimilarity = similarChunks.Min(c => c.Similarity);
            
            _logger.LogInformation(
                "Vector search for {Code} completed in {ElapsedMs}ms. " +
                "Found {ChunkCount} chunks. Avg similarity: {AvgSimilarity:F3}, Min: {MinSimilarity:F3}",
                requirement.Code,
                sw.ElapsedMilliseconds,
                similarChunks.Count,
                avgSimilarity,
                minSimilarity);

            if (avgSimilarity < 0.6)
            {
                _logger.LogWarning(
                    "Low average similarity ({AvgSimilarity:F3}) for requirement {Code}. Consider improving chunking or query.",
                    avgSimilarity,
                    requirement.Code);
            }
        }
        else
        {
            _logger.LogWarning(
                "No chunks found for requirement {Code}",
                requirement.Code);
        }
        
        return similarChunks;
    }

    public async Task<AuditReport> GenerateReportAsync(
        List<AuditDocument> documents,
        List<ComplianceStandard> standards,
        List<AuditFinding> findings,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildReportSystemPrompt();
        var userPrompt = BuildReportUserPrompt(documents, standards, findings);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(userPrompt);

        var response = await _chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);

        var report = ParseReportFromResponse(response.Content ?? string.Empty, documents, standards, findings);

        return report;
    }

    private string BuildAnalysisSystemPrompt(List<ComplianceStandard> standards)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an expert audit analyst specializing in compliance and security audits.");
        sb.AppendLine("Your role is to analyze audit documentation against compliance standards and identify findings.");
        sb.AppendLine();
        sb.AppendLine("Applicable Compliance Standards:");
        
        foreach (var standard in standards)
        {
            sb.AppendLine($"\n## {standard.Name} (Version {standard.Version})");
            sb.AppendLine($"{standard.Description}");
            sb.AppendLine("\nRequirements:");
            
            foreach (var req in standard.Requirements)
            {
                sb.AppendLine($"- {req.Code}: {req.Title} (Severity: {req.Severity})");
                sb.AppendLine($"  {req.Description}");
            }
        }

        sb.AppendLine("\nFor each finding, provide:");
        sb.AppendLine("1. A clear title");
        sb.AppendLine("2. Detailed description of the issue");
        sb.AppendLine("3. Severity (Low, Medium, High, Critical)");
        sb.AppendLine("4. Affected compliance requirements (codes)");
        sb.AppendLine("5. Evidence from the documents");
        sb.AppendLine("6. Specific recommendations");
        sb.AppendLine("7. Impact analysis");
        sb.AppendLine();
        sb.AppendLine("Format your response as JSON array of findings.");

        return sb.ToString();
    }

    private string BuildAnalysisUserPrompt(List<AuditDocument> documents)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Please analyze the following audit documents and identify compliance findings:");
        sb.AppendLine();

        foreach (var doc in documents)
        {
            sb.AppendLine($"## Document: {doc.FileName} (Category: {doc.Category})");
            sb.AppendLine(doc.Content);
            sb.AppendLine("\n---\n");
        }

        return sb.ToString();
    }

    private string BuildReportSystemPrompt()
    {
        return @"You are a professional audit report writer. Generate a comprehensive audit report that includes:
1. Executive Summary - High-level overview for stakeholders
2. Scope - What was audited
3. Methodology - How the audit was conducted
4. Detailed findings analysis
5. Compliance scores per standard
6. Conclusion and recommendations

Write in professional, clear language suitable for executive audiences. Use proper audit report formatting.";
    }

    private string BuildReportUserPrompt(List<AuditDocument> documents, List<ComplianceStandard> standards, List<AuditFinding> findings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Generate a professional audit report based on the following information:");
        sb.AppendLine();
        sb.AppendLine($"Audit Date: {DateTime.UtcNow:yyyy-MM-dd}");
        sb.AppendLine($"Documents Analyzed: {documents.Count}");
        sb.AppendLine($"Applicable Standards: {string.Join(", ", standards.Select(s => s.Name))}");
        sb.AppendLine($"Total Findings: {findings.Count}");
        sb.AppendLine($"Critical: {findings.Count(f => f.Severity == FindingSeverity.Critical)}");
        sb.AppendLine($"High: {findings.Count(f => f.Severity == FindingSeverity.High)}");
        sb.AppendLine($"Medium: {findings.Count(f => f.Severity == FindingSeverity.Medium)}");
        sb.AppendLine($"Low: {findings.Count(f => f.Severity == FindingSeverity.Low)}");
        sb.AppendLine();
        sb.AppendLine("## Findings:");
        
        foreach (var finding in findings)
        {
            sb.AppendLine($"\n### {finding.Title}");
            sb.AppendLine($"**Severity:** {finding.Severity}");
            sb.AppendLine($"**Description:** {finding.Description}");
            sb.AppendLine($"**Affected Requirements:** {string.Join(", ", finding.AffectedRequirements)}");
            sb.AppendLine($"**Recommendation:** {finding.Recommendation}");
        }

        return sb.ToString();
    }

    private List<AuditFinding> ParseFindingsFromResponse(string response)
    {
        try
        {
            var jsonStart = response.IndexOf('[');
            var jsonEnd = response.LastIndexOf(']') + 1;
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                // Use range operator instead of Substring
                var jsonContent = response[jsonStart..jsonEnd];
                var findings = JsonSerializer.Deserialize<List<AuditFinding>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (findings != null)
                {
                    foreach (var finding in findings)
                    {
                        finding.Id = Guid.NewGuid();
                        finding.Status = FindingStatus.Open;
                        finding.IdentifiedAt = DateTime.UtcNow;
                    }
                    
                    _logger.LogDebug("Successfully parsed {Count} findings from AI response", findings.Count);
                    return findings;
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse findings from AI response");
        }

        _logger.LogDebug("No structured findings in response, returning raw response as single finding");
        return new List<AuditFinding>
        {
            new AuditFinding
            {
                Id = Guid.NewGuid(),
                Title = "Analysis Generated",
                Description = response,
                Severity = FindingSeverity.Medium,
                Status = FindingStatus.Open,
                IdentifiedAt = DateTime.UtcNow
            }
        };
    }

    private AuditReport ParseReportFromResponse(string response, List<AuditDocument> documents, List<ComplianceStandard> standards, List<AuditFinding> findings)
    {
        return new AuditReport
        {
            Id = Guid.NewGuid(),
            Title = $"Audit Report - {DateTime.UtcNow:yyyy-MM-dd}",
            ExecutiveSummary = ExtractSection(response, "Executive Summary", "Scope"),
            Scope = ExtractSection(response, "Scope", "Methodology"),
            Methodology = ExtractSection(response, "Methodology", "Findings"),
            AuditDate = DateTime.UtcNow,
            Auditor = "AI Audit Assistant",
            Findings = findings,
            ApplicableStandards = standards,
            ComplianceScores = CalculateComplianceScores(findings, standards),
            Conclusion = ExtractSection(response, "Conclusion", "###END###"),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private string ExtractSection(string content, string startMarker, string endMarker)
    {
        var startIndex = content.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0) return string.Empty;

        startIndex = content.IndexOf('\n', startIndex) + 1;
        var endIndex = content.IndexOf(endMarker, startIndex, StringComparison.OrdinalIgnoreCase);
        
        if (endIndex < 0) endIndex = content.Length;

        // Use range operator instead of Substring
        return content[startIndex..endIndex].Trim();
    }

    private Dictionary<string, string> CalculateComplianceScores(List<AuditFinding> findings, List<ComplianceStandard> standards)
    {
        var scores = new Dictionary<string, string>();
        
        foreach (var standard in standards)
        {
            var totalRequirements = standard.Requirements.Count;
            var affectedRequirements = findings
                .SelectMany(f => f.AffectedRequirements)
                .Distinct()
                .Count(req => standard.Requirements.Any(r => r.Code == req));
            
            var complianceRate = totalRequirements > 0 
                ? ((totalRequirements - affectedRequirements) * 100.0 / totalRequirements)
                : 100.0;
            
            scores[standard.Name] = $"{complianceRate:F1}%";
        }

        return scores;
    }
}
