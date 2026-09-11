using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Helpdesk.KnowledgeBase;
using Helpdesk.Settings;
using Helpdesk.Tickets;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Settings;

namespace Helpdesk.Ai;

public class AiAssistantEngine : IAiAssistantEngine, ITransientDependency
{
    private readonly ISettingProvider _settingProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiAssistantEngine> _logger;

    public AiAssistantEngine(
        ISettingProvider settingProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<AiAssistantEngine> logger)
    {
        _settingProvider = settingProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<TicketSummaryResult> SummarizeTicketAsync(Ticket ticket, List<TicketComment> comments)
    {
        var isEnabled = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Ai.IsEnabled);
        var providerStr = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Ai.Provider) ?? "BuiltInOffline";
        Enum.TryParse<AiProviderType>(providerStr, out var provider);

        var commentsText = string.Join("\n", comments.OrderBy(c => c.CreationTime).Select(c =>
            $"[{(c.IsInternal ? "Ghi chú nội bộ" : "Phản hồi")}] {c.AuthorName ?? "Thành viên"}: {c.Content}"));

        var prompt = $@"Bạn là trợ lý AI chuyên gia hỗ trợ kỹ thuật IT Helpdesk. Hãy tóm tắt sự vụ sau đây theo 3 mục rõ ràng bằng tiếng Việt:
1. VẤN ĐỀ CHÍNH: (1-2 câu tóm lược nguyên nhân/sự cố gốc của khách hàng)
2. TIẾN TRÌNH ĐÃ LÀM: (Tóm tắt những giải pháp/bước kiểm tra mà kỹ thuật viên hoặc khách hàng đã trao đổi)
3. BƯỚC TIẾP THEO: (Hành động cụ thể cần làm tiếp theo và ai là người phụ trách: Khách hàng hay IT Support)

---
THÔNG TIN VÉ:
- Mã vé: {ticket.TicketNumber}
- Tiêu đề: {ticket.Title}
- Mô tả ban đầu: {ticket.Description}
- Trạng thái hiện tại: {ticket.StatusId}
- Người yêu cầu: {ticket.RequesterName}

LỊCH SỬ TRAO ĐỔI & GHI CHÚ:
{(string.IsNullOrWhiteSpace(commentsText) ? "(Chưa có bình luận trao đổi nào)" : commentsText)}
---
Hãy trả lời đúng cấu trúc 3 phần với các tiêu đề: '1. VẤN ĐỀ CHÍNH:', '2. TIẾN TRÌNH ĐÃ LÀM:', '3. BƯỚC TIẾP THEO:'.";

        if (isEnabled && provider != AiProviderType.BuiltInOffline)
        {
            try
            {
                var aiResponse = await CallLlmApiAsync(provider, prompt);
                if (!string.IsNullOrWhiteSpace(aiResponse))
                {
                    return ParseSummaryResponse(aiResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi gọi API AI ngoài ({Provider}), tự động chuyển về Built-in Smart NLP Engine.", provider);
            }
        }

        return GenerateOfflineSummary(ticket, comments);
    }

    public async Task<SuggestedReplyResult> GenerateSuggestedReplyAsync(
        Ticket ticket,
        List<TicketComment> comments,
        AiReplyTone tone,
        string? customInstruction = null,
        List<KnowledgeArticle>? relevantArticles = null)
    {
        var isEnabled = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Ai.IsEnabled);
        var providerStr = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Ai.Provider) ?? "BuiltInOffline";
        Enum.TryParse<AiProviderType>(providerStr, out var provider);

        var articleContext = string.Empty;
        var referencedArticleTitles = new List<string>();

        if (relevantArticles != null && relevantArticles.Any())
        {
            referencedArticleTitles = relevantArticles.Take(2).Select(a => a.Title).ToList();
            articleContext = "TÀI LIỆU CƠ SỞ TRI THỨC THAM KHẢO:\n" +
                string.Join("\n\n", relevantArticles.Take(2).Select(a => $"Tài liệu: {a.Title}\nNội dung: {a.Summary}\n{a.Content}"));
        }

        var tonePrompt = tone switch
        {
            AiReplyTone.TechnicalGuide => "Soạn thảo văn bản phản hồi hướng dẫn các bước kỹ thuật chi tiết, đánh số 1, 2, 3 rõ ràng, dễ hiểu cho người dùng không chuyên.",
            AiReplyTone.RequestMoreInfo => "Soạn thảo email lịch sự, nhã nhặn đề nghị khách hàng bổ sung thêm thông tin (như ảnh chụp lỗi, thời điểm phát sinh, UltraViewer/AnyDesk ID).",
            AiReplyTone.AcknowledgedInvestigating => "Soạn thảo văn bản xác nhận sự cố đã được tiếp nhận khẩn trương, đội ngũ kỹ thuật đang tập trung kiểm tra và sẽ phản hồi sớm.",
            AiReplyTone.CustomPrompt => $"Soạn thảo câu trả lời theo chỉ đạo riêng của kỹ thuật viên: {customInstruction}",
            _ => "Soạn thảo câu trả lời hỗ trợ khách hàng chuyên nghiệp, đồng cảm và rõ ràng."
        };

        var prompt = $@"Bạn là nhân viên hỗ trợ kỹ thuật IT Helpdesk chuyên nghiệp, nhiệt tình và lịch sự.
Nhiệm vụ: {tonePrompt}

YÊU CẦU ĐỊNH DẠNG:
- Mở đầu bằng lời chào thân thiện (ví dụ: 'Chào bạn {ticket.RequesterName},' hoặc 'Kính gửi Anh/Chị,').
- Giọng văn lịch sự, tích cực, đồng cảm với khó khăn của người dùng.
- Trình bày giải pháp mạch lạc, dùng gạch đầu dòng hoặc số thứ tự nếu có nhiều bước.
- Kết thúc bằng lời chúc và thông tin liên hệ sẵn sàng hỗ trợ tiếp.

THÔNG TIN VÉ:
- Mã vé: {ticket.TicketNumber}
- Tiêu đề sự cố: {ticket.Title}
- Mô tả: {ticket.Description}
{(string.IsNullOrWhiteSpace(articleContext) ? string.Empty : "\n" + articleContext)}

{(string.IsNullOrWhiteSpace(customInstruction) ? string.Empty : $"LƯU Ý THÊM: {customInstruction}\n")}
Chỉ trả về nội dung email/tin nhắn phản hồi hoàn chỉnh, không kèm chú thích thừa.";

        if (isEnabled && provider != AiProviderType.BuiltInOffline)
        {
            try
            {
                var aiResponse = await CallLlmApiAsync(provider, prompt);
                if (!string.IsNullOrWhiteSpace(aiResponse))
                {
                    return new SuggestedReplyResult
                    {
                        ReplyText = aiResponse.Trim(),
                        ReferencedArticles = referencedArticleTitles,
                        ToneExplanation = GetToneDescription(tone)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi gọi API AI sinh câu trả lời ({Provider}), dùng Built-in Smart Generator.", provider);
            }
        }

        return GenerateOfflineReply(ticket, tone, customInstruction, relevantArticles);
    }

    public async Task<SentimentAnalysisResult> AnalyzeSentimentAsync(Ticket ticket, List<TicketComment>? comments = null)
    {
        var textToAnalyze = $"{ticket.Title}\n{ticket.Description}";
        if (comments != null && comments.Any())
        {
            var customerComments = comments.Where(c => !c.IsInternal).OrderByDescending(c => c.CreationTime).Take(2);
            foreach (var c in customerComments)
            {
                textToAnalyze += $"\n{c.Content}";
            }
        }

        var isEnabled = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Ai.IsEnabled);
        var providerStr = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Ai.Provider) ?? "BuiltInOffline";
        Enum.TryParse<AiProviderType>(providerStr, out var provider);

        if (isEnabled && provider != AiProviderType.BuiltInOffline)
        {
            var prompt = $@"Hãy phân tích sắc thái cảm xúc của khách hàng trong yêu cầu IT sau đây:
""{textToAnalyze}""

Trả lời theo định dạng JSON duy nhất như sau:
{{
  ""sentiment"": ""Positive"" | ""Neutral"" | ""Frustrated"" | ""UrgentCrisis"",
  ""score"": 1-100,
  ""explanation"": ""Giải thích ngắn gọn lý do đánh giá sắc thái này trong 1 câu"",
  ""suggestedPriority"": ""Low"" | ""Medium"" | ""High"" | ""Critical""
}}";
            try
            {
                var aiResponse = await CallLlmApiAsync(provider, prompt);
                var parsed = ParseSentimentJson(aiResponse);
                if (parsed != null) return parsed;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi gọi API AI phân tích cảm xúc, chuyển sang bộ phân tích nội bộ.");
            }
        }

        return AnalyzeOfflineSentiment(textToAnalyze);
    }

    public async Task<(bool isSuccess, string message)> TestConnectionAsync(
        AiProviderType provider,
        string apiKey,
        string modelName,
        string? baseUrl)
    {
        if (provider == AiProviderType.BuiltInOffline)
        {
            return (true, "Bộ xử lý thông minh tích hợp sẵn (Built-in Offline Smart NLP) luôn hoạt động ổn định và sẵn sàng phục vụ mà không cần kết nối ngoài!");
        }

        if (string.IsNullOrWhiteSpace(apiKey) && provider != AiProviderType.LocalOllama)
        {
            return (false, "Vui lòng nhập API Key hợp lệ cho nhà cung cấp đã chọn.");
        }

        try
        {
            var testPrompt = "Hãy chào bằng một câu ngắn gọn bằng tiếng Việt: 'Xin chào! Hệ thống AI Helpdesk đã kết nối thành công.'";
            var response = await CallExternalProviderAsync(provider, apiKey, modelName, baseUrl, testPrompt);
            if (!string.IsNullOrWhiteSpace(response))
            {
                return (true, $"Kết nối thành công tới {provider} ({modelName})! Phản hồi từ AI: \"{response.Trim()}\"");
            }
            return (false, $"Không nhận được phản hồi từ dịch vụ {provider}.");
        }
        catch (Exception ex)
        {
            return (false, $"Kết nối thất bại: {ex.Message}");
        }
    }

    private async Task<string> CallLlmApiAsync(AiProviderType provider, string prompt)
    {
        var apiKey = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Ai.ApiKey) ?? string.Empty;
        var modelName = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Ai.ModelName) ?? "gemini-1.5-flash";
        var baseUrl = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Ai.BaseUrl);

        return await CallExternalProviderAsync(provider, apiKey, modelName, baseUrl, prompt);
    }

    private async Task<string> CallExternalProviderAsync(
        AiProviderType provider,
        string apiKey,
        string modelName,
        string? baseUrl,
        string prompt)
    {
        var client = _httpClientFactory.CreateClient("AiAssistantClient");
        client.Timeout = TimeSpan.FromSeconds(30);

        if (provider == AiProviderType.GoogleGemini)
        {
            var model = string.IsNullOrWhiteSpace(modelName) ? "gemini-1.5-flash" : modelName.Trim();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey.Trim()}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 1024
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await client.PostAsync(url, content);
            var resStr = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                throw new Exception($"Google Gemini API trả về mã lỗi {res.StatusCode}: {resStr}");
            }

            var jsonNode = JsonNode.Parse(resStr);
            var text = jsonNode?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
            return text ?? string.Empty;
        }
        else if (provider == AiProviderType.OpenAI)
        {
            var model = string.IsNullOrWhiteSpace(modelName) ? "gpt-4o-mini" : modelName.Trim();
            var url = "https://api.openai.com/v1/chat/completions";

            var payload = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.3
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var res = await client.SendAsync(request);
            var resStr = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                throw new Exception($"OpenAI API trả về mã lỗi {res.StatusCode}: {resStr}");
            }

            var jsonNode = JsonNode.Parse(resStr);
            var text = jsonNode?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();
            return text ?? string.Empty;
        }
        else if (provider == AiProviderType.LocalOllama)
        {
            var endpoint = string.IsNullOrWhiteSpace(baseUrl) ? "http://localhost:11434" : baseUrl.TrimEnd('/');
            var url = $"{endpoint}/api/generate";
            var model = string.IsNullOrWhiteSpace(modelName) ? "llama3" : modelName.Trim();

            var payload = new
            {
                model = model,
                prompt = prompt,
                stream = false
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await client.PostAsync(url, content);
            var resStr = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                throw new Exception($"Ollama API trả về mã lỗi {res.StatusCode}: {resStr}");
            }

            var jsonNode = JsonNode.Parse(resStr);
            var text = jsonNode?["response"]?.GetValue<string>();
            return text ?? string.Empty;
        }

        return string.Empty;
    }

    private static TicketSummaryResult ParseSummaryResponse(string raw)
    {
        var result = new TicketSummaryResult { FullSummaryText = raw };

        var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var currentSection = 0;
        var keyIssueSb = new StringBuilder();
        var actionsSb = new StringBuilder();
        var nextStepSb = new StringBuilder();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Contains("VẤN ĐỀ CHÍNH", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("1."))
            {
                currentSection = 1;
                var content = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim();
                if (!string.IsNullOrWhiteSpace(content)) keyIssueSb.AppendLine(content);
                continue;
            }
            if (trimmed.Contains("TIẾN TRÌNH", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("2."))
            {
                currentSection = 2;
                var content = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim();
                if (!string.IsNullOrWhiteSpace(content)) actionsSb.AppendLine(content);
                continue;
            }
            if (trimmed.Contains("BƯỚC TIẾP THEO", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("3."))
            {
                currentSection = 3;
                var content = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim();
                if (!string.IsNullOrWhiteSpace(content)) nextStepSb.AppendLine(content);
                continue;
            }

            if (currentSection == 1) keyIssueSb.AppendLine(trimmed);
            else if (currentSection == 2) actionsSb.AppendLine(trimmed);
            else if (currentSection == 3) nextStepSb.AppendLine(trimmed);
        }

        result.KeyIssue = keyIssueSb.ToString().Trim();
        result.ActionsTaken = actionsSb.ToString().Trim();
        result.NextStep = nextStepSb.ToString().Trim();

        if (string.IsNullOrWhiteSpace(result.KeyIssue)) result.KeyIssue = raw.Length > 200 ? raw.Substring(0, 200) + "..." : raw;
        if (string.IsNullOrWhiteSpace(result.ActionsTaken)) result.ActionsTaken = "Các bên đang tích cực trao đổi và cập nhật trạng thái xử lý sự cố.";
        if (string.IsNullOrWhiteSpace(result.NextStep)) result.NextStep = "Kỹ thuật viên chuyên trách tiếp tục liên hệ khách hàng để kiểm tra dứt điểm.";

        return result;
    }

    private static TicketSummaryResult GenerateOfflineSummary(Ticket ticket, List<TicketComment> comments)
    {
        var keyIssue = $"{ticket.Title}. {(!string.IsNullOrWhiteSpace(ticket.Description) ? ticket.Description : "Không có mô tả bổ sung.")}";
        if (keyIssue.Length > 250) keyIssue = keyIssue.Substring(0, 247) + "...";

        var actionsTaken = "Yêu cầu đã được tiếp nhận vào hệ thống.";
        if (comments.Any())
        {
            var countPublic = comments.Count(c => !c.IsInternal);
            var countInternal = comments.Count(c => c.IsInternal);
            actionsTaken = $"Đã có {comments.Count} trao đổi ghi nhận ({countPublic} phản hồi khách hàng, {countInternal} ghi chú kỹ thuật nội bộ). Bình luận gần nhất từ: {comments.Last().AuthorName ?? "Kỹ thuật viên"}.";
        }

        var nextStep = "Kỹ thuật viên phụ trách tiếp nhận và phân tích nguyên nhân để đưa ra giải pháp khắc phục.";
        if (ticket.ResolvedAt.HasValue)
        {
            nextStep = "Sự cố đã được giải quyết. Chờ khách hàng nghiệm thu hoặc đóng vé tự động.";
        }
        else if (ticket.ClosedAt.HasValue)
        {
            nextStep = "Sự vụ đã hoàn tất và lưu trữ vào hồ sơ.";
        }

        var fullText = $"1. VẤN ĐỀ CHÍNH:\n{keyIssue}\n\n2. TIẾN TRÌNH ĐÃ LÀM:\n{actionsTaken}\n\n3. BƯỚC TIẾP THEO:\n{nextStep}";

        return new TicketSummaryResult
        {
            KeyIssue = keyIssue,
            ActionsTaken = actionsTaken,
            NextStep = nextStep,
            FullSummaryText = fullText
        };
    }

    private static SuggestedReplyResult GenerateOfflineReply(
        Ticket ticket,
        AiReplyTone tone,
        string? customInstruction,
        List<KnowledgeArticle>? relevantArticles)
    {
        var requesterName = string.IsNullOrWhiteSpace(ticket.RequesterName) ? "Quý khách" : ticket.RequesterName;
        var referencedTitles = new List<string>();

        string replyText;

        switch (tone)
        {
            case AiReplyTone.TechnicalGuide:
                var guideContent = "1. Kiểm tra lại kết nối vật lý và cáp nguồn/mạng của thiết bị.\n2. Thử khởi động lại ứng dụng hoặc thiết bị để làm mới phiên làm việc.\n3. Xóa cache trình duyệt hoặc thử đăng nhập lại tài khoản.";
                if (relevantArticles != null && relevantArticles.Any())
                {
                    var art = relevantArticles.First();
                    referencedTitles.Add(art.Title);
                    guideContent = $"Dựa trên tài liệu hướng dẫn '{art.Title}', bạn vui lòng thực hiện theo các bước sau:\n{art.Summary}\n\nChi tiết bài viết: {art.Title}";
                }

                replyText = $"Chào bạn {requesterName},\n\nBộ phận IT Support đã tiếp nhận yêu cầu về vấn đề: \"{ticket.Title}\".\n\nĐể khắc phục sự cố này, bạn vui lòng thực hiện theo hướng dẫn sau:\n{guideContent}\n\nNếu sự cố vẫn chưa được xử lý, bạn hãy phản hồi lại tin nhắn này để kỹ thuật viên hỗ trợ từ xa qua UltraViewer nhé.\n\nTrân trọng,\nĐội Ngũ IT Helpdesk";
                break;

            case AiReplyTone.RequestMoreInfo:
                replyText = $"Chào bạn {requesterName},\n\nĐể bộ phận kỹ thuật có đầy đủ thông tin hỗ trợ xử lý dứt điểm sự cố \"{ticket.Title}\", bạn vui lòng cung cấp thêm một số thông tin sau giúp bên mình nhé:\n- Ảnh chụp toàn màn hình thông báo lỗi (nếu có).\n- Thời điểm phát sinh lỗi và tần suất xảy ra.\n- ID và Mật khẩu UltraViewer / TeamViewer (nếu bạn thuận tiện để kỹ thuật viên kết nối trực tiếp).\n\nCảm ơn bạn đã phối hợp cùng bộ phận hỗ trợ!\n\nTrân trọng,\nĐội Ngũ IT Helpdesk";
                break;

            case AiReplyTone.AcknowledgedInvestigating:
                replyText = $"Chào bạn {requesterName},\n\nBộ phận IT Support xác nhận đã tiếp nhận yêu cầu hỗ trợ mã số #{ticket.TicketNumber} của bạn.\n\nHiện tại sự cố đang được kỹ sư chuyên trách kiểm tra và phối hợp khắc phục khẩn trương. Bộ phận sẽ cập nhật ngay khi có kết quả hoặc khi hoàn tất xử lý.\n\nCảm ơn bạn đã kiên nhẫn chờ đợi!\n\nTrân trọng,\nĐội Ngũ IT Helpdesk";
                break;

            case AiReplyTone.CustomPrompt:
                replyText = $"Chào bạn {requesterName},\n\nLiên quan đến yêu cầu hỗ trợ \"{ticket.Title}\", bộ phận kỹ thuật xin được thông tin như sau:\n\n{customInstruction ?? "Chúng tôi đang tiến hành các bước kiểm tra chuyên sâu để xử lý sự cố cho bạn."}\n\nNếu bạn cần hỗ trợ thêm thông tin nào khác, xin đừng ngần ngại phản hồi lại nhé.\n\nTrân trọng,\nĐội Ngũ IT Helpdesk";
                break;

            default:
                replyText = $"Chào bạn {requesterName},\n\nCảm ơn bạn đã liên hệ bộ phận IT Support. Yêu cầu của bạn đang được chúng tôi xử lý theo quy chuẩn cam kết dịch vụ.\n\nTrân trọng,\nĐội Ngũ IT Helpdesk";
                break;
        }

        return new SuggestedReplyResult
        {
            ReplyText = replyText,
            ReferencedArticles = referencedTitles,
            ToneExplanation = GetToneDescription(tone)
        };
    }

    private static SentimentAnalysisResult AnalyzeOfflineSentiment(string text)
    {
        var lower = text.ToLowerInvariant();

        var crisisWords = new[] { "sập", "khẩn cấp", "cháy", "đình trệ", "gấp", "critical", "urgent", "disaster", "thiệt hại nặng", "liệt", "không thể làm việc" };
        var frustratedWords = new[] { "bực", "tệ", "chậm", "chán", "mãi không", "lỗi hoài", "quá lâu", "thất vọng", "bất tiện", "bực mình", "bức xúc", "kém", "frustrated", "annoyed" };
        var positiveWords = new[] { "cảm ơn", "tốt", "tuyệt vời", "nhanh", "hài lòng", "ổn rồi", "thanks", "great", "excellent" };

        if (crisisWords.Any(w => lower.Contains(w)))
        {
            return new SentimentAnalysisResult
            {
                Sentiment = CustomerSentiment.UrgentCrisis,
                SentimentScore = 95,
                SentimentLabel = "Khẩn cấp / Bức xúc nghiêm trọng",
                Explanation = "Phát hiện các từ khóa sự cố khẩn cấp, nguy cơ gián đoạn hoạt động nghiêm trọng.",
                SuggestedPriority = "Critical"
            };
        }

        if (frustratedWords.Any(w => lower.Contains(w)))
        {
            return new SentimentAnalysisResult
            {
                Sentiment = CustomerSentiment.Frustrated,
                SentimentScore = 75,
                SentimentLabel = "Khó chịu / Thất vọng",
                Explanation = "Người dùng thể hiện tâm lý bực bội, khó chịu về thời gian hoặc sự cố lặp lại.",
                SuggestedPriority = "High"
            };
        }

        if (positiveWords.Any(w => lower.Contains(w)))
        {
            return new SentimentAnalysisResult
            {
                Sentiment = CustomerSentiment.Positive,
                SentimentScore = 20,
                SentimentLabel = "Tích cực / Hài lòng",
                Explanation = "Người dùng có thái độ hợp tác tích cực, bày tỏ lời cảm ơn.",
                SuggestedPriority = "Low"
            };
        }

        return new SentimentAnalysisResult
        {
            Sentiment = CustomerSentiment.Neutral,
            SentimentScore = 50,
            SentimentLabel = "Bình thường / Trung tính",
            Explanation = "Nội dung mô tả sự việc trung tính, không chứa yếu tố cảm xúc cực đoan.",
            SuggestedPriority = "Medium"
        };
    }

    private static SentimentAnalysisResult? ParseSentimentJson(string raw)
    {
        try
        {
            var startIndex = raw.IndexOf('{');
            var endIndex = raw.LastIndexOf('}');
            if (startIndex >= 0 && endIndex > startIndex)
            {
                var jsonStr = raw.Substring(startIndex, endIndex - startIndex + 1);
                var doc = JsonDocument.Parse(jsonStr);
                var root = doc.RootElement;

                var sentimentStr = root.GetProperty("sentiment").GetString();
                Enum.TryParse<CustomerSentiment>(sentimentStr, true, out var sentiment);

                var score = root.TryGetProperty("score", out var sc) ? sc.GetInt32() : 50;
                var exp = root.TryGetProperty("explanation", out var ex) ? ex.GetString() : string.Empty;
                var prio = root.TryGetProperty("suggestedPriority", out var pr) ? pr.GetString() : null;

                var label = sentiment switch
                {
                    CustomerSentiment.UrgentCrisis => "Khẩn cấp / Bức xúc nghiêm trọng",
                    CustomerSentiment.Frustrated => "Khó chịu / Thất vọng",
                    CustomerSentiment.Positive => "Tích cực / Hài lòng",
                    _ => "Bình thường / Trung tính"
                };

                return new SentimentAnalysisResult
                {
                    Sentiment = sentiment,
                    SentimentScore = score,
                    SentimentLabel = label,
                    Explanation = exp ?? "Đánh giá bởi mô hình AI.",
                    SuggestedPriority = prio
                };
            }
        }
        catch
        {
            // fallback
        }
        return null;
    }

    private static string GetToneDescription(AiReplyTone tone) => tone switch
    {
        AiReplyTone.TechnicalGuide => "Hướng dẫn kỹ thuật từng bước",
        AiReplyTone.RequestMoreInfo => "Đề nghị bổ sung thông tin lịch sự",
        AiReplyTone.AcknowledgedInvestigating => "Xác nhận tiếp nhận & đang điều tra",
        AiReplyTone.CustomPrompt => "Theo chỉ đạo riêng của kỹ thuật viên",
        _ => "Chuẩn hóa chuyên nghiệp"
    };
}
