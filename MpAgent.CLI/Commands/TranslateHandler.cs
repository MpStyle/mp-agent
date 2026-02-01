using System.CommandLine;
using MpAgent.GitLab.MergeRequestReviewer.Agents;
using MpAgent.Translate.Agents;
using MpAgent.Translate.Entities;

namespace MpAgent.CLI.Commands;

public class TranslateHandler
{
    private const string Name = "translate";
    private const string TextArgumentName = "text";
    private const string FormalityArgumentName = "formality";
    private const string ContextArgumentName = "context";
    
    private readonly TranslationAgent agent;

    public Command Command { get; }

    public TranslateHandler(TranslationAgent agent)
    {
        this.agent = agent;
        
        this.Command= new Command(Name, "Perform code review on a GitLab MR")
        {
            new Argument<string>(TextArgumentName)
            {
                Description = "The url of the GitLab Merge Request"
            }
        };
        
        this.Command.Options.Add(
            new Option<string>(FormalityArgumentName)
            {
                Description = "The formality level of the translation (informal, neutral, formal)",
            }
        );
        
        this.Command.Options.Add(
            new Option<string>(ContextArgumentName)
            {
                Description = "The context of the translation (email, chat, gitlab_comment, etc.)",
            }
        );
        
        this.Command.SetAction(this.Handler);
    }

    private async Task<int> Handler(ParseResult parseResult, CancellationToken cancellationToken)
    {
        try
        {
            await agent.InitializeAsync();
            var translation = await agent.TranslateAsync(new TranslationRequest
            {
                Text = parseResult.GetRequiredValue<string>(TextArgumentName),
                Formality = parseResult.GetValue<string?>(FormalityArgumentName) ?? "neutral",
                Context = parseResult.GetValue<string?>(ContextArgumentName) ?? "general"
            }, cancellationToken);
            
            Console.WriteLine("\n======= TRANSLATION ======\n");
            Console.WriteLine(translation);
            Console.WriteLine("\n==========================\n");
            return ErrorCode.Success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            return ErrorCode.TranslateError;
        }
    }
}