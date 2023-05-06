using Temporalio.Client;
using Temporalio.Worker;
using TemporalDemo.Workflows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TemporalDemo.Worker;
public sealed class PurchaseWorker : BackgroundService
{
    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger<PurchaseWorker> logger;

    public PurchaseWorker(ILoggerFactory loggerFactory, ILogger<PurchaseWorker> logger)
    {
        this.loggerFactory = loggerFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Create an activity instance since we have instance activities. If we had
        // all static activities, we could just reference those directly.
        var activities = new PurchaseActivities();
        
        using var worker = new TemporalWorker(
            await TemporalClient.ConnectAsync(new()
            {
                TargetHost = "localhost:7233",
                LoggerFactory = loggerFactory,
            }),
            new(taskQueue: TasksQueue.Purchase)
            {
                Activities = { 
                    activities.StartOrderProcess, 
                    activities.CheckPayment, 
                    activities.CheckInventory, 
                    activities.FulfillOrder, 
                    activities.ShipOrder },
                
                Workflows = { typeof(OneClickBuyWorkflow) },
            });
        
        // Run worker until cancelled
        Console.WriteLine("Running worker");
        try
        {
            await worker.ExecuteAsync(stoppingToken);
        }
        catch (OperationCanceledException ex)
        {
            Console.WriteLine("Worker cancelled");
        }
    }
}