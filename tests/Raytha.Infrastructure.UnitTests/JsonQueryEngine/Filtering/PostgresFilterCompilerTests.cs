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
        var resolver = new ContentTypeFieldResolver(BuildContentType(), "title", new List<ContentTypeField>());
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
    [TestCase("2024-01-15")]
    [TestCase("01/15/2024")]
    public void A_date_comparison_binds_a_date_in_iso_or_invariant_format(string value)
    {
        var (sql, parameters) = Compile($"published_on gt '{value}'");

        sql.Should().Contain("TO_DATE");
        sql.Should().Contain("@p0");
        parameters.Single().Should().Be(new DateTime(2024, 1, 15));
    }

    [Test]
    public void Stored_dates_are_read_as_iso_and_anything_else_is_null()
    {
        var (sql, _) = Compile("published_on gt '2024-01-15'");

        sql.Should().Contain("'YYYY-MM-DD'");
        sql.Should().Contain("ELSE NULL END");
        sql.Should().NotContain("MM/DD/YYYY");
    }

    [Test]
    public void A_date_comparison_rejects_text_that_is_not_a_date()
    {
        var act = () => Compile("published_on gt 'yesterday'");

        act.Should().Throw<InvalidFilterException>().WithMessage("*expects a date*");
    }

    [Test]
    public void A_creation_time_comparison_binds_a_utc_timestamp()
    {
        var (_, parameters) = Compile("CreationTime ge '2020-01-02'");

        var value = parameters.Single().Should().BeOfType<DateTime>().Subject;
        value.Should().Be(new DateTime(2020, 1, 2));
        value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Test]
    [TestCase("IsPublished eq 'true'", true)]
    [TestCase("IsDraft eq 'false'", false)]
    public void A_publish_state_comparison_binds_a_boolean(string filter, bool expected)
    {
        var (_, parameters) = Compile(filter);

        parameters.Single().Should().Be(expected);
    }

    [Test]
    [TestCase("published_on gt 'soon'")]
    [TestCase("CreationTime ge 'yesterday'")]
    [TestCase("IsPublished eq 'maybe'")]
    public void A_value_that_does_not_fit_the_column_is_rejected(string filter)
    {
        FluentActions.Invoking(() => Compile(filter)).Should().Throw<InvalidFilterException>();
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
    [TestCase("contains(rank,'5')")]
    [TestCase("contains(published_on,'5')")]
    public void Matching_a_number_or_date_field_matches_its_stored_text(string filter)
    {
        var (sql, parameters) = Compile(filter);

        sql.Should().Contain("ILIKE @p0 ESCAPE '\\'");
        sql.Should().NotContain("TO_DATE");
        sql.Should().NotContain("::decimal");
        parameters.Single().Should().Be("%5%");
    }

    [Test]
    [TestCase("title ne 'a'")]
    [TestCase("not contains(title,'a')")]
    [TestCase("not startswith(title,'a')")]
    [TestCase("not endswith(title,'a')")]
    public void Negations_keep_rows_that_have_no_value(string filter)
    {
        var (sql, _) = Compile(filter);

        // A bare comparison is NULL for a missing value and NOT NULL is NULL, which drops the row.
        sql.Should().Contain("NOT COALESCE(");
    }

    [Test]
    public void Equality_and_range_comparisons_stay_false_for_a_missing_value()
    {
        Compile("title eq 'a'").Sql.Should().StartWith("COALESCE(").And.NotContain("NOT");
        Compile("title gt 'a'").Sql.Should().StartWith("COALESCE(").And.NotContain("NOT");
    }

    [Test]
    public void Equality_on_a_multi_select_field_matches_an_array_element()
    {
        var (sql, parameters) = Compile("tags eq 'jan_feb'");

        sql.Should().Contain("jsonb_array_elements_text");
        sql.Should().Contain("item = @p0");
        parameters.Single().Should().Be("jan_feb");
    }

    [Test]
    public void A_range_comparison_on_a_multi_select_field_is_rejected()
    {
        FluentActions
            .Invoking(() => Compile("tags gt 'x'"))
            .Should()
            .Throw<InvalidFilterException>();
    }

    [Test]
    public void Equality_on_a_relationship_field_with_an_id_matches_the_related_item()
    {
        var guid = Guid.NewGuid();
        var (sql, parameters) = CompileRelationship($"lead_guide eq '{(ShortGuid)guid}'");

        sql.Should().Contain("related_0.\"Id\"");
        parameters.Single().Should().Be(guid);
    }

    [Test]
    public void Equality_on_a_relationship_field_with_text_matches_the_related_primary_field()
    {
        var (sql, parameters) = CompileRelationship("lead_guide eq 'Ada'");

        sql.Should().Contain("->>'name'");
        sql.Should().NotContain("related_0.\"Id\"");
        parameters.Single().Should().Be("Ada");
    }

    private static (string Sql, List<object?> Parameters) CompileRelationship(string filter)
    {
        var relatedPrimaryId = Guid.NewGuid();
        var relatedType = new ContentType
        {
            Id = Guid.NewGuid(),
            PrimaryFieldId = relatedPrimaryId,
            ContentTypeFields = new List<ContentTypeField>
            {
                new()
                {
                    Id = relatedPrimaryId,
                    DeveloperName = "name",
                    FieldType = BaseFieldType.SingleLineText,
                },
            },
        };
        var relationship = new ContentTypeField
        {
            Id = Guid.NewGuid(),
            DeveloperName = "lead_guide",
            FieldType = BaseFieldType.OneToOneRelationship,
            RelatedContentTypeId = relatedType.Id,
            ContentType = relatedType,
        };
        var contentType = BuildContentType();
        contentType.ContentTypeFields.Add(relationship);

        var resolver = new ContentTypeFieldResolver(
            contentType,
            "title",
            new List<ContentTypeField> { relationship }
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
