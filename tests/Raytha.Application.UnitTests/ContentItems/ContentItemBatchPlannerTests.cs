using System.Text.Json;
using CSharpVitamins;
using FluentAssertions;
using Raytha.Application.ContentItems;
using static Raytha.Application.ContentItems.ContentItemBatchPlanner;

namespace Raytha.Application.UnitTests.ContentItems;

public class ContentItemBatchPlannerTests
{
    private static readonly Guid CategoryTypeId = Guid.NewGuid();
    private static readonly Guid PostTypeId = Guid.NewGuid();

    private static readonly RelationshipField Parent = new("parent", "Parent", CategoryTypeId);
    private static readonly RelationshipField Category = new("category", "Category", CategoryTypeId);

    private static IDictionary<string, dynamic> Item(params (string Key, object Value)[] values) =>
        values.ToDictionary(v => v.Key, v => (dynamic)v.Value);

    private static IReadOnlyDictionary<(Guid, string), ReferenceMatch> NoLookups =>
        new Dictionary<(Guid, string), ReferenceMatch>();

    [Test]
    public void ReferenceText_ReadsStringsAndJsonStrings_AndIgnoresEverythingElse()
    {
        ReferenceText(" Books ").Should().Be("Books");
        ReferenceText(JsonDocument.Parse("\"Books\"").RootElement).Should().Be("Books");
        ReferenceText(JsonDocument.Parse("null").RootElement).Should().BeNull();
        ReferenceText(JsonDocument.Parse("5").RootElement).Should().BeNull();
        ReferenceText("  ").Should().BeNull();
        ReferenceText(null).Should().BeNull();
    }

    [Test]
    public void Plan_UsesTheIdOfAnExistingItem()
    {
        var existing = Guid.NewGuid();
        var contents = new[] { Item(("title", "Dune"), ("category", "books")) };
        var lookups = new Dictionary<(Guid, string), ReferenceMatch>
        {
            [(CategoryTypeId, "books")] = new(existing, 1),
        };

        var plan = Plan(PostTypeId, "title", "Title", contents, [Category], lookups);

        plan.Items[0].Errors.Should().BeEmpty();
        plan.Items[0].Resolved["category"].Should().Be(((ShortGuid)existing).ToString());
        plan.Order.Should().Equal(0);
    }

    [Test]
    public void Plan_ReportsAReferenceThatMatchesNothing()
    {
        var contents = new[] { Item(("title", "Dune"), ("category", "books")) };

        var plan = Plan(PostTypeId, "title", "Title", contents, [Category], NoLookups);

        var error = plan.Items[0].Errors.Should().ContainSingle().Subject;
        error.Field.Should().Be("category");
        error.Message.Should().Contain("'books'");
    }

    [Test]
    public void Plan_ReportsAReferenceThatMatchesMoreThanOneExistingItem()
    {
        var contents = new[] { Item(("title", "Dune"), ("category", "books")) };
        var lookups = new Dictionary<(Guid, string), ReferenceMatch>
        {
            [(CategoryTypeId, "books")] = new(null, 2),
        };

        var plan = Plan(PostTypeId, "title", "Title", contents, [Category], lookups);

        plan.Items[0].Errors.Should().ContainSingle().Which.Message.Should().Contain("more than one");
    }

    [Test]
    public void Plan_OrdersAParentBeforeItsChildren_WhateverOrderTheyWereSent()
    {
        var contents = new[]
        {
            Item(("title", "Fiction"), ("parent", "Books")),
            Item(("title", "Sci-fi"), ("parent", "Fiction")),
            Item(("title", "Books")),
        };

        var plan = Plan(CategoryTypeId, "title", "Title", contents, [Parent], NoLookups);

        plan.Items.SelectMany(i => i.Errors).Should().BeEmpty();
        plan.Items[0].DependsOn["parent"].Should().Be(2);
        plan.Items[1].DependsOn["parent"].Should().Be(0);
        plan.Order.Should().Equal(2, 0, 1);
    }

    [Test]
    public void Plan_PrefersAnExistingItemOverOneInTheSameBatch()
    {
        var existing = Guid.NewGuid();
        var contents = new[]
        {
            Item(("title", "Fiction"), ("parent", "Books")),
            Item(("title", "Books")),
        };
        var lookups = new Dictionary<(Guid, string), ReferenceMatch>
        {
            [(CategoryTypeId, "Books")] = new(existing, 1),
        };

        var plan = Plan(CategoryTypeId, "title", "Title", contents, [Parent], lookups);

        plan.Items[0].DependsOn.Should().BeEmpty();
        plan.Items[0].Resolved["parent"].Should().Be(((ShortGuid)existing).ToString());
    }

    [Test]
    public void Plan_DoesNotLookInTheBatchForAnotherContentType()
    {
        var contents = new[]
        {
            Item(("title", "Dune"), ("category", "Books")),
            Item(("title", "Books")),
        };

        var plan = Plan(PostTypeId, "title", "Title", contents, [Category], NoLookups);

        plan.Items[0].Errors.Should().ContainSingle();
    }

    [Test]
    public void Plan_ReportsAReferenceThatMatchesSeveralItemsInTheBatch()
    {
        var contents = new[]
        {
            Item(("title", "Fiction"), ("parent", "Books")),
            Item(("title", "Books")),
            Item(("title", "Books")),
        };

        var plan = Plan(CategoryTypeId, "title", "Title", contents, [Parent], NoLookups);

        plan.Items[0].Errors.Should().ContainSingle().Which.Message.Should().Contain("2 items in this batch");
    }

    [Test]
    public void Plan_DoesNotLetAnItemReferenceItself()
    {
        var contents = new[] { Item(("title", "Books"), ("parent", "Books")) };

        var plan = Plan(CategoryTypeId, "title", "Title", contents, [Parent], NoLookups);

        plan.Items[0].DependsOn.Should().BeEmpty();
        plan.Items[0].Errors.Should().ContainSingle();
    }

    [Test]
    public void Plan_LeavesCyclesOutOfTheOrder_AndReportsThemWithTheItemsWaitingOnThem()
    {
        var contents = new[]
        {
            Item(("title", "A"), ("parent", "B")),
            Item(("title", "B"), ("parent", "A")),
            Item(("title", "C"), ("parent", "A")),
            Item(("title", "D")),
        };

        var plan = Plan(CategoryTypeId, "title", "Title", contents, [Parent], NoLookups);

        plan.Order.Should().Equal(3);
        plan.Items[0].Errors.Should().ContainSingle().Which.Message.Should().Contain("Circular");
        plan.Items[1].Errors.Should().ContainSingle();
        plan.Items[2].Errors.Should().ContainSingle();
        plan.Items[3].Errors.Should().BeEmpty();
    }

    [Test]
    public void BuildContent_SubstitutesResolvedAndCreatedIds_AndLeavesOtherValuesAlone()
    {
        var existing = Guid.NewGuid();
        var contents = new[]
        {
            Item(("title", "Fiction"), ("parent", "Books"), ("category", "x")),
            Item(("title", "Books")),
        };
        var lookups = new Dictionary<(Guid, string), ReferenceMatch>
        {
            [(CategoryTypeId, "x")] = new(existing, 1),
        };
        var plan = Plan(CategoryTypeId, "title", "Title", contents, [Parent, Category], lookups);
        var booksId = ShortGuid.NewGuid();
        var errors = new List<ItemError>();

        var built = BuildContent(plan.Items[0], contents[0], new Dictionary<int, ShortGuid> { [1] = booksId }, errors);

        errors.Should().BeEmpty();
        ((string)built["parent"]).Should().Be(booksId.ToString());
        ((string)built["category"]).Should().Be(((ShortGuid)existing).ToString());
        ((string)built["title"]).Should().Be("Fiction");
        ((string)contents[0]["parent"]).Should().Be("Books", "the request content is not modified");
    }

    [Test]
    public void BuildContent_ReportsADependencyThatWasNotCreated()
    {
        var contents = new[]
        {
            Item(("title", "Fiction"), ("parent", "Books")),
            Item(("title", "Books")),
        };
        var plan = Plan(CategoryTypeId, "title", "Title", contents, [Parent], NoLookups);
        var errors = new List<ItemError>();

        BuildContent(plan.Items[0], contents[0], new Dictionary<int, ShortGuid>(), errors);

        errors.Should().ContainSingle().Which.Message.Should().Contain("item 1");
    }
}
