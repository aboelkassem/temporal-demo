using Temporalio.Client;
using Temporalio.Worker;
using TemporalDemo.Workflows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TemporalDemo.Worker;
public sealed class PurchaseWorker : BackgroundService
{
    private readonly ILoggerFactory _loggerFactory;

    public PurchaseWorker(ILoggerFactory loggerFactory)
    {
        this._loggerFactory = loggerFactory;
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
                LoggerFactory = _loggerFactory,
            }),
            new TemporalWorkerOptions(taskQueue: TasksQueue.Purchase)
                .AddActivity(activities.StartOrderProcess)
                .AddActivity(activities.CheckPayment)
                .AddActivity(activities.CheckInventory)
                .AddActivity(activities.FulfillOrder)
                .AddActivity(activities.ShipOrder)
                //.AddAllActivities(typeof(OneClickBuyWorkflow))
                .AddWorkflow<OneClickBuyWorkflow>()
            );
        
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