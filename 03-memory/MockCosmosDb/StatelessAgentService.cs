using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System.Text.Json;

namespace MockCosmosDb;

class StatelessAgentService
{
    private readonly AIAgent agent;
    private readonly ISessionRepository repository;

    public StatelessAgentService(ISessionRepository repository)
    {
        this.repository = repository;

        // 1. Define the variables we extracted from Microsoft Foundry
        IConfiguration config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
        var endpoint = config["AZURE_OPENAI_ENDPOINT"] ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
        var deploymentName = config["AZURE_OPENAI_DEPLOYMENT_NAME"] ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not valid or does not exist.");

        agent = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
            .GetChatClient(deploymentName)
            .AsAIAgent(
                name: "PersistentGuide",
                instructions: "You are a helpful assistant. You remember details across long periods of time."
                );
    }

    // 2. The Stateless Execution Method (e.g., called by an ASP.NET Controller)
    public async Task<string> HandleUserMessageAsync(string sessionId, string userMessage)
    {
        AgentSession session;

        // Step A: Attempt to retrieve historical state from the database
        string? savedSessionJson = await repository.GetSessionJsonAsync(sessionId);

        if (!string.IsNullOrEmpty(savedSessionJson))
        {
            // Parse the database string back into a JsonElement
            using JsonDocument doc = JsonDocument.Parse(savedSessionJson);

            // Step B: Deserialize the session, restoring the agent's memory
            session = await agent.DeserializeSessionAsync(doc.RootElement);
            Console.WriteLine($"[SYSTEM LOG] Successfully restored session {sessionId} from database.");
        }
        else
        {
            // Step C: Fallback - Create a brand new session if no history exists
            session = await agent.CreateSessionAsync();
            Console.WriteLine($"[SYSTEM LOG] Created new session for {sessionId}.");
        }

        // Step D: Execute the agent with the loaded session
        AgentResponse response = await agent.RunAsync(userMessage, session);

        // Step E: Serialize the newly updated session state
        JsonElement updatedSessionElement = await agent.SerializeSessionAsync(session);
        string updatedJsonString = JsonSerializer.Serialize(updatedSessionElement);

        // Step F: Persist the updated state back to the database
        await repository.SaveSessionJsonAsync(sessionId, updatedJsonString);

        return response.Text;
    }
}
