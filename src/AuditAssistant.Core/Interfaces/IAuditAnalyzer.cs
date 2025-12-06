using AuditAssistant.Core.Models;

namespace AuditAssistant.Core.Interfaces;

public interface IAuditAnalyzer
{
    Task<List<AuditFinding>> AnalyzeDocumentsAsync(
        List<AuditDocument> documents,
        List<ComplianceStandard> standards,
        CancellationToken cancellationToken = default);
    
    Task<AuditReport> GenerateReportAsync(
        List<AuditDocument> documents,
        List<ComplianceStandard> standards,
        List<AuditFinding> findings,
        CancellationToken cancellationToken = default);
}
