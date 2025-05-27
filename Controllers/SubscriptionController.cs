using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using TestPlatform2.Data;
using TestPlatform2.Models;
using TestPlatform2.Repository;

namespace TestPlatform2.Controllers;

[Authorize]
public class SubscriptionController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        UserManager<User> userManager,
        ISubscriptionRepository subscriptionRepository,
        IConfiguration configuration,
        ILogger<SubscriptionController> logger)
    {
        _userManager = userManager;
        _subscriptionRepository = subscriptionRepository;
        _configuration = configuration;
        _logger = logger;
        
        // Configure Stripe
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var model = new SubscriptionViewModel
        {
            IsPro = user.IsPro,
            SubscriptionStartDate = user.SubscriptionStartDate,
            SubscriptionEndDate = user.SubscriptionEndDate,
            RemainingQuestions = await _subscriptionRepository.GetRemainingQuestionsAsync(user.Id),
            RemainingWeeklyInvites = await _subscriptionRepository.GetRemainingWeeklyInvitesAsync(user.Id),
            TotalQuestionsCreated = user.TotalQuestionsCreated,
            WeeklyInvitesSent = user.WeeklyInvitesSent
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCheckoutSession()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var domain = $"{Request.Scheme}://{Request.Host}";
        
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = 500, // $5.00 in cents
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "TestPlatform Pro Subscription",
                            Description = "Unlimited questions and student invites"
                        },
                        Recurring = new SessionLineItemPriceDataRecurringOptions
                        {
                            Interval = "month"
                        }
                    },
                    Quantity = 1
                }
            },
            Mode = "subscription",
            SuccessUrl = $"{domain}/Subscription/Success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{domain}/Subscription/Cancel",
            CustomerEmail = user.Email,
            Metadata = new Dictionary<string, string>
            {
                { "userId", user.Id }
            },
            InvoiceCreation = new SessionInvoiceCreationOptions
            {
                Enabled = true
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return Redirect(session.Url);
    }

    [HttpGet]
    public async Task<IActionResult> Success(string session_id)
    {
        var service = new SessionService();
        var session = await service.GetAsync(session_id);

        if (session.PaymentStatus == "paid")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                // Update subscription status
                await _subscriptionRepository.UpdateSubscriptionStatusAsync(
                    user.Id, 
                    true, 
                    session.CustomerId, 
                    session.SubscriptionId);
                
                TempData["SuccessMessage"] = "Welcome to TestPlatform Pro! You now have unlimited access.";
            }
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Cancel()
    {
        TempData["ErrorMessage"] = "Subscription cancelled. You can subscribe anytime to unlock unlimited features.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> CancelSubscription()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || string.IsNullOrEmpty(user.StripeSubscriptionId))
            return RedirectToAction("Index");

        try
        {
            var service = new SubscriptionService();
            var subscription = await service.CancelAsync(user.StripeSubscriptionId);
            
            // Note: The subscription status will be updated via webhook
            TempData["SuccessMessage"] = "Your subscription has been cancelled. You'll retain Pro access until the end of your billing period.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription for user {UserId}", user.Id);
            TempData["ErrorMessage"] = "Error cancelling subscription. Please try again or contact support.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> ManageSubscription()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || string.IsNullOrEmpty(user.StripeCustomerId))
            return RedirectToAction("Index");

        try
        {
            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = user.StripeCustomerId,
                ReturnUrl = $"{Request.Scheme}://{Request.Host}/Subscription/Index"
            };
            
            var service = new Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(options);
            
            return Redirect(session.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating billing portal session for user {UserId}", user.Id);
            TempData["ErrorMessage"] = "Error accessing billing portal. Please try again.";
            return RedirectToAction("Index");
        }
    }
}