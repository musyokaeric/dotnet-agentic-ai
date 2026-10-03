using Azure.AI.OpenAI;
using Azure.Identity;
using CustomContextProvider;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System.Text.Json;

// 1. Define the variables we extracted from Microsoft Foundry
IConfiguration config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var endpoint = config["AZURE_OPENAI_ENDPOINT"] ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName = config["AZURE_OPENAI_DEPLOYMENT_NAME"] ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not valid or does not exist.");

// 2. Initialize the chat client
ChatClient chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
            .GetChatClient(deploymentName);

// 3. Initialize the Agent with our custom stateful provider
AIAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions()
{
    Name = "CorporateGuide",
    ChatOptions = new() { Instructions = "You are a friendly internal corporate assistant." },
    AIContextProviders = [new EmployeeProfileProvider(chatClient.AsIChatClient())] // custom context provider
});

// 4. Create a new session
AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine("--- Starting Fresh Session ---");
Console.WriteLine(await agent.RunAsync("What is the company policy on remote work?", session));
Console.WriteLine(await agent.RunAsync("My name is Eric and I work in the IT Department.", session));

// 5. Serialize the session (This automatically captures the extracted EmployeeProfile)
JsonElement serializedSession = await agent.SerializeSessionAsync(session);

Console.WriteLine("\n--- Simulating a New Day (Deserializing Session) ---");

// 6. Deserialize and resume
var resumedSession = await agent.DeserializeSessionAsync(serializedSession);

Console.WriteLine(await agent.RunAsync("Can you remind me of my department?", resumedSession));

// 7. Accessing the strongly-typed memory directly from code
var profileProvider = agent.GetService<EmployeeProfileProvider>();
var profile = profileProvider?.GetProfile(resumedSession);

Console.WriteLine("\n[SYSTEM DIAGNOSTICS] Explicitly reading memory component:");
Console.WriteLine($"Extracted Name: {profile?.EmployeeName}");
Console.WriteLine($"Extracted Department: {profile?.Department}");
