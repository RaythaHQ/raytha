using CSharpVitamins;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;
using static Raytha.Application.ContentItems.ContentItemBatchPlanner;

namespace Raytha.Application.ContentItems;

/// <summary>
/// Looks up existing content items from the text a client wrote in a relationship field. A
/// reference is tried as an id, then as a route path, then as the value of the related content
/// type's primary field, and the first form that matches wins.
/// </summary>
public class ContentItemReferenceResolver
{
    private readonly IRaythaDbContext _db;
    private readonly IRaythaDbJsonQueryEngine _jsonQueryEngine;

    public ContentItemReferenceResolver(
        IRaythaDbContext db,
        IRaythaDbJsonQueryEngine jsonQueryEngine
    )
    {
        _db = db;
        _jsonQueryEngine = jsonQueryEngine;
    }

    public async Task<Dictionary<(Guid ContentTypeId, string Value), ReferenceMatch>> ResolveAsync(
        IEnumerable<Reference> references,
        CancellationToken cancellationToken
    )
    {
        var distinct = references
            .Select(r => (ContentTypeId: r.Field.RelatedContentTypeId, r.Value))
            .Distinct()
            .ToArray();

        var contentTypeIds = distinct.Select(d => d.ContentTypeId).Distinct().ToArray();
        var contentTypes = await _db
            .ContentTypes.Include(ct => ct.ContentTypeFields)
            .Where(ct => contentTypeIds.Contains(ct.Id))
            .ToDictionaryAsync(ct => ct.Id, cancellationToken);

        var resolved = new Dictionary<(Guid, string), ReferenceMatch>();
        foreach (var (contentTypeId, value) in distinct)
        {
            resolved[(contentTypeId, value)] = contentTypes.TryGetValue(
                contentTypeId,
                out var contentType
            )
                ? await ResolveOneAsync(contentType, value, cancellationToken)
                : new ReferenceMatch(null, 0);
        }
        return resolved;
    }

    private async Task<ReferenceMatch> ResolveOneAsync(
        ContentType contentType,
        string value,
        CancellationToken cancellationToken
    )
    {
        if (TryParseId(value, out var id))
        {
            var exists = await _db.ContentItems.AnyAsync(
                ci => ci.Id == id && ci.ContentTypeId == contentType.Id,
                cancellationToken
            );
            if (exists)
            {
                return new ReferenceMatch(id, 1);
            }
        }

        var path = value.Trim('/');
        if (path.Length > 0)
        {
            var byPath = await _db
                .Routes.Where(r => r.Path == path && r.ContentItem.ContentTypeId == contentType.Id)
                .Select(r => r.ContentItemId)
                .Take(2)
                .ToListAsync(cancellationToken);
            if (byPath.Count > 0)
            {
                return new ReferenceMatch(byPath.Count == 1 ? byPath[0] : null, byPath.Count);
            }
        }

        var primaryField = contentType.ContentTypeFields.FirstOrDefault(f =>
            f.Id == contentType.PrimaryFieldId
        );
        if (primaryField is null)
        {
            return new ReferenceMatch(null, 0);
        }

        var filter = $"{primaryField.DeveloperName} eq '{value.Replace("'", "''")}'";
        var byPrimary = _jsonQueryEngine
            .QueryContentItems(
                contentType.Id,
                null,
                null,
                [filter],
                2,
                1,
                $"{BuiltInContentTypeField.CreationTime.DeveloperName} asc"
            )
            .Select(ci => ci.Id)
            .ToList();
        return new ReferenceMatch(byPrimary.Count == 1 ? byPrimary[0] : null, byPrimary.Count);
    }

    private static bool TryParseId(string value, out Guid id)
    {
        if (ShortGuid.TryParse(value, out ShortGuid shortGuid))
        {
            id = shortGuid.Guid;
            return true;
        }
        return Guid.TryParse(value, out id);
    }
}
