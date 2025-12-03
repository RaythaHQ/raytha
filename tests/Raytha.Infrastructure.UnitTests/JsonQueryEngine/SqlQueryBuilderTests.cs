using FluentAssertions;
using Raytha.Infrastructure.JsonQueryEngine;

namespace Raytha.Infrastructure.UnitTests.JsonQueryEngine;

[TestFixture]
public class SqlQueryBuilderTests
{
    #region Select Tests

    [Test]
    public void Build_WithSingleSelectColumn_ShouldProduceCorrectSql()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder.Select("Id").From("Users").Build();

        // Assert
        sql.Should().Be("SELECT Id FROM Users");
    }

    [Test]
    public void Build_WithMultipleSelectColumns_ShouldProduceCorrectSql()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder.Select("Id", "Name", "Email").From("Users").Build();

        // Assert
        sql.Should().Be("SELECT Id, Name, Email FROM Users");
    }

    [Test]
    public void Build_WithNoSelectColumns_ShouldProduceSelectStar()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder.Select().From("Users").Build();

        // Assert
        sql.Should().Be("SELECT * FROM Users");
    }

    [Test]
    public void Build_WithMultipleSelectCalls_ShouldCombineColumns()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder.Select("Id").Select("Name", "Email").From("Users").Build();

        // Assert
        sql.Should().Be("SELECT Id, Name, Email FROM Users");
    }

    #endregion

    #region From Tests

    [Test]
    public void Build_WithFromClause_ShouldProduceCorrectSql()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder.Select("*").From("ContentItems").Build();

        // Assert
        sql.Should().Be("SELECT * FROM ContentItems");
    }

    [Test]
    public void Build_WithTableAlias_ShouldProduceCorrectSql()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder.Select("c.Id").From("ContentItems c").Build();

        // Assert
        sql.Should().Be("SELECT c.Id FROM ContentItems c");
    }

    #endregion

    #region Where Tests

    [Test]
    public void Build_WithSingleAndWhereCondition_ShouldProduceCorrectSql()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder.Select("*").From("Users").AndWhere("IsActive = 1").Build();

        // Assert
        sql.Should().Be("SELECT * FROM Users WHERE IsActive = 1");
    }

    [Test]
    public void Build_WithMultipleAndWhereConditions_ShouldJoinWithAnd()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder
            .Select("*")
            .From("Users")
            .AndWhere("IsActive = 1")
            .AndWhere("Role = 'Admin'")
            .Build();

        // Assert - note: implementation adds space before AND
        sql.Should().Be("SELECT * FROM Users WHERE IsActive = 1  AND Role = 'Admin'");
    }

    [Test]
    public void Build_WithOrWhereCondition_ShouldJoinWithOr()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder
            .Select("*")
            .From("Users")
            .AndWhere("IsActive = 1")
            .OrWhere("IsSuperUser = 1")
            .Build();

        // Assert - note: implementation adds space before OR
        sql.Should().Be("SELECT * FROM Users WHERE IsActive = 1  OR IsSuperUser = 1");
    }

    [Test]
    public void Build_WithEmptyWhereCondition_ShouldIgnoreCondition()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder.Select("*").From("Users").AndWhere("").AndWhere("IsActive = 1").Build();

        // Assert
        sql.Should().Be("SELECT * FROM Users WHERE IsActive = 1");
    }

    [Test]
    public void Build_WithWhitespaceWhereCondition_ShouldIgnoreCondition()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder.Select("*").From("Users").AndWhere("   ").AndWhere("IsActive = 1").Build();

        // Assert
        sql.Should().Be("SELECT * FROM Users WHERE IsActive = 1");
    }

    [Test]
    public void Build_WithNullWhereCondition_ShouldIgnoreCondition()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder.Select("*").From("Users").AndWhere(null!).AndWhere("IsActive = 1").Build();

        // Assert
        sql.Should().Be("SELECT * FROM Users WHERE IsActive = 1");
    }

    [Test]
    public void Build_WithOnlyOrWhereCondition_ShouldNotStartWithOr()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder.Select("*").From("Users").OrWhere("IsActive = 1").Build();

        // Assert
        sql.Should().Be("SELECT * FROM Users WHERE IsActive = 1");
    }

    [Test]
    public void Build_WithMixedAndOrConditions_ShouldMaintainCorrectOrder()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder
            .Select("*")
            .From("Users")
            .AndWhere("IsActive = 1")
            .OrWhere("IsAdmin = 1")
            .AndWhere("Email IS NOT NULL")
            .Build();

        // Assert - note: implementation adds space before AND/OR
        sql.Should().Be("SELECT * FROM Users WHERE IsActive = 1  OR IsAdmin = 1  AND Email IS NOT NULL");
    }

    #endregion

    #region OrderBy Tests

    [Test]
    public void Build_WithSingleOrderBy_ShouldProduceCorrectSql()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder.Select("*").From("Users").OrderBy("Name ASC").Build();

        // Assert
        sql.Should().Be("SELECT * FROM Users ORDER BY Name ASC");
    }

    [Test]
    public void Build_WithMultipleOrderBy_ShouldCombineColumns()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder
            .Select("*")
            .From("Users")
            .OrderBy("LastName ASC")
            .OrderBy("FirstName ASC")
            .Build();

        // Assert
        sql.Should().Be("SELECT * FROM Users ORDER BY LastName ASC, FirstName ASC");
    }

    [Test]
    public void Build_WithEmptyOrderBy_ShouldIgnoreOrderBy()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder.Select("*").From("Users").OrderBy("").Build();

        // Assert
        sql.Should().Be("SELECT * FROM Users");
    }

    [Test]
    public void Build_WithWhitespaceOrderBy_ShouldIgnoreOrderBy()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder.Select("*").From("Users").OrderBy("   ").Build();

        // Assert
        sql.Should().Be("SELECT * FROM Users");
    }

    #endregion

    #region Join Tests

    [Test]
    public void Build_WithInnerJoin_ShouldProduceCorrectSql()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder
            .Select("u.Id", "r.Name")
            .From("Users u")
            .Join("Roles r", "u.RoleId = r.Id")
            .Build();

        // Assert
        sql.Should().Be("SELECT u.Id, r.Name FROM Users u INNER JOIN Roles r ON u.RoleId = r.Id");
    }

    [Test]
    public void Build_WithLeftJoin_ShouldProduceCorrectSql()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder
            .Select("u.Id", "p.Title")
            .From("Users u")
            .Join("Posts p", "u.Id = p.UserId", "LEFT")
            .Build();

        // Assert
        sql.Should().Be("SELECT u.Id, p.Title FROM Users u LEFT JOIN Posts p ON u.Id = p.UserId");
    }

    [Test]
    public void Build_WithRightJoin_ShouldProduceCorrectSql()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder
            .Select("*")
            .From("Orders o")
            .Join("Customers c", "o.CustomerId = c.Id", "RIGHT")
            .Build();

        // Assert
        sql.Should().Be("SELECT * FROM Orders o RIGHT JOIN Customers c ON o.CustomerId = c.Id");
    }

    [Test]
    public void Build_WithMultipleJoins_ShouldProduceCorrectSql()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder
            .Select("u.Name", "r.Name", "d.Name")
            .From("Users u")
            .Join("Roles r", "u.RoleId = r.Id")
            .Join("Departments d", "u.DeptId = d.Id", "LEFT")
            .Build();

        // Assert
        sql.Should().Be("SELECT u.Name, r.Name, d.Name FROM Users u INNER JOIN Roles r ON u.RoleId = r.Id LEFT JOIN Departments d ON u.DeptId = d.Id");
    }

    #endregion

    #region Complex Query Tests

    [Test]
    public void Build_WithAllClauses_ShouldProduceCorrectSqlInOrder()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder
            .Select("c.Id", "c.Title", "u.Name AS Author")
            .From("ContentItems c")
            .Join("Users u", "c.CreatorUserId = u.Id")
            .AndWhere("c.IsPublished = 1")
            .AndWhere("c.ContentTypeId = 'abc123'")
            .OrderBy("c.CreationTime DESC")
            .Build();

        // Assert - note: implementation adds space before AND
        sql.Should().Be(
            "SELECT c.Id, c.Title, u.Name AS Author " +
            "FROM ContentItems c " +
            "INNER JOIN Users u ON c.CreatorUserId = u.Id " +
            "WHERE c.IsPublished = 1  AND c.ContentTypeId = 'abc123' " +
            "ORDER BY c.CreationTime DESC"
        );
    }

    [Test]
    public void Build_WithNoFromClause_ShouldOmitFrom()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder.Select("1").Build();

        // Assert
        sql.Should().Be("SELECT 1");
    }

    [Test]
    public void Build_EmptyBuilder_ShouldReturnEmptyString()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder.Build();

        // Assert
        sql.Should().Be("");
    }

    [Test]
    public void Build_ShouldTrimTrailingWhitespace()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var sql = builder.Select("*").From("Users").Build();

        // Assert
        sql.Should().NotEndWith(" ");
        sql.Should().Be("SELECT * FROM Users");
    }

    #endregion

    #region Method Chaining Tests

    [Test]
    public void MethodChaining_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var builder = new SqlQueryBuilder();

        // Act
        var selectResult = builder.Select("Id");
        var fromResult = selectResult.From("Users");
        var whereResult = fromResult.AndWhere("IsActive = 1");
        var orderResult = whereResult.OrderBy("Name");
        var joinResult = orderResult.Join("Roles", "Users.RoleId = Roles.Id");

        // Assert
        selectResult.Should().BeSameAs(builder);
        fromResult.Should().BeSameAs(builder);
        whereResult.Should().BeSameAs(builder);
        orderResult.Should().BeSameAs(builder);
        joinResult.Should().BeSameAs(builder);
    }

    #endregion
}

