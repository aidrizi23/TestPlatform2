namespace TestPlatform2.Data;

public class Subscription
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; }
    public User User { get; set; }
    
    // Stripe Information
    public string StripeCustomerId { get; set; }
    public string StripeSubscriptionId { get; set; }
    public string StripePriceId { get; set; }
    
    // Subscription Details
    public SubscriptionStatus Status { get; set; }
    public SubscriptionPlan Plan { get; set; }
    public decimal PriceAmount { get; set; }
    public string Currency { get; set; } = "usd";
    
    // Dates
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    
    // Billing
    public DateTime? NextBillingDate { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public decimal? LastPaymentAmount { get; set; }
    
    // Trial Information
    public bool HasTrialPeriod { get; set; }
    public DateTime? TrialEndDate { get; set; }
    
    // Additional Information
    public string? CancellationReason { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public enum SubscriptionStatus
{
    Active,
    Trialing,
    PastDue,
    Cancelled,
    Incomplete,
    IncompleteExpired,
    Unpaid,
    Paused
}

public enum SubscriptionPlan
{
    Free,
    Pro,
    Enterprise // For future expansion
}

// Subscription History for tracking changes
public class SubscriptionHistory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SubscriptionId { get; set; }
    public Subscription Subscription { get; set; }
    
    public string Action { get; set; } // Created, Updated, Cancelled, Reactivated, etc.
    public SubscriptionStatus? OldStatus { get; set; }
    public SubscriptionStatus? NewStatus { get; set; }
    public decimal? OldPrice { get; set; }
    public decimal? NewPrice { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? StripeEventId { get; set; } // For tracking which Stripe event caused this
}