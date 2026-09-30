using System.Text.Json;
using CSharpVitamins;
using FluentAssertions;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.ContentItems.Commands;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Application.UnitTests.ContentItems.Commands;

public class RepeaterContentItemTests
{
    private Mock<IRaythaDbContext> _db = null!;
    private ContentItem _item = null!;

    [SetUp]
    public void Setup()
    {
        _item = new ContentItem
        {
            Id = Guid.NewGuid(),
            IsPublished = true,
            _PublishedContent = "{}",
            _DraftContent = "{}",
            ContentType = new ContentType
            {
                DeveloperName = "pages",
                LabelSingular = "Page",
                ContentTypeFields =
                [
                    new ContentTypeField
                    {
                        DeveloperName = "accent",
                        Label = "Accent",
                        FieldType = BaseFieldType.Color,
                    },
                    new ContentTypeField
                    {
                        DeveloperName = "faq",
                        Label = "FAQ",
                        FieldType = BaseFieldType.Repeater,
                        IsRequired = true,
                        SubFields =
                        [
                            new FieldDefinition
                            {
                                DeveloperName = "question",
                                Label = "Question",
                                FieldType = "single_line_text",
                                IsRequired = true,
                            },
                            new FieldDefinition { DeveloperName = "accent", Label = "Accent", FieldType = "color" },
                        ],
                    },
                ],
            },
        };
        _db = new Mock<IRaythaDbContext>();
        _db.Setup(x => x.ContentItems)
            .Returns(new List<ContentItem> { _item }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.ContentItemRevisions)
            .Returns(new List<ContentItemRevision>().AsQueryable().BuildMockDbSet().Object);
    }

    [Test]
    public void Edit_reports_row_errors_against_sub_fields()
    {
        var result = Validate(
            """{"accent":"#fff","faq":[{"question":"Why?","accent":"#123"},{"question":"","accent":"teal"}]}"""
        );

        result
            .Errors.Where(e => e.PropertyName == "faq")
            .Select(e => e.ErrorMessage)
            .Should()
            .Equal("FAQ, row 2: 'Question' field is required.", "FAQ, row 2: 'Accent' is an invalid format.");
    }

    [Test]
    public void Edit_rejects_a_bad_color_and_a_repeater_that_is_not_rows()
    {
        var result = Validate("""{"accent":"orange","faq":{"question":"Why?"}}""");

        result
            .Errors.Select(e => e.ErrorMessage)
            .Should()
            .Equal("'Accent' is an invalid format.", "'FAQ' is an invalid format.");
    }

    [Test]
    public void Edit_requires_at_least_one_row_for_a_required_repeater_unless_drafting()
    {
        Validate("""{"faq":[]}""").Errors.Select(e => e.ErrorMessage).Should().Equal("'FAQ' field is required.");
        Validate("""{"faq":[]}""", saveAsDraft: true).IsValid.Should().BeTrue();
    }

    [Test]
    public async Task Edit_stores_the_canonical_color_and_typed_rows()
    {
        await new EditContentItem.Handler(_db.Object).Handle(
            Command("""{"accent":"#ABC","faq":[{"question":"Why?","accent":"#FFF","stray":1}]}""", false),
            CancellationToken.None
        );

        _item._PublishedContent.Should().Be("""{"accent":"#aabbcc","faq":[{"question":"Why?","accent":"#ffffff"}]}""");
    }

    private FluentValidation.Results.ValidationResult Validate(string content, bool saveAsDraft = false) =>
        new EditContentItem.Validator(_db.Object, Mock.Of<IContentTypeInRoutePath>()).Validate(
            Command(content, saveAsDraft)
        );

    private EditContentItem.Command Command(string content, bool saveAsDraft) =>
        new()
        {
            Id = (ShortGuid)_item.Id,
            SaveAsDraft = saveAsDraft,
            Content = JsonSerializer
                .Deserialize<Dictionary<string, JsonElement>>(content)!
                .ToDictionary(kv => kv.Key, kv => (dynamic)kv.Value),
        };
}
