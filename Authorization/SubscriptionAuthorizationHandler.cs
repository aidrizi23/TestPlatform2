using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TestPlatform2.Data;
using TestPlatform2.Services;

namespace TestPlatform2.Authorization
{
    public class SubscriptionRequirement : IAuthorizationRequirement
    {
        public SubscriptionTier RequiredTier { get; }

        public SubscriptionRequirement(SubscriptionTier requiredTier)
        {
            RequiredTier = requiredTier;
        }
    }

    public class SubscriptionAuthorizationHandler : AuthorizationHandler<SubscriptionRequirement>
    {
        private readonly UserManager<User> _userManager;
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionAuthorizationHandler(
            UserManager<User> userManager,
            ISubscriptionService subscriptionService)
        {
            _userManager = userManager;
            _subscriptionService = subscriptionService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            SubscriptionRequirement requirement)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return;
            }

            // Check if user has the required subscription tier or higher
            if (user.CurrentTier >= requirement.RequiredTier)
            {
                var subscription = await _subscriptionService.GetUserSubscriptionAsync(userId);
                
                // For free tier, always succeed
                if (requirement.RequiredTier == SubscriptionTier.Free)
                {
                    context.Succeed(requirement);
                    return;
                }
                
                // For paid tiers, check if subscription is active
                if (subscription != null && subscription.IsActive)
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}