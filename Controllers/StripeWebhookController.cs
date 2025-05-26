using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using TestPlatform2.Data;
using TestPlatform2.Services;

namespace TestPlatform2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripeWebhookController> _logger;
        private readonly ISubscriptionService _subscriptionService;
        private readonly UserManager<User> _userManager;
        private readonly string _webhookSecret;

        public StripeWebhookController(
            IConfiguration configuration,
            ILogger<StripeWebhookController> logger,
            ISubscriptionService subscriptionService,
            UserManager<User> userManager)
        {
            _configuration = configuration;
            _logger = logger;
            _subscriptionService = subscriptionService;
            _userManager = userManager;
            _webhookSecret = _configuration["Stripe:WebhookSecret"];
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _webhookSecret
                );

                _logger.LogInformation($"Stripe webhook received: {stripeEvent.Type}");

                // Handle the event
                switch (stripeEvent.Type)
                {
                    case Events.CustomerSubscriptionCreated:
                        await HandleSubscriptionCreated(stripeEvent);
                        break;
                    case Events.CustomerSubscriptionUpdated:
                        await HandleSubscriptionUpdated(stripeEvent);
                        break;
                    case Events.CustomerSubscriptionDeleted:
                        await HandleSubscriptionDeleted(stripeEvent);
                        break;
                    case Events.InvoicePaymentSucceeded:
                        await HandleInvoicePaymentSucceeded(stripeEvent);
                        break;
                    case Events.InvoicePaymentFailed:
                        await HandleInvoicePaymentFailed(stripeEvent);
                        break;
                    case Events.CheckoutSessionCompleted:
                        await HandleCheckoutSessionCompleted(stripeEvent);
                        break;
                    default:
                        _logger.LogWarning($"Unhandled event type: {stripeEvent.Type}");
                        break;
                }

                return Ok();
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "Stripe webhook error");
                return BadRequest();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Webhook processing error");
                return StatusCode(500);
            }
        }

        private async Task HandleSubscriptionCreated(Event stripeEvent)
        {
            var subscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (subscription == null) return;

            _logger.LogInformation($"Subscription created: {subscription.Id}");

            // Get user by Stripe customer ID
            var user = await GetUserByStripeCustomerId(subscription.CustomerId);
            if (user == null)
            {
                _logger.LogError($"User not found for customer: {subscription.CustomerId}");
                return;
            }

            // Determine tier based on price ID
            var tier = GetTierFromPriceId(subscription.Items.Data[0].Price.Id);

            // Create subscription record
            await _subscriptionService.CreateSubscriptionAsync(
                user.Id,
                subscription.CustomerId,
                subscription.Id,
                subscription.Items.Data[0].Price.Id,
                tier
            );
        }

        private async Task HandleSubscriptionUpdated(Event stripeEvent)
        {
            var subscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (subscription == null) return;

            _logger.LogInformation($"Subscription updated: {subscription.Id}, Status: {subscription.Status}");

            var status = MapStripeStatusToSubscriptionStatus(subscription.Status);
            
            await _subscriptionService.UpdateSubscriptionStatusAsync(subscription.Id, status);
            await _subscriptionService.UpdateSubscriptionPeriodAsync(
                subscription.Id,
                subscription.CurrentPeriodStart,
                subscription.CurrentPeriodEnd
            );
        }

        private async Task HandleSubscriptionDeleted(Event stripeEvent)
        {
            var subscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (subscription == null) return;

            _logger.LogInformation($"Subscription deleted: {subscription.Id}");

            await _subscriptionService.UpdateSubscriptionStatusAsync(
                subscription.Id, 
                SubscriptionStatus.Canceled
            );
        }

        private async Task HandleInvoicePaymentSucceeded(Event stripeEvent)
        {
            var invoice = stripeEvent.Data.Object as Invoice;
            if (invoice == null) return;

            _logger.LogInformation($"Invoice payment succeeded: {invoice.Id}");

            if (!string.IsNullOrEmpty(invoice.SubscriptionId))
            {
                await _subscriptionService.UpdateSubscriptionStatusAsync(
                    invoice.SubscriptionId,
                    SubscriptionStatus.Active
                );
            }
        }

        private async Task HandleInvoicePaymentFailed(Event stripeEvent)
        {
            var invoice = stripeEvent.Data.Object as Invoice;
            if (invoice == null) return;

            _logger.LogInformation($"Invoice payment failed: {invoice.Id}");

            if (!string.IsNullOrEmpty(invoice.SubscriptionId))
            {
                await _subscriptionService.UpdateSubscriptionStatusAsync(
                    invoice.SubscriptionId,
                    SubscriptionStatus.PastDue
                );
            }
        }

        private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null) return;

            _logger.LogInformation($"Checkout session completed: {session.Id}");

            // Session mode should be "subscription" for subscription checkout
            if (session.Mode == "subscription" && !string.IsNullOrEmpty(session.SubscriptionId))
            {
                // Get the subscription details
                var subscriptionService = new SubscriptionService();
                var subscription = await subscriptionService.GetAsync(session.SubscriptionId);

                if (subscription != null)
                {
                    var user = await GetUserByStripeCustomerId(subscription.CustomerId);
                    if (user != null)
                    {
                        var tier = GetTierFromPriceId(subscription.Items.Data[0].Price.Id);
                        
                        await _subscriptionService.CreateSubscriptionAsync(
                            user.Id,
                            subscription.CustomerId,
                            subscription.Id,
                            subscription.Items.Data[0].Price.Id,
                            tier
                        );
                    }
                }
            }
        }

        private async Task<User> GetUserByStripeCustomerId(string stripeCustomerId)
        {
            return await _userManager.Users
                .FirstOrDefaultAsync(u => u.StripeCustomerId == stripeCustomerId);
        }

        private SubscriptionStatus MapStripeStatusToSubscriptionStatus(string stripeStatus)
        {
            return stripeStatus switch
            {
                "active" => SubscriptionStatus.Active,
                "canceled" => SubscriptionStatus.Canceled,
                "incomplete" => SubscriptionStatus.Incomplete,
                "incomplete_expired" => SubscriptionStatus.IncompleteExpired,
                "past_due" => SubscriptionStatus.PastDue,
                "trialing" => SubscriptionStatus.Trialing,
                "unpaid" => SubscriptionStatus.Unpaid,
                _ => SubscriptionStatus.Canceled
            };
        }

        private SubscriptionTier GetTierFromPriceId(string priceId)
        {
            // You'll need to update this with your actual Stripe price ID
            var proPriceId = _configuration["Stripe:ProPriceId"];
            return priceId == proPriceId ? SubscriptionTier.Pro : SubscriptionTier.Free;
        }
    }
}