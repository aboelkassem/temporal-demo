using TemporalDemo.Workflows;
using Temporalio.Activities;
using Temporalio.Exceptions;

namespace TemporalDemo.Worker;

public record Purchase(string ItemID, string UserID);

public class PurchaseActivities
{
    // See "Why the 'Ref' Pattern" below for an explanation of this
    public static readonly PurchaseActivities Ref = ActivityRefs.Create<PurchaseActivities>();

    public int Attempts { get; set; } = 0;

    [Activity]
    public async Task StartOrderProcess(Purchase purchase)
    {
        PurchaseStatusHelper.SetPurchaseStatus(PurchaseStatusEnum.Initiated.ToString());
        Console.WriteLine("Order initiated");
    }

    [Activity]
    public async Task CheckPayment()
    {
        if (Attempts >= 3)
        {
            PurchaseStatusHelper.SetPurchaseStatus(PurchaseStatusEnum.PaymentAccepted.ToString());
            // success the request
            Console.WriteLine("Payment successful");
        }
        else
        {
            // Throw an exception
            Attempts += 1;
            await Task.Delay(1000);
            PurchaseStatusHelper.SetPurchaseStatus(PurchaseStatusEnum.PaymentDeclined.ToString());
            throw new ApplicationFailureException($"Payment failed in attempt {Attempts}", nonRetryable: false);
        }
    }

    [Activity]
    public async Task<bool> CheckInventory(bool isExist)
    {
        if (isExist)
        {
            Console.WriteLine("Product is exits");
            PurchaseStatusHelper.SetPurchaseStatus(PurchaseStatusEnum.AvailiableInventory.ToString());
            return true;
        }
        else
        {
            Console.WriteLine("Product not exits");
            PurchaseStatusHelper.SetPurchaseStatus(PurchaseStatusEnum.NotAvailiableInventory.ToString());
            return false;
        }
    }

    [Activity]
    public async Task FulfillOrder()
    {
        // success the request
        PurchaseStatusHelper.SetPurchaseStatus(PurchaseStatusEnum.Fulfilled.ToString());
        Console.WriteLine("Order created");
    }

    [Activity]
    public async Task ShipOrder()
    {
        // success the request
        PurchaseStatusHelper.SetPurchaseStatus(PurchaseStatusEnum.Shipped.ToString());
        Console.WriteLine("Order shipped");
    }
}