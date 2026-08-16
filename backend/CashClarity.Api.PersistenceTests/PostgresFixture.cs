using CashClarity.Api.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace CashClarity.Api.PersistenceTests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("cashclarity_tests")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await postgres.DisposeAsync();

    public FinanceDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new FinanceDbContext(options);
    }
}

[CollectionDefinition(Name)]
public sealed class PersistenceCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
