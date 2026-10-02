using CSharpVitamins;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.SitePages.Commands;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.SitePages.Commands;

public class EditSitePageSettingsTests
{
    [TestCase("account/billing")]
    [TestCase("raytha/users")]
    [TestCase("healthz")]
    public void A_path_the_host_answers_first_is_refused(string routePath)
    {
        var page = new SitePage { Id = Guid.NewGuid(), RouteId = Guid.NewGuid() };
        var db = new Mock<IRaythaDbContext>();
        db.Setup(x => x.SitePages).Returns(new List<SitePage> { page }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Routes).Returns(new List<Route>().AsQueryable().BuildMockDbSet().Object);

        var result = new EditSitePageSettings.Validator(db.Object).Validate(
            new EditSitePageSettings.Command { Id = (ShortGuid)page.Id, RoutePath = routePath }
        );

        result.Errors.Should().ContainSingle(e => e.PropertyName == "RoutePath" && e.ErrorMessage.Contains("reserves"));
    }
}
