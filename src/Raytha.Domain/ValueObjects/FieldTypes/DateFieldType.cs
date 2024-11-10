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

    public override string SqlServerOrderByExpression(params string[] args)
    {
        return $" TRY_CONVERT(datetime, JSON_VALUE({args[0]}.{args[1]}, '$.{args[2]}') {args[3]}) {args[4]} ";
    }

    public override string PostgresOrderByExpression(params string[] args)
    {
        return $" CASE WHEN({args[0]}.\"{args[1]}\"->> '{args[2]}') ~'^\\d{4}-\\d{2}-\\d{2}( \\d{2}:\\d{2}:\\d{2})?$' THEN TO_TIMESTAMP({args[0]}.\"{args[1]}\"->> '{args[2]}', 'YYYY-MM-DD HH24:MI:SS') {args[4]} ELSE NULL END ";
    }
}
