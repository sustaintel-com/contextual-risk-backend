using Microsoft.EntityFrameworkCore;

namespace CR.Model.Data;

public class CRContext(DbContextOptions<CRContext> options)
    : DbContext(options)
{
}