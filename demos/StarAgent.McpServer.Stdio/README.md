# StarAgent.McpServer.Stdio

StarAgent demo MCP server for the talk.  
Transport: `stdio` (local host-launched process).

Reusable capabilities (tools/resources/prompts + domain models/services) are provided by:
- `demos/StarAgent.McpServer.Shared`

## Implemented capabilities

### Tools
- `get_chart_position`
  - Input: `songTitle`, `artist`, optional `chart`
  - Output: chart rank/peak/weeks + weekly history as structured content
  - UI: rendered as interactive chart card in hosts that support MCP Apps (see below)
- `book_venue`
  - Input: `artist`, `city`, `date` (`yyyy-MM-dd`), `capacity`
  - Output: booking status (`confirmed`, `waitlist`, `rejected`, `cancelled`) and details
  - Missing `city`, `date`, or `capacity` are requested from the user via elicitation (MRTR on spec 2026-07-28)

### Resource
- `rider://artist/{name}`
  - Returns backstage rider JSON
  - Includes the demo punchline for `van-halen` (`Absolutely NO brown M&Ms`)

### Prompt
- `concert_press_release`
  - Input: `artist`, `venue`, `date`, `tourName`
  - Returns one user message prompt template for a dramatic press release

### MCP App
- `ui://staragent/chart-card.html`
  - Linked to `get_chart_position` via `[McpAppUi]`
  - Shows rank, peak, weeks on chart and the chart history; the chart dropdown calls the tool again directly from the UI
  - Rendered by MCP Apps hosts such as VS Code GitHub Copilot, Claude Desktop, or ChatGPT; other hosts show the JSON result
  - Open `StarAgent.McpServer.Shared/Apps/chart-card.html` directly in a browser to preview it with sample data

## Local development

Run the server from source:

```shell
dotnet run --project demos/StarAgent.McpServer.Stdio
```

Example MCP host registration:

```json
{
  "servers": {
    "StarAgent": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/absolute/path/to/blm2026/demos/StarAgent.McpServer.Stdio"
      ]
    }
  }
}
```

## Packaging

Build NuGet package:

```shell
dotnet pack demos/StarAgent.McpServer.Stdio -c Release
```