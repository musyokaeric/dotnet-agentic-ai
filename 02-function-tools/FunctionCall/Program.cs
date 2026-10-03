using Azure.AI.OpenAI;
using Azure.Identity;
using FunctionCall;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

// Define the variables we extracted from Microsoft Foundry
IConfiguration config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var endpoint = config["AZURE_OPENAI_ENDPOINT"] ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName = config["AZURE_OPENAI_DEPLOYMENT_NAME"] ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not valid or does not exist.");

// Initialize the Agent and Equip the Tool
AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
    .GetChatClient(deploymentName)
    .AsAIAgent(
        name: "LogisticsSupport",
        instructions: "You are a customer support agent. Help users track their orders concisely.",
        // We dynamically generate the AITool and pass it into the agent's capabilities
        tools: [AIFunctionFactory.Create(LogisticsTools.GetOrderStatus)]
    );

Console.WriteLine($"Agent '{agent.Name}' initialized. Ready to assist.\n");

//// --- Execution Pattern 1: Synchronous (Non-Streaming) ---
//Console.WriteLine("--- Synchronous Execution ---");
//string prompt1 = "What is the status of order ORD-12345?";
//Console.WriteLine($"User: {prompt1}");

//AgentResponse response = await agent.RunAsync(prompt1);
//Console.WriteLine($"Agent: {response.Text}\n");

// --- Execution Pattern 2: Real-Time (Streaming) ---
Console.WriteLine("--- Streaming Execution ---");
string prompt2 = "I need an update on ORD-99999, please.";
Console.WriteLine($"User: {prompt2}");
Console.Write("Agent: ");

await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(prompt2))
{
    Console.Write(update.Text);
}
Console.WriteLine("\n");