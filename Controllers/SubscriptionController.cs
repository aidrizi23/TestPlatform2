using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TestPlatform2.Data;
using TestPlatform2.Repository;
using TestPlatform2.Services;

namespace TestPlatform2.Controllers
{
    [Authorize]
    public class SubscriptionController : Controller
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IStripeService _stripeService;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        public SubscriptionController(
            ISubscriptionRepository subscriptionRepository,
            IStripeService stripeService,
            UserManager<User> userManager,
            IConfiguration configuration)
        {
            _subscriptionRepository = subscriptionRepository;
            _stripeService = stripeService;
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var subscription = await _subscriptionRepository.GetByUserIdAsync(user.Id);
            var remainingQuestions = await _subscriptionRepository.GetRemainingQuestionsAsync(user.Id);
            var questionCount = await _subscriptionRepository.GetUserQuestionCountAsync(user.Id);

            var viewModel = new SubscriptionViewModel
            {
                CurrentTier = user.CurrentTier,
                Subscription = subscription,
                RemainingQuestions = remainingQuestions,
                TotalQuestionsCreated = questionCount,
                IsSubscriptionActive = subscription?.IsActive ?? false
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Plans()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var freePlan = await _subscriptionRepository.GetPlanByTierAsync(SubscriptionTier.Free);
            var proPlan = await _subscriptionRepository.GetPlanByTierAsync(SubscriptionTier.Pro);
            var currentSubscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(user.Id);

            var viewModel = new PlansViewModel
            {
                FreePlan = freePlan,
                ProPlan = proPlan,
                CurrentTier = user.CurrentTier,
                HasActiveSubscription = currentSubscription?.IsActive ?? false
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCheckoutSession(string priceId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            try
            {
                // Create or get Stripe customer
                string customerId = user.StripeCustomerId;
                if (string.IsNullOrEmpty(customerId))
                {
                    var customer = await _stripeService.CreateCustomerAsync(user);
                    user.StripeCustomerId = customer.Id;
                    await _userManager.UpdateAsync(user);
                    customerId = customer.Id;
                }

                // Create checkout session
                var successUrl = Url.Action("Success", "Subscription", null, Request.Scheme);
                var cancelUrl = Url.Action("Plans", "Subscription", null, Request.Scheme);

                var session = await _stripeService.CreateCheckoutSessionAsync(
                    customerId, 
                    priceId, 
                    successUrl, 
                    cancelUrl
                );

                return Redirect(session.Url);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unable to create checkout session. Please try again.";
                return RedirectToAction("Plans");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success()
        {
            TempData["SuccessMessage"] = "Subscription created successfully! Welcome to Pro!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Cancel()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            try
            {
                var subscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(user.Id);
                if (subscription != null && !string.IsNullOrEmpty(subscription.StripeSubscriptionId))
                {
                    await _stripeService.CancelSubscriptionAsync(subscription.StripeSubscriptionId);
                    TempData["SuccessMessage"] = "Subscription cancelled successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "No active subscription found.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unable to cancel subscription. Please try again.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> CustomerPortal()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrEmpty(user.StripeCustomerId))
            {
                TempData["ErrorMessage"] = "No billing information found.";
                return RedirectToAction("Index");
            }

            try
            {
                var returnUrl = Url.Action("Index", "Subscription", null, Request.Scheme);
                var portalSession = await _stripeService.CreateCustomerPortalSessionAsync(
                    user.StripeCustomerId, 
                    returnUrl
                );

                return Redirect(portalSession.Url);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unable to access billing portal. Please try again.";
                return RedirectToAction("Index");
            }
        }
    }

    // View Models
    public class SubscriptionViewModel
    {
        public SubscriptionTier CurrentTier { get; set; }
        public Subscription Subscription { get; set; }
        public int RemainingQuestions { get; set; }
        public int TotalQuestionsCreated { get; set; }
        public bool IsSubscriptionActive { get; set; }
    }

    public class PlansViewModel
    {
        public SubscriptionPlan FreePlan { get; set; }
        public SubscriptionPlan ProPlan { get; set; }
        public SubscriptionTier CurrentTier { get; set; }
        public bool HasActiveSubscription { get; set; }
    }
}