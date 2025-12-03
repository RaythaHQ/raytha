using FluentAssertions;
using Raytha.Infrastructure.JsonQueryEngine;

namespace Raytha.Infrastructure.UnitTests.JsonQueryEngine;

[TestFixture]
public class RawSqlColumnTests
{
    #region Constants Tests

    [Test]
    public void ContentItemTableName_ShouldHaveCorrectValue()
    {
        RawSqlColumn.CONTENT_ITEM_TABLE_NAME.Should().Be("ContentItems");
    }

    [Test]
    public void UsersTableName_ShouldHaveCorrectValue()
    {
        RawSqlColumn.USERS_TABLE_NAME.Should().Be("Users");
    }

    [Test]
    public void RoutesTableName_ShouldHaveCorrectValue()
    {
        RawSqlColumn.ROUTES_TABLE_NAME.Should().Be("Routes");
    }

    [Test]
    public void SourceItemColumnName_ShouldHaveCorrectValue()
    {
        RawSqlColumn.SOURCE_ITEM_COLUMN_NAME.Should().Be("source");
    }

    [Test]
    public void RouteColumnName_ShouldHaveCorrectValue()
    {
        RawSqlColumn.ROUTE_COLUMN_NAME.Should().Be("route");
    }

    [Test]
    public void SourceCreatedByColumnName_ShouldHaveCorrectValue()
    {
        RawSqlColumn.SOURCE_CREATED_BY_COLUMN_NAME.Should().Be("created_by_source");
    }

    [Test]
    public void SourceModifiedByColumnName_ShouldHaveCorrectValue()
    {
        RawSqlColumn.SOURCE_MODIFIED_BY_COLUMN_NAME.Should().Be("modified_by_source");
    }

    [Test]
    public void RelatedCreatedByColumnName_ShouldHaveCorrectValue()
    {
        RawSqlColumn.RELATED_CREATED_BY_COLUMN_NAME.Should().Be("created_by_related");
    }

    [Test]
    public void RelatedModifiedByColumnName_ShouldHaveCorrectValue()
    {
        RawSqlColumn.RELATED_MODIFIED_BY_COLUMN_NAME.Should().Be("modified_by_related");
    }

    [Test]
    public void RelatedRouteColumnName_ShouldHaveCorrectValue()
    {
        RawSqlColumn.RELATED_ROUTE_COLUMN_NAME.Should().Be("route_related");
    }

    [Test]
    public void RelatedItemColumnName_ShouldHaveCorrectValue()
    {
        RawSqlColumn.RELATED_ITEM_COLUMN_NAME.Should().Be("related");
    }

    #endregion

    #region Static Column Instances Tests

    [Test]
    [TestCase("Id")]
    [TestCase("CreationTime")]
    [TestCase("CreatorUserId")]
    [TestCase("LastModifierUserId")]
    [TestCase("LastModificationTime")]
    [TestCase("IsPublished")]
    [TestCase("IsDraft")]
    [TestCase("RouteId")]
    [TestCase("Path")]
    [TestCase("_DraftContent")]
    [TestCase("_PublishedContent")]
    [TestCase("ContentTypeId")]
    [TestCase("FirstName")]
    [TestCase("LastName")]
    [TestCase("EmailAddress")]
    [TestCase("Label")]
    [TestCase("DeveloperName")]
    [Parallelizable(ParallelScope.All)]
    public void StaticColumn_ShouldHaveCorrectName(string expectedName)
    {
        // Arrange & Act
        var column = expectedName switch
        {
            "Id" => RawSqlColumn.Id,
            "CreationTime" => RawSqlColumn.CreationTime,
            "CreatorUserId" => RawSqlColumn.CreatorUserId,
            "LastModifierUserId" => RawSqlColumn.LastModifierUserId,
            "LastModificationTime" => RawSqlColumn.LastModificationTime,
            "IsPublished" => RawSqlColumn.IsPublished,
            "IsDraft" => RawSqlColumn.IsDraft,
            "RouteId" => RawSqlColumn.RouteId,
            "Path" => RawSqlColumn.Path,
            "_DraftContent" => RawSqlColumn.DraftContent,
            "_PublishedContent" => RawSqlColumn.PublishedContent,
            "ContentTypeId" => RawSqlColumn.ContentTypeId,
            "FirstName" => RawSqlColumn.FirstName,
            "LastName" => RawSqlColumn.LastName,
            "EmailAddress" => RawSqlColumn.EmailAddress,
            "Label" => RawSqlColumn.Label,
            "DeveloperName" => RawSqlColumn.DeveloperName,
            _ => throw new ArgumentException($"Unknown column: {expectedName}")
        };

        // Assert
        column.Name.Should().Be(expectedName);
    }

    #endregion

    #region NameAsColumn Tests

    [Test]
    public void NameAsColumn_ShouldPrependColumnPrefix()
    {
        // Arrange
        var column = RawSqlColumn.Id;

        // Act
        var result = column.NameAsColumn("source");

        // Assert
        result.Should().Be("source.Id");
    }

    [Test]
    public void NameAsColumn_WithDifferentPrefix_ShouldWorkCorrectly()
    {
        // Arrange
        var column = RawSqlColumn.CreationTime;

        // Act
        var result = column.NameAsColumn("c");

        // Assert
        result.Should().Be("c.CreationTime");
    }

    [Test]
    public void NameAsColumn_WithUnderscoreInColumnName_ShouldWorkCorrectly()
    {
        // Arrange
        var column = RawSqlColumn.PublishedContent;

        // Act
        var result = column.NameAsColumn("item");

        // Assert
        result.Should().Be("item._PublishedContent");
    }

    #endregion

    #region NameAsColumnLabel Tests

    [Test]
    public void NameAsColumnLabel_ShouldCreateUnderscoreSeparatedLabel()
    {
        // Arrange
        var column = RawSqlColumn.Id;

        // Act
        var result = column.NameAsColumnLabel("source");

        // Assert
        result.Should().Be("source_Id");
    }

    [Test]
    public void NameAsColumnLabel_WithDifferentPrefix_ShouldWorkCorrectly()
    {
        // Arrange
        var column = RawSqlColumn.EmailAddress;

        // Act
        var result = column.NameAsColumnLabel("user");

        // Assert
        result.Should().Be("user_EmailAddress");
    }

    #endregion

    #region NameAsFullColumnLabel Tests

    [Test]
    public void NameAsFullColumnLabel_ShouldCreateColumnAsLabelFormat()
    {
        // Arrange
        var column = RawSqlColumn.Id;

        // Act
        var result = column.NameAsFullColumnLabel("source");

        // Assert
        result.Should().Be("source.Id as source_Id");
    }

    [Test]
    public void NameAsFullColumnLabel_WithComplexColumn_ShouldWorkCorrectly()
    {
        // Arrange
        var column = RawSqlColumn.LastModificationTime;

        // Act
        var result = column.NameAsFullColumnLabel("item");

        // Assert
        result.Should().Be("item.LastModificationTime as item_LastModificationTime");
    }

    #endregion

    #region NameAsFullColumnLabelForEnumerable Tests

    [Test]
    public void NameAsFullColumnLabelForEnumerable_ShouldYieldAllColumns()
    {
        // Arrange
        var columns = new[] { RawSqlColumn.Id, RawSqlColumn.CreationTime, RawSqlColumn.IsPublished };

        // Act
        var result = RawSqlColumn.NameAsFullColumnLabelForEnumerable(columns, "src").ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().Be("src.Id as src_Id");
        result[1].Should().Be("src.CreationTime as src_CreationTime");
        result[2].Should().Be("src.IsPublished as src_IsPublished");
    }

    [Test]
    public void NameAsFullColumnLabelForEnumerable_WithEmptyCollection_ShouldReturnEmpty()
    {
        // Arrange
        var columns = Array.Empty<RawSqlColumn>();

        // Act
        var result = RawSqlColumn.NameAsFullColumnLabelForEnumerable(columns, "src").ToList();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region ContentItemColumns Tests

    [Test]
    public void ContentItemColumns_ShouldReturnAllContentItemColumns()
    {
        // Act
        var columns = RawSqlColumn.ContentItemColumns().ToList();

        // Assert
        columns.Should().HaveCount(10);
        columns.Should().Contain(RawSqlColumn.Id);
        columns.Should().Contain(RawSqlColumn.CreationTime);
        columns.Should().Contain(RawSqlColumn.CreatorUserId);
        columns.Should().Contain(RawSqlColumn.LastModifierUserId);
        columns.Should().Contain(RawSqlColumn.LastModificationTime);
        columns.Should().Contain(RawSqlColumn.IsPublished);
        columns.Should().Contain(RawSqlColumn.IsDraft);
        columns.Should().Contain(RawSqlColumn.DraftContent);
        columns.Should().Contain(RawSqlColumn.PublishedContent);
        columns.Should().Contain(RawSqlColumn.ContentTypeId);
    }

    [Test]
    public void ContentItemColumns_ShouldReturnColumnsInCorrectOrder()
    {
        // Act
        var columns = RawSqlColumn.ContentItemColumns().ToList();

        // Assert
        columns[0].Should().Be(RawSqlColumn.Id);
        columns[1].Should().Be(RawSqlColumn.CreationTime);
        columns[2].Should().Be(RawSqlColumn.CreatorUserId);
        columns[3].Should().Be(RawSqlColumn.LastModifierUserId);
        columns[4].Should().Be(RawSqlColumn.LastModificationTime);
        columns[5].Should().Be(RawSqlColumn.IsPublished);
        columns[6].Should().Be(RawSqlColumn.IsDraft);
        columns[7].Should().Be(RawSqlColumn.DraftContent);
        columns[8].Should().Be(RawSqlColumn.PublishedContent);
        columns[9].Should().Be(RawSqlColumn.ContentTypeId);
    }

    #endregion

    #region UserColumns Tests

    [Test]
    public void UserColumns_ShouldReturnAllUserColumns()
    {
        // Act
        var columns = RawSqlColumn.UserColumns().ToList();

        // Assert
        columns.Should().HaveCount(4);
        columns.Should().Contain(RawSqlColumn.Id);
        columns.Should().Contain(RawSqlColumn.FirstName);
        columns.Should().Contain(RawSqlColumn.LastName);
        columns.Should().Contain(RawSqlColumn.EmailAddress);
    }

    [Test]
    public void UserColumns_ShouldReturnColumnsInCorrectOrder()
    {
        // Act
        var columns = RawSqlColumn.UserColumns().ToList();

        // Assert
        columns[0].Should().Be(RawSqlColumn.Id);
        columns[1].Should().Be(RawSqlColumn.FirstName);
        columns[2].Should().Be(RawSqlColumn.LastName);
        columns[3].Should().Be(RawSqlColumn.EmailAddress);
    }

    #endregion

    #region TemplateColumns Tests

    [Test]
    public void TemplateColumns_ShouldReturnAllTemplateColumns()
    {
        // Act
        var columns = RawSqlColumn.TemplateColumns().ToList();

        // Assert
        columns.Should().HaveCount(3);
        columns.Should().Contain(RawSqlColumn.Id);
        columns.Should().Contain(RawSqlColumn.Label);
        columns.Should().Contain(RawSqlColumn.DeveloperName);
    }

    [Test]
    public void TemplateColumns_ShouldReturnColumnsInCorrectOrder()
    {
        // Act
        var columns = RawSqlColumn.TemplateColumns().ToList();

        // Assert
        columns[0].Should().Be(RawSqlColumn.Id);
        columns[1].Should().Be(RawSqlColumn.Label);
        columns[2].Should().Be(RawSqlColumn.DeveloperName);
    }

    #endregion

    #region RouteColumns Tests

    [Test]
    public void RouteColumns_ShouldReturnAllRouteColumns()
    {
        // Act
        var columns = RawSqlColumn.RouteColumns().ToList();

        // Assert
        columns.Should().HaveCount(2);
        columns.Should().Contain(RawSqlColumn.Id);
        columns.Should().Contain(RawSqlColumn.Path);
    }

    [Test]
    public void RouteColumns_ShouldReturnColumnsInCorrectOrder()
    {
        // Act
        var columns = RawSqlColumn.RouteColumns().ToList();

        // Assert
        columns[0].Should().Be(RawSqlColumn.Id);
        columns[1].Should().Be(RawSqlColumn.Path);
    }

    #endregion

    #region Integration Tests

    [Test]
    public void ContentItemColumns_WithFullColumnLabel_ShouldGenerateValidSqlSelectList()
    {
        // Arrange
        var columns = RawSqlColumn.ContentItemColumns();

        // Act
        var selectColumns = RawSqlColumn.NameAsFullColumnLabelForEnumerable(columns, "c").ToList();
        var selectClause = string.Join(", ", selectColumns);

        // Assert
        selectClause.Should().Contain("c.Id as c_Id");
        selectClause.Should().Contain("c.IsPublished as c_IsPublished");
        selectClause.Should().Contain("c._PublishedContent as c__PublishedContent");
    }

    #endregion
}

