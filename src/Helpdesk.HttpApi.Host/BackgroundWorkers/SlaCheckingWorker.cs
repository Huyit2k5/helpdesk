using System;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Sla;
using Helpdesk.Tickets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Domain.Repositories;
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
            var slaManager = workerContext.ServiceProvider.GetRequiredService<SlaManager>();
            var asyncExecuter = workerContext.ServiceProvider.GetRequiredService<IAsyncQueryableExecuter>();

            var query = await ticketRepository.GetQueryableAsync();
            var tickets = await asyncExecuter.ToListAsync(
                query.Where(t => t.SlaPolicyId != null && (!t.IsFirstResponseBreached || !t.IsResolutionBreached)));

            foreach (var ticket in tickets)
            {
                bool wasFirstResponseBreached = ticket.IsFirstResponseBreached;
                bool wasResolutionBreached = ticket.IsResolutionBreached;

                await slaManager.CheckTicketBreachesAsync(ticket);

                if (ticket.IsFirstResponseBreached != wasFirstResponseBreached ||
                    ticket.IsResolutionBreached != wasResolutionBreached)
                {
                    await ticketRepository.UpdateAsync(ticket);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred during SLA breach check background work.");
        }
    }
}
