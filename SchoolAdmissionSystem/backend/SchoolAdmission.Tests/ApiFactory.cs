using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Xunit;

namespace SchoolAdmission.Tests;

/// <summary>Starts the real API against a separate test database so demo data is never touched.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION")
        ?? "Host=localhost;Port=5433;Database=school_admission_test;Username=postgres;Password=postgres";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        EnsureDatabaseExists();
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("HOLD_DURATION_SECONDS", "600");
    }

    private static void EnsureDatabaseExists()
    {
        var csb = new NpgsqlConnectionStringBuilder(ConnectionString);
        var databaseName = csb.Database!;
        csb.Database = "postgres";

        using var conn = new NpgsqlConnection(csb.ConnectionString);
        conn.Open();

        using var check = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", conn);
        check.Parameters.AddWithValue("name", databaseName);
        if (check.ExecuteScalar() is null)
        {
            using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", conn);
            create.ExecuteNonQuery();
        }
    }
}

// One shared factory for all tests, so the database is prepared once and tests run one at a time.
[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
}
