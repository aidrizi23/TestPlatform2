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

    // Helper properties for subscription status
    public bool IsSubscriptionActive
    {
        get
        {
            if (!IsPro) return false;
            
            // If no end date is set, subscription is active
            if (!SubscriptionEndDate.HasValue) return true;
            
            // If end date is in the future, subscription is still active
            return SubscriptionEndDate.Value > DateTime.UtcNow;
        }
    }

    public bool IsSubscriptionCancelled => IsPro && SubscriptionEndDate.HasValue;

    public bool IsSubscriptionExpiringSoon
    {
        get
        {
            if (!IsPro || !SubscriptionEndDate.HasValue) return false;
            
            var daysUntilExpiration = (SubscriptionEndDate.Value - DateTime.UtcNow).TotalDays;
            return daysUntilExpiration <= 7 && daysUntilExpiration > 0;
        }
    }

    public int DaysUntilExpiration
    {
        get
        {
            if (!SubscriptionEndDate.HasValue) return -1;
            
            var days = (int)Math.Ceiling((SubscriptionEndDate.Value - DateTime.UtcNow).TotalDays);
            return Math.Max(0, days);
        }
    }

    public string SubscriptionStatusMessage
    {
        get
        {
            if (!IsPro)
                return "You're on the free plan";
            
            if (!SubscriptionEndDate.HasValue)
                return "Your Pro subscription is active";
            
            if (SubscriptionEndDate.Value <= DateTime.UtcNow)
                return "Your Pro subscription has expired";
            
            var daysLeft = DaysUntilExpiration;
            if (daysLeft == 0)
                return "Your Pro subscription expires today";
            
            if (daysLeft == 1)
                return "Your Pro subscription expires tomorrow";
            
            if (daysLeft <= 7)
                return $"Your Pro subscription expires in {daysLeft} days";
            
            return $"Your Pro subscription is active until {SubscriptionEndDate:MMMM dd, yyyy}";
        }
    }

    public string SubscriptionStatusClass
    {
        get
        {
            if (!IsPro) return "text-muted";
            
            if (IsSubscriptionExpiringSoon) return "text-warning";
            
            if (!IsSubscriptionActive) return "text-danger";
            
            return "text-success";
        }
    }
}