# MpAgent

## Project purpose
MpAgent is my personal **AI twin**, designed to automate and assist with real-world
developer tasks through **AI agents**.

The application is intentionally built as a **multi-purpose command-line tool**:
each command triggers a specific agent-driven workflow (for example, automated
code reviews, analysis tasks, or future developer utilities).

The goal of the project is not only task automation, but also to explore how
**AI agents can reason, apply project-specific rules, and produce actionable output**
in a developer-centric workflow.

---

## Project context
- Primary language: **C# (.NET)**
- Application type: **Command Line Interface (CLI)**
- CLI framework: **System.CommandLine**
- AI stack:
    - Microsoft Agent Framework
    - GitHub Copilot SDK
- External integrations:
    - GitLab REST APIs (including on-premise GitLab instances)

---

## Core use case (current)
The first implemented capability is an **AI-powered code review agent**.

Given a GitLab **Merge Request URL**, the agent:
1. Retrieves the merge request diff using GitLab APIs
2. Loads and interprets the `.editorconfig` file from the repository root (if present)
3. Reviews code changes with a focus on **C# and TypeScript**
4. Produces structured review notes with:
    - Precise file and line references
    - Clear explanations
    - Severity classification

Severity levels are expressed conceptually as:
- 🔴 **Red**: mandatory, high-risk issues that should be fixed
- 🟠 **Orange**: important recommendations
- 🟢 **Green**: optional improvements or stylistic suggestions

The agent behaves like a **senior code reviewer**, not a static analyzer.

---

## Additional capability: Translation (experimental)

In addition to the code-review agent, MpAgent now includes a lightweight translation and writing-improvement capability driven by an AI agent.

Overview:
- Command: `translate`
- Purpose: translate input text into English while improving clarity, fluency, and tone to match a specified context and formality level. This is not a literal translation; the agent preserves meaning and intent while improving expression.

Inputs:
- Text: the source text to translate
- Formality: one of `informal`, `neutral`, or `formal` (defaults to `neutral`)
- Context: usage context such as `email`, `chat`, `gitlab_comment`, or `general` (defaults to `general`)

Behavior and guarantees:
- The translation agent returns only the translated and improved text (no commentary or metadata).
- It must preserve original meaning, intent, and factual content.
- It must not add new information or remove important details.

Implementation notes:
- Command handler: `MpAgent.CLI/Commands/TranslateHandler.cs` parses arguments and invokes the agent.
- Agent: `MpAgent.Translate/Agents/TranslationAgent.cs` uses the GitHub Copilot SDK to run a focused prompt that both translates and improves the text.
- DTO: `MpAgent.Translate/Entities/TranslationRequest.cs` describes the request shape (Text, Context, Formality).

When to use:
- Drafting or polishing English text for comments, commit messages, emails, or other developer-facing communication.
- Not intended for high-stakes legal or safety-critical translations without human review.

---

## Architecture overview

```
Program (CLI)
↓
Command
↓
Agent
↓
AI Function (reasoning + prompt)
↓
Tool (GitLab, filesystem, etc.)
```

- The CLI is command-driven and extensible.
- Each command is responsible for:
    - Validating input
    - Building the execution context
    - Invoking the appropriate agent
- Agents encapsulate **reasoning and intent**.
- Tools handle **side effects and external systems** (GitLab APIs, filesystem access).

Each agent lives in its own project to keep responsibilities isolated and explicit.

---

## Design principles
- Prefer **composition over inheritance**
- Keep agents **stateless** where possible
- Tools should be deterministic and testable
- AI reasoning should be explicit and prompt-driven
- Favor **clear, readable code** over clever abstractions

---

## What Copilot should assume
- This is an **agent-based system**, not a traditional CRUD application
- Business logic often lives in **agents and prompts**, not only in code
- Extensibility is a primary goal
- Code suggestions should prioritize:
    - clarity
    - explicit intent
    - maintainability
    - real-world developer workflows

Copilot is encouraged to suggest improvements that align with these principles.
