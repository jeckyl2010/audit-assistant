namespace AuditAssistant.Core.Models;

public class AuditFinding
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FindingSeverity Severity { get; set; }
    public FindingStatus Status { get; set; }
    public List<string> AffectedRequirements { get; set; } = new();
    public List<string> Evidence { get; set; } = new();
    public string Recommendation { get; set; } = string.Empty;
    public string ImpactAnalysis { get; set; } = string.Empty;
    public DateTime IdentifiedAt { get; set; }
}

public enum FindingSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum FindingStatus
{
    Open,
    InProgress,
    Resolved,
    Accepted
}
