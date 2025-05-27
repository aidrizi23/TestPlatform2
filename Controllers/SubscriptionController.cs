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
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) 
            {
                _logger.LogWarning("User not found in subscription index");
                return RedirectToAction("Login", "Account");
            }

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading subscription page for user");
            TempData["ErrorMessage"] = "Error loading subscription information. Please try again.";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateCheckoutSession()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) 
            {
                _logger.LogWarning("User not found when creating checkout session");
                return RedirectToAction("Login", "Account");
            }

            if (user.IsPro)
            {
                TempData["InfoMessage"] = "You already have an active Pro subscription!";
                return RedirectToAction("Index");
            }

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
                    { "userId", user.Id },
                    { "userEmail", user.Email ?? "" }
                },
                InvoiceCreation = new SessionInvoiceCreationOptions
                {
                    Enabled = true
                },
                AllowPromotionCodes = true // Allow discount codes
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Created Stripe checkout session {SessionId} for user {UserId}", session.Id, user.Id);
            return Redirect(session.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe checkout session");
            TempData["ErrorMessage"] = "Error creating checkout session. Please try again.";
            return RedirectToAction("Index");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Success(string session_id)
    {
        try
        {
            if (string.IsNullOrEmpty(session_id))
            {
                _logger.LogWarning("Success callback received without session_id");
                TempData["ErrorMessage"] = "Invalid session. Please try again.";
                return RedirectToAction("Index");
            }

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
                    
                    _logger.LogInformation("Updated subscription status for user {UserId} after successful payment", user.Id);
                    TempData["SuccessMessage"] = "Welcome to TestPlatform Pro! You now have unlimited access.";
                }
                else
                {
                    _logger.LogWarning("User not found during success callback for session {SessionId}", session_id);
                }
            }
            else
            {
                _logger.LogWarning("Payment not completed for session {SessionId}. Status: {PaymentStatus}", session_id, session.PaymentStatus);
                TempData["ErrorMessage"] = "Payment was not completed. Please try again.";
            }

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing success callback for session {SessionId}", session_id);
            TempData["ErrorMessage"] = "Error processing payment confirmation. Please contact support if you were charged.";
            return RedirectToAction("Index");
        }
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
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || string.IsNullOrEmpty(user.StripeSubscriptionId))
            {
                TempData["ErrorMessage"] = "No active subscription found to cancel.";
                return RedirectToAction("Index");
            }

            var service = new SubscriptionService();
            var subscription = await service.CancelAsync(user.StripeSubscriptionId);
            
            _logger.LogInformation("Cancelled subscription {SubscriptionId} for user {UserId}", user.StripeSubscriptionId, user.Id);
            
            // Note: The subscription status will be updated via webhook
            TempData["SuccessMessage"] = "Your subscription has been cancelled. You'll retain Pro access until the end of your billing period.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription for user {UserId}", User.Identity?.Name);
            TempData["ErrorMessage"] = "Error cancelling subscription. Please try again or contact support.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> ManageSubscription()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || string.IsNullOrEmpty(user.StripeCustomerId))
            {
                TempData["ErrorMessage"] = "No billing information found. Please contact support.";
                return RedirectToAction("Index");
            }

            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = user.StripeCustomerId,
                ReturnUrl = $"{Request.Scheme}://{Request.Host}/Subscription/Index"
            };
            
            var service = new Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(options);
            
            _logger.LogInformation("Created billing portal session for user {UserId}", user.Id);
            return Redirect(session.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating billing portal session for user {UserId}", User.Identity?.Name);
            TempData["ErrorMessage"] = "Error accessing billing portal. Please try again.";
            return RedirectToAction("Index");
        }
    }
}