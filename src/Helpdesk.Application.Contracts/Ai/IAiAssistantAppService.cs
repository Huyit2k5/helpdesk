using System;
using System.Threading.Tasks;
using Helpdesk.Ai.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.Ai;

public interface IAiAssistantAppService : IApplicationService
{
    Task<AiSettingsDto> GetSettingsAsync();
    Task UpdateSettingsAsync(UpdateAiSettingsDto input);
    Task<TestAiConnectionResultDto> TestConnectionAsync(TestAiConnectionInputDto input);
    Task<TicketAiSummaryDto> SummarizeTicketAsync(Guid ticketId);
    Task<GenerateAiReplyResultDto> GenerateReplyAsync(GenerateAiReplyInputDto input);
    Task<AnalyzeSentimentResultDto> AnalyzeTicketSentimentAsync(Guid ticketId);
}
