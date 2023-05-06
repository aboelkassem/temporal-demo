using Temporalio.Client;
using TemporalDemo.Worker;
using TemporalDemo.Workflows;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Setup console logging
builder.Logging.AddSimpleConsole().SetMinimumLevel(LogLevel.Information);

// Set a singleton for the client _task_. Errors will not happen here, only when
// the await is performed.
builder.Services.AddSingleton(ctx =>
    // TODO(cretz): It is not great practice to pass around tasks to be awaited
    // on separately (VSTHRD003). We may prefer a direct DI extension, see
    // https://github.com/temporalio/sdk-dotnet/issues/46.
    TemporalClient.ConnectAsync(new()
    {
        TargetHost = "localhost:7233",
        LoggerFactory = ctx.GetRequiredService<ILoggerFactory>(),
    }));

// start temporal workers
builder.Services.AddHostedService<PurchaseWorker>();

var app = builder.Build();

app.MapGet("/", async (Task<TemporalClient> clientTask, string? name) =>
{
    
    var client = await clientTask;

    // Start a workflow
    var handle = await client.StartWorkflowAsync(
        OneClickBuyWorkflow.Ref.RunAsync,
        new Purchase(ItemID: "item1", UserID: "user1"),
        new(id: "purchase-workflow", taskQueue: TasksQueue.Purchase)
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
        OneClickBuyWorkflow.Ref.UpdatePurchaseAsync,
        new Purchase(ItemID: "item2", UserID: "user1"));

    // We can cancel it if we want
    //await handle.CancelAsync();

    // We can query its status, even if the workflow is complete
    var currentPurshaseStatus = await handle.QueryAsync(OneClickBuyWorkflow.Ref.CurrentStatus);
    Console.WriteLine(currentPurshaseStatus);

    // We can also wait on the result (which for our example is the same as query)
    //status = await handle.GetResultAsync();
    //Console.WriteLine($"Purchase workflow result: {status}");
    
    return currentPurshaseStatus;
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