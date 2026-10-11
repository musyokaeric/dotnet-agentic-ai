using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 1. Register AG-UI services
builder.Services.AddHttpClient().AddLogging();
builder.Services.AddAGUIServer();

// 2. Initialize the LLM Chat Client and Define the Backend Agent
var endpoint = builder.Configuration["AZURE_OPENAI_ENDPOINT"] ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName = builder.Configuration["AZURE_OPENAI_DEPLOYMENT_NAME"] ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not valid or does not exist.");

AIAgent enterpriseAgent = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
     .GetChatClient(deploymentName)
     .AsAIAgent(
         name: "EnterpriseSupportAgent",
         instructions: "You are a helpful enterprise support agent."
     );

var app = builder.Build();

// Configure the HTTP request pipeline.

// 3. Expose the Agent via the AG-UI Protocol
// This single extension method automatically wires up HTTP POST processing and SSE streaming
app.MapAGUIServer("/agui/support", enterpriseAgent);

app.Run("http://localhost:5000");