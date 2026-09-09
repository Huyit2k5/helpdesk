using System;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Notifications;
using Helpdesk.Sla;
using Helpdesk.Tickets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Emailing;
using Volo.Abp.Identity;
using Volo.Abp.Linq;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace Helpdesk.BackgroundWorkers;

public class SlaCheckingWorker : AsyncPeriodicBackgroundWorkerBase
{
    public SlaCheckingWorker(
        AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        Timer.Period = 60000; // Run every 60 seconds
    }

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        try
        {
            var ticketRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<Ticket, Guid>>();
            var userRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<IdentityUser, Guid>>();
            var slaManager = workerContext.ServiceProvider.GetRequiredService<SlaManager>();
            var notificationManager = workerContext.ServiceProvider.GetRequiredService<NotificationManager>();
            var emailSender = workerContext.ServiceProvider.GetRequiredService<IEmailSender>();
            var asyncExecuter = workerContext.ServiceProvider.GetRequiredService<IAsyncQueryableExecuter>();

            var query = await ticketRepository.GetQueryableAsync();
            var tickets = await asyncExecuter.ToListAsync(
                query.Where(t => t.SlaPolicyId != null && (!t.IsFirstResponseBreached || !t.IsResolutionBreached)));

            foreach (var ticket in tickets)
            {
                bool wasFirstResponseBreached = ticket.IsFirstResponseBreached;
                bool wasResolutionBreached = ticket.IsResolutionBreached;

                await slaManager.CheckTicketBreachesAsync(ticket);

                var newlyBreached = (ticket.IsFirstResponseBreached && !wasFirstResponseBreached) ||
                                     (ticket.IsResolutionBreached && !wasResolutionBreached);

                if (ticket.IsFirstResponseBreached != wasFirstResponseBreached ||
                    ticket.IsResolutionBreached != wasResolutionBreached)
                {
                    await ticketRepository.UpdateAsync(ticket);
                }

                if (newlyBreached && ticket.AssigneeId.HasValue)
                {
                    await notificationManager.CreateAsync(
                        ticket.AssigneeId.Value,
                        NotificationType.SlaBreached,
                        "Vé vi phạm cam kết SLA",
                        $"Vé {ticket.TicketNumber} \"{ticket.Title}\" đã vi phạm cam kết thời gian xử lý (SLA).",
                        ticket.Id
                    );

                    var assigneeUser = await userRepository.FindAsync(ticket.AssigneeId.Value);
                    if (!string.IsNullOrWhiteSpace(assigneeUser?.Email))
                    {
                        await emailSender.SendAsync(
                            assigneeUser.Email,
                            $"[Helpdesk] CẢNH BÁO: Vé {ticket.TicketNumber} vi phạm SLA",
                            $"Vé \"{ticket.Title}\" bạn đang phụ trách vừa vi phạm cam kết thời gian xử lý (SLA). Vui lòng kiểm tra ngay."
                        );
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred during SLA breach check background work.");
        }
    }
}
