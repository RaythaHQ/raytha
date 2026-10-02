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

    [Test]
    public void A_nested_group_is_joined_only_inside_its_parentheses()
    {
        var root = Group(null, BooleanOperator.AND);
        var either = Group(root.Id, BooleanOperator.OR);
        var filter = new List<FilterCondition>
        {
            root,
            Condition(root.Id, "title", "Alpha"),
            either,
            Condition(either.Id, "tone", "calm"),
            Condition(either.Id, "tone", "bold"),
        };

        var odata = new FilterConditionToODataUtility(Posts()).ToODataFilter(filter);

        odata.Should().Be("(contains(title, 'Alpha') and (contains(tone, 'calm') or contains(tone, 'bold')))");
    }

    [Test]
    public void Conditions_below_the_second_level_are_kept()
    {
        var root = Group(null, BooleanOperator.OR);
        var middle = Group(root.Id, BooleanOperator.AND);
        var inner = Group(middle.Id, BooleanOperator.OR);
        var filter = new List<FilterCondition>
        {
            root,
            middle,
            Condition(middle.Id, "title", "Alpha"),
            inner,
            Condition(inner.Id, "tone", "calm"),
            Condition(inner.Id, "tone", "bold"),
            Condition(root.Id, "title", "Bravo"),
        };

        var odata = new FilterConditionToODataUtility(Posts()).ToODataFilter(filter);

        odata
            .Should()
            .Be(
                "((contains(title, 'Alpha') and (contains(tone, 'calm') or contains(tone, 'bold'))) or contains(title, 'Bravo'))"
            );
    }

    [Test]
    public void An_empty_nested_group_is_skipped()
    {
        var root = Group(null, BooleanOperator.AND);
        var filter = new List<FilterCondition>
        {
            root,
            Group(root.Id, BooleanOperator.OR),
            Condition(root.Id, "title", "Alpha"),
        };

        var odata = new FilterConditionToODataUtility(Posts()).ToODataFilter(filter);

        odata.Should().Be("(contains(title, 'Alpha'))");
    }

    private static ContentType Posts() =>
        new()
        {
            Id = Guid.NewGuid(),
            DeveloperName = "posts",
            ContentTypeFields = new List<ContentTypeField>
            {
                new() { Id = Guid.NewGuid(), DeveloperName = "title", FieldType = BaseFieldType.SingleLineText },
                new() { Id = Guid.NewGuid(), DeveloperName = "tone", FieldType = BaseFieldType.SingleLineText },
            },
        };

    private static FilterCondition Group(Guid? parentId, BooleanOperator groupOperator) =>
        new()
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Type = FilterConditionType.FilterConditionGroup,
            GroupOperator = groupOperator,
        };

    private static FilterCondition Condition(Guid parentId, string field, string value) =>
        new()
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Type = FilterConditionType.FilterCondition,
            Field = field,
            ConditionOperator = ConditionOperator.CONTAINS,
            Value = value,
        };
}
