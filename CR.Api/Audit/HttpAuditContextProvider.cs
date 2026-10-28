using System.Diagnostics;
using System.Security.Claims;

namespace CR.Api.Audit;

public sealed class HttpAuditContextProvider(IHttpContextAccessor accessor) : IAuditContextProvider
{
    private readonly IHttpContextAccessor _accessor = accessor;

    public AuditContext Get()
    {
        var http = _accessor.HttpContext;

        var userId = http?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var correlationId = http?.TraceIdentifier ?? Activity.Current?.Id;

        return new AuditContext(userId, correlationId, DateTime.UtcNow);
    }
}
