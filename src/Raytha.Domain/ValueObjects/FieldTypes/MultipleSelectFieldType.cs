using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Domain.ValueObjects.FieldValues;

public class MultipleSelectFieldType : BaseFieldType
{
    public MultipleSelectFieldType() : base("Multiple select", "multiple_select", true) { }

    public override IEnumerable<ConditionOperator> SupportedConditionOperators
    {
        get
        {
            yield return ConditionOperator.HAS;
            yield return ConditionOperator.NOT_HAS;
            yield return ConditionOperator.IS_EMPTY;
            yield return ConditionOperator.IS_NOT_EMPTY;
        }
    }

    public override BaseFieldValue FieldValueFrom(dynamic value)
    {
        return new ArrayFieldValue(value);
    }

    public override string SqlServerOrderByExpression(params string[] args)
    {
        return $"ISNULL(JSON_VALUE({args[0]}.{args[1]}, '$.{args[2]}[0]'), '') {args[3]}";
    }

    public override string SqlServerLikeJsonValue(params string[] args)
    {
        if (args[3] == "[]")
        {
            return $" ((JSON_QUERY({args[0]}.{args[1]}, '$.{args[2]}') IS NULL) OR NOT EXISTS (SELECT * FROM OPENJSON({args[0]}.{args[1]}, '$.{args[2]}')))";
        }
        else
        {
            return $" ((ISJSON(JSON_QUERY({args[0]}.{args[1]}, '$.{args[2]}'))) = 1 AND EXISTS (SELECT * FROM OPENJSON({args[0]}.{args[1]}, '$.{args[2]}') as temp WHERE temp.value = '{args[3]}'))";
        }
    }

    public override string PostgresOrderByExpression(params string[] args)
    {
        return $" COALESCE((SELECT value from jsonb_array_elements_text({args[0]}.\"{args[1]}\"->'{args[2]}') AS value LIMIT 1), '') {args[3]} ";
    }

    public override string PostgresLikeJsonValue(params string[] args)
    {
        if (args[3] == "[]")
        {
            return $"(({args[0]}.\"{args[1]}\"->'{args[2]}') IS NULL OR NOT EXISTS (SELECT 1 FROM jsonb_array_elements_text({args[0]}.\"{args[1]}\"->'{args[2]}')))";
        }
        else
        {
            return $"(({args[0]}.\"{args[1]}\"->'{args[2]}') IS NOT NULL AND EXISTS (SELECT 1 FROM jsonb_array_elements_text({args[0]}.\"{args[1]}\"->'{args[2]}') AS item  WHERE item = '{args[3]}'))";
        }
    }
}