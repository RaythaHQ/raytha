using FluentAssertions;
using Raytha.Application.ContentTypes;
using Raytha.Application.Views;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using static Raytha.Application.ContentTypes.SchemaImportPlanner;

namespace Raytha.Application.UnitTests.ContentTypes;

public class SchemaImportPlannerTests
{
    private static readonly Guid ListTemplateId = Guid.NewGuid();

    private static TemplateInfo ListTemplate(Guid? accessibleTo = null) =>
        new(
            ListTemplateId,
            BuiltInWebTemplate.ContentItemListViewPage.DeveloperName,
            false,
            true,
            accessibleTo is null ? new HashSet<Guid>() : new HashSet<Guid> { accessibleTo.Value }
        );

    private static ExistingSchema Site(
        IEnumerable<ContentType>? contentTypes = null,
        IEnumerable<string>? routePaths = null,
        Dictionary<Guid, IReadOnlySet<string>>? deletedFields = null,
        IEnumerable<TemplateInfo>? templates = null,
        Dictionary<Guid, WebTemplateViewRelation>? viewRelations = null
    ) =>
        new(
            (contentTypes ?? []).ToList(),
            deletedFields ?? [],
            (routePaths ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase),
            viewRelations ?? [],
            (templates ?? [ListTemplate()]).ToList()
        );

    private static SchemaField Field(
        string name,
        string type = "single_line_text",
        string? related = null,
        params SchemaChoice[] choices
    ) =>
        new()
        {
            DeveloperName = name,
            Label = name.ToUpperInvariant(),
            FieldType = type,
            RelatedContentType = related,
            Choices = choices,
        };

    private static SchemaContentType Type(
        string name,
        IReadOnlyList<SchemaField>? fields = null,
        string? primary = "title",
        IReadOnlyList<SchemaView>? views = null
    ) =>
        new()
        {
            DeveloperName = name,
            LabelPlural = name + "s",
            LabelSingular = name,
            DefaultRouteTemplate = name + "/{PrimaryField}",
            PrimaryField = primary,
            Fields = fields ?? [Field("title")],
            Views = views ?? [],
        };

    private static SchemaDocument Doc(params SchemaContentType[] types) =>
        new() { ContentTypes = types };

    private static ContentType ExistingPost()
    {
        var typeId = Guid.NewGuid();
        var titleId = Guid.NewGuid();
        var viewId = Guid.NewGuid();
        return new ContentType
        {
            Id = typeId,
            DeveloperName = "post",
            LabelPlural = "posts",
            LabelSingular = "post",
            Description = "",
            DefaultRouteTemplate = "post/{PrimaryField}",
            PrimaryFieldId = titleId,
            ContentTypeFields =
            [
                new ContentTypeField
                {
                    Id = titleId,
                    ContentTypeId = typeId,
                    DeveloperName = "title",
                    Label = "TITLE",
                    Description = "",
                    FieldOrder = 1,
                    FieldType = BaseFieldType.SingleLineText,
                },
                new ContentTypeField
                {
                    Id = Guid.NewGuid(),
                    ContentTypeId = typeId,
                    DeveloperName = "body",
                    Label = "BODY",
                    Description = "",
                    FieldOrder = 2,
                    FieldType = BaseFieldType.LongText,
                },
            ],
            Views =
            [
                new View
                {
                    Id = viewId,
                    ContentTypeId = typeId,
                    DeveloperName = "post",
                    Label = "All posts",
                    Description = "",
                    IsPublished = true,
                    Columns = ["PrimaryField"],
                    Route = new Route { ViewId = viewId, Path = "post" },
                },
            ],
        };
    }

    [Test]
    public void SchemaVersion_MustBeSupported()
    {
        var plan = Plan(new SchemaDocument { SchemaVersion = 7, ContentTypes = [Type("post")] }, Site());

        plan.Errors.Should().ContainSingle().Which.Should().Contain("schemaVersion 7");
    }

    [Test]
    public void EmptySchema_IsAnError()
    {
        Plan(new SchemaDocument(), Site()).Errors.Should().ContainSingle();
    }

    [Test]
    public void NewType_IsCreatedWithItsFieldsInOrder_AndAGeneratedDefaultView()
    {
        var plan = Plan(
            Doc(Type("post", [Field("title"), Field("body", "long_text")])),
            Site()
        );

        plan.Errors.Should().BeEmpty();
        var type = plan.Types.Should().ContainSingle().Subject;
        type.Action.Should().Be(PlanAction.Created);
        type.Fields.Select(f => (f.DeveloperName, f.Order)).Should().Equal(("title", 1), ("body", 2));
        type.PrimaryFieldId.Should().Be(type.Fields[0].Id);

        var view = type.Views.Should().ContainSingle().Subject;
        view.DeveloperName.Should().Be("post");
        view.RoutePath.Should().Be("post");
        view.TemplateId.Should().Be(ListTemplateId);
    }

    [Test]
    public void ExactSameModel_IsUnchanged()
    {
        var existing = ExistingPost();
        var document = Doc(
            Type(
                "post",
                [Field("title"), Field("body", "long_text")],
                views:
                [
                    new SchemaView
                    {
                        DeveloperName = "post",
                        Label = "All posts",
                        IsPublished = true,
                        Columns = ["PrimaryField"],
                        RoutePath = "post",
                    },
                ]
            )
        );

        var plan = Plan(document, Site([existing], ["post"], templates: [ListTemplate(existing.Id)]));

        plan.Errors.Should().BeEmpty();
        plan.Types[0].Action.Should().Be(PlanAction.Unchanged);
        plan.Types[0].Fields.Should().OnlyContain(f => f.Action == PlanAction.Unchanged);
        plan.Types[0].Views.Should().OnlyContain(v => v.Action == PlanAction.Unchanged);
    }

    [Test]
    public void ChangedLabelsAndOrder_AreReportedPerProperty()
    {
        var existing = ExistingPost();
        var document = Doc(
            Type(
                "post",
                [Field("body", "long_text") with { Label = "Text" }, Field("title")],
                views: [new SchemaView { DeveloperName = "post", Label = "All posts", IsPublished = true, Columns = ["PrimaryField"] }]
            ) with
            {
                LabelPlural = "Articles",
            }
        );

        var plan = Plan(document, Site([existing], ["post"], templates: [ListTemplate(existing.Id)]));

        plan.Errors.Should().BeEmpty();
        plan.Types[0].Changes.Should().Equal("labelPlural");
        plan.Types[0].Fields.Single(f => f.DeveloperName == "body").Changes.Should().Equal("label", "order");
        plan.Types[0].Fields.Single(f => f.DeveloperName == "title").Changes.Should().Equal("order");
    }

    [Test]
    public void FieldsTheDocumentLeavesOut_KeepTheirPlaceAfterTheListedOnes()
    {
        var existing = ExistingPost();

        var plan = Plan(
            Doc(Type("post", [Field("extra"), Field("title")])),
            Site([existing], ["post"], templates: [ListTemplate(existing.Id)])
        );

        var body = existing.ContentTypeFields.Single(f => f.DeveloperName == "body");
        plan.Types[0].UnlistedOrders.Should().ContainKey(body.Id).WhoseValue.Should().Be(3);
    }

    [Test]
    public void ChangingAFieldType_IsAnError()
    {
        var existing = ExistingPost();

        var plan = Plan(
            Doc(Type("post", [Field("title"), Field("body", "number")])),
            Site([existing], ["post"], templates: [ListTemplate(existing.Id)])
        );

        plan.Errors.Should().ContainSingle().Which.Should().Contain("post.body").And.Contain("cannot change");
    }

    [Test]
    public void ReservedUnknownAndDuplicateFields_AreErrors()
    {
        var plan = Plan(
            Doc(Type("post", [Field("title"), Field("title"), Field("id"), Field("x", "nope")])),
            Site()
        );

        plan.Errors.Should().Contain(e => e.Contains("appears more than once"));
        plan.Errors.Should().Contain(e => e.Contains("reserved word"));
        plan.Errors.Should().Contain(e => e.Contains("unknown field type 'nope'"));
    }

    [Test]
    public void AFieldWhoseNameWasUsedByADeletedField_IsAnError()
    {
        var existing = ExistingPost();
        var deleted = new Dictionary<Guid, IReadOnlySet<string>>
        {
            [existing.Id] = new HashSet<string> { "summary" },
        };

        var plan = Plan(
            Doc(Type("post", [Field("title"), Field("summary")])),
            Site([existing], ["post"], deleted, [ListTemplate(existing.Id)])
        );

        plan.Errors.Should().ContainSingle().Which.Should().Contain("previously deleted");
    }

    [Test]
    public void Choices_AreRequiredAndMustBeUnique()
    {
        var plan = Plan(
            Doc(
                Type(
                    "post",
                    [
                        Field("title"),
                        Field("empty", "dropdown"),
                        Field(
                            "dupes",
                            "radio",
                            choices:
                            [
                                new SchemaChoice { DeveloperName = "a", Label = "A" },
                                new SchemaChoice { DeveloperName = "a", Label = "B" },
                            ]
                        ),
                    ]
                )
            ),
            Site()
        );

        plan.Errors.Should().Contain(e => e.Contains("post.empty") && e.Contains("at least one choice"));
        plan.Errors.Should().Contain(e => e.Contains("post.dupes") && e.Contains("unique"));
    }

    [Test]
    public void Relationships_ResolveToTypesInTheSchemaOrOnTheSite_IncludingEachOther()
    {
        var existing = ExistingPost();

        var plan = Plan(
            Doc(
                Type("author", [Field("title"), Field("favorite", "one_to_one_relationship", "book")]),
                Type("book", [Field("title"), Field("author", "one_to_one_relationship", "author"), Field("post", "one_to_one_relationship", "post")])
            ),
            Site([existing], templates: [ListTemplate()])
        );

        plan.Errors.Should().BeEmpty();
        var author = plan.Types.Single(t => t.DeveloperName == "author");
        var book = plan.Types.Single(t => t.DeveloperName == "book");
        author.Fields[1].RelatedContentTypeId.Should().Be(book.Id);
        book.Fields[1].RelatedContentTypeId.Should().Be(author.Id);
        book.Fields[2].RelatedContentTypeId.Should().Be(existing.Id);
    }

    [Test]
    public void ARelationshipToNothing_IsAnError()
    {
        var plan = Plan(
            Doc(Type("post", [Field("title"), Field("a", "one_to_one_relationship", "ghost"), Field("b", "one_to_one_relationship")])),
            Site()
        );

        plan.Errors.Should().Contain(e => e.Contains("'ghost'"));
        plan.Errors.Should().Contain(e => e.Contains("post.b") && e.Contains("required"));
    }

    [Test]
    public void PrimaryField_MustExistAndBeSingleLineText()
    {
        Plan(Doc(Type("post", primary: null)), Site()).Errors.Should().ContainSingle().Which.Should().Contain("primaryField is required");
        Plan(Doc(Type("post", primary: "nope")), Site()).Errors.Should().ContainSingle().Which.Should().Contain("not a field");
        Plan(Doc(Type("post", [Field("title", "number")])), Site()).Errors.Should().ContainSingle().Which.Should().Contain("single_line_text");
    }

    [Test]
    public void ChangingThePrimaryField_IsReportedOnTheType()
    {
        var existing = ExistingPost();

        var plan = Plan(
            Doc(Type("post", [Field("title"), Field("body", "long_text"), Field("slug")], primary: "slug")),
            Site([existing], ["post"], templates: [ListTemplate(existing.Id)])
        );

        plan.Errors.Should().BeEmpty();
        plan.Types[0].Changes.Should().Contain("primaryField");
        plan.Types[0].Action.Should().Be(PlanAction.Updated);
    }

    [Test]
    public void Views_ValidateColumnsSortFiltersAndPaging()
    {
        var view = new SchemaView
        {
            DeveloperName = "recent",
            Label = "Recent",
            Columns = ["title", "ghost"],
            Sort = [new SchemaSort { DeveloperName = "title", Direction = "sideways" }],
            Filter =
            [
                new FilterConditionInputDto { Type = "filter_condition", Field = "ghost", ConditionOperator = "eq", Value = "x" },
                new FilterConditionInputDto { Type = "filter_condition", Field = "title", ConditionOperator = "eq" },
                new FilterConditionInputDto { Type = "bogus" },
            ],
            DefaultNumberOfItemsPerPage = 50,
            MaxNumberOfItemsPerPage = 10,
        };

        var plan = Plan(Doc(Type("post", views: [view])), Site());

        plan.Errors.Should().Contain(e => e.Contains("column 'ghost'"));
        plan.Errors.Should().Contain(e => e.Contains("sort direction 'sideways'"));
        plan.Errors.Should().Contain(e => e.Contains("filter condition 1") && e.Contains("not a field"));
        plan.Errors.Should().Contain(e => e.Contains("filter condition 2") && e.Contains("missing a value"));
        plan.Errors.Should().Contain(e => e.Contains("filter condition 3"));
        plan.Errors.Should().Contain(e => e.Contains("maxNumberOfItemsPerPage"));
    }

    [Test]
    public void Views_RejectAFlatFilterListInsteadOfApplyingOnlyTheFirstCondition()
    {
        var view = new SchemaView
        {
            DeveloperName = "recent",
            Label = "Recent",
            Filter =
            [
                new FilterConditionInputDto { Id = Guid.NewGuid(), Type = "filter_condition", Field = "title", ConditionOperator = "eq", Value = "a" },
                new FilterConditionInputDto { Id = Guid.NewGuid(), Type = "filter_condition", Field = "title", ConditionOperator = "eq", Value = "b" },
            ],
        };

        var plan = Plan(Doc(Type("post", views: [view])), Site());

        plan.Errors.Should().Contain(e => e.Contains("filter_condition_group"));
    }

    [Test]
    public void ViewRoutePaths_MustBeFreeAndUnprotected_AndAreGeneratedWhenOmitted()
    {
        var views = new[]
        {
            new SchemaView { DeveloperName = "taken", Label = "T", RoutePath = "news" },
            new SchemaView { DeveloperName = "protected_one", Label = "P", RoutePath = "raytha/x" },
            new SchemaView { DeveloperName = "generated", Label = "G" },
        };

        var plan = Plan(Doc(Type("post", views: views)), Site(routePaths: ["news"]));

        plan.Errors.Should().Contain(e => e.Contains("'news' is already in use"));
        plan.Errors.Should().Contain(e => e.Contains("protected path"));
        plan.Types[0].Views.Single(v => v.DeveloperName == "generated").RoutePath.Should().Be("post/generated");
    }

    [Test]
    public void TwoViewsCannotClaimTheSameRoutePath()
    {
        var views = new[]
        {
            new SchemaView { DeveloperName = "a", Label = "A", RoutePath = "list" },
            new SchemaView { DeveloperName = "b", Label = "B", RoutePath = "list" },
        };

        Plan(Doc(Type("post", views: views)), Site()).Errors.Should().ContainSingle().Which.Should().Contain("already in use");
    }

    [Test]
    public void AnUnavailableViewTemplate_FallsBackWithAWarning()
    {
        var views = new[] { new SchemaView { DeveloperName = "all", Label = "All", Template = "missing" } };

        var plan = Plan(Doc(Type("post", views: views)), Site());

        plan.Errors.Should().BeEmpty();
        plan.Warnings.Should().ContainSingle().Which.Should().Contain("'missing'");
        plan.Types[0].Views[0].TemplateId.Should().Be(ListTemplateId);
    }

    [Test]
    public void ANewTypeNeedsATemplateOpenToNewTypes()
    {
        var closed = ListTemplate() with { AllowAccessForNewContentTypes = false };

        Plan(Doc(Type("post")), Site(templates: [closed])).Errors.Should().NotBeEmpty();
    }

    [Test]
    public void TheSameTypeTwice_IsAnError()
    {
        Plan(Doc(Type("post"), Type("Post")), Site()).Errors.Should().ContainSingle().Which.Should().Contain("more than once");
    }
}
