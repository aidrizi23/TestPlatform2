using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TestPlatform2.Data;

namespace TestPlatform2.Repository
{
    public interface ISubscriptionRepository
    {
        Task<Subscription> GetByIdAsync(string subscriptionId);
        Task<Subscription> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId);
        Task<Subscription> GetActiveSubscriptionByUserIdAsync(string userId);
        Task<Subscription> GetByUserIdAsync(string userId);
        Task<Subscription> GetByStripeCustomerIdAsync(string stripeCustomerId);
        Task<SubscriptionPlan> GetPlanByTierAsync(SubscriptionTier tier);
        Task<bool> CanUserCreateQuestionAsync(string userId);
        Task<int> GetUserQuestionCountAsync(string userId);
        Task<int> GetRemainingQuestionsAsync(string userId);
        Task Create(Subscription subscription);
        Task Update(Subscription subscription);
        Task Delete(Subscription subscription);
        Task UpdateSubscriptionStatusAsync(string stripeSubscriptionId, SubscriptionStatus status);
        Task UpdateSubscriptionPeriodAsync(string stripeSubscriptionId, DateTime periodStart, DateTime periodEnd);
        Task CancelUserSubscriptionAsync(string userId);
        Task IncrementUserQuestionCountAsync(string userId);
        Task CreateUserSubscriptionAsync(string userId, string stripeCustomerId, string stripeSubscriptionId, string stripePriceId, SubscriptionTier tier);
    }

    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Subscription> GetByIdAsync(string subscriptionId)
        {
            return await _context.Subscriptions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);
        }

        public async Task<Subscription> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId)
        {
            return await _context.Subscriptions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId);
        }

        public async Task<Subscription> GetActiveSubscriptionByUserIdAsync(string userId)
        {
            return await _context.Subscriptions
                .Include(s => s.User)
                .Where(s => s.UserId == userId && 
                           (s.Status == SubscriptionStatus.Active || 
                            s.Status == SubscriptionStatus.Trialing))
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<Subscription> GetByUserIdAsync(string userId)
        {
            return await _context.Subscriptions
                .Include(s => s.User)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<Subscription> GetByStripeCustomerIdAsync(string stripeCustomerId)
        {
            return await _context.Subscriptions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StripeCustomerId == stripeCustomerId);
        }

        public async Task<SubscriptionPlan> GetPlanByTierAsync(SubscriptionTier tier)
        {
            return await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Tier == tier);
        }

        public async Task<bool> CanUserCreateQuestionAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            var plan = await GetPlanByTierAsync(user.CurrentTier);
            if (plan == null) return false;

            // Pro tier has unlimited questions
            if (plan.MaxQuestionsPerTest == -1) return true;

            var questionCount = await GetUserQuestionCountAsync(userId);
            return questionCount < plan.MaxQuestionsPerTest;
        }

        public async Task<int> GetUserQuestionCountAsync(string userId)
        {
            return await _context.Questions
                .Join(_context.Tests,
                    question => question.TestId,
                    test => test.Id,
                    (question, test) => new { question, test })
                .Where(qt => qt.test.UserId == userId)
                .CountAsync();
        }

        public async Task<int> GetRemainingQuestionsAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return 0;

            var plan = await GetPlanByTierAsync(user.CurrentTier);
            if (plan == null) return 0;

            // Pro tier has unlimited questions
            if (plan.MaxQuestionsPerTest == -1) return -1;

            var questionCount = await GetUserQuestionCountAsync(userId);
            return Math.Max(0, plan.MaxQuestionsPerTest - questionCount);
        }

        public async Task Create(Subscription subscription)
        {
            await _context.Subscriptions.AddAsync(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task Update(Subscription subscription)
        {
            subscription.UpdatedAt = DateTime.UtcNow;
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(Subscription subscription)
        {
            _context.Subscriptions.Remove(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSubscriptionStatusAsync(string stripeSubscriptionId, SubscriptionStatus status)
        {
            var subscription = await GetByStripeSubscriptionIdAsync(stripeSubscriptionId);
            if (subscription == null) return;

            subscription.Status = status;
            subscription.UpdatedAt = DateTime.UtcNow;

            if (status == SubscriptionStatus.Canceled)
            {
                subscription.CancelledAt = DateTime.UtcNow;
                subscription.EndDate = DateTime.UtcNow;
            }

            await Update(subscription);

            // Update user's tier if subscription is no longer active
            if (!subscription.IsActive)
            {
                var user = await _context.Users.FindAsync(subscription.UserId);
                if (user != null)
                {
                    user.CurrentTier = SubscriptionTier.Free;
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task UpdateSubscriptionPeriodAsync(string stripeSubscriptionId, DateTime periodStart, DateTime periodEnd)
        {
            var subscription = await GetByStripeSubscriptionIdAsync(stripeSubscriptionId);
            if (subscription == null) return;

            subscription.CurrentPeriodStart = periodStart;
            subscription.CurrentPeriodEnd = periodEnd;
            subscription.UpdatedAt = DateTime.UtcNow;

            await Update(subscription);
        }

        public async Task CancelUserSubscriptionAsync(string userId)
        {
            var subscription = await GetActiveSubscriptionByUserIdAsync(userId);
            if (subscription == null) return;

            subscription.Status = SubscriptionStatus.Canceled;
            subscription.CancelledAt = DateTime.UtcNow;
            subscription.EndDate = subscription.CurrentPeriodEnd;
            subscription.UpdatedAt = DateTime.UtcNow;

            await Update(subscription);

            // Update user tier
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.CurrentTier = SubscriptionTier.Free;
                await _context.SaveChangesAsync();
            }
        }

        public async Task IncrementUserQuestionCountAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.TotalQuestionsCreated++;
                user.LastQuestionCreatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task CreateUserSubscriptionAsync(string userId, string stripeCustomerId, string stripeSubscriptionId, string stripePriceId, SubscriptionTier tier)
        {
            // Cancel any existing subscription
            await CancelUserSubscriptionAsync(userId);

            var subscription = new Subscription
            {
                UserId = userId,
                StripeCustomerId = stripeCustomerId,
                StripeSubscriptionId = stripeSubscriptionId,
                StripePriceId = stripePriceId,
                Tier = tier,
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow,
                CurrentPeriodStart = DateTime.UtcNow,
                CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1)
            };

            await Create(subscription);

            // Update user's current tier
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.CurrentTier = tier;
                await _context.SaveChangesAsync();
            }
        }
    }
}