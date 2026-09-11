using System;
using System.Threading.Tasks;
using Helpdesk.Automations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace Helpdesk.BackgroundWorkers;

/// <summary>
/// Background Worker chạy định kỳ để quét và thực thi các quy tắc tự động hóa theo thời gian (ScheduledTime).
/// Ví dụ: Tự động đóng vé nhàn rỗi sau 48h không có phản hồi, cảnh báo vé tồn đọng...
/// </summary>
public class AutomationPeriodicWorker : AsyncPeriodicBackgroundWorkerBase
{
    public AutomationPeriodicWorker(
        AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        Timer.Period = 60000; // Quét mỗi 60 giây (1 phút)
    }

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        try
        {
            var ruleEngine = workerContext.ServiceProvider.GetRequiredService<IAutomationRuleEngine>();
            var affectedCount = await ruleEngine.ExecuteScheduledRulesAsync();
            if (affectedCount > 0)
            {
                Logger.LogInformation("AutomationPeriodicWorker: Đã xử lý tự động thành công cho {Count} sự vụ.", affectedCount);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Lỗi khi chạy AutomationPeriodicWorker.");
        }
    }
}
