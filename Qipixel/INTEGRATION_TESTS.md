# Integration testing (Playwright) + Playwright MCP

## Setup

```bash
cd Qipixel
npm i
npx playwright install chromium
```

## Happy-path E2E test

Runs a browser-level integration test against the Vite dev server and mocks `/api/*` responses in the browser (no backend required).

```bash
cd Qipixel
npm run test:e2e
```

- Spec: `Qipixel/tests/e2e/happy-path.spec.ts`
- Flow: open app → sign in → land on dashboard → open “The Routine” scene

## Playwright MCP server

Starts an MCP server that exposes Playwright browser automation tools to an MCP client.

```bash
cd Qipixel
npm run mcp:playwright
```

Example MCP client config (adjust to your client’s format):

```json
{
  "mcpServers": {
    "playwright": {
      "command": "npx",
      "args": ["mcp-server-playwright"]
    }
  }
}
```

