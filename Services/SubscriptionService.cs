using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TestPlatform2.Data;
using TestPlatform2.Repository;

namespace TestPlatform2.Services
{
    public interface ISubscriptionService
    {
        Task<Subscription> GetUserSubscriptionAsync(string userId);
        Task<Subscription> CreateSubscriptionAsync(string userId, string stripeCustomerId, string stripeSubscriptionId, string stripePriceId, SubscriptionTier tier);
        Task<Subscription> UpdateSubscriptionStatusAsync(string stripeSubscriptionId, SubscriptionStatus status);
        Task<Subscription> UpdateSubscriptionPeriodAsync(string stripeSubscriptionId, DateTime periodStart, DateTime periodEnd);
        Task<bool> CanCreateQuestionAsync(string userId);
        Task<int> GetRemainingQuestionsAsync(string userId);
        Task<SubscriptionPlan> GetSubscriptionPlanAsync(SubscriptionTier tier);
        Task<bool> HasActiveSubscriptionAsync(string userId);
        Task IncrementQuestionCountAsync(string userId);
        Task<Subscription> CancelSubscriptionAsync(string userId);
    }

    public class SubscriptionService : ISubscriptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISubscriptionRepository _subscriptionRepository;

        public SubscriptionService(ApplicationDbContext context, ISubscriptionRepository subscriptionRepository)
        {
            _context = context;
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<Subscription> GetUserSubscriptionAsync(string userId)
        {
            return await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);
        }

        public async Task<Subscription> CreateSubscriptionAsync(string userId, string stripeCustomerId, string stripeSubscriptionId, string stripePriceId, SubscriptionTier tier)
        {
            // Cancel any existing subscription
            var existingSubscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);
            if (existingSubscription != null)
            {
                existingSubscription.Status = SubscriptionStatus.Canceled;
                existingSubscription.CancelledAt = DateTime.UtcNow;
                existingSubscription.UpdatedAt = DateTime.UtcNow;
                await _subscriptionRepository.Update(existingSubscription);
            }

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

            await _subscriptionRepository.Create(subscription);

            // Update user's current tier
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.CurrentTier = tier;
                await _context.SaveChangesAsync();
            }

            return subscription;
        }

        public async Task<Subscription> UpdateSubscriptionStatusAsync(string stripeSubscriptionId, SubscriptionStatus status)
        {
            var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSubscriptionId);
            if (subscription == null)
                return null;

            subscription.Status = status;
            subscription.UpdatedAt = DateTime.UtcNow;

            if (status == SubscriptionStatus.Canceled)
            {
                subscription.CancelledAt = DateTime.UtcNow;
            }

            await _subscriptionRepository.Update(subscription);

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

            return subscription;
        }

        public async Task<Subscription> UpdateSubscriptionPeriodAsync(string stripeSubscriptionId, DateTime periodStart, DateTime periodEnd)
        {
            var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSubscriptionId);
            if (subscription == null)
                return null;

            subscription.CurrentPeriodStart = periodStart;
            subscription.CurrentPeriodEnd = periodEnd;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _subscriptionRepository.Update(subscription);
            return subscription;
        }

        public async Task<bool> CanCreateQuestionAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            var plan = await GetSubscriptionPlanAsync(user.CurrentTier);
            if (plan == null)
                return false;

            // Pro tier has unlimited questions
            if (plan.MaxQuestionsPerTest == -1)
                return true;

            // Get all questions created by the user across all tests
            var totalQuestions = await _context.Questions
                .Join(_context.Tests,
                    question => question.TestId,
                    test => test.Id,
                    (question, test) => new { question, test })
                .Where(qt => qt.test.UserId == userId)
                .CountAsync();

            return totalQuestions < plan.MaxQuestionsPerTest;
        }

        public async Task<int> GetRemainingQuestionsAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return 0;

            var plan = await GetSubscriptionPlanAsync(user.CurrentTier);
            if (plan == null)
                return 0;

            // Pro tier has unlimited questions
            if (plan.MaxQuestionsPerTest == -1)
                return -1; // -1 indicates unlimited

            // Get all questions created by the user across all tests
            var totalQuestions = await _context.Questions
                .Join(_context.Tests,
                    question => question.TestId,
                    test => test.Id,
                    (question, test) => new { question, test })
                .Where(qt => qt.test.UserId == userId)
                .CountAsync();

            return Math.Max(0, plan.MaxQuestionsPerTest - totalQuestions);
        }

        public async Task<SubscriptionPlan> GetSubscriptionPlanAsync(SubscriptionTier tier)
        {
            return await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Tier == tier);
        }

        public async Task<bool> HasActiveSubscriptionAsync(string userId)
        {
            var subscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);
            return subscription != null && subscription.IsActive;
        }

        public async Task IncrementQuestionCountAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.TotalQuestionsCreated++;
                user.LastQuestionCreatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Subscription> CancelSubscriptionAsync(string userId)
        {
            var subscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);
            if (subscription == null)
                return null;

            subscription.Status = SubscriptionStatus.Canceled;
            subscription.CancelledAt = DateTime.UtcNow;
            subscription.EndDate = subscription.CurrentPeriodEnd; // Will end at the end of current billing period
            subscription.UpdatedAt = DateTime.UtcNow;

            await _subscriptionRepository.Update(subscription);

            return subscription;
        }
    }
}