using MongoDB.Bson;
using MongoDB.Driver;
using SimpleWebAppReact.Entities;

namespace SimpleWebAppReact.Services;

/// <summary>
/// Connects to the database named by ConnectionStrings:DbConnection and ConnectionStrings:DatabaseName
/// </summary>
public class MongoDbService
{
    public MongoDbService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DbConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DbConnection is not configured. Set it in .env, user-secrets, or appsettings.Development.json.");
        }

        var databaseName = configuration.GetConnectionString("DatabaseName") ?? "test";
        Database = new MongoClient(connectionString).GetDatabase(databaseName);

        Database.GetCollection<User>("user").Indexes.CreateOne(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Username), new CreateIndexOptions { Unique = true }));
        Database.GetCollection<Review>("review").Indexes.CreateOne(new CreateIndexModel<Review>(
            Builders<Review>.IndexKeys.Ascending(r => r.BuildingId).Descending(r => r.CreatedAt)));
        Database.GetCollection<Review>("review").Indexes.CreateOne(new CreateIndexModel<Review>(
            Builders<Review>.IndexKeys.Descending(r => r.CreatedAt)));
        Database.GetCollection<Events>("events").Indexes.CreateOne(new CreateIndexModel<Events>(
            Builders<Events>.IndexKeys.Ascending(e => e.Date)));
    }

    public IMongoDatabase Database { get; }

    /// <summary>
    /// whether id is a valid ObjectId; querying with an invalid one throws
    /// </summary>
    public static bool IsValidId(string? id) => id != null && ObjectId.TryParse(id, out _);
}
