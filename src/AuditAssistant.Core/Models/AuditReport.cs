namespace AuditAssistant.Core.Models;

public class AuditReport
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ExecutiveSummary { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string Methodology { get; set; } = string.Empty;
    public DateTime AuditDate { get; set; }
    public string Auditor { get; set; } = string.Empty;
    public List<AuditFinding> Findings { get; set; } = new();
    public List<ComplianceStandard> ApplicableStandards { get; set; } = new();
    public Dictionary<string, string> ComplianceScores { get; set; } = new();
    public string Conclusion { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}
