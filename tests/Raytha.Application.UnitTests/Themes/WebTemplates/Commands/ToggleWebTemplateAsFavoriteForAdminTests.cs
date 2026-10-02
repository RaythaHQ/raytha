using CSharpVitamins;
using FluentAssertions;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Themes.WebTemplates.Commands;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.Themes.WebTemplates.Commands;

public class ToggleWebTemplateAsFavoriteForAdminTests
{
    private Mock<IRaythaDbContext> _dbMock = null!;
    private WebTemplate _template = null!;
    private User _admin = null!;
    private User _otherAdmin = null!;

    [SetUp]
    public void Setup()
    {
        _admin = new User { Id = Guid.NewGuid() };
        _otherAdmin = new User { Id = Guid.NewGuid() };
        _template = new WebTemplate { Id = Guid.NewGuid(), ThemeId = Guid.NewGuid() };
        _template.UserFavorites.Add(_otherAdmin);

        _dbMock = new Mock<IRaythaDbContext>();
        _dbMock
            .Setup(x => x.WebTemplates)
            .Returns(new List<WebTemplate> { _template }.AsQueryable().BuildMockDbSet().Object);
        _dbMock
            .Setup(x => x.Users)
            .Returns(new List<User> { _admin, _otherAdmin }.AsQueryable().BuildMockDbSet().Object);
    }

    [Test]
    public async Task Handler_AddsTheAdmin_WhenSettingAFavorite()
    {
        var result = await Send(_admin.Id, setAsFavorite: true);

        result.Success.Should().BeTrue();
        result.Result.Guid.Should().Be(_template.Id);
        _template.UserFavorites.Should().BeEquivalentTo([_otherAdmin, _admin]);
        _dbMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handler_IsIdempotent_WhenSettingAnExistingFavorite()
    {
        await Send(_admin.Id, setAsFavorite: true);
        await Send(_admin.Id, setAsFavorite: true);

        _template.UserFavorites.Count(u => u.Id == _admin.Id).Should().Be(1);
    }

    [Test]
    public async Task Handler_RemovesOnlyTheAdmin_WhenClearingAFavorite()
    {
        _template.UserFavorites.Add(_admin);

        await Send(_admin.Id, setAsFavorite: false);

        _template.UserFavorites.Should().BeEquivalentTo([_otherAdmin]);
    }

    [Test]
    public async Task Handler_LeavesFavoritesUntouched_WhenClearingATemplateTheAdminNeverStarred()
    {
        await Send(_admin.Id, setAsFavorite: false);

        _template.UserFavorites.Should().BeEquivalentTo([_otherAdmin]);
    }

    [Test]
    public async Task Handler_ThrowsNotFound_WhenTheTemplateDoesNotExist()
    {
        var handler = new ToggleWebTemplateAsFavoriteForAdmin.Handler(_dbMock.Object);

        var act = async () =>
            await handler.Handle(
                new ToggleWebTemplateAsFavoriteForAdmin.Command
                {
                    Id = Guid.NewGuid(),
                    UserId = _admin.Id,
                    SetAsFavorite = true,
                },
                CancellationToken.None
            );

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handler_ThrowsNotFound_WhenTheAdminDoesNotExist()
    {
        var act = async () => await Send(Guid.NewGuid(), setAsFavorite: true);

        await act.Should().ThrowAsync<NotFoundException>();
        _dbMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private async Task<Raytha.Application.Common.Models.CommandResponseDto<ShortGuid>> Send(
        Guid userId,
        bool setAsFavorite
    )
    {
        var handler = new ToggleWebTemplateAsFavoriteForAdmin.Handler(_dbMock.Object);
        return await handler.Handle(
            new ToggleWebTemplateAsFavoriteForAdmin.Command
            {
                Id = _template.Id,
                UserId = userId,
                SetAsFavorite = setAsFavorite,
            },
            CancellationToken.None
        );
    }
}
