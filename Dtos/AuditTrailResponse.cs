namespace AuditService.Dtos;

public record AuditTrailResponse(
    long AuditId,
    string TableName,
    string RecordId,
    string Action,
    string? OldValues,
    string? NewValues,
    string ChangedBy,
    DateTime ChangedAt);