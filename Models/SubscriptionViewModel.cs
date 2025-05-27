namespace TestPlatform2.Models;

public class SubscriptionViewModel
{
    public bool IsPro { get; set; }
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public int RemainingQuestions { get; set; } // -1 for unlimited
    public int RemainingWeeklyInvites { get; set; } // -1 for unlimited
    public int TotalQuestionsCreated { get; set; }
    public int WeeklyInvitesSent { get; set; }
    
    public string RemainingQuestionsDisplay => 
        RemainingQuestions == -1 ? "Unlimited" : RemainingQuestions.ToString();
    
    public string RemainingInvitesDisplay => 
        RemainingWeeklyInvites == -1 ? "Unlimited" : RemainingWeeklyInvites.ToString();
}