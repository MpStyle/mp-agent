namespace MpAgent.Translate.Entities;

public sealed class TranslationRequest
{
    public required string Text { get; init; }
    public required string Context { get; init; }   // email, chat, gitlab_comment, etc.
    public required string Formality { get; init; } // informal, neutral, formal
}
