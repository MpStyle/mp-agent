# MpAgent

## Architecture overview

```
┌────────────┐
│  Program   │
└─────┬──────┘
      │
┌─────▼────────┐
│    Agent     │
└─────┬────────┘
      │ 
┌─────▼────────┐
│  AI Function │
└─────┬────────┘
      │ 
┌─────▼───────┐
│    Tool     │
└─────────────┘
```

This section briefly explains the components shown in the diagram and how they are used in the application's pipeline.

- Agent
  - Role: a high-level orchestrator responsible for the review or workflow logic (for example: reviewing a merge request, translating text, etc.).
  - Behavior: builds the execution context from CLI inputs, applies validation rules, invokes AI Functions, and aggregates the results into a user-friendly format.
  - Design: agents are intended to be as stateless as possible and to focus on reasoning and intent (prompt + instructions) rather than direct I/O.

- AI Function
  - Role: a logical endpoint that exposes structured data and specific operations to the AI (for example: a function that returns merge request metadata and diffs).
  - Behavior: encapsulates deterministic data reads and transformations (structured JSON) that the agent/AI can use for reasoning.
  - Benefits: separates reasoning (agent prompts and instructions) from data access, reduces prompt ambiguity, and improves repeatability and testability.

- Tool
  - Role: components that perform external operations and side effects (for example: HTTP clients for GitLab, filesystem access, etc.).
  - Behavior: implement API calls, parsing, and low-level transformations; tools should be deterministic, testable, and reusable.
  - Example: `GitLabMergeRequestTool` performs REST requests to GitLab to fetch merge request information and diffs.

How they are used together (flow):
1. `Program` (CLI) receives the command and builds the DI container.
2. The appropriate `Agent` is invoked by the command handler with the provided arguments.
3. The `Agent` prepares prompts and may call one or more `AI Function`s to retrieve structured data (e.g., metadata and diffs).
4. `AI Function`s use `Tool`s to perform external calls or read files (for example, reading a `.editorconfig`).
5. The `Agent` receives the data, guides the AI model (prompt + instructions), and formats the final output for the user.

Relevant design principles:
- Prefer composition over inheritance: favour small, reusable components.
- Keep agents stateless: external state should live in tools or external storage, not inside agents.
- Make tools deterministic and testable: isolate I/O in classes that can be easily mocked in tests.

---

## Command-line usage

This section explains how to run the program from the command line, the prerequisites, and the available commands/arguments.

Prerequisites
- .NET SDK compatible with the project's target frameworks (see project files; example builds include net10.0).
- GitHub Copilot CLI: the project depends on the GitHub Copilot SDK for AI integration, so having the GitHub Copilot CLI installed and authenticated on your system is required. Follow the official GitHub Copilot CLI documentation for installation and authentication steps.
- GitLab settings: the application reads GitLab configuration from the `GitLab` section of `appsettings.json` or from environment variables (for example `GitLab__Host`, `GitLab__Token`, etc.). Make sure the host and token are configured correctly.

Examples (zsh)

- Run from source (development):

```bash
dotnet run --project MpAgent.CLI -- merge-request-review <MR_URL>
```

- Show help for the application or a command:

```bash
# Global help
dotnet run --project MpAgent.CLI -- --help

# Help for the merge-request-review command
dotnet run --project MpAgent.CLI -- merge-request-review --help
```

- Publish and run the produced binary:

```bash
dotnet publish MpAgent.CLI -c Release -o out
./out/MpAgent.CLI merge-request-review <MR_URL>
```

Commands and arguments

- merge-request-review <url>
  - Description: Runs an AI-driven review of a GitLab Merge Request.
  - Arguments:
    - `url` (required): the full URL of the GitLab Merge Request to review.
  - Example:

```bash
dotnet run --project MpAgent.CLI -- merge-request-review "https://gitlab.example.com/group/project/-/merge_requests/123"
```

- translate <text>
  - Description: Translate text into English and improve clarity, tone and formality using an AI translation agent. Useful for preparing messages, comments, or code review feedback in English.
  - Arguments:
    - `text` (required): the text to translate and improve. For multi-word text or longer passages, wrap the argument in quotes.
  - Options:
    - `--formality <level>` (optional): desired formality level. Supported values: `informal`, `neutral` (default), `formal`.
    - `--context <context>` (optional): the context where the translation will be used (examples: `email`, `chat`, `gitlab_comment`, `documentation`, `general`). Default: `general`.
  - Example:

```bash
dotnet run --project MpAgent.CLI -- translate "grazie per il tuo aiuto, ho aggiornato il codice" --formality formal --context gitlab_comment
```

Notes and troubleshooting
- If you encounter errors related to the AI service authentication, verify that the GitHub Copilot CLI is installed and that your account is authenticated according to the official GitHub instructions.
- Double-check your GitLab settings (host and token). You can provide them via `appsettings.json` in the `MpAgent.CLI` folder or via environment variables.
- The project is designed for extensibility: new commands and agents can be added following the multi-layer architecture shown above.
