using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.FeatureFlags;
using Raytha.Domain.Entities;

namespace Raytha.Infrastructure.FeatureFlags;

public sealed class EfFeatureFlagStore : IFeatureFlagStore
{
    private readonly IRaythaDbContext _db;

    public EfFeatureFlagStore(IRaythaDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyDictionary<string, bool>> GetAllAsync(
        string scope,
        CancellationToken cancellationToken = default
    )
    {
        var rows = await _db
            .FeatureFlags.AsNoTracking()
            .Where(f => f.Scope == scope)
            .Select(f => new { f.Key, f.IsEnabled })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Key, r => r.IsEnabled, StringComparer.OrdinalIgnoreCase);
    }

    public async Task SetAsync(
        string scope,
        string key,
        bool enabled,
        CancellationToken cancellationToken = default
    )
    {
        var existing = await _db.FeatureFlags.FirstOrDefaultAsync(
            f => f.Scope == scope && f.Key == key,
            cancellationToken
        );

        if (existing is null)
        {
            _db.FeatureFlags.Add(
                new FeatureFlag
                {
                    Id = Guid.NewGuid(),
                    Scope = scope,
                    Key = key,
                    IsEnabled = enabled,
                }
            );
        }
        else
        {
            existing.IsEnabled = enabled;
            existing.LastModificationTime = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
