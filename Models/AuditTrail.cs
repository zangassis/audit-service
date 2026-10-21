namespace AuditService.Models;

public class AuditTrail
{
    public long AuditId { get; set; }
    public string TableName { get; set; } = null!;
    public string RecordId { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string ChangedBy { get; set; } = null!;
    public DateTime ChangedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? AppContext { get; set; }
}
