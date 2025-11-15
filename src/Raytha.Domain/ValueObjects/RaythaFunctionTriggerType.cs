namespace Raytha.Domain.ValueObjects;

public class RaythaFunctionTriggerType : ValueObject
{
    private RaythaFunctionTriggerType(string label, string developerName)
    {
        Label = label;
        DeveloperName = developerName;
    }

    public static RaythaFunctionTriggerType From(string developerName)
    {
        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName);

        if (type == null)
        {
            throw new UnsupportedRaythaFunctionTriggerTypeException(developerName);
        }

        return type;
    }

    public static RaythaFunctionTriggerType HttpRequest => new("Http request", "http_request");
    public static RaythaFunctionTriggerType ContentItemCreated =>
        new("Content item created", "content_item_created");
    public static RaythaFunctionTriggerType ContentItemUpdated =>
        new("Content item updated", "content_item_updated");
    public static RaythaFunctionTriggerType ContentItemDeleted =>
        new("Content item deleted", "content_item_deleted");

    public string Label { get; }
    public string DeveloperName { get; }

    public static implicit operator string(RaythaFunctionTriggerType type)
    {
        return type.DeveloperName;
    }

    public static explicit operator RaythaFunctionTriggerType(string type)
    {
        return From(type);
    }

    public static IEnumerable<RaythaFunctionTriggerType> SupportedTypes
    {
        get
        {
            yield return HttpRequest;
            yield return ContentItemCreated;
            yield return ContentItemUpdated;
            yield return ContentItemDeleted;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}
