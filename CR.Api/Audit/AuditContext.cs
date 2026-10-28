namespace CR.Api.Audit;

public record AuditContext(string? UserId, string? CorrelationId, DateTime UtcNow);
