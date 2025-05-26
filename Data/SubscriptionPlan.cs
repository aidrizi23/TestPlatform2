namespace TestPlatform2.Data;

public class SubscriptionPlan
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Description { get; set; }
    public SubscriptionTier Tier { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "usd";
    public string StripePriceId { get; set; }
        
    // Feature limits
    public int MaxQuestionsPerTest { get; set; }
    public int MaxTestsPerMonth { get; set; }
    public int MaxStudentsPerTest { get; set; }
    public bool AdvancedAnalytics { get; set; }
    public bool PrioritySupport { get; set; }
        
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}