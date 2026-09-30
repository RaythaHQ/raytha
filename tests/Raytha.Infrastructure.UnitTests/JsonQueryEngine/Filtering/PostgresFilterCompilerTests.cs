using CSharpVitamins;
using FluentAssertions;
using Raytha.Application.Common.Exceptions;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Infrastructure.JsonQueryEngine.Filtering;

namespace Raytha.Infrastructure.UnitTests.JsonQueryEngine.Filtering;

[TestFixture]
public class PostgresFilterCompilerTests
{
    private const string Payload = "zzzz' OR 1=1 OR 'x'='";

    private static string PayloadAsODataLiteral => Payload.Replace("'", "''");

    private static (string Sql, List<object?> Parameters) Compile(string filter)
    {
        var resolver = new ContentTypeFieldResolver(
            BuildContentType(),
            "title",
            new List<ContentTypeField>(),
            "MM/DD/YYYY"
        );
        var compiler = new PostgresFilterCompiler(resolver);
        var node = ODataFilterParser.Parse(filter);
        var parameters = new List<object?>();
        var sql = compiler.Compile(
            node,
            value =>
            {
                parameters.Add(value);
                return $"@p{parameters.Count - 1}";
            }
        );
        return (sql, parameters);
    }

    [Test]
    [TestCase("contains({0})")]
    [TestCase("startswith({0})")]
    [TestCase("endswith({0})")]
    public void Match_sql_is_identical_for_a_benign_value_and_an_injection_payload(string template)
    {
        var call = template.Replace("{0}", "title,'{0}'");
        var benign = Compile(call.Replace("{0}", "hello"));
        var malicious = Compile(call.Replace("{0}", PayloadAsODataLiteral));

        malicious.Sql.Should().Be(benign.Sql);
        malicious.Sql.Should().NotContain("1=1");
        malicious.Parameters.Should().ContainSingle();
    }

    [Test]
    [TestCase("title eq '{0}'")]
    [TestCase("title ne '{0}'")]
    [TestCase("contains(tags,'{0}')")]
    public void Predicate_sql_is_identical_for_a_benign_value_and_an_injection_payload(
        string template
    )
    {
        var benign = Compile(template.Replace("{0}", "hello"));
        var malicious = Compile(template.Replace("{0}", PayloadAsODataLiteral));

        malicious.Sql.Should().Be(benign.Sql);
        malicious.Sql.Should().NotContain("1=1");
        malicious.Parameters.Should().ContainSingle();
    }

    [Test]
    public void Match_uses_case_insensitive_like_with_an_escape_clause()
    {
        var (sql, _) = Compile("contains(title,'hello')");

        sql.Should().Contain("ILIKE @p0 ESCAPE '\\'");
    }

    [Test]
    public void Like_wildcards_in_a_value_are_escaped_so_they_match_literally()
    {
        var (_, parameters) = Compile("contains(title,'a%b_c')");

        parameters.Single().Should().Be("%a\\%b\\_c%");
    }

    [Test]
    public void A_number_value_is_bound_as_a_decimal()
    {
        var (sql, parameters) = Compile("rank eq '5'");

        sql.Should().Contain("::decimal(18, 2)");
        parameters.Single().Should().Be(5m);
    }

    [Test]
    public void An_id_value_is_bound_as_a_guid()
    {
        var guid = Guid.NewGuid();
        var shortGuid = (ShortGuid)guid;

        var (sql, parameters) = Compile($"Id eq 'guid_{shortGuid}'");

        sql.Should().Contain("source.\"Id\"");
        parameters.Single().Should().Be(guid);
    }

    [Test]
    public void A_date_comparison_parses_the_column_and_binds_the_value()
    {
        var (sql, parameters) = Compile("published_on gt '2024-01-01'");

        sql.Should().Contain("TO_DATE");
        sql.Should().Contain("@p0");
        parameters.Single().Should().Be("2024-01-01");
    }

    [Test]
    public void A_null_check_becomes_is_null_with_no_parameter()
    {
        var (sql, parameters) = Compile("published_on eq null");

        sql.Should().Contain("IS NULL");
        parameters.Should().BeEmpty();
    }

    [Test]
    [TestCase("nosuchfield eq 'x'")]
    [TestCase("contains(nosuchfield,'x')")]
    public void An_unknown_field_is_rejected(string filter)
    {
        FluentActions.Invoking(() => Compile(filter)).Should().Throw<InvalidFilterException>();
    }

    [Test]
    [TestCase("contains(title,")]
    [TestCase("title eq")]
    public void A_malformed_filter_is_rejected(string filter)
    {
        FluentActions.Invoking(() => Compile(filter)).Should().Throw<InvalidFilterException>();
    }

    [Test]
    public void Matching_a_number_field_is_rejected()
    {
        FluentActions
            .Invoking(() => Compile("contains(rank,'5')"))
            .Should()
            .Throw<InvalidFilterException>();
    }

    [Test]
    public void Comparing_a_multi_select_field_is_rejected()
    {
        FluentActions
            .Invoking(() => Compile("tags eq 'x'"))
            .Should()
            .Throw<InvalidFilterException>();
    }

    private static ContentType BuildContentType() =>
        new()
        {
            Id = Guid.NewGuid(),
            DeveloperName = "posts",
            ContentTypeFields = new List<ContentTypeField>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    DeveloperName = "title",
                    FieldType = BaseFieldType.SingleLineText,
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    DeveloperName = "rank",
                    FieldType = BaseFieldType.Number,
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    DeveloperName = "published_on",
                    FieldType = BaseFieldType.Date,
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    DeveloperName = "tags",
                    FieldType = BaseFieldType.MultipleSelect,
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    DeveloperName = "faq",
                    FieldType = BaseFieldType.Repeater,
                },
            },
        };
}
