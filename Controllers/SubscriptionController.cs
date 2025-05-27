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
        
        // Configure Stripe with validation
        var secretKey = _configuration["Stripe:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            _logger.LogError("Stripe:SecretKey not found in configuration");
            throw new InvalidOperationException("Stripe configuration is missing. Please add Stripe:SecretKey to appsettings.json");
        }
        
        StripeConfiguration.ApiKey = secretKey;
        _logger.LogInformation("Stripe configuration initialized successfully");
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
            // Validate Stripe configuration first
            var secretKey = _configuration["Stripe:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                _logger.LogError("Stripe:SecretKey not found in configuration when creating checkout session");
                TempData["ErrorMessage"] = "Payment system is not properly configured. Please contact support.";
                return RedirectToAction("Index");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) 
            {
                _logger.LogWarning("User not found when creating checkout session");
                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation("Creating checkout session for user {UserId} ({UserEmail})", user.Id, user.Email);

            if (user.IsPro)
            {
                _logger.LogInformation("User {UserId} already has Pro subscription", user.Id);
                TempData["InfoMessage"] = "You already have an active Pro subscription!";
                return RedirectToAction("Index");
            }

            var domain = $"{Request.Scheme}://{Request.Host}";
            _logger.LogInformation("Using domain: {Domain}", domain);
            
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
                AllowPromotionCodes = true // Allow discount codes
                // Note: InvoiceCreation is not needed for subscription mode - invoices are created automatically
            };

            _logger.LogInformation("Creating Stripe session with options: {@Options}", new
            {
                PaymentMethodTypes = options.PaymentMethodTypes,
                Mode = options.Mode,
                CustomerEmail = options.CustomerEmail,
                SuccessUrl = options.SuccessUrl,
                CancelUrl = options.CancelUrl
            });

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Successfully created Stripe checkout session {SessionId} for user {UserId}. Redirecting to: {CheckoutUrl}", 
                session.Id, user.Id, session.Url);
                
            return Redirect(session.Url);
        }
        catch (StripeException stripeEx)
        {
            _logger.LogError(stripeEx, "Stripe API error when creating checkout session. Error Code: {ErrorCode}, Error Type: {ErrorType}", 
                stripeEx.StripeError?.Code, stripeEx.StripeError?.Type);
            TempData["ErrorMessage"] = $"Payment system error: {stripeEx.StripeError?.Message ?? stripeEx.Message}. Please try again or contact support.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating Stripe checkout session for user {UserId}", User.Identity?.Name);
            TempData["ErrorMessage"] = $"Error creating checkout session: {ex.Message}. Please try again.";
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

            _logger.LogInformation("Processing success callback for session {SessionId}. Payment Status: {PaymentStatus}, Customer: {CustomerId}, Subscription: {SubscriptionId}", 
                session_id, session.PaymentStatus, session.CustomerId, session.SubscriptionId);

            if (session.PaymentStatus == "paid")
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    // Verify the subscription exists in Stripe before saving
                    if (!string.IsNullOrEmpty(session.SubscriptionId))
                    {
                        try
                        {
                            var subscriptionService = new SubscriptionService();
                            var stripeSubscription = await subscriptionService.GetAsync(session.SubscriptionId);
                            
                            _logger.LogInformation("Verified subscription {SubscriptionId} exists in Stripe with status: {Status}", 
                                session.SubscriptionId, stripeSubscription.Status);
                            
                            // Update subscription status
                            await _subscriptionRepository.UpdateSubscriptionStatusAsync(
                                user.Id, 
                                true, 
                                session.CustomerId, 
                                session.SubscriptionId);
                            
                            _logger.LogInformation("Successfully updated subscription status for user {UserId}. Customer: {CustomerId}, Subscription: {SubscriptionId}", 
                                user.Id, session.CustomerId, session.SubscriptionId);
                            
                            TempData["SuccessMessage"] = "Welcome to TestPlatform Pro! You now have unlimited access.";
                        }
                        catch (StripeException stripeEx)
                        {
                            _logger.LogError(stripeEx, "Failed to verify subscription {SubscriptionId} in Stripe", session.SubscriptionId);
                            TempData["ErrorMessage"] = "Payment completed but there was an issue setting up your subscription. Please contact support.";
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No subscription ID found in session {SessionId}", session_id);
                        TempData["ErrorMessage"] = "Payment completed but subscription setup incomplete. Please contact support.";
                    }
                }
                else
                {
                    _logger.LogWarning("User not found during success callback for session {SessionId}", session_id);
                    TempData["ErrorMessage"] = "User not found. Please login and try again.";
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
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found. Please login and try again.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(user.StripeSubscriptionId))
            {
                _logger.LogWarning("No Stripe subscription ID found for user {UserId}", user.Id);
                TempData["ErrorMessage"] = "No active subscription found to cancel.";
                return RedirectToAction("Index");
            }

            _logger.LogInformation("Attempting to cancel subscription {SubscriptionId} for user {UserId}", 
                user.StripeSubscriptionId, user.Id);

            var service = new SubscriptionService();
            
            // First, try to get the subscription to see if it exists and its current status
            try
            {
                var existingSubscription = await service.GetAsync(user.StripeSubscriptionId);
                _logger.LogInformation("Found subscription {SubscriptionId} with status: {Status}", 
                    user.StripeSubscriptionId, existingSubscription.Status);

                // Check if subscription is already cancelled
                if (existingSubscription.Status == "canceled" || existingSubscription.Status == "cancelled")
                {
                    _logger.LogInformation("Subscription {SubscriptionId} is already cancelled", user.StripeSubscriptionId);
                    
                    // Update local database to reflect the cancellation
                    await _subscriptionRepository.UpdateSubscriptionStatusAsync(user.Id, false, user.StripeCustomerId, null);
                    
                    TempData["InfoMessage"] = "Your subscription was already cancelled.";
                    return RedirectToAction("Index");
                }

                // If subscription exists and is active, cancel it
                var cancelledSubscription = await service.CancelAsync(user.StripeSubscriptionId);
                
                _logger.LogInformation("Successfully cancelled subscription {SubscriptionId} for user {UserId}. New status: {Status}", 
                    user.StripeSubscriptionId, user.Id, cancelledSubscription.Status);
                
                // Note: Don't update local status here - let the webhook handle it
                // This ensures consistency with Stripe's system
                TempData["SuccessMessage"] = "Your subscription has been cancelled. You'll retain Pro access until the end of your billing period.";
            }
            catch (StripeException stripeEx) when (stripeEx.StripeError?.Code == "resource_missing")
            {
                _logger.LogWarning("Subscription {SubscriptionId} not found in Stripe for user {UserId}. Updating local status.", 
                    user.StripeSubscriptionId, user.Id);
                
                // Subscription doesn't exist in Stripe, so update local database
                await _subscriptionRepository.UpdateSubscriptionStatusAsync(user.Id, false, user.StripeCustomerId, null);
                
                TempData["InfoMessage"] = "Your subscription has been cancelled.";
            }
        }
        catch (StripeException stripeEx)
        {
            _logger.LogError(stripeEx, "Stripe error cancelling subscription for user {UserId}. Error Code: {ErrorCode}", 
                User.Identity?.Name, stripeEx.StripeError?.Code);
            TempData["ErrorMessage"] = $"Error cancelling subscription: {stripeEx.StripeError?.Message}. Please try again or contact support.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error cancelling subscription for user {UserId}", User.Identity?.Name);
            TempData["ErrorMessage"] = "Error cancelling subscription. Please try again or contact support.";
        }

        return RedirectToAction("Index");
    }

    // // Add this temporary action for debugging subscription issues
    // [HttpGet]
    // public async Task<IActionResult> DebugSubscription()
    // {
    //     try
    //     {
    //         var user = await _userManager.GetUserAsync(User);
    //         if (user == null) return Json(new { error = "User not found" });
    //
    //         var result = new
    //         {
    //             UserId = user.Id,
    //             UserEmail = user.Email,
    //             IsPro = user.IsPro,
    //             StripeCustomerId = user.StripeCustomerId,
    //             StripeSubscriptionId = user.StripeSubscriptionId,
    //             SubscriptionStartDate = user.SubscriptionStartDate,
    //             SubscriptionEndDate = user.SubscriptionEndDate
    //         };
    //
    //         _logger.LogInformation("Debug subscription info for user {UserId}: {@SubscriptionInfo}", user.Id, result);
    //
    //         // If we have a subscription ID, try to get it from Stripe
    //         if (!string.IsNullOrEmpty(user.StripeSubscriptionId))
    //         {
    //             try
    //             {
    //                 var service = new SubscriptionService();
    //                 var stripeSubscription = await service.GetAsync(user.StripeSubscriptionId);
    //                 
    //                 return Json(new
    //                 {
    //                     localData = result,
    //                     stripeData = new
    //                     {
    //                         Id = stripeSubscription.Id,
    //                         Status = stripeSubscription.Status,
    //                         CustomerId = stripeSubscription.CustomerId,
    //                         CurrentPeriodStart = stripeSubscription.CurrentPeriodStart,
    //                         CurrentPeriodEnd = stripeSubscription.CurrentPeriodEnd,
    //                         CancelAtPeriodEnd = stripeSubscription.CancelAtPeriodEnd
    //                     }
    //                 });
    //             }
    //             catch (StripeException stripeEx)
    //             {
    //                 return Json(new
    //                 {
    //                     localData = result,
    //                     stripeError = new
    //                     {
    //                         Code = stripeEx.StripeError?.Code,
    //                         Message = stripeEx.StripeError?.Message,
    //                         Type = stripeEx.StripeError?.Type
    //                     }
    //                 });
    //             }
    //         }
    //
    //         return Json(new { localData = result, stripeData = "No subscription ID found" });
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error in debug subscription");
    //         return Json(new { error = ex.Message });
    //     }
    // }
    // {
    //     try
    //     {
    //         var user = await _userManager.GetUserAsync(User);
    //         if (user == null || string.IsNullOrEmpty(user.StripeCustomerId))
    //         {
    //             TempData["ErrorMessage"] = "No billing information found. Please contact support.";
    //             return RedirectToAction("Index");
    //         }
    //
    //         var options = new Stripe.BillingPortal.SessionCreateOptions
    //         {
    //             Customer = user.StripeCustomerId,
    //             ReturnUrl = $"{Request.Scheme}://{Request.Host}/Subscription/Index"
    //         };
    //         
    //         var service = new Stripe.BillingPortal.SessionService();
    //         var session = await service.CreateAsync(options);
    //         
    //         _logger.LogInformation("Created billing portal session for user {UserId}", user.Id);
    //         return Redirect(session.Url);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error creating billing portal session for user {UserId}", User.Identity?.Name);
    //         TempData["ErrorMessage"] = "Error accessing billing portal. Please try again.";
    //         return RedirectToAction("Index");
    //     }
    // }
}