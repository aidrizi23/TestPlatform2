using Microsoft.EntityFrameworkCore;
using TestPlatform2.Data;

namespace TestPlatform2.Repository;

public interface ISubscriptionRepository
{
    Task<bool> CanCreateQuestionAsync(string userId);
    Task<bool> CanSendInviteAsync(string userId);
    Task IncrementQuestionCountAsync(string userId);
    Task IncrementInviteCountAsync(string userId);
    Task UpdateSubscriptionStatusAsync(string userId, bool isPro, string? stripeCustomerId, string? stripeSubscriptionId);
    Task<User?> GetUserByStripeCustomerIdAsync(string stripeCustomerId);
    Task ResetWeeklyInvitesIfNeededAsync(string userId);
    Task<int> GetRemainingQuestionsAsync(string userId);
    Task<int> GetRemainingWeeklyInvitesAsync(string userId);
}

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly ApplicationDbContext _context;
    private const int FREE_QUESTION_LIMIT = 30;
    private const int FREE_WEEKLY_INVITE_LIMIT = 10;

    public SubscriptionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanCreateQuestionAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;
        
        // Pro users have unlimited questions
        if (user.IsPro) return true;
        
        // Free users have a 30 question limit
        return user.TotalQuestionsCreated < FREE_QUESTION_LIMIT;
    }

    public async Task<bool> CanSendInviteAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;
        
        // Pro users have unlimited invites
        if (user.IsPro) return true;
        
        // Reset weekly invites if needed
        await ResetWeeklyInvitesIfNeededAsync(userId);
        
        // Refresh user data after potential reset
        user = await _context.Users.FindAsync(userId);
        if (user == null) return false;
        
        // Check if user has reached weekly limit
        return user.WeeklyInvitesSent < FREE_WEEKLY_INVITE_LIMIT;
    }

    public async Task IncrementQuestionCountAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return;
        
        user.TotalQuestionsCreated++;
        user.LastQuestionCreatedAt = DateTime.UtcNow;
        user.EnsureUtcDates();
        
        await _context.SaveChangesAsync();
    }

    public async Task IncrementInviteCountAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return;
        
        // Reset weekly invites if needed
        await ResetWeeklyInvitesIfNeededAsync(userId);
        
        // Refresh user data after potential reset
        user = await _context.Users.FindAsync(userId);
        if (user == null) return;
        
        user.WeeklyInvitesSent++;
        user.EnsureUtcDates();
        
        await _context.SaveChangesAsync();
    }

    public async Task UpdateSubscriptionStatusAsync(string userId, bool isPro, string? stripeCustomerId, string? stripeSubscriptionId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return;
        
        user.IsPro = isPro;
        user.StripeCustomerId = stripeCustomerId;
        user.StripeSubscriptionId = stripeSubscriptionId;
        
        if (isPro)
        {
            user.SubscriptionStartDate = DateTime.UtcNow;
            user.SubscriptionEndDate = null; // Will be set by webhook when subscription ends
        }
        else
        {
            user.SubscriptionEndDate = DateTime.UtcNow;
        }
        
        user.EnsureUtcDates();
        await _context.SaveChangesAsync();
    }

    public async Task<User?> GetUserByStripeCustomerIdAsync(string stripeCustomerId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.StripeCustomerId == stripeCustomerId);
    }

    public async Task ResetWeeklyInvitesIfNeededAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return;
        
        var now = DateTime.UtcNow;
        
        // Calculate the start of the user's current week (from registration date)
        var daysSinceRegistration = (now - user.RegistrationDate).TotalDays;
        var weeksSinceRegistration = (int)(daysSinceRegistration / 7);
        var currentWeekStart = user.RegistrationDate.AddDays(weeksSinceRegistration * 7);
        
        // If we're in a new week or no reset date is set, reset the counter
        if (user.WeeklyInviteResetDate == null || user.WeeklyInviteResetDate < currentWeekStart)
        {
            user.WeeklyInvitesSent = 0;
            user.WeeklyInviteResetDate = DateTime.SpecifyKind(currentWeekStart, DateTimeKind.Utc);
            user.EnsureUtcDates();
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetRemainingQuestionsAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return 0;
        
        if (user.IsPro) return -1; // Unlimited
        
        return Math.Max(0, FREE_QUESTION_LIMIT - user.TotalQuestionsCreated);
    }

    public async Task<int> GetRemainingWeeklyInvitesAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return 0;
        
        if (user.IsPro) return -1; // Unlimited
        
        await ResetWeeklyInvitesIfNeededAsync(userId);
        
        // Refresh user data after potential reset
        user = await _context.Users.FindAsync(userId);
        if (user == null) return 0;
        
        return Math.Max(0, FREE_WEEKLY_INVITE_LIMIT - user.WeeklyInvitesSent);
    }
}