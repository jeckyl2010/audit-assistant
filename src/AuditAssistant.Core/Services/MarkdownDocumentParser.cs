using AuditAssistant.Core.Interfaces;
using AuditAssistant.Core.Models;
using Markdig;

namespace AuditAssistant.Core.Services;

public class MarkdownDocumentParser : IDocumentParser
{
    public async Task<AuditDocument> ParseAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        var fileName = Path.GetFileName(filePath);

        return new AuditDocument
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            Content = content,
            Category = InferCategory(fileName),
            UploadedAt = DateTime.UtcNow,
            Metadata = ExtractMetadata(content)
        };
    }

    public async Task<List<AuditDocument>> ParseDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        var documents = new List<AuditDocument>();
        var markdownFiles = Directory.GetFiles(directoryPath, "*.md", SearchOption.AllDirectories);

        foreach (var filePath in markdownFiles)
        {
            var document = await ParseAsync(filePath, cancellationToken);
            documents.Add(document);
        }

        return documents;
    }

    private string InferCategory(string fileName)
    {
        var lowerFileName = fileName.ToLowerInvariant();
        
        if (lowerFileName.Contains("compliance") || lowerFileName.Contains("standard"))
            return "Compliance";
        if (lowerFileName.Contains("sop") || lowerFileName.Contains("procedure"))
            return "SOP";
        if (lowerFileName.Contains("policy"))
            return "Policy";
        if (lowerFileName.Contains("audit"))
            return "Audit";
        
        return "General";
    }

    private Dictionary<string, string> ExtractMetadata(string content)
    {
        var metadata = new Dictionary<string, string>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines.Take(20))
        {
            if (line.StartsWith("# "))
                metadata["Title"] = line.Substring(2).Trim();
            else if (line.ToLowerInvariant().Contains("version:"))
                metadata["Version"] = line.Split(':', 2)[1].Trim();
            else if (line.ToLowerInvariant().Contains("date:"))
                metadata["Date"] = line.Split(':', 2)[1].Trim();
        }

        return metadata;
    }
}
