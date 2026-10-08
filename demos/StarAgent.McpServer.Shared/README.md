# StarAgent.McpServer.Shared

Shared StarAgent MCP capability library used by multiple hosts:
- `demos/StarAgent.McpServer.Stdio`
- `demos/StarAgent.McpServer.Http`

## Contains
- MCP Tools: `get_chart_position`, `book_venue`
- MCP Resource: `rider://artist/{name}`
- MCP Prompt: `concert_press_release`
- MCP App (UI): `ui://staragent/chart-card.html` – interactive chart card for `get_chart_position`
- Shared domain models and deterministic demo data services

## Why this project exists

It keeps transport-agnostic logic in one place so stdio and HTTP hosts can stay thin and only define transport/bootstrap concerns.
