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
        _webhookSecret = configuration["Stripe:WebhookSecret"];
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
    }

    [HttpPost]
    public async Task<IActionResult> Index()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        
        _logger.LogInformation("Stripe webhook received: {EventJson}", json);

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _webhookSecret
            );

            _logger.LogInformation("Stripe webhook event type: {EventType}", stripeEvent.Type);

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
                    _logger.LogWarning("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return Ok();
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Stripe webhook error");
            return BadRequest();
        }
    }

    private async Task HandleSubscriptionUpdate(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null)
        {
            _logger.LogWarning("Failed to cast event data object to Subscription");
            return;
        }

        var user = await _subscriptionRepository.GetUserByStripeCustomerIdAsync(subscription.CustomerId);
        if (user == null)
        {
            _logger.LogWarning("No user found for Stripe customer id: {CustomerId}", subscription.CustomerId);
            return;
        }

        bool isPro = subscription.Status == "active" || subscription.Status == "trialing";
        await _subscriptionRepository.UpdateSubscriptionStatusAsync(
            user.Id,
            isPro,
            subscription.CustomerId,
            subscription.Id);

        if (isPro)
        {
            await _emailService.SendEmailAsync(
                user.Email,
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
        }
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null)
        {
            _logger.LogWarning("Failed to cast event data object to Subscription");
            return;
        }

        var user = await _subscriptionRepository.GetUserByStripeCustomerIdAsync(subscription.CustomerId);
        if (user == null)
        {
            _logger.LogWarning("No user found for Stripe customer id: {CustomerId}", subscription.CustomerId);
            return;
        }

        await _subscriptionRepository.UpdateSubscriptionStatusAsync(
            user.Id,
            false,
            subscription.CustomerId,
            null);

        await _emailService.SendEmailAsync(
            user.Email,
            "TestPlatform Pro Subscription Ended",
            $"<h2>Your Pro Subscription Has Ended</h2>" +
            $"<p>Hi {user.FirstName},</p>" +
            $"<p>Your TestPlatform Pro subscription has ended. You now have access to the free tier which includes:</p>" +
            $"<ul>" +
            $"<li>30 lifetime questions</li>" +
            $"<li>10 test invites per week</li>" +
            $"</ul>" +
            $"<p>You can resubscribe at any time to regain unlimited access.</p>"
        );
    }

    private async Task HandleInvoicePaymentSucceeded(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (invoice == null)
        {
            _logger.LogWarning("Failed to cast event data object to Invoice");
            return;
        }

        var user = await _subscriptionRepository.GetUserByStripeCustomerIdAsync(invoice.CustomerId);
        if (user == null)
        {
            _logger.LogWarning("No user found for Stripe customer id: {CustomerId}", invoice.CustomerId);
            return;
        }

        await _emailService.SendEmailAsync(
            user.Email,
            "Payment Received - TestPlatform Pro",
            $"<h2>Payment Confirmation</h2>" +
            $"<p>Hi {user.FirstName},</p>" +
            $"<p>We've received your payment of ${invoice.AmountPaid / 100.0:F2} for TestPlatform Pro.</p>" +
            $"<p>Invoice Number: {invoice.Number}</p>" +
            $"<p>You can view and download your invoice from your Stripe customer portal.</p>" +
            $"<p>Thank you for your continued support!</p>"
        );
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (invoice == null)
        {
            _logger.LogWarning("Failed to cast event data object to Invoice");
            return;
        }

        var user = await _subscriptionRepository.GetUserByStripeCustomerIdAsync(invoice.CustomerId);
        if (user == null)
        {
            _logger.LogWarning("No user found for Stripe customer id: {CustomerId}", invoice.CustomerId);
            return;
        }

        await _emailService.SendEmailAsync(
            user.Email,
            "Payment Failed - TestPlatform Pro",
            $"<h2>Payment Failed</h2>" +
            $"<p>Hi {user.FirstName},</p>" +
            $"<p>We were unable to process your payment for TestPlatform Pro.</p>" +
            $"<p>Please update your payment method to continue enjoying unlimited access.</p>" +
            $"<p>If you don't update your payment method, your subscription will be cancelled.</p>" +
            $"<p><a href='https://yourdomain.com/Subscription/ManageSubscription'>Update Payment Method</a></p>"
        );
    }
}
