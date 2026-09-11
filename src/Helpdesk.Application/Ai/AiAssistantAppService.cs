using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Ai.Dtos;
using Helpdesk.KnowledgeBase;
using Helpdesk.Permissions;
using Helpdesk.Settings;
using Helpdesk.Tickets;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;

namespace Helpdesk.Ai;

public class AiAssistantAppService : ApplicationService, IAiAssistantAppService
{
    private readonly ISettingProvider _settingProvider;
    private readonly ISettingManager _settingManager;
    private readonly IAiAssistantEngine _aiEngine;
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<TicketComment, Guid> _commentRepository;
    private readonly IRepository<KnowledgeArticle, Guid> _articleRepository;

    public AiAssistantAppService(
        ISettingProvider settingProvider,
        ISettingManager settingManager,
        IAiAssistantEngine aiEngine,
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<TicketComment, Guid> commentRepository,
        IRepository<KnowledgeArticle, Guid> articleRepository)
    {
        _settingProvider = settingProvider;
        _settingManager = settingManager;
        _aiEngine = aiEngine;
        _ticketRepository = ticketRepository;
        _commentRepository = commentRepository;
        _articleRepository = articleRepository;
    }

    [Authorize(HelpdeskPermissions.AiSettings.Manage)]
    public async Task<AiSettingsDto> GetSettingsAsync()
    {
        var providerStr = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Ai.Provider) ?? "BuiltInOffline";
        if (!Enum.TryParse<AiProviderType>(providerStr, true, out var provider))
        {
            provider = AiProviderType.BuiltInOffline;
        }

        var apiKey = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Ai.ApiKey);
        var maskedApiKey = string.IsNullOrWhiteSpace(apiKey) ? string.Empty : "••••••••";

        var tempStr = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Ai.Temperature) ?? "0.3";
        double.TryParse(tempStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var temperature);

        return new AiSettingsDto
        {
            IsEnabled = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Ai.IsEnabled),
            Provider = provider,
            ApiKey = maskedApiKey,
            ModelName = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Ai.ModelName) ?? "gemini-1.5-flash",
            BaseUrl = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Ai.BaseUrl) ?? string.Empty,
            Temperature = temperature,
            EnableAutoSentiment = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Ai.EnableAutoSentiment),
            CustomSystemPrompt = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Ai.CustomSystemPrompt) ?? string.Empty
        };
    }

    [Authorize(HelpdeskPermissions.AiSettings.Manage)]
    public async Task UpdateSettingsAsync(UpdateAiSettingsDto input)
    {
        await SetSettingAsync(HelpdeskSettings.Ai.IsEnabled, input.IsEnabled.ToString().ToLowerInvariant());
        await SetSettingAsync(HelpdeskSettings.Ai.Provider, input.Provider.ToString());

        if (!string.IsNullOrWhiteSpace(input.ApiKey) && input.ApiKey != "••••••••")
        {
            await SetSettingAsync(HelpdeskSettings.Ai.ApiKey, input.ApiKey.Trim());
        }

        await SetSettingAsync(HelpdeskSettings.Ai.ModelName, input.ModelName?.Trim() ?? string.Empty);
        await SetSettingAsync(HelpdeskSettings.Ai.BaseUrl, input.BaseUrl?.Trim() ?? string.Empty);
        await SetSettingAsync(HelpdeskSettings.Ai.Temperature, input.Temperature.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await SetSettingAsync(HelpdeskSettings.Ai.EnableAutoSentiment, input.EnableAutoSentiment.ToString().ToLowerInvariant());
        await SetSettingAsync(HelpdeskSettings.Ai.CustomSystemPrompt, input.CustomSystemPrompt?.Trim() ?? string.Empty);
    }

    [Authorize(HelpdeskPermissions.AiSettings.Manage)]
    public async Task<TestAiConnectionResultDto> TestConnectionAsync(TestAiConnectionInputDto input)
    {
        var effectiveApiKey = input.ApiKey;
        if (string.IsNullOrWhiteSpace(effectiveApiKey) || effectiveApiKey == "••••••••")
        {
            effectiveApiKey = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Ai.ApiKey);
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var (isSuccess, message) = await _aiEngine.TestConnectionAsync(input.Provider, effectiveApiKey ?? string.Empty, input.ModelName ?? string.Empty, input.BaseUrl);
        sw.Stop();

        return new TestAiConnectionResultDto
        {
            Success = isSuccess,
            Message = message,
            LatencyMs = sw.ElapsedMilliseconds
        };
    }

    [Authorize(HelpdeskPermissions.AiAssistant.Use)]
    public async Task<TicketAiSummaryDto> SummarizeTicketAsync(Guid ticketId)
    {
        var ticket = await _ticketRepository.GetAsync(ticketId);
        var comments = await _commentRepository.GetListAsync(c => c.TicketId == ticketId);

        var result = await _aiEngine.SummarizeTicketAsync(ticket, comments);

        ticket.AiSummary = result.FullSummaryText;
        await _ticketRepository.UpdateAsync(ticket);

        return new TicketAiSummaryDto
        {
            Summary = result.FullSummaryText,
            MainIssue = result.KeyIssue,
            CurrentProgress = result.ActionsTaken,
            NextSteps = result.NextStep,
            ModelUsed = "Built-in Smart NLP Engine"
        };
    }

    [Authorize(HelpdeskPermissions.AiAssistant.Use)]
    public async Task<GenerateAiReplyResultDto> GenerateReplyAsync(GenerateAiReplyInputDto input)
    {
        var ticket = await _ticketRepository.GetAsync(input.TicketId);
        var comments = await _commentRepository.GetListAsync(c => c.TicketId == input.TicketId);

        List<KnowledgeArticle>? articles = null;
        if (input.IncludeKnowledgeBase)
        {
            var allArticles = await _articleRepository.GetListAsync(a => a.IsPublished);
            var words = (ticket.Title + " " + ticket.Description)
                .Split(new[] { ' ', ',', '.', ';', '?', '!', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .Select(w => w.ToLower())
                .Distinct()
                .ToList();

            articles = allArticles
                .Where(a => words.Any(w => a.Title.ToLower().Contains(w) || (a.Tags != null && a.Tags.ToLower().Contains(w))))
                .Take(3)
                .ToList();
        }

        var result = await _aiEngine.GenerateSuggestedReplyAsync(ticket, comments, input.Tone, input.UserGuidance, articles);

        return new GenerateAiReplyResultDto
        {
            ReplyText = result.ReplyText,
            RelevantArticles = result.ReferencedArticles,
            ModelUsed = result.ToneExplanation
        };
    }

    [Authorize(HelpdeskPermissions.AiAssistant.Use)]
    public async Task<AnalyzeSentimentResultDto> AnalyzeTicketSentimentAsync(Guid ticketId)
    {
        var ticket = await _ticketRepository.GetAsync(ticketId);
        var comments = await _commentRepository.GetListAsync(c => c.TicketId == ticketId);

        var result = await _aiEngine.AnalyzeSentimentAsync(ticket, comments);

        ticket.AiSentiment = result.Sentiment;
        ticket.AiSentimentReason = result.Explanation;
        await _ticketRepository.UpdateAsync(ticket);

        return new AnalyzeSentimentResultDto
        {
            Sentiment = result.Sentiment,
            Reason = result.Explanation,
            ConfidenceScore = result.SentimentScore / 100.0,
            SuggestedUrgency = result.SuggestedPriority ?? "Bình thường"
        };
    }

    private async Task SetSettingAsync(string name, string value)
    {
        if (CurrentTenant.Id.HasValue)
        {
            await _settingManager.SetForTenantAsync(CurrentTenant.Id.Value, name, value);
        }
        else
        {
            await _settingManager.SetGlobalAsync(name, value);
        }
    }
}
