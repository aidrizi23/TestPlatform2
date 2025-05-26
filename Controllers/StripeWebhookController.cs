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
using TestPlatform2.Repository;
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
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly UserManager<User> _userManager;
        private readonly IStripeService _stripeService;

        public StripeWebhookController(
            IConfiguration configuration,
            ILogger<StripeWebhookController> logger,
            ISubscriptionRepository subscriptionRepository,
            UserManager<User> userManager,
            IStripeService stripeService)
        {
            _configuration = configuration;
            _logger = logger;
            _subscriptionRepository = subscriptionRepository;
            _userManager = userManager;
            _stripeService = stripeService;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = _stripeService.ConstructEvent(
                    json, 
                    Request.Headers["Stripe-Signature"]
                );

                _logger.LogInformation($"Stripe webhook received: {stripeEvent.Type}");

                // Handle the event
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
                    case "checkout.session.completed":
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
            await _subscriptionRepository.CreateUserSubscriptionAsync(
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
            
            await _subscriptionRepository.UpdateSubscriptionStatusAsync(subscription.Id, status);
            
            // Convert Unix timestamps to DateTime - handle nullable values
            if (subscription.CurrentPeriodStart.HasValue && subscription.CurrentPeriodEnd.HasValue)
            {
                var periodStart = DateTimeOffset.FromUnixTimeSeconds(subscription.CurrentPeriodStart.Value).DateTime;
                var periodEnd = DateTimeOffset.FromUnixTimeSeconds(subscription.CurrentPeriodEnd.Value).DateTime;
                
                await _subscriptionRepository.UpdateSubscriptionPeriodAsync(
                    subscription.Id,
                    periodStart,
                    periodEnd
                );
            }
        }

        private async Task HandleSubscriptionDeleted(Event stripeEvent)
        {
            var subscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (subscription == null) return;

            _logger.LogInformation($"Subscription deleted: {subscription.Id}");

            await _subscriptionRepository.UpdateSubscriptionStatusAsync(
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
                await _subscriptionRepository.UpdateSubscriptionStatusAsync(
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
                await _subscriptionRepository.UpdateSubscriptionStatusAsync(
                    invoice.SubscriptionId,
                    SubscriptionStatus.PastDue
                );
            }
        }

        private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            if (session == null) return;

            _logger.LogInformation($"Checkout session completed: {session.Id}");

            // Session mode should be "subscription" for subscription checkout
            if (session.Mode == "subscription" && !string.IsNullOrEmpty(session.SubscriptionId))
            {
                // Get the subscription details
                var subscription = await _stripeService.GetSubscriptionAsync(session.SubscriptionId);

                if (subscription != null)
                {
                    var user = await GetUserByStripeCustomerId(subscription.CustomerId);
                    if (user != null)
                    {
                        var tier = GetTierFromPriceId(subscription.Items.Data[0].Price.Id);
                        
                        await _subscriptionRepository.CreateUserSubscriptionAsync(
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