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
                AllowPromotionCodes = true,
                ClientReferenceId = user.Id // This helps identify the user
            };

            _logger.LogInformation("Creating Stripe session with options: {@Options}", new
            {
                PaymentMethodTypes = options.PaymentMethodTypes,
                Mode = options.Mode,
                CustomerEmail = options.CustomerEmail,
                SuccessUrl = options.SuccessUrl,
                CancelUrl = options.CancelUrl,
                ClientReferenceId = options.ClientReferenceId
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

            _logger.LogInformation("Processing success callback for session {SessionId}", session_id);

            var service = new SessionService();
            var session = await service.GetAsync(session_id);

            _logger.LogInformation("Retrieved session {SessionId}. Payment Status: {PaymentStatus}, Customer: {CustomerId}, Subscription: {SubscriptionId}", 
                session_id, session.PaymentStatus, session.CustomerId, session.SubscriptionId);

            if (session.PaymentStatus == "paid")
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    _logger.LogInformation("Processing successful payment for user {UserId} ({UserEmail})", user.Id, user.Email);

                    // IMPORTANT: Save the StripeCustomerId immediately to avoid race condition with webhooks
                    if (!string.IsNullOrEmpty(session.CustomerId) && string.IsNullOrEmpty(user.StripeCustomerId))
                    {
                        _logger.LogInformation("Saving StripeCustomerId {CustomerId} for user {UserId}", session.CustomerId, user.Id);
                        
                        user.StripeCustomerId = session.CustomerId;
                        var updateResult = await _userManager.UpdateAsync(user);
                        
                        if (updateResult.Succeeded)
                        {
                            _logger.LogInformation("Successfully saved StripeCustomerId for user {UserId}", user.Id);
                        }
                        else
                        {
                            _logger.LogError("Failed to save StripeCustomerId for user {UserId}. Errors: {Errors}", 
                                user.Id, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                        }
                    }

                    // Verify the subscription exists in Stripe before saving
                    if (!string.IsNullOrEmpty(session.SubscriptionId))
                    {
                        try
                        {
                            var subscriptionService = new SubscriptionService();
                            var stripeSubscription = await subscriptionService.GetAsync(session.SubscriptionId);
                            
                            _logger.LogInformation("Verified subscription {SubscriptionId} exists in Stripe with status: {Status}", 
                                session.SubscriptionId, stripeSubscription.Status);
                            
                            // Update subscription status - this should happen after StripeCustomerId is saved
                            await _subscriptionRepository.UpdateSubscriptionStatusAsync(
                                user.Id, 
                                true, 
                                session.CustomerId, 
                                session.SubscriptionId,
                                null); // No end date for active subscription
                            
                            _logger.LogInformation("Successfully updated subscription status for user {UserId}. Customer: {CustomerId}, Subscription: {SubscriptionId}", 
                                user.Id, session.CustomerId, session.SubscriptionId);

                            // Create subscription record if it doesn't exist
                            try
                            {
                                var existingSubscription = await _subscriptionRepository.GetSubscriptionByStripeIdAsync(session.SubscriptionId);
                                if (existingSubscription == null)
                                {
                                    var subscriptionRecord = new TestPlatform2.Data.Subscription
                                    {
                                        UserId = user.Id,
                                        StripeCustomerId = session.CustomerId,
                                        StripeSubscriptionId = session.SubscriptionId,
                                        StripePriceId = stripeSubscription.Items.Data.FirstOrDefault()?.Price?.Id,
                                        Status = MapStripeStatusToSubscriptionStatus(stripeSubscription.Status),
                                        Plan = TestPlatform2.Data.SubscriptionPlan.Pro,
                                        PriceAmount = stripeSubscription.Items.Data.FirstOrDefault()?.Price?.UnitAmount / 100m ?? 0,
                                        Currency = stripeSubscription.Items.Data.FirstOrDefault()?.Price?.Currency ?? "usd",
                                        StartDate = stripeSubscription.StartDate,
                                        CurrentPeriodStart = stripeSubscription.CurrentPeriodStart,
                                        CurrentPeriodEnd = stripeSubscription.CurrentPeriodEnd,
                                        HasTrialPeriod = stripeSubscription.TrialEnd.HasValue,
                                        TrialEndDate = stripeSubscription.TrialEnd,
                                        Metadata = new Dictionary<string, string>
                                        {
                                            ["stripe_session_id"] = session_id,
                                            ["created_via"] = "checkout_success"
                                        }
                                    };

                                    await _subscriptionRepository.CreateSubscriptionAsync(subscriptionRecord);
                                    _logger.LogInformation("Created subscription record {SubscriptionId} for user {UserId} via checkout success", 
                                        subscriptionRecord.Id, user.Id);
                                }
                                else
                                {
                                    _logger.LogInformation("Subscription record already exists for Stripe subscription {StripeSubscriptionId}", session.SubscriptionId);
                                }
                            }
                            catch (Exception subEx)
                            {
                                _logger.LogError(subEx, "Failed to create subscription record for user {UserId}, Stripe subscription {StripeSubscriptionId}", 
                                    user.Id, session.SubscriptionId);
                                // Don't fail the success flow because of this
                            }
                            
                            TempData["SuccessMessage"] = "Welcome to TestPlatform Pro! You now have unlimited access. Check your email for confirmation.";
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
        _logger.LogInformation("User cancelled subscription checkout");
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

            _logger.LogInformation("User {UserId} ({UserEmail}) attempting to cancel subscription", user.Id, user.Email);

            if (string.IsNullOrEmpty(user.StripeSubscriptionId))
            {
                _logger.LogWarning("No Stripe subscription ID found for user {UserId}", user.Id);
                TempData["ErrorMessage"] = "No active subscription found to cancel.";
                return RedirectToAction("Index");
            }

            _logger.LogInformation("Attempting to cancel subscription {SubscriptionId} for user {UserId}", 
                user.StripeSubscriptionId, user.Id);

            var service = new SubscriptionService();
            
            // Get the subscription to check its current status
            try
            {
                var existingSubscription = await service.GetAsync(user.StripeSubscriptionId);
                _logger.LogInformation("Found subscription {SubscriptionId} with status: {Status}, cancel_at_period_end: {CancelAtPeriodEnd}", 
                    user.StripeSubscriptionId, existingSubscription.Status, existingSubscription.CancelAtPeriodEnd);

                // Check if subscription is already cancelled
                if (existingSubscription.Status == "canceled" || existingSubscription.Status == "cancelled")
                {
                    _logger.LogInformation("Subscription {SubscriptionId} is already cancelled", user.StripeSubscriptionId);
                    
                    // Update local database to reflect the cancellation
                    await _subscriptionRepository.UpdateSubscriptionStatusAsync(
                        user.Id, 
                        false, 
                        user.StripeCustomerId, 
                        null,
                        DateTime.UtcNow);
                    
                    TempData["InfoMessage"] = "Your subscription was already cancelled.";
                    return RedirectToAction("Index");
                }

                // If already set to cancel at period end, inform user
                if (existingSubscription.CancelAtPeriodEnd)
                {
                    _logger.LogInformation("Subscription {SubscriptionId} is already set to cancel at period end", user.StripeSubscriptionId);
                    var endDate = existingSubscription.CurrentPeriodEnd;
                    TempData["InfoMessage"] = $"Your subscription is already set to cancel on {endDate:MMMM dd, yyyy}.";
                    return RedirectToAction("Index");
                }

                // Cancel the subscription at the end of the period
                var cancelledSubscription = await service.UpdateAsync(user.StripeSubscriptionId, new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = true
                });
                
                var periodEndDate = cancelledSubscription.CurrentPeriodEnd;
                _logger.LogInformation("Successfully set subscription {SubscriptionId} to cancel at period end for user {UserId}. Will end on: {EndDate}", 
                    user.StripeSubscriptionId, user.Id, periodEndDate);
                
                // Update local database to reflect the scheduled cancellation
                await _subscriptionRepository.UpdateSubscriptionStatusAsync(
                    user.Id, 
                    true, // Still Pro until end of period
                    user.StripeCustomerId, 
                    user.StripeSubscriptionId,
                    periodEndDate); // Set end date
                
                TempData["SuccessMessage"] = $"Your subscription has been cancelled and will end on {periodEndDate:MMMM dd, yyyy}. You'll retain Pro access until then. Check your email for confirmation.";
            }
            catch (StripeException stripeEx) when (stripeEx.StripeError?.Code == "resource_missing")
            {
                _logger.LogWarning("Subscription {SubscriptionId} not found in Stripe for user {UserId}. Updating local status.", 
                    user.StripeSubscriptionId, user.Id);
                
                // Subscription doesn't exist in Stripe, so update local database
                await _subscriptionRepository.UpdateSubscriptionStatusAsync(
                    user.Id, 
                    false, 
                    user.StripeCustomerId, 
                    null,
                    DateTime.UtcNow);
                
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

    private static SubscriptionStatus MapStripeStatusToSubscriptionStatus(string stripeStatus)
    {
        return stripeStatus?.ToLower() switch
        {
            "active" => SubscriptionStatus.Active,
            "trialing" => SubscriptionStatus.Trialing,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Cancelled,
            "incomplete" => SubscriptionStatus.Incomplete,
            "incomplete_expired" => SubscriptionStatus.IncompleteExpired,
            "unpaid" => SubscriptionStatus.Unpaid,
            "paused" => SubscriptionStatus.Paused,
            _ => SubscriptionStatus.Incomplete
        };
    }
}