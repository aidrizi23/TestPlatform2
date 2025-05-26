using Microsoft.AspNetCore.Identity;

namespace TestPlatform2.Data;

public class User : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";

    public virtual ICollection<Test> Tests { get; set; } = new List<Test>();
    
    // Subscription related
    public string StripeCustomerId { get; set; }
    public virtual Subscription Subscription { get; set; }
    public SubscriptionTier CurrentTier { get; set; } = SubscriptionTier.Free;
    
    // Usage tracking
    public int TotalQuestionsCreated { get; set; } = 0;
    public DateTime? LastQuestionCreatedAt { get; set; }
}