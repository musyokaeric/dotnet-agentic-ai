using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using OpenAI.Responses;

// 1. Define the variables we extracted from Microsoft Foundry
IConfiguration config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var endpoint = config["AZURE_OPENAI_ENDPOINT"] ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName = config["AZURE_OPENAI_DEPLOYMENT_NAME"] ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not valid or does not exist.");

// Build the tools list and add the native Code Interpreter ResponseTool via the AITool bridge extension
List<AITool> tools = [];

#pragma warning disable OPENAI001
// Pragma warning is used because this feature is experimental and can change
tools.Add(ResponseTool.CreateCodeInterpreterTool(
    new CodeInterpreterToolContainer(CodeInterpreterToolContainerConfiguration.CreateAutomaticContainerConfiguration([]))));
#pragma warning restore OPENAI001

// Initialize the Agent and inject the native Code Interpreter tool
AIAgent mathAgent = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
    .GetChatClient(deploymentName)
    .AsAIAgent(
        name: "DataAnalyst",
        instructions: "You are a data analyst. You must write and execute Python code to answer complex math and data questions. Never guess the answer.",
        tools: tools
    );

Console.WriteLine($"Agent '{mathAgent.Name}' is online with a Python sandbox.\n");

// The agent will autonomously write a script to solve this, run it, and return the result.
string prompt = "Calculate the 10th Fibonacci number and determine if it is a prime number.";
Console.WriteLine($"User: {prompt}");

AgentResponse response = await mathAgent.RunAsync(prompt);
Console.WriteLine($"Agent: {response.Text}");