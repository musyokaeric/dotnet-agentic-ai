# .NET Agentic AI

A progressive, hands-on reference for building production-minded AI agents in **.NET 10** with the **Microsoft Agent Framework**, **Azure OpenAI / Microsoft Foundry**, and open agent protocols (**MCP**, **A2A**, **AG-UI**).

Each numbered module is a self-contained set of runnable samples that builds on the previous one, taking you from a single chat agent to multi-agent workflows, retrieval, and interoperable agent services.


## Contents

- [Tech stack](#tech-stack)
- [Architecture overview](#architecture-overview)
- [Repository layout](#repository-layout)
- [Getting started](#getting-started)
- [Running the samples](#running-the-samples)
- [Design notes](#design-notes)
- [Known limitations](#known-limitations)

## Tech stack

| Area | Technology |
|------|------------|
| Runtime | .NET 10, C# (nullable enabled, implicit usings) |
| Agent framework | `Microsoft.Agents.AI` (+ `.Workflows`, `.Hosting`, `.DevUI`) |
| LLM provider | Azure OpenAI via `Azure.AI.OpenAI`, `Microsoft.Extensions.AI` (`IChatClient`) |
| Authentication | `Azure.Identity` (`AzureCliCredential`, no API keys in code) |
| Vector store | Qdrant (`CommunityToolkit.VectorData.Qdrant`) with `text-embedding-3-small` |
| Protocols | Model Context Protocol (`ModelContextProtocol`), A2A, AG-UI |
| Orchestration & observability | .NET Aspire (AppHost + ServiceDefaults), OpenTelemetry |
| Configuration | .NET User Secrets |

## Architecture overview

```
 Single agent ─► Tools / HITL ─► Memory ─► Workflows ─► Patterns ─► RAG
      (01)           (02)         (03)       (04)         (05)      (06)
                                                                      │
                          Interoperability: A2A (07) · MCP (08) · AG-UI (09)
```

All samples share the same shape: an `IChatClient` is created from an Azure OpenAI deployment, wrapped as an `AIAgent`, and then composed with tools, context providers, workflows, or a transport.

## Repository layout

| Module | Project | What it demonstrates |
|--------|---------|----------------------|
| **01 – Getting started** | `BasicAgent` | Creating an `AIAgent` from an `IChatClient`; sync vs. streaming responses |
| | `MultiTurnConversation` | `AgentSession` to carry conversation history across turns |
| | `StructuredOutput` | Strongly typed output with `RunAsync<T>` and C# records |
| | `MinimalAgent` (Aspire) | Hosting an agent as a Web API with DevUI, orchestrated by an Aspire AppHost |
| **02 – Function tools** | `FunctionCall` | Exposing C# methods as tools (`LogisticsTools`) |
| | `CodeInterpreter` | Hosted Python sandbox tool for math/data questions |
| | `HITL` | Human-in-the-loop approval via `ApprovalRequiredAIFunction` for sensitive actions (refunds) |
| | `MinimalAgent` (Aspire) | Tool-enabled agent hosted behind a Web API |
| **03 – Memory** | `CustomContextProvider` | Custom `AIContextProvider` that extracts an employee profile and survives session serialization |
| | `MockCosmosDb` | Stateless agent service persisting session state to a repository (Cosmos DB/Redis pattern) |
| **04 – Multi-agent workflows** | `FirstWorkflow` | Graph workflow with executors, conditional edges, and an audit node (no LLM needed) |
| | `MultiAgentWorkflow` | Triage → specialist routing; workflow wrapped as a single agent via `.AsAgent()` |
| **05 – Workflow patterns** | `AgenticWorkflowPatterns` | Sequential, Concurrent, Handoff, and Group Chat topologies |
| | `MinimalAgent` (Aspire) | Hosting the orchestrator as a service |
| **06 – Agentic RAG** | `BasicTextRAG` | `TextSearchProvider` over a mock policy search adapter with source citations |
| | `QdrantVectorStore` | Embeddings + Qdrant vector search over architecture decision records |
| **07 – A2A** | `EnterpriseComplianceService` | Exposes an agent over the A2A HTTP+JSON protocol at `/a2a/compliance` |
| **08 – MCP** | `LocalGithubMcp` | Consuming the GitHub MCP server locally over stdio (`npx`) |
| | `MicrosoftGovernanceMCP` | Hosted remote MCP tool (Microsoft Learn) with `AlwaysRequire` approval governance |
| **09 – AG-UI** | `Server` / `Client` | Serving an agent via AG-UI (HTTP POST + SSE) and consuming it remotely |

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- An Azure OpenAI / Microsoft Foundry resource with a chat model deployment (the course uses `gpt-5-mini`)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli), signed in with `az login`
- Optional, per module:
  - Docker (module 06 – Qdrant)
  - Node.js (module 08 – local GitHub MCP server)
  - A GitHub personal access token (module 08)
  - `text-embedding-3-small` deployment (module 06 – Qdrant)

### Configuration

Secrets are never committed. Configure each project you run with User Secrets:

```powershell
cd 01-getting-started\BasicAgent
dotnet user-secrets set "AZURE_OPENAI_ENDPOINT" "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AZURE_OPENAI_DEPLOYMENT_NAME" "<your-deployment>"
```

Some web projects (07, 09) read their settings from `appsettings*.json` / user secrets; check each `Program.cs` for the keys it expects. Authentication uses `AzureCliCredential`, so no API key is required.

## Running the samples

Build everything:

```powershell
dotnet build dotnet-agentic-ai.slnx
```

Run a console sample:

```powershell
dotnet run --project 01-getting-started\BasicAgent
```

Run an Aspire-hosted agent (module 01, 02, or 05):

```powershell
dotnet run --project 01-getting-started\MinimalAgent\AppHost
```

Run Qdrant for module 06:

```powershell
docker run -d --name agentic-ai-qdrant -p 6333:6333 -p 6334:6334 qdrant/qdrant:latest
dotnet run --project 06-agentic-rag\QdrantVectorStore
```

Run the A2A and AG-UI services (start the server first, then the client):

```powershell
dotnet run --project 09-acp-agui\Server    # http://localhost:5000/agui/support
dotnet run --project 09-acp-agui\Client
```

For module 08 (local GitHub MCP), set `GITHUB_PERSONAL_ACCESS_TOKEN` before running.

## Design notes

- **Provider-agnostic core.** Agents are built on `IChatClient`, so the model provider can be swapped without touching agent logic.
- **Keyless auth.** `AzureCliCredential` (Entra ID) avoids storing API keys; endpoints and deployment names live in User Secrets.
- **Human oversight by default for risky actions.** Both local tools (`ApprovalRequiredAIFunction`) and remote MCP tools (`AlwaysRequire`) pause for approval; sessions keep context across the pause.
- **Stateless services, externalised state.** Session serialization enables moving conversations between servers, backed by a durable store in production.
- **Composable workflows.** Workflow graphs can be wrapped as agents, enabling hierarchies of agents used as tools by other agents.
- **Open protocols.** MCP for tools, A2A for agent-to-agent calls, and AG-UI for agent-to-frontend streaming.
- **Observability.** Aspire ServiceDefaults wire OpenTelemetry, health checks, resilience, and service discovery.

## Known limitations

- Samples are educational: search adapters and the Cosmos DB repository are mocks; swap in Azure AI Search / Cosmos DB / Redis for production.
- No automated tests; samples are validated by running them.
- Some APIs used (e.g. hosted code interpreter) are experimental and may change between package versions.