# Agentic System Design — RagAgentsPlatform

This document describes the lightweight agentic extension added to RagAgentsPlatform. It documents architecture, component contracts, data flow, safety controls, deployment, and testing guidance so the team can operate and evolve the agent safely.

## Goals
- Provide a simple, extensible agent that can plan actions using the LLM and execute a small set of safe tools (search, PDF ingest, etc.).
- Keep the runtime lightweight and DI-friendly so tools reuse existing services (AzureSearch, OpenAI, PdfIngest).
- Enforce safety (whitelist, step limits, timeouts) and observability (structured logs, telemetry).

## High-level components

- AgentService (RagAgents.Core.Agents.AgentService)
  - Coordinates planning (LLM prompt -> JSON plan), normalization (aliases -> canonical tool names), execution (invoke ITool implementations), and result aggregation.
  - Safety: enforces allowed-tools whitelist, max steps, and logs failures.
  - Registered in DI as IAgent, scoped per request.

- Tool contract (RagAgents.Core.Agents.ITool)
  - Interface: string Name { get; } and Task<string> RunAsync(string input)
  - Tools implement business actions: SearchTool, PdfIngestTool, ...

- SearchTool
  - Uses IAzureOpenAIService to generate embedding for input text and calls IAzureSearchService.VectorSearchAsync(embedding).
  - Returns a concatenated string of chunk results.

- PdfIngestTool
  - Wraps IPdfIngestService.IngestAsync. Accepts data URI (base64) or local file path.
  - Returns status messages and uses existing ingestion pipeline (Form Recognizer -> embed -> index).

- IAzureOpenAIService, IAzureSearchService, IPdfIngestService
  - Existing core services. Agent tools call them via DI; in local/dev a NoOpPdfIngestService fallback exists.

- API Endpoint
  - POST /api/agent/run accepts { "goal": "..." } and returns final tool result.
  - Controller validates input, calls IAgent.RunAsync, and returns structured JSON.

## Data flow (request path)
1. Client posts a goal to /api/agent/run (optionally with conversationId).
2. AgentService constructs a planning prompt that explicitly lists allowed tool names and asks the LLM to return a JSON array of steps (each {"tool":"Name","input":"..."}).
3. LLM returns plan (JSON). AgentService parses and normalizes tool names via alias map, trims to max steps.
4. For each normalized step AgentService locates the ITool implementation and invokes RunAsync(input). Execution happens sequentially; the last tool result is returned to caller.
5. AgentService logs each step and result; optionally saves steps/results to ConversationStore for audit.

## Prompt contract and example
- Planner must return pure JSON (array of objects). Example plan:

  [{"tool":"Search","input":"Find docs about Form Recognizer PDF ingestion"}, {"tool":"PdfIngest","input":"data:application/pdf;base64,..."}]

- Prompt guidance (included in AgentService): include explicit allowed tool names and an example JSON. Keep prompt small and deterministic.

## Safety & limits
- Whitelist allowed tools built from DI-registered ITool.Name values.
- Alias mapping normalizes common synonyms to canonical tool names.
- Max steps (default 10) prevents runaway plans.
- Per-step timeout and retries (recommended next step) to avoid long blocking calls.
- Limit token budget and call frequency for LLM to control cost.
- Validate tool inputs (size, type) before executing tools.

## Observability
- Use ILogger in AgentService and tools for structured logs: plan prompt, parsed steps, normalized tool names, start/finish, errors.
- Correlate logs using a request/correlation id propagated from API request.
- Send telemetry to Application Insights: custom events for plan generated, step executed, tool failure.

## Security
- Protect /api/agent/run with authentication and roles as appropriate (Authorize attribute). Configure JwtBearer with RoleClaimType = "roles".
- Do not accept arbitrary executable commands; tools are the only allowed surface.
- Use Key Vault and Managed Identity for production secrets (OpenAI keys, Search keys). local.settings.json and appsettings.example for local dev.

## Deployment & configuration
- AgentService and tools are registered in API DI (Program.cs). Ensure core services (OpenAI, Search) are registered and configured.
- For Functions-based ingestion, keep Pdf ingestion in RagAgents.Functions and call via shared IPdfIngestService if needed.
- Provide appsettings.json.example and document required environment variables (AzureOpenAI, AzureSearch, DocumentAI).

## Testing strategy
- Unit tests: mock IAzureOpenAIService and ITool implementations to test parsing, normalization, and execution flow.
- Integration tests: run agent end-to-end with a test vector store or Azurite for blob storage.
- Fuzzy tests: feed malformed LLM output to ensure fallback behavior.

## Next steps
1. Add per-step timeouts and retry policy (CancellationToken + Polly) — recommended.
2. Persist plan and step results for audit and replay (ConversationStore or Cosmos DB).
3. Add Swagger auth config to test secured endpoint.
4. Optionally migrate to Semantic Kernel for richer planning and skill composition if agent complexity grows.

---
Document maintained by: RagAgentsPlatform team — update as architecture evolves.
