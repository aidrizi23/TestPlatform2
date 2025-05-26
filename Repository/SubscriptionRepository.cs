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
        Task<Subscription> GetByStripeCustomerIdAsync(string stripeCustomerId);
        Task Create(Subscription subscription);
        Task Update(Subscription subscription);
        Task Delete(Subscription subscription);
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

        public async Task<Subscription> GetByStripeCustomerIdAsync(string stripeCustomerId)
        {
            return await _context.Subscriptions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StripeCustomerId == stripeCustomerId);
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
    }
}