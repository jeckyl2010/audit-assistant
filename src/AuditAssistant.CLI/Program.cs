#pragma warning disable SKEXP0001
using AuditAssistant.AI.Services;
using AuditAssistant.CLI.Services;
using AuditAssistant.Core.Interfaces;
using AuditAssistant.Core.Models;
using AuditAssistant.Core.Services;
using AuditAssistant.Data;
using AuditAssistant.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

var builder = Host.CreateApplicationBuilder(args);

var configuration = builder.Configuration;

builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseNpgsql(
        configuration.GetConnectionString("PostgreSQL"),
        o => o.UseVector()
    )
);

var aiProvider = configuration["AI:Provider"] ?? "OpenAI";

var kernelBuilder = Kernel.CreateBuilder();

switch (aiProvider.ToLowerInvariant())
{
    case "openai":
        var openAiKey = configuration["AI:OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not configured");
        var openAiModel = configuration["AI:OpenAI:Model"] ?? "gpt-4";
        var openAiEmbeddingModel = configuration["AI:OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
        
        kernelBuilder.AddOpenAIChatCompletion(openAiModel, openAiKey);
        kernelBuilder.AddOpenAITextEmbeddingGeneration(openAiEmbeddingModel, openAiKey);
        break;

    case "azureopenai":
        var azureEndpoint = configuration["AI:AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("Azure OpenAI endpoint not configured");
        var azureKey = configuration["AI:AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("Azure OpenAI API key not configured");
        var azureDeployment = configuration["AI:AzureOpenAI:DeploymentName"] ?? "gpt-4";
        var azureEmbeddingDeployment = configuration["AI:AzureOpenAI:EmbeddingDeploymentName"] ?? "text-embedding-3-small";
        
        kernelBuilder.AddAzureOpenAIChatCompletion(azureDeployment, azureEndpoint, azureKey);
        kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(azureEmbeddingDeployment, azureEndpoint, azureKey);
        break;

    default:
        throw new InvalidOperationException($"Unsupported AI provider: {aiProvider}");
}

var kernel = kernelBuilder.Build();

// Configuration options
var chunkingOptions = configuration.GetSection("Chunking").Get<ChunkingOptions>() ?? new ChunkingOptions();
var ragOptions = configuration.GetSection("RAG").Get<RAGOptions>() ?? new RAGOptions();

builder.Services.AddSingleton(kernel);
builder.Services.AddSingleton(kernel.GetRequiredService<Microsoft.SemanticKernel.Embeddings.ITextEmbeddingGenerationService>());
builder.Services.AddSingleton(chunkingOptions);
builder.Services.AddSingleton(ragOptions);
builder.Services.AddScoped<IDocumentParser, MarkdownDocumentParser>();
builder.Services.AddScoped<ITextChunker, SemanticTextChunker>();
builder.Services.AddScoped<IAuditAnalyzer, AuditAnalyzerService>();
builder.Services.AddScoped<IVectorStore, PostgresVectorStore>();
builder.Services.AddScoped<EmbeddingService>();
builder.Services.AddScoped<AuditWorkflowOrchestrator>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
Console.WriteLine("║          AI Audit Assistant - .NET Edition                ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
Console.WriteLine();

var documentParser = host.Services.GetRequiredService<IDocumentParser>();
var textChunker = host.Services.GetRequiredService<ITextChunker>();
var auditAnalyzer = host.Services.GetRequiredService<IAuditAnalyzer>();
var embeddingService = host.Services.GetRequiredService<EmbeddingService>();
var vectorStore = host.Services.GetRequiredService<IVectorStore>();

Console.Write("Enter path to audit documents directory: ");
var documentsPath = Console.ReadLine()?.Trim();

if (string.IsNullOrEmpty(documentsPath) || !Directory.Exists(documentsPath))
{
    Console.WriteLine("❌ Invalid directory path. Exiting.");
    return;
}

Console.Write("Enter path to compliance standards directory: ");
var standardsPath = Console.ReadLine()?.Trim();

if (string.IsNullOrEmpty(standardsPath) || !Directory.Exists(standardsPath))
{
    Console.WriteLine("❌ Invalid directory path. Exiting.");
    return;
}

Console.WriteLine();
Console.WriteLine("📄 Parsing audit documents...");
var documents = await documentParser.ParseDirectoryAsync(documentsPath);
Console.WriteLine($"✅ Parsed {documents.Count} documents");

Console.WriteLine();
Console.WriteLine("📋 Parsing compliance standards...");
var standardDocuments = await documentParser.ParseDirectoryAsync(standardsPath);
var standards = standardDocuments.Select(doc => new ComplianceStandard
{
    Id = doc.Id,
    Name = doc.Metadata.GetValueOrDefault("Title", doc.FileName),
    Version = doc.Metadata.GetValueOrDefault("Version", "1.0"),
    Description = doc.Content.Split('\n').FirstOrDefault(l => l.StartsWith("##"))?.Trim('#', ' ') ?? string.Empty,
    Requirements = ParseRequirements(doc.Content)
}).ToList();
Console.WriteLine($"✅ Parsed {standards.Count} compliance standards");

Console.WriteLine();
Console.WriteLine($"🔄 Chunking documents (MaxSize: {chunkingOptions.MaxChunkSize} chars, Overlap: {chunkingOptions.OverlapSize} chars)...");
var allChunks = new List<DocumentChunk>();
foreach (var doc in documents)
{
    var chunks = textChunker.ChunkDocument(doc);
    allChunks.AddRange(chunks);
    Console.WriteLine($"   {doc.FileName}: {chunks.Count} chunks");
}
Console.WriteLine($"✅ Created {allChunks.Count} total chunks");

Console.WriteLine();
Console.WriteLine("🔄 Generating embeddings and storing in vector database...");
var processedChunks = 0;
foreach (var chunk in allChunks)
{
    var embedding = await embeddingService.GenerateEmbeddingAsync(chunk.Content);
    await vectorStore.StoreChunkAsync(
        chunk.DocumentId, 
        chunk.Id, 
        chunk.ChunkIndex, 
        chunk.Content, 
        embedding, 
        chunk.Metadata);
    
    processedChunks++;
    if (processedChunks % 10 == 0)
    {
        Console.WriteLine($"   Processed {processedChunks}/{allChunks.Count} chunks...");
    }
}
Console.WriteLine($"✅ Stored {processedChunks} chunk embeddings successfully");

Console.WriteLine();
Console.WriteLine("🔍 Analyzing documents against compliance standards...");
var findings = await auditAnalyzer.AnalyzeDocumentsAsync(documents, standards);
Console.WriteLine($"✅ Identified {findings.Count} findings");
Console.WriteLine($"   Critical: {findings.Count(f => f.Severity == FindingSeverity.Critical)}");
Console.WriteLine($"   High: {findings.Count(f => f.Severity == FindingSeverity.High)}");
Console.WriteLine($"   Medium: {findings.Count(f => f.Severity == FindingSeverity.Medium)}");
Console.WriteLine($"   Low: {findings.Count(f => f.Severity == FindingSeverity.Low)}");

Console.WriteLine();
Console.WriteLine("📊 Generating audit report...");
var report = await auditAnalyzer.GenerateReportAsync(documents, standards, findings);
Console.WriteLine("✅ Report generated successfully");

var outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"AuditReport_{DateTime.Now:yyyyMMdd_HHmmss}.md");
await SaveReportAsync(report, outputPath);
Console.WriteLine($"✅ Report saved to: {outputPath}");

Console.WriteLine();
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine("Audit complete! Press any key to exit...");
Console.ReadKey();

static List<ComplianceRequirement> ParseRequirements(string content)
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

static async Task SaveReportAsync(AuditReport report, string outputPath)
{
    using var writer = new StreamWriter(outputPath);
    
    await writer.WriteLineAsync($"# {report.Title}");
    await writer.WriteLineAsync();
    await writer.WriteLineAsync($"**Generated:** {report.GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}");
    await writer.WriteLineAsync($"**Auditor:** {report.Auditor}");
    await writer.WriteLineAsync();
    
    await writer.WriteLineAsync("## Executive Summary");
    await writer.WriteLineAsync(report.ExecutiveSummary);
    await writer.WriteLineAsync();
    
    await writer.WriteLineAsync("## Scope");
    await writer.WriteLineAsync(report.Scope);
    await writer.WriteLineAsync();
    
    await writer.WriteLineAsync("## Methodology");
    await writer.WriteLineAsync(report.Methodology);
    await writer.WriteLineAsync();
    
    await writer.WriteLineAsync("## Compliance Scores");
    foreach (var score in report.ComplianceScores)
    {
        await writer.WriteLineAsync($"- **{score.Key}:** {score.Value}");
    }
    await writer.WriteLineAsync();
    
    await writer.WriteLineAsync("## Findings");
    await writer.WriteLineAsync();
    
    var groupedFindings = report.Findings.GroupBy(f => f.Severity).OrderByDescending(g => g.Key);
    
    foreach (var group in groupedFindings)
    {
        await writer.WriteLineAsync($"### {group.Key} Severity");
        await writer.WriteLineAsync();
        
        foreach (var finding in group)
        {
            await writer.WriteLineAsync($"#### {finding.Title}");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync($"**Description:** {finding.Description}");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync($"**Affected Requirements:** {string.Join(", ", finding.AffectedRequirements)}");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync($"**Recommendation:** {finding.Recommendation}");
            await writer.WriteLineAsync();
            
            if (!string.IsNullOrEmpty(finding.ImpactAnalysis))
            {
                await writer.WriteLineAsync($"**Impact:** {finding.ImpactAnalysis}");
                await writer.WriteLineAsync();
            }
            
            if (finding.Evidence.Any())
            {
                await writer.WriteLineAsync("**Evidence:**");
                foreach (var evidence in finding.Evidence)
                {
                    await writer.WriteLineAsync($"- {evidence}");
                }
                await writer.WriteLineAsync();
            }
        }
    }
    
    await writer.WriteLineAsync("## Conclusion");
    await writer.WriteLineAsync(report.Conclusion);
}
