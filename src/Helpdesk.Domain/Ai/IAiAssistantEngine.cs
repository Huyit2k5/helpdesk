using System.Collections.Generic;
using System.Threading.Tasks;
using Helpdesk.KnowledgeBase;
using Helpdesk.Tickets;

namespace Helpdesk.Ai;

public class TicketSummaryResult
{
    public string KeyIssue { get; set; } = string.Empty;
    public string ActionsTaken { get; set; } = string.Empty;
    public string NextStep { get; set; } = string.Empty;
    public string FullSummaryText { get; set; } = string.Empty;
}

public class SuggestedReplyResult
{
    public string ReplyText { get; set; } = string.Empty;
    public List<string> ReferencedArticles { get; set; } = new();
    public string ToneExplanation { get; set; } = string.Empty;
}

public class SentimentAnalysisResult
{
    public CustomerSentiment Sentiment { get; set; }
    public int SentimentScore { get; set; }
    public string SentimentLabel { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string? SuggestedPriority { get; set; }
}

public interface IAiAssistantEngine
{
    Task<TicketSummaryResult> SummarizeTicketAsync(Ticket ticket, List<TicketComment> comments);

    Task<SuggestedReplyResult> GenerateSuggestedReplyAsync(
        Ticket ticket,
        List<TicketComment> comments,
        AiReplyTone tone,
        string? customInstruction = null,
        List<KnowledgeArticle>? relevantArticles = null);

    Task<SentimentAnalysisResult> AnalyzeSentimentAsync(Ticket ticket, List<TicketComment>? comments = null);

    Task<(bool isSuccess, string message)> TestConnectionAsync(AiProviderType provider, string apiKey, string modelName, string? baseUrl);
}
