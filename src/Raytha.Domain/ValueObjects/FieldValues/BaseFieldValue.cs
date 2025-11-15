namespace Raytha.Domain.ValueObjects.FieldValues;

public abstract record BaseFieldValue
{
    public abstract dynamic Value { get; }
    public abstract string Text { get; }
    public abstract bool HasValue { get; }
}
