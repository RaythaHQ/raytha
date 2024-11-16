using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.ValueObjects.FieldTypes;

public class DateFieldType : BaseFieldType
{
    public DateFieldType() : base("Date", "date", false) { }

    public override IEnumerable<ConditionOperator> SupportedConditionOperators
    {
        get
        {
            yield return ConditionOperator.EQUALS;
            yield return ConditionOperator.NOT_EQUALS;
            yield return ConditionOperator.GREATER_THAN;
            yield return ConditionOperator.GREATER_THAN_OR_EQUAL;
            yield return ConditionOperator.LESS_THAN;
            yield return ConditionOperator.LESS_THAN_OR_EQUAL;
            yield return ConditionOperator.IS_EMPTY;
            yield return ConditionOperator.IS_NOT_EMPTY;
        }
    }

    public override BaseFieldValue FieldValueFrom(dynamic value)
    {
        return new DateTimeFieldValue(value);
    }

    public override string SqlServerSingleJsonValue(params string[] args)
    {
        int sqlDateOutput = args[3] switch {
            "MM/dd/yyyy" => 101,
            "dd/MM/yyyy" => 103,
            _ => 0
        };
        return $" TRY_CONVERT(datetime, JSON_VALUE({args[0]}.{args[1]}, '$.{args[2]}'), {sqlDateOutput}) ";
    }

    public override string SqlServerOrderByExpression(params string[] args)
    {
        int sqlDateOutput = args[3] switch {
            "MM/dd/yyyy" => 101,
            "dd/MM/yyyy" => 103,
            _ => 0
        };
        return $" TRY_CONVERT(datetime, JSON_VALUE({args[0]}.{args[1]}, '$.{args[2]}'), {sqlDateOutput}) {args[4]} ";
    }

    public override string PostgresSingleJsonValue(params string[] args)
    {
        string dateFormat = args[3].ToUpper(); //US or UK format
        return $@"
        TO_DATE(
            NULLIF({args[0]}.""{args[1]}""->>'{args[2]}', ''),
            '{dateFormat}'
        )";
    }
    public override string PostgresOrderByExpression(params string[] args)
    {
        string dateFormat = args[3].ToUpper(); //US or UK format
        string sortOrder = args[4]?.ToUpper() == "DESC" ? "DESC" : "ASC"; // Default to ASC if not provided
        return $@"
        TO_DATE(
            NULLIF({args[0]}.""{args[1]}""->>'{args[2]}', ''),
            '{dateFormat}'
        ) {sortOrder}";
    }
}
