using Microsoft.AspNetCore.Identity;

namespace TestPlatform2.Data;

public class User : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";

    public virtual ICollection<Test> Tests { get; set; } = new List<Test>();
    
    // Usage tracking
    public int TotalQuestionsCreated { get; set; } = 0;
    public DateTime? LastQuestionCreatedAt { get; set; }
    
    // Subscription fields
    public bool IsPro { get; set; } = false;
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    
    // Invite tracking
    public int WeeklyInvitesSent { get; set; } = 0;
    public DateTime? WeeklyInviteResetDate { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
}