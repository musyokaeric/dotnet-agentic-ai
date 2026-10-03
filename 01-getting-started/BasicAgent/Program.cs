using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

// 1. Define the variables we extracted from Microsoft Foundry
// ==============================================================

// Use this if you have a global environment variable setup in your pc
// ===
//var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
//var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-5-mini";

IConfiguration config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var endpoint = config["AZURE_OPENAI_ENDPOINT"] ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName = config["AZURE_OPENAI_DEPLOYMENT_NAME"] ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not valid or does not exist.");


// 2. Instantiate the universal chat client
// ========================================
IChatClient chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
    .GetChatClient(deploymentName).AsIChatClient();

// 3. Define the Agent's Anatomy
// =============================
AIAgent supportAgent = chatClient.AsAIAgent(
    name: "NetworkSupport",
    instructions: "You are a Tier 1 IT Support Agent. Your answers must me concise, professional, and limited strictly to troubleshooting network and VPN connectivity. Keep your answers brief."
    );

Console.WriteLine($"Agent '{supportAgent.Name}' is online.\n");

// 4. Execute the Agent
// ====================
string userIssue = "I am getting a DNS resolution error when connecting to the corporate VPN from a coffee shop.";
Console.WriteLine($"User: {userIssue}\n");

// Synchronous response: waits for the complete response to complete before returning
//AgentResponse response = await supportAgent.RunAsync(userIssue); 
//Console.WriteLine($"Agent: {response}");

// Streaming response: creates a smoother UI experience
await foreach (AgentResponseUpdate update in supportAgent.RunStreamingAsync(userIssue))
{
    Console.Write(update.Text);
}