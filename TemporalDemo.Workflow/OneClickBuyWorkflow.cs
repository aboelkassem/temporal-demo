namespace TemporalDemo.Workflows;

using TemporalDemo.Worker;
using Temporalio.Workflows;

public enum PurchaseStatusEnum
{
    Pending,
    Initiated,
    PendingPayment,
    PaymentAccepted,
    PaymentDeclined,
    AvailiableInventory,
    NotAvailiableInventory,
    Fulfilled,
    PendingShipping,
    Shipped,
    Cancelled,
    Completed
}

[Workflow]
public class OneClickBuyWorkflow
{
    public static readonly OneClickBuyWorkflow Ref = WorkflowRefs.Create<OneClickBuyWorkflow>();

    private PurchaseStatusEnum currentStatus = PurchaseStatusEnum.Pending;
    private Purchase? currentPurchase;

    [WorkflowRun]
    public async Task<PurchaseStatusEnum> RunAsync(Purchase purchase)
    {
        PurchaseStatusHelper.SetPurchaseStatus(currentStatus.ToString());
        currentPurchase = purchase;
        
        // current status = pending
        await Workflow.ExecuteActivityAsync(
            PurchaseActivities.Ref.StartOrderProcess,
            currentPurchase!,
            new() 
            { 
                StartToCloseTimeout = TimeSpan.FromSeconds(90), // schedule a retry if the Activity function doesn't return within 90 seconds
                RetryPolicy = new()
                    {
                        InitialInterval = TimeSpan.FromSeconds(15), // first try will occur after 15 seconds
                        BackoffCoefficient = 2, // double the delay after each retry
                        MaximumInterval = TimeSpan.FromMinutes(1), // up to a maximum delay of 1 minute
                        MaximumAttempts = 100 // fail the activity after 100 attempts
                    }
            });

        // current status = CheckInventory
        var isExists = await Workflow.ExecuteActivityAsync(PurchaseActivities.Ref.CheckInventory,
            true, // Just for Demo: parameter to pass to the activity for if exists or not
            new()
            {
                StartToCloseTimeout = TimeSpan.FromSeconds(90),
            });

        if (!isExists)
        {
            // cancel the order
            currentStatus = PurchaseStatusEnum.Cancelled;
            return currentStatus;
        }

        // current status = PendingPayment
        currentStatus = PurchaseStatusEnum.PendingPayment;
        PurchaseStatusHelper.SetPurchaseStatus(currentStatus.ToString());
        await Workflow.ExecuteActivityAsync(PurchaseActivities.Ref.CheckPayment, new()
        {
            StartToCloseTimeout = TimeSpan.FromSeconds(90),
        });

        // FulfillOrder
        await Workflow.ExecuteActivityAsync(PurchaseActivities.Ref.FulfillOrder, new()
        {
            StartToCloseTimeout = TimeSpan.FromSeconds(90),
        });

        // current status = PendingShipping
        currentStatus = PurchaseStatusEnum.PendingShipping;
        PurchaseStatusHelper.SetPurchaseStatus(currentStatus.ToString());
        await Workflow.ExecuteActivityAsync(PurchaseActivities.Ref.ShipOrder, new()
        {
            StartToCloseTimeout = TimeSpan.FromSeconds(90),
        });

        currentStatus = PurchaseStatusEnum.Completed;
        PurchaseStatusHelper.SetPurchaseStatus(currentStatus.ToString());

        return currentStatus;
    }

    [WorkflowSignal]
    public async Task UpdatePurchaseAsync(Purchase purchase) => currentPurchase = purchase;

    [WorkflowQuery]
    public string CurrentStatus() => $"Current purchase status is {currentStatus}";

    [WorkflowQuery]
    public Purchase GetCurrentPurchaseData() => currentPurchase;
}