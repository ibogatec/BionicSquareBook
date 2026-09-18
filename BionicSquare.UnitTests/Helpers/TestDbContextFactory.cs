using System.Data.Common;
using BionicSquare.DataAccess;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BionicSquare.UnitTests.Helpers;

public class ZeroSaveApplicationDbContext : ApplicationDbContext
{
    public ZeroSaveApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
}

public static class TestDbContextFactory
{
    public static ApplicationDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static (ApplicationDbContext Context, DbConnection Connection) CreateSqlite()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return (context, connection);
    }

    public static ApplicationDbContext CreateWithZeroSave()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ZeroSaveApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
