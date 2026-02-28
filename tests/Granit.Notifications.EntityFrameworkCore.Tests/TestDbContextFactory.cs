using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Granit.Notifications.EntityFrameworkCore.Tests;

/// <summary>
/// Creates <see cref="NotificationDbContext"/> instances backed by SQLite in-memory
/// for fast, isolated integration tests that support all relational operations
/// (including <c>ExecuteUpdateAsync</c> / <c>ExecuteDeleteAsync</c>).
/// </summary>
internal sealed class TestDbContextFactory : IDbContextFactory<NotificationDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<NotificationDbContext> _options;

    private TestDbContextFactory(SqliteConnection connection, DbContextOptions<NotificationDbContext> options)
    {
        _connection = connection;
        _options = options;
    }

    public static TestDbContextFactory Create()
    {
        SqliteConnection connection = new("DataSource=:memory:");
        connection.Open();

        DbContextOptionsBuilder<NotificationDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlite(connection);
        optionsBuilder.ReplaceService<IModelCustomizer, SqliteCompatibleModelCustomizer>();

        DbContextOptions<NotificationDbContext> options = optionsBuilder.Options;

        // Create the schema
        using (NotificationDbContext db = new(options))
        {
            db.Database.EnsureCreated();
        }

        return new TestDbContextFactory(connection, options);
    }

    public NotificationDbContext CreateDbContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}

/// <summary>
/// Model customizer that remaps PostgreSQL-specific types (jsonb, DateTimeOffset)
/// to SQLite-compatible types with value converters.
/// </summary>
internal sealed class SqliteCompatibleModelCustomizer(ModelCustomizerDependencies dependencies) : RelationalModelCustomizer(dependencies)
{
    private static readonly ValueConverter<JsonElement, string> JsonElementConverter = new(
        v => v.GetRawText(),
        v => JsonDocument.Parse(v, default).RootElement);

    private static readonly ValueConverter<DateTimeOffset, long> DateTimeOffsetConverter = new(
        v => v.ToUnixTimeMilliseconds(),
        v => DateTimeOffset.FromUnixTimeMilliseconds(v));

    private static readonly ValueConverter<DateTimeOffset?, long?> NullableDateTimeOffsetConverter = new(
        v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : null,
        v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (IMutableProperty property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(JsonElement))
                {
                    property.SetColumnType("TEXT");
                    property.SetValueConverter(JsonElementConverter);
                }
                else if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(DateTimeOffsetConverter);
                }
                else if (property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(NullableDateTimeOffsetConverter);
                }
            }
        }
    }
}
