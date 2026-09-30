using System.Text.RegularExpressions;
using FluentAssertions;
using Raytha.Application.Themes.WidgetTemplates;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.UnitTests.SitePages.Widgets;

public class BuiltInWidgetFieldsTests
{
    private static IEnumerable<BuiltInWidgetType> WidgetTypes => BuiltInWidgetType.WidgetTypes;

    [TestCaseSource(nameof(WidgetTypes))]
    public void Field_keys_match_the_default_liquid(BuiltInWidgetType type)
    {
        var liquid = type.DefaultTemplateContent;
        var read = Regex.Matches(liquid, @"widget\.settings\.([a-zA-Z0-9_]+)").Select(m => m.Groups[1].Value).Distinct();
        var documented = Regex.Matches(liquid, @"^  - ([a-zA-Z0-9_]+):", RegexOptions.Multiline).Select(m => m.Groups[1].Value);

        type.Fields.Select(f => f.DeveloperName).Should().BeEquivalentTo(read);
        type.Fields.Select(f => f.DeveloperName).Should().BeEquivalentTo(documented);
    }

    [Test]
    public void Repeater_sub_field_keys_match_the_default_liquid()
    {
        var read = Regex.Matches(BuiltInWidgetType.FAQ.DefaultTemplateContent, @"\bitem\.([a-zA-Z0-9_]+)")
            .Select(m => m.Groups[1].Value)
            .Distinct();

        BuiltInWidgetType.FAQ.Fields.Single(f => f.FieldType == "repeater")
            .SubFields.Select(f => f.DeveloperName)
            .Should()
            .BeEquivalentTo(read);
    }

    [TestCaseSource(nameof(WidgetTypes))]
    public void Fields_pass_the_widget_template_field_rules(BuiltInWidgetType type)
    {
        WidgetFieldDefinitions.Validate(type.Fields).Should().BeEmpty();
    }
}
