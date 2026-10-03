using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

IConfiguration config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var endpoint = config["AZURE_OPENAI_ENDPOINT"] ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName = config["AZURE_OPENAI_DEPLOYMENT_NAME"] ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not valid or does not exist.");

IChatClient chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
    .GetChatClient(deploymentName).AsIChatClient();

AIAgent agent = chatClient.AsAIAgent(
    name: "HistoryBuff",
    instructions: "You are a helpful history teacher. You answer questions and help students make connections between events."
);

// Create the sessions (The memory container)
// This object will accumulate the conversation history
AgentSession session = await agent.CreateSessionAsync();
Console.WriteLine("History Teacher is online. Type 'exit' to quit.\n");

// The conversation loop
while (true)
{
    Console.WriteLine("User: ");
    string? input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "exit") break;

    // We pass the 'session' into RunAsync.
    // Here, the framework automatically appends the user's input to this session,
    // sends the full history to the cloud, and appends the agent's response back to the session.

    // Synchronous
    //AgentResponse response = await agent.RunAsync(input, session);
    //Console.WriteLine($"Agent: {response.Text}\n");

    await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(input, session))
    {
        Console.Write(update.Text);
    }
}