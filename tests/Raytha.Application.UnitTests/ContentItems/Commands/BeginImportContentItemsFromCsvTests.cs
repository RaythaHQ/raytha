using CSharpVitamins;
using FluentAssertions;
using MockQueryable.Moq;
using MockQueryable;
using Moq;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.ContentItems.Commands;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.ContentItems.Commands;

public class BeginImportContentItemsFromCsvTests
{
    private Mock<IBackgroundTaskQueue> _taskQueueMock;
    private Mock<IRaythaDbContext> _dbMock;
    private Mock<IContentTypeInRoutePath> _contentTypeInRoutePathMock;
    private Mock<ICsvService> _csvServiceMock;
    private BeginImportContentItemsFromCsv.Validator _validator;

    [SetUp]
    public void Setup()
    {
        _taskQueueMock = new Mock<IBackgroundTaskQueue>();
        _dbMock = new Mock<IRaythaDbContext>();
        _contentTypeInRoutePathMock = new Mock<IContentTypeInRoutePath>();
        _csvServiceMock = new Mock<ICsvService>();
        _validator = new BeginImportContentItemsFromCsv.Validator(_dbMock.Object, _csvServiceMock.Object);
    }

    [Test]
    public void Validator_ShouldHaveError_WhenContentTypeNotFound()
    {
        var contentTypes = new List<ContentType>().AsQueryable().BuildMockDbSet();
        _dbMock.Setup(x => x.ContentTypes).Returns(contentTypes.Object);

        var command = new BeginImportContentItemsFromCsv.Command 
        { 
            ContentTypeId = ShortGuid.NewGuid(),
            ImportMethod = "add_new_records_only"
        };

        Action act = () => _validator.Validate(command);

        act.Should().Throw<Raytha.Application.Common.Exceptions.NotFoundException>();
    }

    [Test]
    public void Validator_ShouldHaveError_WhenImportMethodIsInvalid()
    {
        var contentTypeId = ShortGuid.NewGuid();
        var contentTypes = new List<ContentType> { new ContentType { Id = contentTypeId.Guid } }.AsQueryable().BuildMockDbSet();
        _dbMock.Setup(x => x.ContentTypes).Returns(contentTypes.Object);

        var command = new BeginImportContentItemsFromCsv.Command 
        { 
            ContentTypeId = contentTypeId,
            ImportMethod = "invalid_method"
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("Unknown import method"));
    }

    [Test]
    public void Validator_ShouldHaveError_WhenCsvIsMissing()
    {
        var contentTypeId = ShortGuid.NewGuid();
        var contentTypes = new List<ContentType> { new ContentType { Id = contentTypeId.Guid } }.AsQueryable().BuildMockDbSet();
        _dbMock.Setup(x => x.ContentTypes).Returns(contentTypes.Object);

        var command = new BeginImportContentItemsFromCsv.Command 
        { 
            ContentTypeId = contentTypeId,
            ImportMethod = "add_new_records_only",
            CsvAsBytes = null
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage == "You must upload a CSV file.");
    }

    [Test]
    public async Task Handler_ShouldEnqueueTask_WhenCommandIsValid()
    {
        var contentTypeId = Guid.NewGuid();
        var contentType = new ContentType { Id = contentTypeId, DeveloperName = "post" };
        var contentTypes = new List<ContentType> { contentType }.AsQueryable().BuildMockDbSet();
        _dbMock.Setup(x => x.ContentTypes).Returns(contentTypes.Object);

        var jobId = Guid.NewGuid();
        _taskQueueMock.Setup(x => x.EnqueueAsync<BeginImportContentItemsFromCsv.BackgroundTask>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobId);

        var handler = new BeginImportContentItemsFromCsv.Handler(_taskQueueMock.Object, _dbMock.Object, _contentTypeInRoutePathMock.Object);
        var command = new BeginImportContentItemsFromCsv.Command 
        { 
            ContentTypeId = (ShortGuid)contentTypeId,
            ImportMethod = "add_new_records_only",
            CsvAsBytes = new byte[] { 1, 2, 3 }
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Result.Should().Be((ShortGuid)jobId);
        _contentTypeInRoutePathMock.Verify(x => x.ValidateContentTypeInRoutePathMatchesValue("post"), Times.Once);
        _taskQueueMock.Verify(x => x.EnqueueAsync<BeginImportContentItemsFromCsv.BackgroundTask>(command, It.IsAny<CancellationToken>()), Times.Once);
    }
}

