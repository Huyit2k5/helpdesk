export enum AiProviderType {
  BuiltInOffline = 1,
  GoogleGemini = 2,
  OpenAI = 3,
  LocalOllama = 4
}

export enum CustomerSentiment {
  Positive = 1,
  Neutral = 2,
  Frustrated = 3,
  UrgentCrisis = 4
}

export enum AiReplyTone {
  Professional = 0,
  TechnicalGuide = 1,
  RequestMoreInfo = 2,
  AcknowledgedInvestigating = 3,
  CustomPrompt = 4
}

export interface AiSettingsDto {
  isEnabled: boolean;
  provider: AiProviderType;
  apiKey?: string;
  modelName?: string;
  baseUrl?: string;
  temperature: number;
  enableAutoSentiment: boolean;
  customSystemPrompt?: string;
}

export interface UpdateAiSettingsDto {
  isEnabled: boolean;
  provider: AiProviderType;
  apiKey?: string;
  modelName?: string;
  baseUrl?: string;
  temperature: number;
  enableAutoSentiment: boolean;
  customSystemPrompt?: string;
}

export interface TestAiConnectionInputDto {
  provider: AiProviderType;
  apiKey?: string;
  modelName?: string;
  baseUrl?: string;
}

export interface TestAiConnectionResultDto {
  success: boolean;
  message: string;
  latencyMs: number;
}

export interface TicketAiSummaryDto {
  summary: string;
  mainIssue: string;
  currentProgress: string;
  nextSteps: string;
  modelUsed: string;
}

export interface GenerateAiReplyInputDto {
  ticketId: string;
  tone: AiReplyTone;
  userGuidance?: string;
  includeKnowledgeBase: boolean;
}

export interface GenerateAiReplyResultDto {
  replyText: string;
  relevantArticles: string[];
  modelUsed: string;
}

export interface AnalyzeSentimentResultDto {
  sentiment: CustomerSentiment;
  reason: string;
  confidenceScore: number;
  suggestedUrgency: string;
}
