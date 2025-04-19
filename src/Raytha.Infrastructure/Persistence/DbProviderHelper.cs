namespace Raytha.Infrastructure.Persistence;

interface IPostgresConfiguration { }
interface ISqlServerConfiguration { }

public enum DatabaseProviderType
{
    Postgres,
    SqlServer
}

public static class DbProviderHelper
{
    public static DatabaseProviderType GetDatabaseProviderTypeFromConnectionString(string connectionString)
    {
        bool isPostgres = connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase);
        bool isSqlServer = connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
                           connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase);

        if (isPostgres)
        {
            return DatabaseProviderType.Postgres;
        }
        else if (isSqlServer)
        {
            return DatabaseProviderType.SqlServer;
        }
        else
        {
            throw new NotSupportedException("Database provider not supported");
        }
    }
}
