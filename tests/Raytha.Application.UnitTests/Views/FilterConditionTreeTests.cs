using FluentAssertions;
using Raytha.Application.Views;

namespace Raytha.Application.UnitTests.Views;

public class FilterConditionTreeTests
{
    private static FilterConditionInputDto Group(Guid id, Guid? parent = null, string op = "and") =>
        new()
        {
            Id = id,
            ParentId = parent,
            Type = "filter_condition_group",
            GroupOperator = op,
        };

    private static FilterConditionInputDto Condition(Guid? parent, Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            ParentId = parent,
            Type = "filter_condition",
            Field = "title",
            ConditionOperator = "eq",
            Value = "x",
        };

    [Test]
    public void EmptyFilter_IsValid() => FilterConditionTree.Problems([]).Should().BeEmpty();

    [Test]
    public void ARootGroupHoldingConditionsAndNestedGroups_IsValid()
    {
        var root = Guid.NewGuid();
        var nested = Guid.NewGuid();
        var filter = new[]
        {
            Group(root),
            Condition(root),
            Group(nested, root, "or"),
            Condition(nested),
            Condition(nested),
        };

        FilterConditionTree.Problems(filter).Should().BeEmpty();
    }

    [Test]
    public void ALoneCondition_IsValid() =>
        FilterConditionTree.Problems([Condition(null)]).Should().BeEmpty();

    [Test]
    public void ConditionsListedSideBySide_AreRejectedWithTheShapeThatWorks()
    {
        var problems = FilterConditionTree.Problems([Condition(null), Condition(null)]);

        problems.Should().ContainSingle().Which.Should().Contain("filter_condition_group").And.Contain("parentId");
    }

    [Test]
    public void ARootThatIsAConditionBesideOtherRows_IsRejected()
    {
        var root = Guid.NewGuid();
        FilterConditionTree.Problems([Condition(null, root), Condition(root)]).Should().NotBeEmpty();
    }

    [Test]
    public void ARowWhoseParentIsMissingOrNotAGroup_IsRejected()
    {
        var root = Guid.NewGuid();
        var leaf = Condition(root);

        FilterConditionTree.Problems([Group(root), Condition(Guid.NewGuid())]).Should().NotBeEmpty();
        FilterConditionTree.Problems([Group(root), leaf, Condition(leaf.Id)]).Should().NotBeEmpty();
    }

    [Test]
    public void AGroupDetachedFromTheRoot_IsRejected()
    {
        var root = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        FilterConditionTree.Problems([Group(root), Group(a, b), Group(b, a)]).Should().NotBeEmpty();
    }

    [Test]
    public void MissingOrDuplicateIds_AreRejected()
    {
        var root = Guid.NewGuid();

        FilterConditionTree.Problems([Group(Guid.Empty), Condition(null, Guid.Empty)]).Should().NotBeEmpty();
        FilterConditionTree.Problems([Group(root), Condition(root, root)]).Should().NotBeEmpty();
    }

    [Test]
    public void AGroupJoiningSeveralRowsNeedsAndOrOr()
    {
        var root = Guid.NewGuid();
        var filter = new[] { Group(root, op: ""), Condition(root), Condition(root) };

        FilterConditionTree.Problems(filter).Should().ContainSingle().Which.Should().Contain("groupOperator");
    }
}
