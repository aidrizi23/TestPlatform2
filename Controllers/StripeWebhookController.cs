using Microsoft.AspNetCore.Mvc;
using Stripe;
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
                stripeEvent = EventUtility.ParseEvent(json);
            }
            else
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    stripeSignature,
                    _webhookSecret
                );
            }

            _logger.LogInformation("Stripe webhook event type: {EventType}, ID: {EventId}", stripeEvent.Type, stripeEvent.Id);

            switch (stripeEvent.Type)
            {
                case "customer.subscription.created":
                case "customer.subscription.updated":
                    await HandleSubscriptionUpdate(stripeEvent);
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

    private async Task HandleSubscriptionUpdate(Event stripeEvent)
    {
        try
        {
            var subscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (subscription == null)
            {
                _logger.LogWarning("Failed to cast event data object to Subscription for event {EventId}", stripeEvent.Id);
                return;
            }

            _logger.LogInformation("Processing subscription update for customer {CustomerId}, status: {Status}", 
                subscription.CustomerId, subscription.Status);

            var user = await _subscriptionRepository.GetUserByStripeCustomerIdAsync(subscription.CustomerId);
            if (user == null)
            {
                _logger.LogWarning("No user found for Stripe customer id: {CustomerId} in event {EventId}", 
                    subscription.CustomerId, stripeEvent.Id);
                return;
            }

            bool isPro = subscription.Status == "active" || subscription.Status == "trialing";
            await _subscriptionRepository.UpdateSubscriptionStatusAsync(
                user.Id,
                isPro,
                subscription.CustomerId,
                subscription.Id);

            _logger.LogInformation("Updated subscription status for user {UserId} to Pro: {IsPro}", user.Id, isPro);

            if (isPro && stripeEvent.Type == "customer.subscription.created")
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        user.Email!,
                        "Welcome to TestPlatform Pro!",
                        $"<h2>Welcome to TestPlatform Pro!</h2>" +
                        $"<p>Hi {user.FirstName},</p>" +
                        $"<p>Your Pro subscription is now active. You have unlimited access to:</p>" +
                        $"<ul>" +
                        $"<li>Create unlimited questions</li>" +
                        $"<li>Send unlimited test invites</li>" +
                        $"<li>Priority support</li>" +
                        $"</ul>" +
                        $"<p>Thank you for choosing TestPlatform Pro!</p>"
                    );
                    
                    _logger.LogInformation("Sent welcome email to user {UserId}", user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send welcome email to user {UserId}", user.Id);
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

            var user = await _subscriptionRepository.GetUserByStripeCustomerIdAsync(subscription.CustomerId);
            if (user == null)
            {
                _logger.LogWarning("No user found for Stripe customer id: {CustomerId} in event {EventId}", 
                    subscription.CustomerId, stripeEvent.Id);
                return;
            }

            await _subscriptionRepository.UpdateSubscriptionStatusAsync(
                user.Id,
                false,
                subscription.CustomerId,
                null);

            _logger.LogInformation("Updated subscription status for user {UserId} to free tier", user.Id);

            try
            {
                await _emailService.SendEmailAsync(
                    user.Email!,
                    "TestPlatform Pro Subscription Ended",
                    $"<h2>Your Pro Subscription Has Ended</h2>" +
                    $"<p>Hi {user.FirstName},</p>" +
                    $"<p>Your TestPlatform Pro subscription has ended. You now have access to the free tier which includes:</p>" +
                    $"<ul>" +
                    $"<li>30 lifetime questions</li>" +
                    $"<li>10 test invites per week</li>" +
                    $"</ul>" +
                    $"<p>You can resubscribe at any time to regain unlimited access.</p>" +
                    $"<p><a href='https://yourdomain.com/Subscription'>Resubscribe Now</a></p>"
                );
                
                _logger.LogInformation("Sent subscription ended email to user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send subscription ended email to user {UserId}", user.Id);
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

            var user = await _subscriptionRepository.GetUserByStripeCustomerIdAsync(invoice.CustomerId);
            if (user == null)
            {
                _logger.LogWarning("No user found for Stripe customer id: {CustomerId} in event {EventId}", 
                    invoice.CustomerId, stripeEvent.Id);
                return;
            }

            try
            {
                await _emailService.SendEmailAsync(
                    user.Email!,
                    "Payment Received - TestPlatform Pro",
                    $"<h2>Payment Confirmation</h2>" +
                    $"<p>Hi {user.FirstName},</p>" +
                    $"<p>We've received your payment of ${invoice.AmountPaid / 100.0:F2} for TestPlatform Pro.</p>" +
                    $"<p>Invoice Number: {invoice.Number}</p>" +
                    $"<p>You can view and download your invoice from your Stripe customer portal.</p>" +
                    $"<p>Thank you for your continued support!</p>"
                );
                
                _logger.LogInformation("Sent payment confirmation email to user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payment confirmation email to user {UserId}", user.Id);
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

            var user = await _subscriptionRepository.GetUserByStripeCustomerIdAsync(invoice.CustomerId);
            if (user == null)
            {
                _logger.LogWarning("No user found for Stripe customer id: {CustomerId} in event {EventId}", 
                    invoice.CustomerId, stripeEvent.Id);
                return;
            }

            try
            {
                await _emailService.SendEmailAsync(
                    user.Email!,
                    "Payment Failed - TestPlatform Pro",
                    $"<h2>Payment Failed</h2>" +
                    $"<p>Hi {user.FirstName},</p>" +
                    $"<p>We were unable to process your payment for TestPlatform Pro.</p>" +
                    $"<p>Amount: ${invoice.AmountDue / 100.0:F2}</p>" +
                    $"<p>Please update your payment method to continue enjoying unlimited access.</p>" +
                    $"<p>If you don't update your payment method, your subscription will be cancelled.</p>" +
                    $"<p><a href='https://yourdomain.com/Subscription/ManageSubscription'>Update Payment Method</a></p>"
                );
                
                _logger.LogInformation("Sent payment failed email to user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payment failed email to user {UserId}", user.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling invoice payment failed for event {EventId}", stripeEvent.Id);
        }
    }
}