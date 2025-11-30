using CSharpVitamins;
using FluentAssertions;
using MockQueryable;
using MockQueryable; // Add this
using MockQueryable.Moq;
using Moq;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.ContentItems.Commands;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.ContentItems.Commands;

public class RestoreContentItemTests
{
    private Mock<IRaythaDbContext> _dbMock;
    private Mock<IContentTypeInRoutePath> _contentTypeInRoutePathMock;

    [SetUp]
    public void Setup()
    {
        _dbMock = new Mock<IRaythaDbContext>();
        _contentTypeInRoutePathMock = new Mock<IContentTypeInRoutePath>();
    }

    [Test]
    public async Task Handler_ShouldThrowNotFoundException_WhenDeletedContentItemNotFound()
    {
        var deletedContentItems = new List<DeletedContentItem>().AsQueryable().BuildMockDbSet();
        _dbMock.Setup(x => x.DeletedContentItems).Returns(deletedContentItems.Object);

        var handler = new RestoreContentItem.Handler(
            _dbMock.Object,
            _contentTypeInRoutePathMock.Object
        );
        var command = new RestoreContentItem.Command { Id = ShortGuid.NewGuid() };

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<Raytha.Application.Common.Exceptions.NotFoundException>();
    }
}
