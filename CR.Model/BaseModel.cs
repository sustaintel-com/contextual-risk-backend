namespace CR.Model;

public class BaseModel 
{
    public IdType Id { get; set; }
    public IdType? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public IdType? UpdatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public IdType? DeletedBy { get; set; }
}