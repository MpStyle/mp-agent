using GitHub.Copilot.SDK;
using Microsoft.Agents.AI;
using MpAgent.Translate.Entities;

namespace MpAgent.Translate.Agents;

public sealed class TranslationAgent : IAsyncDisposable
{
    private CopilotClient? copilotClient;
    private AIAgent? agent;
    
    public async Task InitializeAsync()
    {
        if (copilotClient != null)
            return;

        copilotClient = new CopilotClient();
        await copilotClient.StartAsync();

        agent = copilotClient.AsAIAgent(
            instructions:
            """
            You are a professional bilingual writing assistant.
            
            Your task is NOT a literal translation.
            Your task is to:
            - translate the text into English
            - improve clarity, fluency, and readability
            - adapt tone and structure to the given context and formality level
            - returns only the translated and improved text, without any additional commentary
            
            You must preserve:
            - original meaning
            - intent
            - factual content
            
            You must NOT:
            - add new information
            - remove important details
            - change the message intent
            """,
            tools: []
        );
    }

    public async Task<string> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken = default)
    {
        if (agent == null)
            throw new InvalidOperationException("Agent not initialized");

        var prompt = $"""
                       Translate the following text into English and improve its writing quality.
                       
                       Context:
                       {request.Context}
                       
                       Formality level:
                       {request.Formality}
                       
                       Guidelines:
                       - Use natural, idiomatic English
                       - Improve sentence structure and flow
                       - Adjust tone to match the context
                       - Keep the message aligned with the original intent
                       - Be concise, clear, and professional when appropriate
                       
                       Text:
                       {request.Text}
                       """;

        var response = await agent.RunAsync(prompt, cancellationToken: cancellationToken);
        return response.Text;
    }
    
    public async ValueTask DisposeAsync()
    {
        if (copilotClient != null)
        {
            await copilotClient.DisposeAsync();
            copilotClient = null;
        }
    }
}
