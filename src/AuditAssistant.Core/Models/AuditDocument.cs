namespace AuditAssistant.Core.Models;

public class AuditDocument
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
