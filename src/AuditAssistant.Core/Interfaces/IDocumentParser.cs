using AuditAssistant.Core.Models;

namespace AuditAssistant.Core.Interfaces;

public interface IDocumentParser
{
    Task<AuditDocument> ParseAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<AuditDocument>> ParseDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);
}
