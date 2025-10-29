#nullable enable
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace Raytha.Web.Areas.Shared.Utils;

/// <summary>
/// Extension methods for view models and display name retrieval.
/// </summary>
public static class ViewModelExtensions
{
    /// <summary>
    /// Retrieves the display name for a property using reflection to check for [Display(Name="")] attributes.
    /// This method uses reflection and should be used sparingly in non-hot paths.
    /// </summary>
    /// <typeparam name="TModel">The model type containing the property.</typeparam>
    /// <param name="expression">An expression that identifies the property.</param>
    /// <returns>The display name from the [Display] attribute, or empty string if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when expression is null.</exception>
    public static string GetDisplayName<TModel>(Expression<Func<TModel, object>> expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        Type type = typeof(TModel);

        string? propertyName = null;
        string[]? properties = null;
        IEnumerable<string> propertyList;
        //unless it's a root property the expression NodeType will always be Convert
        switch (expression.Body.NodeType)
        {
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
                var ue = expression.Body as UnaryExpression;
                propertyList =
                    (ue != null ? ue.Operand : null)?.ToString().Split(".".ToCharArray()).Skip(1)
                    ?? Enumerable.Empty<string>(); //don't use the root property
                break;
            default:
                propertyList = expression.Body.ToString().Split(".".ToCharArray()).Skip(1);
                break;
        }

        // The property name is what we're after
        propertyName = propertyList.Last();
        // List of properties - the last property name
        properties = propertyList.Take(propertyList.Count() - 1).ToArray(); // Grab all the parent properties

        Expression? expr = null;
        foreach (string property in properties)
        {
            PropertyInfo? propertyInfo = type.GetProperty(property);
            if (propertyInfo == null)
            {
                return string.Empty;
            }
            expr = Expression.Property(expr, propertyInfo);
            type = propertyInfo.PropertyType;
        }

        PropertyInfo? targetProperty = type.GetProperty(propertyName);
        if (targetProperty == null)
        {
            return string.Empty;
        }

        DisplayAttribute? attr = (DisplayAttribute?)
            targetProperty.GetCustomAttributes(typeof(DisplayAttribute), true).SingleOrDefault();

        // Look for [MetadataType] attribute in type hierarchy
        // See: http://stackoverflow.com/questions/1910532/attribute-isdefined-doesnt-see-attributes-applied-with-metadatatype-class
        if (attr == null)
        {
            MetadataTypeAttribute? metadataType = (MetadataTypeAttribute?)
                type.GetCustomAttributes(typeof(MetadataTypeAttribute), true).FirstOrDefault();
            if (metadataType != null)
            {
                var property = metadataType.MetadataClassType.GetProperty(propertyName);
                if (property != null)
                {
                    attr = (DisplayAttribute?)
                        property
                            .GetCustomAttributes(typeof(DisplayNameAttribute), true)
                            .SingleOrDefault();
                }
            }
        }
        return attr?.Name ?? string.Empty;
    }
}
