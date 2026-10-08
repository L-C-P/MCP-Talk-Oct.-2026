# AGENTS.md

Agent guidance for this repository.

## Scope and precedence

- This file defines repository-level defaults.
- If a subdirectory contains its own `AGENTS.md`, that file overrides these rules for that subtree.

## Source documents

- Main Slidev deck: `slidev/slides.md`

## Session baseline (must remain consistent)

- Talk format: frontal presentation with one live demo
- Duration: 60 minutes
- Slide language: English
- Spoken delivery: German
- Audience: .NET developers, architects, tech leads, and AI decision-makers

## Concept summary (critical content to preserve)

The talk explains why MCP exists, how host/client/server interact, and how to implement a practical MCP server with
.NET.

Keep these pillars intact:

- MCP standardizes AI integration and reduces custom connector complexity.
- The host is the control layer and protocol translator.
- Capability model must stay explicit:
    - Tool = execution (may have side effects)
    - Resource = read-only data by URI
    - Prompt = reusable workflow template
- Runtime narrative stays clear: discovery (`tools/list`) → invocation (`tools/call`) → result loop.
- Transport options are a deployment decision: local `stdio`, Streamable HTTP, and serverless Azure Functions.
- .NET implementation focus remains practical: SDK setup, attribute-based registration, host invocation.
- Demo anchor remains StarAgent with:
    - `get_chart_position`
    - `book_venue`
    - `rider://artist/{name}`
    - `concert_press_release`
- Auto-discovery is presented as an active development topic, not as universally mature.

## Editing rules

- Keep slide text in English unless explicitly requested otherwise.
- Keep speaker notes in German unless explicitly requested otherwise.
- Preserve existing structure and story arc: problem → architecture → runtime → implementation → demo → outlook.
- When updating slide titles, keep the setlist slide aligned.
- When topics, ordering, or section names change, update the table of contents (setlist) accordingly.
- Preserve UTF-8 encoding and Slidev separators (`---`).
- In Mermaid labels, use `<br/>` for line breaks and never literal `\n`.

## Build workflow

- A build server is running for this project.
- Do not run a local build by default after every change.
- Run a local build only when explicitly needed for targeted testing or when requested.

## Quality checks before handoff

- No broken Markdown, code fences, or Mermaid syntax.
- Technical claims match the concept source.
- Changes keep the deck compatible with a 45-minute delivery including Q&A.
