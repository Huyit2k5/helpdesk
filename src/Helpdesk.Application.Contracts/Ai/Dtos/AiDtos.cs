using System;
using System.Collections.Generic;
using Helpdesk.Ai;

namespace Helpdesk.Ai.Dtos;

public class AiSettingsDto
{
    public bool IsEnabled { get; set; } = true;
    public AiProviderType Provider { get; set; } = AiProviderType.BuiltInOffline;
    public string? ApiKey { get; set; }
    public string? ModelName { get; set; }
    public string? BaseUrl { get; set; }
    public double Temperature { get; set; } = 0.3;
    public bool EnableAutoSentiment { get; set; } = true;
    public string? CustomSystemPrompt { get; set; }
}

public class UpdateAiSettingsDto
{
    public bool IsEnabled { get; set; }
    public AiProviderType Provider { get; set; }
    public string? ApiKey { get; set; }
    public string? ModelName { get; set; }
    public string? BaseUrl { get; set; }
    public double Temperature { get; set; }
    public bool EnableAutoSentiment { get; set; }
    public string? CustomSystemPrompt { get; set; }
}

public class TestAiConnectionInputDto
{
    public AiProviderType Provider { get; set; }
    public string? ApiKey { get; set; }
    public string? ModelName { get; set; }
    public string? BaseUrl { get; set; }
}

public class TestAiConnectionResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long LatencyMs { get; set; }
}

public class TicketAiSummaryDto
{
    public string Summary { get; set; } = string.Empty;
    public string MainIssue { get; set; } = string.Empty;
    public string CurrentProgress { get; set; } = string.Empty;
    public string NextSteps { get; set; } = string.Empty;
    public string ModelUsed { get; set; } = string.Empty;
}

public class GenerateAiReplyInputDto
{
    public Guid TicketId { get; set; }
    public AiReplyTone Tone { get; set; } = AiReplyTone.Professional;
    public string? UserGuidance { get; set; }
    public bool IncludeKnowledgeBase { get; set; } = true;
}

public class GenerateAiReplyResultDto
{
    public string ReplyText { get; set; } = string.Empty;
    public List<string> RelevantArticles { get; set; } = new();
    public string ModelUsed { get; set; } = string.Empty;
}

public class AnalyzeSentimentResultDto
{
    public CustomerSentiment Sentiment { get; set; }
    public string Reason { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public string SuggestedUrgency { get; set; } = string.Empty;
}
