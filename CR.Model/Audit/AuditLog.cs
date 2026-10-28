namespace ESG.Model.Audit;

public sealed class AuditLog
{
    public IdType Id { get; set; }
    public string EntityName { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public string Action { get; set; } = default!;

    public string ChangesJson { get; set; } = default!;

    public IdType? UserId { get; set; }
    public DateTime OccurredAtUtc { get; set; }

    public string? CorrelationId { get; set; }
}
