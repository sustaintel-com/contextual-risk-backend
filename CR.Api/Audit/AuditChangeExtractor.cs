using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ESG.Api.Audit;

internal static class AuditChangeExtractor
{
    public static Dictionary<string, object?> Extract(EntityEntry entry)
    {
        return entry.State switch
        {
            EntityState.Added => GetAdded(entry),
            EntityState.Modified => GetModified(entry),
            EntityState.Deleted => GetDeleted(entry),
            _ => throw new NotSupportedException(),
        };
    }

    private static Dictionary<string, object?> GetAdded(EntityEntry entry)
    {
        // return entry.CurrentValues.Properties.ToDictionary(
        //     p => p.Name,
        //     p => entry.CurrentValues[p.Name]
        // );

        var data = entry.CurrentValues.Properties.ToDictionary(
            p => p.Name,
            p => NormalizeValue(entry.CurrentValues[p.Name])
        );

        // foreach (var item in data)
        // {
        //     Console.WriteLine($"Adding Entity {item.Key}: {item.Value}");
        // }

        return data;
    }

    private static Dictionary<string, object?> GetDeleted(EntityEntry entry)
    {
        return entry.OriginalValues.Properties.ToDictionary(
            p => p.Name,
            p => NormalizeValue(entry.OriginalValues[p.Name])
        );
    }

    private static Dictionary<string, object?> GetModified(EntityEntry entry)
    {
        var changes = new Dictionary<string, object?>();

        foreach (var prop in entry.Properties)
        {
            if (!prop.IsModified)
                continue;

            changes[prop.Metadata.Name] = new
            {
                Old = NormalizeValue(prop.OriginalValue),
                New = NormalizeValue(prop.CurrentValue),
            };
        }

        return changes;
    }

    private static object? NormalizeValue(object? value)
    {
        // EF Core's LTree exposes query-only members (e.g. NLevel) that throw on JSON serialization.
        if (value is LTree ltree)
            return ltree.ToString();

        return value;
    }
}
