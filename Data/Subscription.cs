using System;

namespace TestPlatform2.Data
{
    public class Subscription
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public User User { get; set; }
        
        public string StripeCustomerId { get; set; }
        public string StripeSubscriptionId { get; set; }
        public string StripePriceId { get; set; }
        
        public SubscriptionStatus Status { get; set; }
        public SubscriptionTier Tier { get; set; }
        
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime CurrentPeriodStart { get; set; }
        public DateTime CurrentPeriodEnd { get; set; }
        
        public bool IsActive => Status == SubscriptionStatus.Active || Status == SubscriptionStatus.Trialing;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public enum SubscriptionStatus
    {
        Active,
        Canceled,
        PastDue,
        Unpaid,
        Trialing,
        Incomplete,
        IncompleteExpired
    }
    
    public enum SubscriptionTier
    {
        Free = 0,
        Pro = 1
    }
}