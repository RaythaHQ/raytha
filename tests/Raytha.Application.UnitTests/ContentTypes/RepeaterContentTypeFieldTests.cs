using CSharpVitamins;
using FluentAssertions;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.ContentTypes.Commands;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Application.UnitTests.ContentTypes;

public class RepeaterContentTypeFieldTests
{
    private readonly Guid _contentTypeId = Guid.NewGuid();
    private Mock<IRaythaDbContext> _db = null!;
    private List<ContentTypeField> _fields = null!;

    [SetUp]
    public void Setup()
    {
        _db = new Mock<IRaythaDbContext>();
        _fields = [];
        var fields = _fields.AsQueryable().BuildMockDbSet();
        _db.Setup(x => x.ContentTypeFields).Returns(fields.Object);
        _db.Setup(x => x.ContentTypes)
            .Returns(new List<ContentType>().AsQueryable().BuildMockDbSet().Object);
    }

    [Test]
    public void Create_rejects_a_repeater_without_sub_fields()
    {
        var result = new CreateContentTypeField.Validator(_db.Object).Validate(Create([]));

        result.Errors.Select(e => e.ErrorMessage).Should().Contain("A repeater needs at least one sub-field.");
        result.Errors.Should().OnlyContain(e => e.PropertyName == "SubFields");
    }

    [Test]
    public void Create_rejects_invalid_sub_fields()
    {
        var result = new CreateContentTypeField.Validator(_db.Object).Validate(
            Create(
                [
                    new FieldDefinition { Label = "Q", DeveloperName = "q", FieldType = "single_line_text" },
                    new FieldDefinition { Label = "Nested", DeveloperName = "nested", FieldType = "repeater" },
                ]
            )
        );

        result
            .Errors.Select(e => e.ErrorMessage)
            .Should()
            .Equal("'Nested' cannot use the 'repeater' field type in a repeater.");
    }

    [Test]
    public void Create_accepts_valid_sub_fields()
    {
        new CreateContentTypeField.Validator(_db.Object)
            .Validate(Create([Question()]))
            .IsValid.Should()
            .BeTrue();
    }

    [Test]
    public async Task Create_stores_normalized_sub_fields()
    {
        await new CreateContentTypeField.Handler(_db.Object).Handle(
            Create([Question() with { DeveloperName = "The Question" }]),
            CancellationToken.None
        );

        _db.Verify(x =>
            x.ContentTypeFields.Add(
                It.Is<ContentTypeField>(f =>
                    f.FieldType.DeveloperName == "repeater" && f.SubFields.Single().DeveloperName == "the_question"
                )
            )
        );
    }

    [Test]
    public async Task Create_ignores_sub_fields_for_other_types()
    {
        await new CreateContentTypeField.Handler(_db.Object).Handle(
            Create([Question()]) with { FieldType = "single_line_text" },
            CancellationToken.None
        );

        _db.Verify(x => x.ContentTypeFields.Add(It.Is<ContentTypeField>(f => f.SubFields.Count == 0)));
    }

    [Test]
    public void Edit_checks_sub_fields_of_an_existing_repeater()
    {
        var id = Guid.NewGuid();
        _fields.Add(new ContentTypeField { Id = id, FieldType = BaseFieldType.Repeater });

        var result = new EditContentTypeField.Validator(_db.Object).Validate(
            new EditContentTypeField.Command
            {
                Id = id,
                Label = "FAQ",
                SubFields = [Question(), Question()],
            }
        );

        result
            .Errors.Select(e => e.ErrorMessage)
            .Should()
            .Equal("Sub-field developer names must be unique. Duplicates: question");
    }

    [Test]
    public async Task Edit_replaces_sub_fields_so_they_can_be_added_reordered_and_removed()
    {
        var id = Guid.NewGuid();
        var field = new ContentTypeField
        {
            Id = id,
            FieldType = BaseFieldType.Repeater,
            SubFields = [Question(), new FieldDefinition { Label = "Old", DeveloperName = "old", FieldType = "number" }],
        };
        _fields.Add(field);
        var answer = new FieldDefinition { Label = "Answer", DeveloperName = "answer", FieldType = "long_text" };

        await new EditContentTypeField.Handler(_db.Object).Handle(
            new EditContentTypeField.Command
            {
                Id = id,
                Label = "FAQ",
                SubFields = [answer, Question()],
            },
            CancellationToken.None
        );

        field.SubFields.Select(s => s.DeveloperName).Should().Equal("answer", "question");
    }

    private CreateContentTypeField.Command Create(IReadOnlyList<FieldDefinition> subFields) =>
        new()
        {
            ContentTypeId = (ShortGuid)_contentTypeId,
            FieldType = "repeater",
            DeveloperName = "faq",
            Label = "FAQ",
            Description = "",
            SubFields = subFields,
        };

    private static FieldDefinition Question() =>
        new()
        {
            Label = "Question",
            DeveloperName = "question",
            FieldType = "single_line_text",
            IsRequired = true,
        };
}
