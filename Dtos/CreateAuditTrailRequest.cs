namespace AuditService.Dtos;

public record CreateAuditTrailRequest(
   string TableName,
   string RecordId,
   string Action,
   string? OldValues,
   string? NewValues,
   string ChangedBy,
   string? IpAddress,
   string? AppContext);