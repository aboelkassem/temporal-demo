using Temporalio.Client;
using TemporalDemo.Worker;
using TemporalDemo.Workflows;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole().SetMinimumLevel(LogLevel.Information);

var connection = await TemporalConnection.ConnectAsync(new TemporalConnectionOptions("localhost:7233"));
builder.Services.AddSingleton<ITemporalClient>(provider => new TemporalClient(connection, new()
{
    LoggerFactory = provider.GetRequiredService<ILoggerFactory>(),
}));

// start temporal workers
builder.Services.AddHostedService<PurchaseWorker>();

var app = builder.Build();

app.MapGet("/", async (ITemporalClient client, string? name) =>
{
    // Start a workflow
    var handle = await client.StartWorkflowAsync(
        (OneClickBuyWorkflow wf) => wf.RunAsync(new("item1", "user1")),
        new(id: "process-order-number-90743818", taskQueue: TasksQueue.Purchase)
        {
            //RetryPolicy = new()
            //{
            //    InitialInterval = TimeSpan.FromSeconds(15), // first try will occur after 15 seconds
            //    BackoffCoefficient = 2, // double the delay after each retry
            //    MaximumInterval = TimeSpan.FromMinutes(1), // up to a maximum delay of 1 minute
            //    MaximumAttempts = 100 // fail the activity after 100 attempts
            //}
        });

    // business logic

    // We can update the purchase if we want
    await handle.SignalAsync(
        (OneClickBuyWorkflow wf) => wf.UpdatePurchaseAsync(new("item2", "user1")));

    // We can cancel it if we want
    //await handle.CancelAsync();

    // We can query its status, even if the workflow is complete
    var currentPurchaseStatus = await handle
                        .QueryAsync((OneClickBuyWorkflow wf) => wf.CurrentStatus());
    Console.WriteLine(currentPurchaseStatus);

    // We can also wait on the result (which for our example is the same as query)
    //status = await handle.GetResultAsync();
    //Console.WriteLine($"Purchase workflow result: {status}");
    
    return currentPurchaseStatus;
});

app.MapGet("/history", () =>
{
    return PurchaseStatusHelper.GetPurchaseStatusList();
});

app.MapGet("/clear", () =>
{
    return PurchaseStatusHelper.Clear();
});

app.Run();