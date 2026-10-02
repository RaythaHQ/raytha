using CSharpVitamins;
using FluentAssertions;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.ContentItems.Commands;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.ContentItems.Commands;

public class BeginBatchCreateContentItemsTests
{
    private Mock<IRaythaDbContext> _dbMock;
    private BeginBatchCreateContentItems.Validator _validator;

    [SetUp]
    public void Setup()
    {
        _dbMock = new Mock<IRaythaDbContext>();
        _dbMock
            .Setup(x => x.ContentTypes)
            .Returns(
                new List<ContentType> { new() { Id = Guid.NewGuid(), DeveloperName = "post" } }
                    .AsQueryable()
                    .BuildMockDbSet()
                    .Object
            );
        _validator = new BeginBatchCreateContentItems.Validator(_dbMock.Object);
    }

    private static BeginBatchCreateContentItems.BatchItem Item(ShortGuid? templateId = null) =>
        new() { TemplateId = templateId ?? ShortGuid.Empty, Content = new Dictionary<string, dynamic>() };

    [Test]
    public void Command_SurvivesTheRoundTripThroughTheTaskQueue_WhenNoTemplateIsGiven()
    {
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new Raytha.Domain.JsonConverters.ShortGuidConverter() },
        };
        var item = System.Text.Json.JsonSerializer.Deserialize<BeginBatchCreateContentItems.BatchItem>(
            """{"content":{"title":"Dune"}}""",
            options
        )!;
        var command = new BeginBatchCreateContentItems.Command
        {
            ContentTypeDeveloperName = "post",
            Items = [item],
        };

        var json = System.Text.Json.JsonSerializer.Serialize(command, options);
        var back = System.Text.Json.JsonSerializer.Deserialize<BeginBatchCreateContentItems.Command>(
            json,
            options
        )!;

        back.TemplateId.Should().Be(ShortGuid.Empty);
        back.Items.Should().ContainSingle().Which.TemplateId.Should().Be(ShortGuid.Empty);
        ((object)back.Items[0].Content["title"]).ToString().Should().Be("Dune");
    }

    [Test]
    public void Validator_Throws_WhenTheContentTypeDoesNotExist()
    {
        var command = new BeginBatchCreateContentItems.Command
        {
            ContentTypeDeveloperName = "unknown",
            TemplateId = ShortGuid.NewGuid(),
            Items = [Item()],
        };

        var act = () => _validator.Validate(command);

        act.Should().Throw<NotFoundException>();
    }

    [Test]
    public void Validator_Rejects_AnEmptyBatch()
    {
        var command = new BeginBatchCreateContentItems.Command
        {
            ContentTypeDeveloperName = "post",
            TemplateId = ShortGuid.NewGuid(),
        };

        var result = _validator.Validate(command);

        result.Errors.Should().ContainSingle(e => e.PropertyName == "Items");
    }

    [Test]
    public void Validator_Rejects_ABatchOverTheLimit()
    {
        var command = new BeginBatchCreateContentItems.Command
        {
            ContentTypeDeveloperName = "post",
            TemplateId = ShortGuid.NewGuid(),
            Items = Enumerable
                .Range(0, Raytha.Application.ContentItems.ContentItemBatchPlanner.MaxBatchSize + 1)
                .Select(_ => Item())
                .ToList(),
        };

        var result = _validator.Validate(command);

        result.Errors.Should().ContainSingle(e => e.PropertyName == "Items");
    }

    [Test]
    public void Validator_RequiresATemplateOnTheItemOrTheBatch()
    {
        var command = new BeginBatchCreateContentItems.Command
        {
            ContentTypeDeveloperName = "post",
            Items = [Item(ShortGuid.NewGuid()), Item()],
        };

        var result = _validator.Validate(command);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("Items[1].TemplateId");
    }

    [Test]
    public void Validator_Passes_WhenTheBatchTemplateCoversEveryItem()
    {
        var command = new BeginBatchCreateContentItems.Command
        {
            ContentTypeDeveloperName = "post",
            TemplateId = ShortGuid.NewGuid(),
            Items = [Item(), Item(ShortGuid.NewGuid())],
        };

        _validator.Validate(command).IsValid.Should().BeTrue();
    }
}
