using MockCosmosDb;

// GOAL: State Management - Persisting agent state across sessions
// Maintaining sessions when moving between environments e.g. request 1 goes to server A, request 2 moves to server B
// Persistent mandate - extraction session state from volatile memory (RAM of a single server) into a highly available distributed storage
// e.g. Redis (for fast, volatile session management with automatic expiration), Cosmos Db (ideal architectural fit for storing native JSON documents)

// Setup our mock database and agent service
var repository = new MockCosmosDbRepository();
var agentService = new StatelessAgentService(repository);

string userId = "user-778899";

Console.WriteLine("--- Monday Morning ---");
string response1 = await agentService.HandleUserMessageAsync(userId, "Hi, I am planning a trip to Tokyo next month.");
Console.WriteLine($"Agent: {response1}\n");

// The application could completely shut down or restart here.
// The memory is safely stored in the repository.

Console.WriteLine("--- Friday Afternoon (Simulating a new server request) ---");
string response2 = await agentService.HandleUserMessageAsync(userId, "Do you remember where I said I was traveling?");
Console.WriteLine($"Agent: {response2}\n");