using AuditAssistant.AI.Services;
using AuditAssistant.Core.Interfaces;
using AuditAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AuditAssistant.CLI.Services;

public class AuditWorkflowOrchestrator
{
    private readonly IDocumentParser _documentParser;
    private readonly ITextChunker _textChunker;
    private readonly EmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly IAuditAnalyzer _auditAnalyzer;
    private readonly ILogger<AuditWorkflowOrchestrator> _logger;

    public AuditWorkflowOrchestrator(
        IDocumentParser documentParser,
        ITextChunker textChunker,
        EmbeddingService embeddingService,
        IVectorStore vectorStore,
        IAuditAnalyzer auditAnalyzer,
        ILogger<AuditWorkflowOrchestrator> logger)
    {
        _documentParser = documentParser;
        _textChunker = textChunker;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _auditAnalyzer = auditAnalyzer;
        _logger = logger;
    }

    public async Task<AuditReport> RunAuditAsync(
        string documentsPath,
        string standardsPath,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var overallSw = Stopwatch.StartNew();

        try
        {
            progress?.Report("📄 Parsing audit documents...");
            var documents = await ParseDocumentsAsync(documentsPath, cancellationToken);

            progress?.Report("📋 Parsing compliance standards...");
            var standards = await ParseStandardsAsync(standardsPath, cancellationToken);

            progress?.Report("🔄 Chunking documents and generating embeddings...");
            await ChunkAndEmbedDocumentsAsync(documents, cancellationToken);

            progress?.Report("🔍 Analyzing documents against compliance standards...");
            var findings = await _auditAnalyzer.AnalyzeDocumentsAsync(documents, standards, cancellationToken);

            progress?.Report("📊 Generating audit report...");
            var report = await _auditAnalyzer.GenerateReportAsync(documents, standards, findings, cancellationToken);

            overallSw.Stop();
            _logger.LogInformation(
                "Complete audit workflow finished in {ElapsedSeconds:F1}s",
                overallSw.Elapsed.TotalSeconds);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit workflow failed");
            throw;
        }
    }

    private async Task<List<AuditDocument>> ParseDocumentsAsync(
        string directoryPath,
        CancellationToken cancellationToken)
    {
        var documents = await _documentParser.ParseDirectoryAsync(directoryPath, cancellationToken);
        _logger.LogInformation("Parsed {DocumentCount} documents", documents.Count);
        return documents;
    }

    private async Task<List<ComplianceStandard>> ParseStandardsAsync(
        string directoryPath,
        CancellationToken cancellationToken)
    {
        var standardDocuments = await _documentParser.ParseDirectoryAsync(directoryPath, cancellationToken);
        
        var standards = standardDocuments.Select(doc => new ComplianceStandard
        {
            Id = doc.Id,
            Name = doc.Metadata.GetValueOrDefault("Title", doc.FileName),
            Version = doc.Metadata.GetValueOrDefault("Version", "1.0"),
            Description = doc.Content.Split('\n').FirstOrDefault(l => l.StartsWith("##"))?.Trim('#', ' ') ?? string.Empty,
            Requirements = ParseRequirements(doc.Content)
        }).ToList();
        
        _logger.LogInformation("Parsed {StandardCount} compliance standards with {RequirementCount} total requirements",
            standards.Count,
            standards.Sum(s => s.Requirements.Count));

        return standards;
    }

    private async Task ChunkAndEmbedDocumentsAsync(
        List<AuditDocument> documents,
        CancellationToken cancellationToken)
    {
        var allChunks = new List<DocumentChunk>();

        foreach (var doc in documents)
        {
            var chunks = _textChunker.ChunkDocument(doc);
            allChunks.AddRange(chunks);
        }

        _logger.LogInformation("Created {TotalChunks} total chunks", allChunks.Count);

        var newChunks = new List<DocumentChunk>();
        foreach (var chunk in allChunks)
        {
            if (!await _vectorStore.ChunkExistsAsync(chunk.Id, cancellationToken))
            {
                newChunks.Add(chunk);
            }
        }

        if (!newChunks.Any())
        {
            _logger.LogInformation("All chunks already exist, skipping embedding generation");
            return;
        }

        _logger.LogInformation("Generating embeddings for {NewChunkCount} new chunks", newChunks.Count);

        var batchData = new List<(Guid, Guid, int, string, float[], Dictionary<string, string>)>();
        foreach (var chunk in newChunks)
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Content, cancellationToken);
            batchData.Add((chunk.DocumentId, chunk.Id, chunk.ChunkIndex, chunk.Content, embedding, chunk.Metadata));
        }

        await _vectorStore.StoreChunksBatchAsync(batchData, cancellationToken);
        _logger.LogInformation("Successfully stored {ChunkCount} chunks", newChunks.Count);
    }

    private List<ComplianceRequirement> ParseRequirements(string content)
    {
        var requirements = new List<ComplianceRequirement>();
        var lines = content.Split('\n');
        ComplianceRequirement? current = null;
        
        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("###"))
            {
                if (current != null)
                    requirements.Add(current);
                
                var parts = line.Trim('#', ' ').Split(':', 2);
                current = new ComplianceRequirement
                {
                    Code = parts.Length > 0 ? parts[0].Trim() : string.Empty,
                    Title = parts.Length > 1 ? parts[1].Trim() : string.Empty,
                    Severity = "Medium"
                };
            }
            else if (current != null && !string.IsNullOrWhiteSpace(line))
            {
                current.Description += line.Trim() + " ";
            }
        }
        
        if (current != null)
            requirements.Add(current);
        
        return requirements;
    }
}
