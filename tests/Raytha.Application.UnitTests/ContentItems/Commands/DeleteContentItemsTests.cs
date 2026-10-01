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

public class DeleteContentItemsTests
{
    private Mock<IRaythaDbContext> _db = null!;
    private DeleteContentItems.Validator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _db = new Mock<IRaythaDbContext>();
        _validator = new DeleteContentItems.Validator(_db.Object);
    }

    [Test]
    public void Validator_rejects_an_empty_id_list()
    {
        var contentTypeId = Guid.NewGuid();
        Stub(contentTypeId, [], homePageId: null);

        var result = _validator.Validate(
            new DeleteContentItems.Command { ContentTypeId = contentTypeId, Ids = [] }
        );

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Ids");
    }

    [Test]
    public void Validator_rejects_an_id_that_is_not_an_item_of_the_content_type()
    {
        var contentTypeId = Guid.NewGuid();
        var keeper = Item(contentTypeId, "Keep");
        Stub(contentTypeId, [keeper], homePageId: null);

        var result = _validator.Validate(
            new DeleteContentItems.Command
            {
                ContentTypeId = contentTypeId,
                Ids = [(ShortGuid)Guid.NewGuid()],
            }
        );

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("not found"));
    }

    [Test]
    public void Validator_rejects_deleting_the_home_page()
    {
        var contentTypeId = Guid.NewGuid();
        var home = Item(contentTypeId, "Home");
        Stub(contentTypeId, [home], home.Id);

        var result = _validator.Validate(
            new DeleteContentItems.Command { ContentTypeId = contentTypeId, Ids = [(ShortGuid)home.Id] }
        );

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("home page"));
    }

    [Test]
    public async Task Handler_trashes_only_the_listed_items_and_reads_the_primary_field_from_stored_json()
    {
        var contentTypeId = Guid.NewGuid();
        var primaryFieldId = Guid.NewGuid();
        var keep = Item(contentTypeId, "Keep me");
        var trash = Item(contentTypeId, "Trash me");
        Stub(contentTypeId, [keep, trash], homePageId: null, primaryFieldId);

        _db.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new DeleteContentItems.Handler(_db.Object);
        var result = await handler.Handle(
            new DeleteContentItems.Command
            {
                ContentTypeId = contentTypeId,
                Ids = [(ShortGuid)trash.Id],
            },
            CancellationToken.None
        );

        result.Success.Should().BeTrue();
        _db.Verify(db => db.ContentItems.Remove(trash), Times.Once);
        _db.Verify(db => db.ContentItems.Remove(keep), Times.Never);
        _db.Verify(
            db =>
                db.DeletedContentItems.Add(
                    It.Is<DeletedContentItem>(deleted =>
                        deleted.OriginalContentItemId == trash.Id && deleted.PrimaryField == "Trash me"
                    )
                ),
            Times.Once
        );
    }

    private void Stub(Guid contentTypeId, ContentItem[] items, Guid? homePageId, Guid? primaryFieldId = null)
    {
        var fieldId = primaryFieldId ?? Guid.NewGuid();
        var contentType = new ContentType
        {
            Id = contentTypeId,
            DeveloperName = "posts",
            PrimaryFieldId = fieldId,
            ContentTypeFields =
            [
                new ContentTypeField
                {
                    Id = fieldId,
                    DeveloperName = "title",
                    FieldType = BaseFieldType.SingleLineText,
                },
            ],
        };

        _db.Setup(db => db.ContentTypes)
            .Returns(new List<ContentType> { contentType }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(db => db.ContentItems).Returns(items.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(db => db.Routes)
            .Returns(items.Select(item => item.Route).AsQueryable().BuildMockDbSet().Object);
        _db.Setup(db => db.DeletedContentItems)
            .Returns(new List<DeletedContentItem>().AsQueryable().BuildMockDbSet().Object);
        _db.Setup(db => db.WebTemplateContentItemRelations)
            .Returns(new List<WebTemplateContentItemRelation>().AsQueryable().BuildMockDbSet().Object);
        _db.Setup(db => db.OrganizationSettings)
            .Returns(
                new List<Raytha.Domain.Entities.OrganizationSettings> { new() { HomePageId = homePageId } }
                    .AsQueryable()
                    .BuildMockDbSet()
                    .Object
            );
    }

    private static ContentItem Item(Guid contentTypeId, string title)
    {
        var id = Guid.NewGuid();
        return new ContentItem
        {
            Id = id,
            ContentTypeId = contentTypeId,
            _PublishedContent = "{\"title\":\"" + title + "\"}",
            Route = new Route { Path = "posts/" + title.ToLowerInvariant().Replace(' ', '-'), ContentItemId = id },
        };
    }
}
