using CSharpVitamins;
using Fluid;
using Fluid.Values;
using FluentAssertions;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Raytha.Application.Common.Models;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.Routes;
using Raytha.Application.Routes.Queries;
using Raytha.Application.Views;
using Raytha.Application.Views.Queries;
using Raytha.Web.Areas.Public.Controllers;
using Raytha.Web.Services;

namespace Raytha.Architecture.Tests;

/// <summary>
/// Visitors must not see drafts or unpublished items in a list when the item's own page 404s.
/// </summary>
public class PublicContentListTests
{
    [Test]
    public async Task A_public_view_page_lists_published_items_only()
    {
        ShortGuid viewId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<GetRouteByPath.Query>(), It.IsAny<CancellationToken>()))
            .Returns(
                new ValueTask<IQueryResponseDto<RouteDto>>(
                    new QueryResponseDto<RouteDto>(new RouteDto { ViewId = viewId, Path = "posts" })
                )
            );
        sender
            .Setup(x => x.Send(It.IsAny<GetViewById.Query>(), It.IsAny<CancellationToken>()))
            .Returns(
                new ValueTask<IQueryResponseDto<ViewDto>>(
                    new QueryResponseDto<ViewDto>(
                        new ViewDto { Id = viewId, IsPublished = true, DefaultNumberOfItemsPerPage = 25 }
                    )
                )
            );
        var sent = CaptureListQuery(sender);
        var controller = new MainController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = new ServiceCollection()
                        .AddSingleton(sender.Object)
                        .BuildServiceProvider(),
                },
            },
        };

        var act = () => controller.Index("posts");

        await act.Should().ThrowAsync<ListQuerySent>();
        sent().PublishedOnly.Should().BeTrue();
    }

    [Test]
    public async Task Liquid_get_content_items_lists_published_items_only()
    {
        var mediator = new Mock<IMediator>();
        var sent = CaptureListQuery(mediator);
        var engine = new RenderEngine(mediator.Object, null!, null!, null!, null!, null!);
        var arguments = new FunctionArguments().Add("ContentType", new StringValue("posts"));

        var act = async () => await engine.GetContentItems().InvokeAsync(arguments, new TemplateContext());

        await act.Should().ThrowAsync<ListQuerySent>();
        sent().PublishedOnly.Should().BeTrue();
    }

    private static Func<GetContentItems.Query> CaptureListQuery<TSender>(Mock<TSender> sender)
        where TSender : class, ISender
    {
        GetContentItems.Query? sent = null;
        sender
            .Setup(x => x.Send(It.IsAny<GetContentItems.Query>(), It.IsAny<CancellationToken>()))
            .Callback(
                (IRequest<IQueryResponseDto<ListResultDto<ContentItemDto>>> query, CancellationToken _) =>
                    sent = (GetContentItems.Query)query
            )
            .Throws(new ListQuerySent());
        return () => sent ?? throw new AssertionException("GetContentItems was not sent");
    }

    private sealed class ListQuerySent : Exception;
}
