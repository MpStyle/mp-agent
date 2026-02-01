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