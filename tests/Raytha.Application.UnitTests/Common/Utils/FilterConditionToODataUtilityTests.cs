using FluentAssertions;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Application.UnitTests.Common.Utils;

public class FilterConditionToODataUtilityTests
{
    [Test]
    public void A_value_with_a_single_quote_is_escaped_into_a_well_formed_odata_literal()
    {
        var groupId = Guid.NewGuid();
        var contentType = new ContentType
        {
            Id = Guid.NewGuid(),
            DeveloperName = "posts",
            ContentTypeFields = new List<ContentTypeField>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    DeveloperName = "title",
                    FieldType = BaseFieldType.SingleLineText,
                },
            },
        };

        var filter = new List<FilterCondition>
        {
            new()
            {
                Id = groupId,
                ParentId = null,
                Type = FilterConditionType.FilterConditionGroup,
                GroupOperator = BooleanOperator.AND,
            },
            new()
            {
                Id = Guid.NewGuid(),
                ParentId = groupId,
                Type = FilterConditionType.FilterCondition,
                Field = "title",
                ConditionOperator = ConditionOperator.CONTAINS,
                Value = "O'Brien' OR 1=1--",
            },
        };

        var odata = new FilterConditionToODataUtility(contentType).ToODataFilter(filter);

        odata.Should().Be("(contains(title, 'O''Brien'' OR 1=1--'))");
    }
}
