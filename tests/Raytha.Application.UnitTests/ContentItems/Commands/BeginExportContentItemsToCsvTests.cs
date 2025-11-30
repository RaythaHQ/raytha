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

public class BeginExportContentItemsToCsvTests
{
    private Mock<IBackgroundTaskQueue> _taskQueueMock;
    private Mock<IRaythaDbContext> _dbMock;
    private Mock<IContentTypeInRoutePath> _contentTypeInRoutePathMock;
    private BeginExportContentItemsToCsv.Validator _validator;

    [SetUp]
    public void Setup()
    {
        _taskQueueMock = new Mock<IBackgroundTaskQueue>();
        _dbMock = new Mock<IRaythaDbContext>();
        _contentTypeInRoutePathMock = new Mock<IContentTypeInRoutePath>();
        _validator = new BeginExportContentItemsToCsv.Validator();
    }

    [Test]
    public void Validator_ShouldHaveError_WhenViewIdIsEmpty()
    {
        var command = new BeginExportContentItemsToCsv.Command { ViewId = ShortGuid.Empty };
        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "ViewId");
    }

    [Test]
    public async Task Handler_ShouldEnqueueTask_WhenCommandIsValid()
    {
        var viewId = Guid.NewGuid();
        var contentType = new ContentType { DeveloperName = "post" };
        var view = new View { Id = viewId, ContentType = contentType };

        var views = new List<View> { view }.AsQueryable().BuildMockDbSet();
        _dbMock.Setup(x => x.Views).Returns(views.Object);

        var jobId = Guid.NewGuid();
        _taskQueueMock.Setup(x => x.EnqueueAsync<BeginExportContentItemsToCsv.BackgroundTask>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobId);

        var handler = new BeginExportContentItemsToCsv.Handler(_taskQueueMock.Object, _dbMock.Object, _contentTypeInRoutePathMock.Object);
        var command = new BeginExportContentItemsToCsv.Command { ViewId = (ShortGuid)viewId };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Result.Should().Be((ShortGuid)jobId);
        _contentTypeInRoutePathMock.Verify(x => x.ValidateContentTypeInRoutePathMatchesValue("post"), Times.Once);
        _taskQueueMock.Verify(x => x.EnqueueAsync<BeginExportContentItemsToCsv.BackgroundTask>(command, It.IsAny<CancellationToken>()), Times.Once);
    }
}

