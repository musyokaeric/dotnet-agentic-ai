using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using OpenAI.Chat;
using System.Text.Json;

IConfiguration config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var endpoint = config["AZURE_OPENAI_ENDPOINT"] ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName = config["AZURE_OPENAI_DEPLOYMENT_NAME"] ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not valid or does not exist.");
var gihtubToken = config["GITHUB_PERSONAL_ACCESS_TOKEN"] ?? throw new InvalidOperationException("GITHUB_PERSONAL_ACCESS_TOKEN is not valid or does not exist.");

Environment.SetEnvironmentVariable("GITHUB_PERSONAL_ACCESS_TOKEN", gihtubToken);
Console.WriteLine("[System] Github PAT is configured.\n");
// the Github MCP server is designed to work with both public AND private repos.
// Without specific user/system instructions, it defaults to querying public repos

// Testinf PAT directly first
Console.WriteLine("[System] Testing PAT access...\n");
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("Authorization", $"token {gihtubToken}");
httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
httpClient.DefaultRequestHeaders.Add("User-Agent", "MyApp");
try
{
    var commitsResponse = await httpClient.GetAsync($"https://api.github.com/repos/musyokaeric/dotnet-agentic-ai/commits?per_page=3");
    if (!commitsResponse.IsSuccessStatusCode)
    {
        Console.WriteLine("[ERROR] PAT cannot access commits!");
        Console.WriteLine($"Status: {commitsResponse.StatusCode}");
        var errorContent = await commitsResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"Error: {errorContent}");
        return;
    }
    Console.WriteLine("[System] PAT can access commits directly");

    var content = await commitsResponse.Content.ReadAsStringAsync();
    var json = JsonDocument.Parse(content);
    var commitCount = json.RootElement.ValueKind == JsonValueKind.Array ? json.RootElement.GetArrayLength() : 0;
    Console.WriteLine($"[System] Retrieved {commitCount} commits.\n");
}
catch (Exception ex)
{
    Console.WriteLine($"[ERROR] PAT test failed: {ex.Message}");
    return;
}


Console.WriteLine("--- Initializing Local DevOps Swarm ---");

// 1. Establish the Local MCP Connection
// The framework will execute 'npx' to launch the GitHub MCP server locally.
// NOTE: This requires Node.js to be installed on the host machine and a GITHUB_PERSONAL_ACCESS_TOKEN environment variable.
await using var mcpClient = await McpClient.CreateAsync(new StdioClientTransport(new()
{
    Name = "MCPServer",
    Command = "npx",
    Arguments = ["-y", "--verbose", "@modelcontextprotocol/server-github"]
}));

// 2. Capability Discovery
// Ask the MCP server what tools it exposes (e.g., search_repositories, get_commit, list_issues)
var mcpTools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
Console.WriteLine($"[System] Discovered {mcpTools.Count()} tools from the local GitHub MCP server.");

// 3. Initialize the Enterprise Agent
// We cast the discovered MCP tools into native AITool definitions and inject them into the agent.
AIAgent releaseAgent = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
     .GetChatClient(deploymentName)
     .AsAIAgent(
         name: "ReleaseManager",
         instructions: "You are a DevOps Release Manager. You must only answer questions related to GitHub repositories. Use your tools to fetch commit history and summarize it into professional release notes.",
         tools: [.. mcpTools.Cast<AITool>()]
     );

// 4. Execution
//string prompt = "Fetch the last 3 commits from the microsoft/agent-framework repository and summarize them for our V1.5 release notes.";
string prompt = "Fetch the last 3 commits from the musyokaeric/dotnet-agentic-ai repository and summarize them for our V1.5 release notes.";
Console.WriteLine($"\nUser: {prompt}\n");
Console.WriteLine("[System] Agent is autonomously querying GitHub...");

AgentResponse response = await releaseAgent.RunAsync(prompt);
Console.WriteLine($"\nRelease Manager:\n{response.Text}");