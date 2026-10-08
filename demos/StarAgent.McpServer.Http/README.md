# StarAgent.McpServer.Http

StarAgent MCP demo server over Streamable HTTP.

## Transport endpoint
- MCP endpoint: `http://localhost:3001/mcp` (stateless Streamable HTTP)
- Health/info endpoint: `http://localhost:3001/`

## Discovery endpoints (SEP-2127 Server Cards)
- AI Catalog: `http://localhost:3001/.well-known/ai-catalog.json`
- Server Card: `http://localhost:3001/mcp/server-card`

## Shared capability model

Tools, resources, prompts, models, and services are reused from:
- `demos/StarAgent.McpServer.Shared`

Registered capabilities:
- Tool: `get_chart_position`
- Tool: `book_venue`
- Resource: `rider://artist/{name}`
- Prompt: `concert_press_release`
- MCP App: `ui://staragent/chart-card.html`

## Local run

```shell
dotnet run --project demos/StarAgent.McpServer.Http
```

## MCP host registration example

```json
{
  "servers": {
    "StarAgentHttp": {
      "type": "http",
      "url": "http://localhost:3001/mcp"
    }
  }
}
```
