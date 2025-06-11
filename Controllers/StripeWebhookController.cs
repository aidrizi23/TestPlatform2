using Microsoft.AspNetCore.Mvc;
using Stripe;
using TestPlatform2.Data;
using TestPlatform2.Repository;
using TestPlatform2.Services;

namespace TestPlatform2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StripeWebhookController : ControllerBase
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<StripeWebhookController> _logger;
    private readonly string _webhookSecret;

    public StripeWebhookController(
        ISubscriptionRepository subscriptionRepository,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<StripeWebhookController> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _emailService = emailService;
        _logger = logger;
        _webhookSecret = configuration["Stripe:WebhookSecret"] ?? "";
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
    }

    [HttpPost]
    public async Task<IActionResult> Index()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        
        if (string.IsNullOrEmpty(json))
        {
            _logger.LogWarning("Received empty webhook payload");
            return BadRequest("Empty payload");
        }

        _logger.LogInformation("Stripe webhook received with payload length: {PayloadLength}", json.Length);
        _logger.LogDebug("Webhook payload: {Payload}", json);

        try
        {
            string stripeSignature = Request.Headers["Stripe-Signature"];
            
            if (string.IsNullOrEmpty(stripeSignature))
            {
                _logger.LogWarning("Stripe webhook received without signature");
                return BadRequest("Missing Stripe signature");
            }

            Event stripeEvent;
            
            if (string.IsNullOrEmpty(_webhookSecret))
            {
                _logger.LogWarning("Webhook secret not configured, parsing without verification");
                stripeEvent = EventUtility.ParseEvent(json, throwOnApiVersionMismatch: false);
            }
            else
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    stripeSignature,
                    _webhookSecret,
                    tolerance: 300,
                    throwOnApiVersionMismatch: false
                );
            }

            _logger.LogInformation("Stripe webhook event type: {EventType}, ID: {EventId}", stripeEvent.Type, stripeEvent.Id);

            switch (stripeEvent.Type)
            {
                case "customer.subscription.created":
                    await HandleSubscriptionCreated(stripeEvent);
                    break;
                    
                case "customer.subscription.updated":
                    await HandleSubscriptionUpdated(stripeEvent);
                    break;
                    
                case "customer.subscription.deleted":
                    await HandleSubscriptionDeleted(stripeEvent);
                    break;
                    
                case "invoice.payment_succeeded":
                    await HandleInvoicePaymentSucceeded(stripeEvent);
                    break;
                    
                case "invoice.payment_failed":
                    await HandleInvoicePaymentFailed(stripeEvent);
                    break;
                
                default:
                    _logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return Ok(new { received = true });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature verification failed");
            return BadRequest("Webhook signature verification failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return StatusCode(500, "Internal server error");
        }
    }

    private async Task HandleSubscriptionCreated(Event stripeEvent)
    {
        try
        {
            var subscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (subscription == null)
            {
                _logger.LogWarning("Failed to cast event data object to Subscription for event {EventId}", stripeEvent.Id);
                return;
            }

            _logger.LogInformation("Processing subscription creation for customer {CustomerId}, subscription {SubscriptionId}, status: {Status}", 
                subscription.CustomerId, subscription.Id, subscription.Status);

            // Try to find user with retry logic
            var user = await FindUserWithRetry(subscription.CustomerId);
            if (user == null)
            {
                _logger.LogError("No user found for Stripe customer id: {CustomerId} in event {EventId} after retries", 
                    subscription.CustomerId, stripeEvent.Id);
                return;
            }

            _logger.LogInformation("Found user {UserId} ({UserEmail}) for customer {CustomerId}", 
                user.Id, user.Email, subscription.CustomerId);

            // Update subscription status
            bool isPro = subscription.Status == "active" || subscription.Status == "trialing";
            await _subscriptionRepository.UpdateSubscriptionStatusAsync(
                user.Id,
                isPro,
                subscription.CustomerId,
                subscription.Id,
                null); // No end date for new active subscription

            _logger.LogInformation("Updated subscription status for user {UserId} to Pro: {IsPro}", user.Id, isPro);

            // Create Subscription record
            try
            {
                var subscriptionRecord = new TestPlatform2.Data.Subscription
                {
                    UserId = user.Id,
                    StripeCustomerId = subscription.CustomerId,
                    StripeSubscriptionId = subscription.Id,
                    StripePriceId = subscription.Items.Data.FirstOrDefault()?.Price?.Id,
                    Status = MapStripeStatusToSubscriptionStatus(subscription.Status),
                    Plan = SubscriptionPlan.Pro,
                    PriceAmount = subscription.Items.Data.FirstOrDefault()?.Price?.UnitAmount / 100m ?? 0,
                    Currency = subscription.Items.Data.FirstOrDefault()?.Price?.Currency ?? "usd",
                    StartDate = subscription.StartDate,
                    CurrentPeriodStart = subscription.CurrentPeriodStart,
                    CurrentPeriodEnd = subscription.CurrentPeriodEnd,
                    HasTrialPeriod = subscription.TrialEnd.HasValue,
                    TrialEndDate = subscription.TrialEnd,
                    Metadata = new Dictionary<string, string>
                    {
                        ["stripe_event_id"] = stripeEvent.Id,
                        ["stripe_event_type"] = stripeEvent.Type
                    }
                };

                await _subscriptionRepository.CreateSubscriptionAsync(subscriptionRecord);
                _logger.LogInformation("Created subscription record {SubscriptionId} for user {UserId}", subscriptionRecord.Id, user.Id);
            }
            catch (Exception subEx)
            {
                _logger.LogError(subEx, "Failed to create subscription record for user {UserId}, Stripe subscription {StripeSubscriptionId}", 
                    user.Id, subscription.Id);
                // Don't fail the webhook because of this
            }

            // Send welcome email
            if (isPro)
            {
                try
                {
                    _logger.LogInformation("Attempting to send welcome email to {Email}", user.Email);
                    
                    var emailBody = $"<h2>Welcome to TestPlatform Pro!</h2>" +
                                   $"<p>Hi {user.FirstName},</p>" +
                                   $"<p>Your Pro subscription is now active. You have unlimited access to:</p>" +
                                   $"<ul>" +
                                   $"<li>Create unlimited questions</li>" +
                                   $"<li>Send unlimited test invites</li>" +
                                   $"<li>Priority support</li>" +
                                   $"</ul>" +
                                   $"<p>Thank you for choosing TestPlatform Pro!</p>";

                    await _emailService.SendEmailAsync(user.Email!, "Welcome to TestPlatform Pro!", emailBody);
                    
                    _logger.LogInformation("Successfully sent welcome email to user {UserId} at {Email}", user.Id, user.Email);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send welcome email to user {UserId} at {Email}. Error: {Error}", 
                        user.Id, user.Email, emailEx.Message);
                    
                    // Don't fail the webhook because of email issues
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling subscription creation for event {EventId}", stripeEvent.Id);
        }
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent)
    {
        try
        {
            var subscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (subscription == null)
            {
                _logger.LogWarning("Failed to cast event data object to Subscription for event {EventId}", stripeEvent.Id);
                return;
            }

            var currentPeriodEnd = subscription.CurrentPeriodEnd;
            _logger.LogInformation("Processing subscription update for customer {CustomerId}, status: {Status}, cancel_at_period_end: {CancelAtPeriodEnd}, current_period_end: {CurrentPeriodEnd}", 
                subscription.CustomerId, subscription.Status, subscription.CancelAtPeriodEnd, currentPeriodEnd);

            var user = await FindUserWithRetry(subscription.CustomerId);
            if (user == null)
            {
                _logger.LogError("No user found for Stripe customer id: {CustomerId} in event {EventId}", 
                    subscription.CustomerId, stripeEvent.Id);
                return;
            }

            _logger.LogInformation("Found user {UserId} ({UserEmail}) for customer {CustomerId}", 
                user.Id, user.Email, subscription.CustomerId);

            // Determine if user should be Pro based on subscription status
            bool isPro = subscription.Status == "active" || subscription.Status == "trialing";
            
            // Calculate when subscription should end
            DateTime? subscriptionEndDate = null;
            if (subscription.CancelAtPeriodEnd || subscription.Status == "canceled")
            {
                // If cancelled, set end date to current period end
                subscriptionEndDate = currentPeriodEnd;
                _logger.LogInformation("Subscription will end at: {EndDate}", subscriptionEndDate);
            }
            else if (isPro)
            {
                // Active subscription, no end date
                subscriptionEndDate = null;
            }
            else
            {
                // Inactive subscription, end immediately
                subscriptionEndDate = DateTime.UtcNow;
            }

            await _subscriptionRepository.UpdateSubscriptionStatusAsync(
                user.Id,
                isPro,
                subscription.CustomerId,
                subscription.Id,
                subscriptionEndDate);

            _logger.LogInformation("Updated subscription status for user {UserId} to Pro: {IsPro}, EndDate: {EndDate}", 
                user.Id, isPro, subscriptionEndDate);

            // Update or create Subscription record
            try
            {
                var existingSubscription = await _subscriptionRepository.GetSubscriptionByStripeIdAsync(subscription.Id);
                if (existingSubscription != null)
                {
                    // Update existing subscription
                    existingSubscription.Status = MapStripeStatusToSubscriptionStatus(subscription.Status);
                    existingSubscription.CurrentPeriodStart = subscription.CurrentPeriodStart;
                    existingSubscription.CurrentPeriodEnd = subscription.CurrentPeriodEnd;
                    existingSubscription.EndDate = subscriptionEndDate;
                    if (subscription.CancelAtPeriodEnd || subscription.Status == "canceled")
                    {
                        existingSubscription.CancelledAt = DateTime.UtcNow;
                        existingSubscription.CancellationReason = subscription.CancellationDetails?.Reason;
                    }
                    existingSubscription.Metadata["stripe_event_id"] = stripeEvent.Id;
                    existingSubscription.Metadata["stripe_event_type"] = stripeEvent.Type;
                    
                    await _subscriptionRepository.UpdateSubscriptionAsync(existingSubscription);
                    _logger.LogInformation("Updated subscription record {SubscriptionId} for user {UserId}", existingSubscription.Id, user.Id);
                }
                else
                {
                    // Create new subscription record if it doesn't exist (shouldn't happen in normal flow)
                    var subscriptionRecord = new TestPlatform2.Data.Subscription
                    {
                        UserId = user.Id,
                        StripeCustomerId = subscription.CustomerId,
                        StripeSubscriptionId = subscription.Id,
                        StripePriceId = subscription.Items.Data.FirstOrDefault()?.Price?.Id,
                        Status = MapStripeStatusToSubscriptionStatus(subscription.Status),
                        Plan = SubscriptionPlan.Pro,
                        PriceAmount = subscription.Items.Data.FirstOrDefault()?.Price?.UnitAmount / 100m ?? 0,
                        Currency = subscription.Items.Data.FirstOrDefault()?.Price?.Currency ?? "usd",
                        StartDate = subscription.StartDate,
                        CurrentPeriodStart = subscription.CurrentPeriodStart,
                        CurrentPeriodEnd = subscription.CurrentPeriodEnd,
                        EndDate = subscriptionEndDate,
                        HasTrialPeriod = subscription.TrialEnd.HasValue,
                        TrialEndDate = subscription.TrialEnd,
                        Metadata = new Dictionary<string, string>
                        {
                            ["stripe_event_id"] = stripeEvent.Id,
                            ["stripe_event_type"] = stripeEvent.Type
                        }
                    };

                    if (subscription.CancelAtPeriodEnd || subscription.Status == "canceled")
                    {
                        subscriptionRecord.CancelledAt = DateTime.UtcNow;
                        subscriptionRecord.CancellationReason = subscription.CancellationDetails?.Reason;
                    }

                    await _subscriptionRepository.CreateSubscriptionAsync(subscriptionRecord);
                    _logger.LogInformation("Created new subscription record {SubscriptionId} for user {UserId}", subscriptionRecord.Id, user.Id);
                }
            }
            catch (Exception subEx)
            {
                _logger.LogError(subEx, "Failed to update subscription record for user {UserId}, Stripe subscription {StripeSubscriptionId}", 
                    user.Id, subscription.Id);
                // Don't fail the webhook because of this
            }

            // Send cancellation email if subscription was cancelled
            if (subscription.CancelAtPeriodEnd && isPro)
            {
                try
                {
                    _logger.LogInformation("Attempting to send cancellation notice email to {Email}", user.Email);
                    
                    var endDate = subscriptionEndDate ?? currentPeriodEnd;
                    var emailBody = $"<h2>Your Pro Subscription Has Been Cancelled</h2>" +
                                   $"<p>Hi {user.FirstName},</p>" +
                                   $"<p>Your TestPlatform Pro subscription has been cancelled and will end on {endDate:MMMM dd, yyyy}.</p>" +
                                   $"<p>You'll continue to have Pro access until then, including:</p>" +
                                   $"<ul>" +
                                   $"<li>Unlimited questions</li>" +
                                   $"<li>Unlimited test invites</li>" +
                                   $"<li>Priority support</li>" +
                                   $"</ul>" +
                                   $"<p>After {endDate:MMMM dd, yyyy}, you'll be moved to the free tier.</p>" +
                                   $"<p>You can reactivate your subscription at any time before then.</p>";

                    await _emailService.SendEmailAsync(user.Email!, "TestPlatform Pro Subscription Cancelled", emailBody);
                    
                    _logger.LogInformation("Successfully sent cancellation notice email to user {UserId}", user.Id);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send cancellation notice email to user {UserId} at {Email}", 
                        user.Id, user.Email);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling subscription update for event {EventId}", stripeEvent.Id);
        }
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        try
        {
            var subscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (subscription == null)
            {
                _logger.LogWarning("Failed to cast event data object to Subscription for event {EventId}", stripeEvent.Id);
                return;
            }

            _logger.LogInformation("Processing subscription deletion for customer {CustomerId}", subscription.CustomerId);

            var user = await FindUserWithRetry(subscription.CustomerId);
            if (user == null)
            {
                _logger.LogError("No user found for Stripe customer id: {CustomerId} in event {EventId}", 
                    subscription.CustomerId, stripeEvent.Id);
                return;
            }

            _logger.LogInformation("Found user {UserId} ({UserEmail}) for customer {CustomerId}", 
                user.Id, user.Email, subscription.CustomerId);

            // Subscription has been deleted, revoke Pro access immediately
            await _subscriptionRepository.UpdateSubscriptionStatusAsync(
                user.Id,
                false,
                subscription.CustomerId,
                null,
                DateTime.UtcNow);

            _logger.LogInformation("Updated subscription status for user {UserId} to free tier due to subscription deletion", user.Id);

            try
            {
                _logger.LogInformation("Attempting to send subscription ended email to {Email}", user.Email);
                
                var emailBody = $"<h2>Your Pro Subscription Has Ended</h2>" +
                               $"<p>Hi {user.FirstName},</p>" +
                               $"<p>Your TestPlatform Pro subscription has ended. You now have access to the free tier which includes:</p>" +
                               $"<ul>" +
                               $"<li>30 lifetime questions</li>" +
                               $"<li>10 test invites per week</li>" +
                               $"</ul>" +
                               $"<p>You can resubscribe at any time to regain unlimited access.</p>";

                await _emailService.SendEmailAsync(user.Email!, "TestPlatform Pro Subscription Ended", emailBody);
                
                _logger.LogInformation("Successfully sent subscription ended email to user {UserId}", user.Id);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send subscription ended email to user {UserId} at {Email}", 
                    user.Id, user.Email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling subscription deletion for event {EventId}", stripeEvent.Id);
        }
    }

    private async Task HandleInvoicePaymentSucceeded(Event stripeEvent)
    {
        try
        {
            var invoice = stripeEvent.Data.Object as Invoice;
            if (invoice == null)
            {
                _logger.LogWarning("Failed to cast event data object to Invoice for event {EventId}", stripeEvent.Id);
                return;
            }

            _logger.LogInformation("Processing successful payment for customer {CustomerId}, amount: {Amount}", 
                invoice.CustomerId, invoice.AmountPaid);

            var user = await FindUserWithRetry(invoice.CustomerId);
            if (user == null)
            {
                _logger.LogError("No user found for Stripe customer id: {CustomerId} in event {EventId}", 
                    invoice.CustomerId, stripeEvent.Id);
                return;
            }

            _logger.LogInformation("Found user {UserId} ({UserEmail}) for customer {CustomerId}", 
                user.Id, user.Email, invoice.CustomerId);

            try
            {
                _logger.LogInformation("Attempting to send payment confirmation email to {Email}", user.Email);
                
                var emailBody = $"<h2>Payment Confirmation</h2>" +
                               $"<p>Hi {user.FirstName},</p>" +
                               $"<p>We've received your payment of ${invoice.AmountPaid / 100.0:F2} for TestPlatform Pro.</p>" +
                               $"<p>Invoice Number: {invoice.Number}</p>" +
                               $"<p>Thank you for your continued support!</p>";

                await _emailService.SendEmailAsync(user.Email!, "Payment Received - TestPlatform Pro", emailBody);
                
                _logger.LogInformation("Successfully sent payment confirmation email to user {UserId}", user.Id);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send payment confirmation email to user {UserId} at {Email}", 
                    user.Id, user.Email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling invoice payment succeeded for event {EventId}", stripeEvent.Id);
        }
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent)
    {
        try
        {
            var invoice = stripeEvent.Data.Object as Invoice;
            if (invoice == null)
            {
                _logger.LogWarning("Failed to cast event data object to Invoice for event {EventId}", stripeEvent.Id);
                return;
            }

            _logger.LogInformation("Processing failed payment for customer {CustomerId}, amount: {Amount}", 
                invoice.CustomerId, invoice.AmountDue);

            var user = await FindUserWithRetry(invoice.CustomerId);
            if (user == null)
            {
                _logger.LogError("No user found for Stripe customer id: {CustomerId} in event {EventId}", 
                    invoice.CustomerId, stripeEvent.Id);
                return;
            }

            _logger.LogInformation("Found user {UserId} ({UserEmail}) for customer {CustomerId}", 
                user.Id, user.Email, invoice.CustomerId);

            try
            {
                _logger.LogInformation("Attempting to send payment failed email to {Email}", user.Email);
                
                var emailBody = $"<h2>Payment Failed</h2>" +
                               $"<p>Hi {user.FirstName},</p>" +
                               $"<p>We were unable to process your payment for TestPlatform Pro.</p>" +
                               $"<p>Amount: ${invoice.AmountDue / 100.0:F2}</p>" +
                               $"<p>Please update your payment method to continue enjoying unlimited access.</p>" +
                               $"<p>If you don't update your payment method, your subscription will be cancelled.</p>";

                await _emailService.SendEmailAsync(user.Email!, "Payment Failed - TestPlatform Pro", emailBody);
                
                _logger.LogInformation("Successfully sent payment failed email to user {UserId}", user.Id);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send payment failed email to user {UserId} at {Email}", 
                    user.Id, user.Email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling invoice payment failed for event {EventId}", stripeEvent.Id);
        }
    }

    private async Task<Data.User?> FindUserWithRetry(string stripeCustomerId, int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Attempting to find user by StripeCustomerId: {CustomerId} (Attempt {Attempt}/{MaxRetries})", 
                    stripeCustomerId, attempt, maxRetries);
                
                var user = await _subscriptionRepository.GetUserByStripeCustomerIdAsync(stripeCustomerId);
                
                if (user != null)
                {
                    _logger.LogInformation("Successfully found user {UserId} ({UserEmail}) for StripeCustomerId: {CustomerId}", 
                        user.Id, user.Email, stripeCustomerId);
                    return user;
                }
                
                _logger.LogWarning("User not found for StripeCustomerId: {CustomerId} on attempt {Attempt}", 
                    stripeCustomerId, attempt);
                
                if (attempt < maxRetries)
                {
                    // Wait a bit before retrying (exponential backoff)
                    var delayMs = 1000 * attempt; // 1s, 2s, 3s
                    _logger.LogInformation("Waiting {DelayMs}ms before retry", delayMs);
                    await Task.Delay(delayMs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding user by StripeCustomerId: {CustomerId} on attempt {Attempt}", 
                    stripeCustomerId, attempt);
                
                if (attempt >= maxRetries)
                {
                    throw;
                }
            }
        }
        
        return null;
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