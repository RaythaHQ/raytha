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

public class SetAsHomePageTests
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
    public async Task Handler_ShouldSetHomePage_WhenCommandIsValid()
    {
        var contentItemId = Guid.NewGuid();
        var orgSettings = new Raytha.Domain.Entities.OrganizationSettings 
        { 
            HomePageId = Guid.NewGuid() 
        };
        
        var contentItem = new ContentItem 
        { 
            Id = contentItemId,
            ContentType = new ContentType { DeveloperName = "post" }
        };

        var contentItems = new List<ContentItem> { contentItem }.AsQueryable().BuildMockDbSet();
        _dbMock.Setup(x => x.ContentItems).Returns(contentItems.Object);

        var settingsList = new List<Raytha.Domain.Entities.OrganizationSettings> { orgSettings }.AsQueryable().BuildMockDbSet();
        _dbMock.Setup(x => x.OrganizationSettings).Returns(settingsList.Object);

        var handler = new SetAsHomePage.Handler(_dbMock.Object);
        var command = new SetAsHomePage.Command { Id = (ShortGuid)contentItemId };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        orgSettings.HomePageId.Should().Be(contentItemId);
        _dbMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

