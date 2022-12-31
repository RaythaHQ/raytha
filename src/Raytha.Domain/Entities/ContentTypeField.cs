using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Domain.Entities;

public class ContentTypeField : BaseFullAuditableEntity
{
    public string? Label { get; set; }
    public string? DeveloperName { get; set; }
    public string? Description { get; set; }
    public int FieldOrder { get; set; }
    public bool IsRequired { get; set; } = false;
    public Guid? RelatedContentTypeId { get; set; }
    public virtual ContentType? RelatedContentType { get; set; }
    public Guid ContentTypeId { get; set; }
    public virtual ContentType? ContentType { get; set; }

    public BaseFieldType FieldType { get; set; }
    public string _Choices { get; set; } = "[]";

    [NotMapped]
    public IEnumerable<ContentTypeFieldChoice> Choices
    {
        get { return JsonSerializer.Deserialize<IEnumerable<ContentTypeFieldChoice>>(_Choices ?? "[]") ?? new List<ContentTypeFieldChoice>(); }
        set { _Choices = JsonSerializer.Serialize(value); }
    }
}

public class BuiltInContentTypeField : ValueObject
{
    static BuiltInContentTypeField()
    {
    }

    private BuiltInContentTypeField()
    {
    }

    private BuiltInContentTypeField(string label, string developerName, BaseFieldType fieldType)
    {
        Label = label;
        DeveloperName = developerName;
        FieldType = fieldType;
    }

    public static BuiltInContentTypeField From(string developerName)
    {
        var type = ReservedContentTypeFields.FirstOrDefault(p => p.DeveloperName == developerName);

        if (type == null)
        {
            throw new ReservedContentTypeFieldNotFoundException(developerName);
        }

        return type;
    }

    public static BuiltInContentTypeField PrimaryField => new("Primary field", "PrimaryField", BaseFieldType.SingleLineText);
    public static BuiltInContentTypeField CreationTime => new("Created at", "CreationTime", BaseFieldType.Date);
    public static BuiltInContentTypeField CreatorUser => new("Created by", "CreatorUser", BaseFieldType.SingleLineText);
    public static BuiltInContentTypeField LastModificationTime => new("Last modified at", "LastModificationTime", BaseFieldType.Date);
    public static BuiltInContentTypeField LastModifierUser => new("Last modified by", "LastModifierUser", BaseFieldType.SingleLineText);
    public static BuiltInContentTypeField Id => new("Id", "Id", BaseFieldType.Id);
    public static BuiltInContentTypeField IsDraft => new("Is draft", "IsDraft", BaseFieldType.Checkbox);
    public static BuiltInContentTypeField IsPublished => new("Is published", "IsPublished", BaseFieldType.Checkbox);
    public static BuiltInContentTypeField Template => new("Template", "Template", BaseFieldType.SingleLineText);

    public string Label { get; private set; } = string.Empty;
    public string DeveloperName { get; private set; } = string.Empty;
    public BaseFieldType? FieldType { get; private set; }

    public static implicit operator string(BuiltInContentTypeField scheme)
    {
        return scheme.DeveloperName;
    }

    public static explicit operator BuiltInContentTypeField(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<BuiltInContentTypeField> ReservedContentTypeFields
    {
        get
        {
            yield return Id;
            yield return PrimaryField;
            yield return CreationTime;
            yield return CreatorUser;
            yield return LastModificationTime;
            yield return LastModifierUser;
            yield return IsDraft;
            yield return IsPublished;
            yield return Template;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}
