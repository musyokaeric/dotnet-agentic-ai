using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;


// Scaling problem - integrating a sprawling 15-onde graph into an API is cumbersome
// .AsAgent() extension - wraps the entire compiled workflow graph into a single AI agent interface
// Hierarchies if intelligence (await workflowAgent.RunAsync()) - uses multi-agent swarms as functional tools for other master agents


// 1. The Shared Workflow State (payload)
public record TicketState(string UserQuery, string Category = "Unassigned", string FinalResolution = "");


internal class Program
{
    private static async Task Main(string[] args)
    {
        IConfiguration config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
        var endpoint = config["AZURE_OPENAI_ENDPOINT"] ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
        var deploymentName = config["AZURE_OPENAI_DEPLOYMENT_NAME"] ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not valid or does not exist.");

        IChatClient chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
            .GetChatClient(deploymentName)
            .AsIChatClient();

        // 2. Create the Agents

        // 2a. The Router
        AIAgent triageAgent = chatClient.AsAIAgent(
            name: "Triage",
            instructions: "Analyze the user's IT request. Categorize it strictly as either 'Hardware' or 'Software'. Output only the category word."
        );

        // 2b. The Hardware Specialists
        AIAgent hardwareAgent = chatClient.AsAIAgent(
            name: "HardwareSupport",
            instructions: "You are an enterprise hardware specialist. Provide concise troubleshooting steps for physical device issues."
        );

        // 2c. The Software Specialists
        AIAgent softwareAgent = chatClient.AsAIAgent(
            name: "SoftwareSupport",
            instructions: "You are an enterprise software specialist. Provide concise troubleshooting steps for application, OS, and network issues."
        );

        // Configure the execution logic / Workflow nodes

        // 3a. Triage Node Execution Logic
        Func<TicketState, TicketState> triageFunc = state =>
        {
            Console.WriteLine($"[Triage] Analyzing ticket: '{state.UserQuery}'");
            AgentResponse response = triageAgent.RunAsync(state.UserQuery).GetAwaiter().GetResult();

            string category = response.Text.Trim();
            Console.WriteLine($"[Triage] Decision: Routed to {category} Department.");

            // Return a mutated copy of the state with the new category
            return state with { Category = category };
        };
        var triageNode = triageFunc.BindAsExecutor("TriageNode");

        // 3b. Hardware Node Execution Logic
        Func<TicketState, TicketState> hardwareFunc = state =>
        {
            Console.WriteLine($"[Hardware Support] Generating resolution...");
            AgentResponse response = hardwareAgent.RunAsync(state.UserQuery).GetAwaiter().GetResult();
            return state with { FinalResolution = response.Text };
        };
        var hardwareNode = hardwareFunc.BindAsExecutor("HardwareNode");

        // 3c. Software Node Execution Logic
        Func<TicketState, TicketState> softwareFunc = state =>
        {
            Console.WriteLine($"[Software Support] Generating resolution...");
            AgentResponse response = softwareAgent.RunAsync(state.UserQuery).GetAwaiter().GetResult();
            return state with { FinalResolution = response.Text };
        };
        var softwareNode = softwareFunc.BindAsExecutor("SoftwareNode");

        // 4. Build the Graph with Conditional Edges
        var workflow = new WorkflowBuilder(triageNode)
            // If Triage says Hardware, route to the Hardware Agent
            .AddEdge<TicketState>(triageNode, hardwareNode, condition: state =>
                state != null && state.Category.Contains("Hardware", StringComparison.OrdinalIgnoreCase))
            // If Triage says Software, route to the Software Agent
            .AddEdge<TicketState>(triageNode, softwareNode, condition: state =>
                state != null && state.Category.Contains("Software", StringComparison.OrdinalIgnoreCase))
            .Build();

        Console.WriteLine("--- Incoming Enterprise IT Ticket ---\n");
        var initialTicket = new TicketState("My laptop screen is flickering aggressively and the hinge feels loose.");

        // 5. Execute the Workflow Graph
        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, initialTicket);

        TicketState? finalState = null;

        // Observe the events as the payload travels between the agents
        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is ExecutorCompletedEvent executorComplete)
            {
                Console.WriteLine($"[System] -> Node '{executorComplete.ExecutorId}' completed.");

                // Cast Data to TicketState
                if (executorComplete.Data is TicketState ticketState)
                {
                    finalState = ticketState;
                    Console.WriteLine($"         State: Category='{ticketState.Category}', Resolution='{(string.IsNullOrEmpty(ticketState.FinalResolution) ? "(pending)" : "set")}'");
                }
            }
        }

        Console.WriteLine($"\n--- Final Resolution ---\n{finalState?.FinalResolution}");
    }
}