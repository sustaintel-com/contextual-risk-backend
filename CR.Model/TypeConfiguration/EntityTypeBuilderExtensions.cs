using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
 
namespace CR.Model.TypeConfiguration
{
    public static class EntityTypeBuilderExtensions
    {
        public static void ConfigureBaseEntity<TEntity>(this EntityTypeBuilder<TEntity> builder) where TEntity : BaseModel
        {
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd()
                .HasSentinel(new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.Zero));
        }
    }
}
 
 