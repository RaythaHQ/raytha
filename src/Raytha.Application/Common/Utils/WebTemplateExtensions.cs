using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Raytha.Application.Themes.WebTemplates;
using Raytha.Domain.Entities;

namespace Raytha.Application.Common.Utils;

public static class WebTemplateExtensions
{
    public const string RENDERBODY_REGEX = @"{%\s*\b(renderbody)\s*%}";

    public static bool HasRenderBodyTag(string s)
    {
        if (string.IsNullOrEmpty(s))
            return false;

        return Regex.IsMatch(s, RENDERBODY_REGEX, RegexOptions.IgnoreCase);
    }

    public static string ContentAssembledFromParents(string currentContent, WebTemplateDto? parent)
    {
        if (parent == null)
            return currentContent;

        var updatedContent = Regex.Replace(parent.Content, RENDERBODY_REGEX, currentContent, RegexOptions.IgnoreCase);

        return ContentAssembledFromParents(updatedContent, parent.ParentTemplate);
    }

    public static IQueryable<TEntity> IncludeParentTemplates<TEntity>(this IQueryable<TEntity> query, Expression<Func<TEntity, WebTemplate?>> navigationProperty) where TEntity : class
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        var resultQuery = query
            .Include(navigationProperty)
            .ThenIncludeParentTemplates();

        var depth = 10;
        while (resultQuery.Any(BuildParentTemplateIdHasValueExpression(navigationProperty, depth)) && depth < 100)
        {
            resultQuery = resultQuery.ThenIncludeParentTemplates();
            depth += 10;
        }

        return resultQuery;
    }

    private static IIncludableQueryable<TEntity, WebTemplate?> ThenIncludeParentTemplates<TEntity>(this IIncludableQueryable<TEntity, WebTemplate?> query) where TEntity : class
    {
        return query
            .ThenInclude(wt => wt.ParentTemplate)
            .ThenInclude(wt => wt.ParentTemplate)
            .ThenInclude(wt => wt.ParentTemplate)
            .ThenInclude(wt => wt.ParentTemplate)
            .ThenInclude(wt => wt.ParentTemplate)
            .ThenInclude(wt => wt.ParentTemplate)
            .ThenInclude(wt => wt.ParentTemplate)
            .ThenInclude(wt => wt.ParentTemplate)
            .ThenInclude(wt => wt.ParentTemplate)
            .ThenInclude(wt => wt.ParentTemplate);
    }

    private static Expression<Func<TEntity, bool>> BuildParentTemplateIdHasValueExpression<TEntity>(Expression<Func<TEntity, WebTemplate?>> navigationProperty, int depth)
    {
        var parameter = navigationProperty.Parameters[0];
        var expression = navigationProperty.Body;

        for (var i = 0; i < depth; i++)
        {
            expression = Expression.Property(expression, "ParentTemplate");
        }

        var parentTemplateIdProperty = Expression.Property(expression, "ParentTemplateId");
        var hasValueProperty = Expression.Property(parentTemplateIdProperty, "HasValue");

        return Expression.Lambda<Func<TEntity, bool>>(hasValueProperty, parameter);
    }
}
