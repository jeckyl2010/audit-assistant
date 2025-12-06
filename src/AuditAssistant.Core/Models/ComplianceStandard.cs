namespace AuditAssistant.Core.Models;

public class ComplianceStandard
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ComplianceRequirement> Requirements { get; set; } = new();
}

public class ComplianceRequirement
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public List<string> ControlObjectives { get; set; } = new();
}
