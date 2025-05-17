using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Raytha.Web.Areas.Admin.Views.Shared.Utils;

public static class ViewModelExtensions
{
    public static string GetDisplayName<TModel>(Expression<Func<TModel, object>> expression)
    {
        Type type = typeof(TModel);

        string propertyName = null;
        string[] properties = null;
        IEnumerable<string> propertyList;
        //unless it's a root property the expression NodeType will always be Convert
        switch (expression.Body.NodeType)
        {
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
                var ue = expression.Body as UnaryExpression;
                propertyList = (ue != null ? ue.Operand : null)
                    .ToString()
                    .Split(".".ToCharArray())
                    .Skip(1); //don't use the root property
                break;
            default:
                propertyList = expression.Body.ToString().Split(".".ToCharArray()).Skip(1);
                break;
        }

        //the propert name is what we're after
        propertyName = propertyList.Last();
        //list of properties - the last property name
        properties = propertyList.Take(propertyList.Count() - 1).ToArray(); //grab all the parent properties

        Expression expr = null;
        foreach (string property in properties)
        {
            PropertyInfo propertyInfo = type.GetProperty(property);
            expr = Expression.Property(expr, type.GetProperty(property));
            type = propertyInfo.PropertyType;
        }

        DisplayAttribute attr;
        attr = (DisplayAttribute)
            type.GetProperty(propertyName)
                .GetCustomAttributes(typeof(DisplayAttribute), true)
                .SingleOrDefault();

        // Look for [MetadataType] attribute in type hierarchy
        // http://stackoverflow.com/questions/1910532/attribute-isdefined-doesnt-see-attributes-applied-with-metadatatype-class
        if (attr == null)
        {
            MetadataTypeAttribute metadataType = (MetadataTypeAttribute)
                type.GetCustomAttributes(typeof(MetadataTypeAttribute), true).FirstOrDefault();
            if (metadataType != null)
            {
                var property = metadataType.MetadataClassType.GetProperty(propertyName);
                if (property != null)
                {
                    attr = (DisplayAttribute)
                        property
                            .GetCustomAttributes(typeof(DisplayNameAttribute), true)
                            .SingleOrDefault();
                }
            }
        }
        return (attr != null) ? attr.Name : String.Empty;
    }

    public static string ActionName<T>(Expression<Action<T>> action)
        where T : Controller
    {
        return nameof(action);
    }
}
