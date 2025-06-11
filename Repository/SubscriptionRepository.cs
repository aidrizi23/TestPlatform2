using Microsoft.EntityFrameworkCore;
using TestPlatform2.Data;

namespace TestPlatform2.Repository;

public interface ISubscriptionRepository
{
    Task<bool> CanCreateQuestionAsync(string userId);
    Task<bool> CanSendInviteAsync(string userId);
    Task IncrementQuestionCountAsync(string userId);
    Task IncrementInviteCountAsync(string userId);
    Task UpdateSubscriptionStatusAsync(string userId, bool isPro, string? stripeCustomerId, string? stripeSubscriptionId, DateTime? subscriptionEndDate = null);
    Task<User?> GetUserByStripeCustomerIdAsync(string stripeCustomerId);
    Task ResetWeeklyInvitesIfNeededAsync(string userId);
    Task<int> GetRemainingQuestionsAsync(string userId);
    Task<int> GetRemainingWeeklyInvitesAsync(string userId);
    Task<List<User>> GetExpiredSubscriptionsAsync();
    Task RevokeExpiredSubscriptionsAsync();
    
    // New Subscription entity methods
    Task<Subscription> CreateSubscriptionAsync(Subscription subscription);
    Task<Subscription?> GetSubscriptionByStripeIdAsync(string stripeSubscriptionId);
    Task<Subscription?> GetActiveSubscriptionByUserIdAsync(string userId);
    Task<List<Subscription>> GetSubscriptionsByUserIdAsync(string userId);
    Task UpdateSubscriptionAsync(Subscription subscription);
    Task<Subscription?> GetSubscriptionByIdAsync(string subscriptionId);
}

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SubscriptionRepository> _logger;
    private const int FREE_QUESTION_LIMIT = 30;
    private const int FREE_WEEKLY_INVITE_LIMIT = 10;

    public SubscriptionRepository(ApplicationDbContext context, ILogger<SubscriptionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> CanCreateQuestionAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;
        
        // Check if subscription has expired
        await CheckAndRevokeExpiredSubscription(user);
        
        // Pro users have unlimited questions
        if (user.IsPro) return true;
        
        // Free users have a 30 question limit
        return user.TotalQuestionsCreated < FREE_QUESTION_LIMIT;
    }

    public async Task<bool> CanSendInviteAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;
        
        // Check if subscription has expired
        await CheckAndRevokeExpiredSubscription(user);
        
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

    public async Task UpdateSubscriptionStatusAsync(string userId, bool isPro, string? stripeCustomerId, string? stripeSubscriptionId, DateTime? subscriptionEndDate = null)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) 
        {
            _logger.LogWarning("User {UserId} not found when updating subscription status", userId);
            return;
        }
        
        _logger.LogInformation("Updating subscription for user {UserId}: IsPro={IsPro}, EndDate={EndDate}", 
            userId, isPro, subscriptionEndDate);
        
        user.IsPro = isPro;
        user.StripeCustomerId = stripeCustomerId;
        user.StripeSubscriptionId = stripeSubscriptionId;
        
        if (isPro)
        {
            // Starting or continuing pro subscription
            if (user.SubscriptionStartDate == null)
            {
                user.SubscriptionStartDate = DateTime.UtcNow;
            }
            user.SubscriptionEndDate = subscriptionEndDate; // This will be null for active subscriptions, or set to period end for cancelled ones
        }
        else
        {
            // Ending pro subscription
            user.SubscriptionEndDate = subscriptionEndDate ?? DateTime.UtcNow;
        }
        
        user.EnsureUtcDates();
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Successfully updated subscription for user {UserId}", userId);
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
        
        // Check if subscription has expired
        await CheckAndRevokeExpiredSubscription(user);
        
        if (user.IsPro) return -1; // Unlimited
        
        return Math.Max(0, FREE_QUESTION_LIMIT - user.TotalQuestionsCreated);
    }

    public async Task<int> GetRemainingWeeklyInvitesAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return 0;
        
        // Check if subscription has expired
        await CheckAndRevokeExpiredSubscription(user);
        
        if (user.IsPro) return -1; // Unlimited
        
        await ResetWeeklyInvitesIfNeededAsync(userId);
        
        // Refresh user data after potential reset
        user = await _context.Users.FindAsync(userId);
        if (user == null) return 0;
        
        return Math.Max(0, FREE_WEEKLY_INVITE_LIMIT - user.WeeklyInvitesSent);
    }

    public async Task<List<User>> GetExpiredSubscriptionsAsync()
    {
        var now = DateTime.UtcNow;
        
        return await _context.Users
            .Where(u => u.IsPro && 
                       u.SubscriptionEndDate.HasValue && 
                       u.SubscriptionEndDate <= now)
            .ToListAsync();
    }

    public async Task RevokeExpiredSubscriptionsAsync()
    {
        var expiredUsers = await GetExpiredSubscriptionsAsync();
        
        if (!expiredUsers.Any())
        {
            return;
        }
        
        _logger.LogInformation("Found {Count} expired subscriptions to revoke", expiredUsers.Count);
        
        foreach (var user in expiredUsers)
        {
            _logger.LogInformation("Revoking expired subscription for user {UserId} (expired on {ExpiredDate})", 
                user.Id, user.SubscriptionEndDate);
            
            user.IsPro = false;
            user.StripeSubscriptionId = null; // Clear the subscription ID since it's expired
            user.EnsureUtcDates();
        }
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Successfully revoked {Count} expired subscriptions", expiredUsers.Count);
    }

    private async Task CheckAndRevokeExpiredSubscription(User user)
    {
        if (user.IsPro && 
            user.SubscriptionEndDate.HasValue && 
            user.SubscriptionEndDate <= DateTime.UtcNow)
        {
            _logger.LogInformation("Revoking expired subscription for user {UserId} (expired on {ExpiredDate})", 
                user.Id, user.SubscriptionEndDate);
            
            user.IsPro = false;
            user.StripeSubscriptionId = null;
            user.EnsureUtcDates();
            
            await _context.SaveChangesAsync();
        }
    }

    // New Subscription entity methods
    public async Task<Subscription> CreateSubscriptionAsync(Subscription subscription)
    {
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    public async Task<Subscription?> GetSubscriptionByStripeIdAsync(string stripeSubscriptionId)
    {
        return await _context.Subscriptions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId);
    }

    public async Task<Subscription?> GetActiveSubscriptionByUserIdAsync(string userId)
    {
        return await _context.Subscriptions
            .Include(s => s.User)
            .Where(s => s.UserId == userId)
            .Where(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Subscription>> GetSubscriptionsByUserIdAsync(string userId)
    {
        return await _context.Subscriptions
            .Include(s => s.User)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateSubscriptionAsync(Subscription subscription)
    {
        _context.Subscriptions.Update(subscription);
        await _context.SaveChangesAsync();
    }

    public async Task<Subscription?> GetSubscriptionByIdAsync(string subscriptionId)
    {
        return await _context.Subscriptions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId);
    }
}