# MpAgent

## Review GitLab Merge Requests

### Architecture overview

```
┌────────────┐
│  Program   │
└─────┬──────┘
      │
┌─────▼───────────────┐
│       Agent         │
│ (GitLabReviewAgent) │
└─────┬───────────────┘
      │ 
┌─────▼───────────────┐
│     AI Function     │
│ (GitLabAiFunctions) │
└─────┬───────────────┘
      │ 
┌─────▼────────────────────┐
│           Tool           │
│ (GitLabMergeRequestTool) │
└──────────────────────────┘
```

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

---
