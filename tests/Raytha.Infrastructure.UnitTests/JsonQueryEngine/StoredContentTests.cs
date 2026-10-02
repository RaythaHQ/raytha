using System.Data;
using FluentAssertions;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Domain.ValueObjects.FieldValues;
using Raytha.Infrastructure.JsonQueryEngine;

namespace Raytha.Infrastructure.UnitTests.JsonQueryEngine;

public class StoredContentTests
{
    [Test]
    public void A_stored_date_that_does_not_parse_does_not_fail_the_item()
    {
        var contentType = new ContentType
        {
            ContentTypeFields =
            [
                new ContentTypeField { DeveloperName = "title", FieldType = BaseFieldType.SingleLineText },
                new ContentTypeField { DeveloperName = "starts_on", FieldType = BaseFieldType.Date },
            ],
        };

        var content = (Dictionary<string, dynamic>)new Probe().Read(
            """{"title":"UK notice","starts_on":"13/13/2026"}""",
            contentType
        );

        ((string)content["title"].Text).Should().Be("UK notice");
        var startsOn = (DateTimeFieldValue)content["starts_on"];
        startsOn.HasValue.Should().BeFalse();
        startsOn.Text.Should().Be("13/13/2026");
    }

    private sealed class Probe : AbstractRaythaDbJsonQueryEngine
    {
        public dynamic Read(string json, ContentType contentType) =>
            ConvertJsonContentToDynamic(json, contentType);

        public override ContentItem FirstOrDefault(Guid entityId) => throw new NotSupportedException();

        public override IEnumerable<ContentItem> QueryContentItems(
            Guid contentTypeId,
            string[] searchOnColumns,
            string search,
            string[] filters,
            int pageSize,
            int pageNumber,
            string orderBy,
            IDbTransaction transaction = null!
        ) => throw new NotSupportedException();

        public override IEnumerable<ContentItem> QueryAllContentItemsAsTransaction(
            Guid contentTypeId,
            string[] searchOnColumns,
            string search,
            string[] filters,
            string orderBy
        ) => throw new NotSupportedException();

        public override int CountContentItems(
            Guid contentTypeId,
            string[] searchOnColumns,
            string search,
            string[] filters,
            IDbTransaction transaction = null!
        ) => throw new NotSupportedException();

        protected override SqlQueryBuilder PrepareContentItemsDataSelect(SqlQueryBuilder sqlBuilder) =>
            throw new NotSupportedException();

        protected override SqlQueryBuilder PrepareFrom(SqlQueryBuilder sqlBuilder) =>
            throw new NotSupportedException();

        protected override SqlQueryBuilder PrepareSearch(
            SqlQueryBuilder sqlBuilder,
            string search,
            string[] searchOnColumns = null!
        ) => throw new NotSupportedException();

        protected override SqlQueryBuilder PrepareOrderBy(SqlQueryBuilder sqlBuilder, string orderBy) =>
            throw new NotSupportedException();
    }
}
