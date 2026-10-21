namespace AuditService.Dtos;
public record AuditTrailResponseByStatus(
    long AuditId,
    string TableName,
    string RecordId,
    string Action,
    string ChangedBy,
    DateTime ChangedAt);