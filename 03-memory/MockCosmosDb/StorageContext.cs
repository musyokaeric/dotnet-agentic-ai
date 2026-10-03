namespace MockCosmosDb;

// 1. Define the Enterprise Storage Contract
public interface ISessionRepository
{
    Task<string?> GetSessionJsonAsync(string sessionId);
    Task SaveSessionJsonAsync(string sessionId, string jsonPayload);
}

// (Mock implementation of a database like Cosmos DB or SQL Server)
public class MockCosmosDbRepository : ISessionRepository
{
    private readonly Dictionary<string, string> datastore = new();

    public Task<string?> GetSessionJsonAsync(string sessionId) =>
        Task.FromResult(datastore.TryGetValue(sessionId, out var json) ? json : null);

    public Task SaveSessionJsonAsync(string sessionId, string jsonPayload)
    {
        datastore[sessionId] = jsonPayload;
        return Task.CompletedTask;
    }
}
