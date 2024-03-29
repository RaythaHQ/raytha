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

    public static RaythaFunctionTriggerType HttpRequest => new("Http Request", "http_request");

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
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}