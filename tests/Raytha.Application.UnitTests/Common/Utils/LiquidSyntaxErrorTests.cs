using FluentAssertions;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.UnitTests.Common.Utils;

public class LiquidSyntaxErrorTests
{
    [Test]
    public void A_broken_tag_keeps_the_parser_line_and_column()
    {
        var error = LiquidSyntaxError.FromParserMessage("Invalid 'if' tag at (1:6)");

        error.Should().NotBeNull();
        error!.Line.Should().Be(1);
        error.Column.Should().Be(6);
        error.Describe().Should().StartWith("Line 1, column 6:");
    }

    [Test]
    public void A_message_without_a_position_leaves_line_and_column_empty()
    {
        var error = LiquidSyntaxError.FromParserMessage("The template could not be parsed.");

        error!.Line.Should().BeNull();
        error.Column.Should().BeNull();
        error.Describe().Should().Be("The template could not be parsed.");
    }
}
