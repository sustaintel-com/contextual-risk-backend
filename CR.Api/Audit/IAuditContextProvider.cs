namespace CR.Api.Audit;

public interface IAuditContextProvider
{
    AuditContext Get();
}
