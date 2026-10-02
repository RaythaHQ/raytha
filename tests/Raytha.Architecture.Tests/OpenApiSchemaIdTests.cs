using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FluentAssertions;
using Microsoft.AspNetCore.OpenApi;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.Users.Commands;
using Raytha.Web.Middlewares;

namespace Raytha.Architecture.Tests;

[TestFixture]
public class OpenApiSchemaIdTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    [Test]
    public void A_content_item_body_is_named_after_its_command()
    {
        Id(typeof(CreateContentItem.Command)).Should().Be("ContentItems.Commands.CreateContentItemCommand");
        Id(typeof(CreateUser.Command)).Should().Be("Users.Commands.CreateUserCommand");
    }

    [Test]
    public void Commands_with_the_same_short_name_in_different_features_stay_distinct()
    {
        Id(typeof(Raytha.Application.Admins.Commands.ResetPassword.Command))
            .Should()
            .NotBe(Id(typeof(ResetPassword.Command)));
        Id(typeof(Raytha.Application.Admins.Commands.SetIsActive.Command))
            .Should()
            .NotBe(Id(typeof(SetIsActive.Command)));
    }

    [Test]
    public void A_top_level_dto_keeps_the_framework_schema_id()
    {
        var info = Options.GetTypeInfo(typeof(ContentItemDto));

        OpenApiSchemaIds.Create(info).Should().Be(OpenApiOptions.CreateDefaultSchemaReferenceId(info));
    }

    [Test]
    public void Every_nested_command_and_query_gets_its_own_schema_id()
    {
        var types = typeof(CreateUser).Assembly
            .GetTypes()
            .Where(type => type is { IsNested: true, IsGenericTypeDefinition: false } && type.Name is "Command" or "Query")
            .ToArray();

        var ids = types.Select(Id).ToArray();

        ids.Should().OnlyHaveUniqueItems();
        ids.Should().NotContain("Command");
        ids.Should().NotContain("Query");
        ids.Should().HaveCountGreaterThan(10);
    }

    private static string Id(Type type)
    {
        var id = OpenApiSchemaIds.Create(Options.GetTypeInfo(type));
        id.Should().NotBeNull();
        return id!;
    }
}
