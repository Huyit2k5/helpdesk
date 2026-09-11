namespace Helpdesk.Ai;

public enum AiProviderType
{
    BuiltInOffline = 1,
    GoogleGemini = 2,
    OpenAI = 3,
    LocalOllama = 4
}

public enum CustomerSentiment
{
    Positive = 1,
    Neutral = 2,
    Frustrated = 3,
    UrgentCrisis = 4
}

public enum AiReplyTone
{
    Professional = 0,
    TechnicalGuide = 1,
    RequestMoreInfo = 2,
    AcknowledgedInvestigating = 3,
    CustomPrompt = 4
}
