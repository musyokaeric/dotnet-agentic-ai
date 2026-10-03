using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System.Text.Json.Serialization;

IConfiguration config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var endpoint = config["AZURE_OPENAI_ENDPOINT"] ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName = config["AZURE_OPENAI_DEPLOYMENT_NAME"] ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not valid or does not exist.");

AIAgent meetingAgent = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
    .GetChatClient(deploymentName)
    .AsAIAgent(
        name: "meetingAnalyst",
        instructions: "You are an AI analyst. Extract the topic, action items, and overall sentiment from the provided transcript"
    );

string transcript = "We discussed the Q4 marketing push. Sarah needs to finalize the budget by Tuesday. John will contact the ad agency. Overall, everyone felt very optimistic about the campaign.";
Console.WriteLine($"Analyzing Transcript:\n{transcript}\n");

// Execute the Agent with RunAsync<T> for strongly-typed structured output
AgentResponse<MeetingAnalysis> response = await meetingAgent.RunAsync<MeetingAnalysis>(transcript);

// Access the strongly-typed Result directly (no manual deserialization needed)
MeetingAnalysis? analysis = response.Result;

// Utilize deterministic C# objects
if (analysis != null)
{
    Console.WriteLine($"Full Analysis: {analysis}\n");
    Console.WriteLine($"Topic: {analysis.Topic}");
    Console.WriteLine($"Sentiment: {analysis.Sentiment}");
    Console.WriteLine($"Action Items Count: {analysis.ActionItems.Length}");
    Console.WriteLine($"Action Items:");
    foreach (var item in analysis.ActionItems)
    {
        Console.WriteLine($"- {item}");
    }
}


// Data Contract
public record MeetingAnalysis(
    [property: JsonPropertyName("topic")] string Topic,
    [property: JsonPropertyName("actionItems")] string[] ActionItems,
    [property: JsonPropertyName("sentiment")] string Sentiment
);