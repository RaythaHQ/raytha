using FluentAssertions;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentTypes;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Application.UnitTests.Common.Utils;

public class TemplateInsertVariablesTests
{
    [Test]
    public void ForEmail_groups_organization_and_email_data()
    {
        var groups = TemplateInsertVariables.ForEmail(
            BuiltInEmailTemplate.AdminWelcomeEmail.DeveloperName
        );

        groups.Select(g => g.Category).Should().Equal("Organization", "Email data");
        Paths(groups, "Organization").Should().Contain("CurrentOrganization.OrganizationName");
        Paths(groups, "Email data").Should().Contain(["Target.EmailAddress", "Target.NewPassword"]);
        AllPaths(groups).Should().NotContain("Menu.Label");
    }

    [Test]
    public void ForEmail_skips_unknown_developer_names()
    {
        var groups = TemplateInsertVariables.ForEmail("not_a_real_template");

        groups.Select(g => g.Category).Should().Equal("Organization");
        AllPaths(groups).Should().NotContain(p => p.StartsWith("Target."));
    }

    [Test]
    public void ForWeb_built_in_login_groups_shared_and_page_data()
    {
        var groups = TemplateInsertVariables.ForWeb(
            BuiltInWebTemplate.LoginWithEmailAndPasswordPage.DeveloperName,
            isBuiltInTemplate: true,
            [Posts()]
        );

        groups
            .Select(g => g.Category)
            .Should()
            .Equal("Request", "Organization", "Current user", "Menu", "Menu item", "Page data");
        Paths(groups, "Request").Should().Contain("PathBase");
        Paths(groups, "Current user").Should().Contain("CurrentUser.EmailAddress");
        Paths(groups, "Menu item").Should().Contain("MenuItem.Url");
        Paths(groups, "Page data").Should().Contain("Target.ReturnUrl");
        AllPaths(groups).Should().NotContain("Target.PrimaryField");
        AllPaths(groups).Should().NotContain(p => p.StartsWith("Target.PublishedContent."));
    }

    [Test]
    public void ForWeb_layout_or_custom_groups_content_and_content_type_fields()
    {
        var layout = TemplateInsertVariables.ForWeb(
            BuiltInWebTemplate._Layout.DeveloperName,
            isBuiltInTemplate: true,
            [Posts()]
        );
        var custom = TemplateInsertVariables.ForWeb("my_page", isBuiltInTemplate: false, [Posts()]);

        foreach (var groups in new[] { layout, custom })
        {
            Paths(groups, "Content list").Should().Contain(["Target.Items", "Target.RoutePath"]);
            Paths(groups, "Content item").Should().Contain(["Target.PrimaryField", "Target.RoutePath"]);
            Paths(groups, "Post fields")
                .Should()
                .Equal(
                    "Target.PublishedContent.title.Text",
                    "Target.PublishedContent.title.Value",
                    "Target.PublishedContent.author"
                );
            Paths(groups, "Menu").Should().Contain("Menu.Label");
        }
    }

    [Test]
    public void Variables_carry_descriptions_and_examples_where_known()
    {
        var groups = TemplateInsertVariables.ForWeb("my_page", isBuiltInTemplate: false, []);
        var items = groups.SelectMany(g => g.Variables).First(v => v.Path == "Target.Items");
        var pathBase = groups.SelectMany(g => g.Variables).First(v => v.Path == "PathBase");

        items.Description.Should().NotBeNullOrWhiteSpace();
        items.Example.Should().Contain("{% for item in Target.Items %}");
        pathBase.Description.Should().NotBeNullOrWhiteSpace();
        pathBase.Example.Should().BeNull();
    }

    [Test]
    public void Repeater_fields_offer_a_loop_over_their_rows()
    {
        var faqs = Posts() with
        {
            ContentTypeFields =
            [
                new ContentTypeFieldDto
                {
                    DeveloperName = "faq",
                    Label = "FAQ",
                    FieldType = BaseFieldType.Repeater,
                    SubFields =
                    [
                        new FieldDefinition { DeveloperName = "question", FieldType = "single_line_text" },
                        new FieldDefinition { DeveloperName = "answer", FieldType = "long_text" },
                    ],
                },
            ],
        };

        var groups = TemplateInsertVariables.ForWeb("my_page", isBuiltInTemplate: false, [faqs]);

        var loop = groups.Single(g => g.Category == "Post fields").Variables.Single();
        loop.Path.Should().Be("Target.PublishedContent.faq.Value");
        loop.Description.Should().Contain("rows");
        loop.Example.Should()
            .Be(
                "{% for row in Target.PublishedContent.faq.Value %}\n  {{ row.question }}\n  {{ row.answer }}\n{% endfor %}"
            );
    }

    [Test]
    public void Duplicate_content_type_labels_get_distinct_categories()
    {
        var groups = TemplateInsertVariables.ForWeb(
            "my_page",
            isBuiltInTemplate: false,
            [Posts(), Posts() with { DeveloperName = "news" }]
        );

        groups.Select(g => g.Category).Should().Contain(["Post fields", "Post (news) fields"]);
    }

    private static ContentTypeDto Posts() =>
        new()
        {
            LabelSingular = "Post",
            LabelPlural = "Posts",
            DeveloperName = "posts",
            ContentTypeFields =
            [
                new ContentTypeFieldDto
                {
                    DeveloperName = "title",
                    Label = "Title",
                    FieldType = BaseFieldType.SingleLineText,
                },
                new ContentTypeFieldDto
                {
                    DeveloperName = "author",
                    Label = "Author",
                    FieldType = BaseFieldType.OneToOneRelationship,
                },
            ],
        };

    private static IEnumerable<string> Paths(
        IReadOnlyList<TemplateVariableGroupDto> groups,
        string category
    ) => groups.Single(g => g.Category == category).Variables.Select(v => v.Path);

    private static IEnumerable<string> AllPaths(IReadOnlyList<TemplateVariableGroupDto> groups) =>
        groups.SelectMany(g => g.Variables).Select(v => v.Path);
}
