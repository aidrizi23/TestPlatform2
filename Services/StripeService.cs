using Stripe;
using Stripe.Checkout;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestPlatform2.Data;

namespace TestPlatform2.Services
{
    public interface IStripeService
    {
        Task<Customer> CreateCustomerAsync(User user);
        Task<Session> CreateCheckoutSessionAsync(string customerId, string priceId, string successUrl, string cancelUrl);
        Task<Stripe.Subscription> CreateSubscriptionAsync(string customerId, string priceId);
        Task<Stripe.Subscription> CancelSubscriptionAsync(string subscriptionId);
        Task<Stripe.Subscription> GetSubscriptionAsync(string subscriptionId);
        Task<Customer> GetCustomerAsync(string customerId);
        Task<Session> GetCheckoutSessionAsync(string sessionId);
        Task<PortalSession> CreateCustomerPortalSessionAsync(string customerId, string returnUrl);
        Task<List<PaymentMethod>> GetPaymentMethodsAsync(string customerId);
        Task<PaymentMethod> AttachPaymentMethodAsync(string paymentMethodId, string customerId);
        Task<Customer> UpdateDefaultPaymentMethodAsync(string customerId, string paymentMethodId);
    }

    public class StripeService : IStripeService
    {
        private readonly IConfiguration _configuration;
        private readonly string _webhookSecret;

        public StripeService(IConfiguration configuration)
        {
            _configuration = configuration;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
            _webhookSecret = _configuration["Stripe:WebhookSecret"];
        }

        public async Task<Customer> CreateCustomerAsync(User user)
        {
            var options = new CustomerCreateOptions
            {
                Email = user.Email,
                Name = user.FullName,
                Metadata = new Dictionary<string, string>
                {
                    { "UserId", user.Id }
                }
            };

            var service = new CustomerService();
            return await service.CreateAsync(options);
        }

        public async Task<Session> CreateCheckoutSessionAsync(string customerId, string priceId, string successUrl, string cancelUrl)
        {
            var options = new SessionCreateOptions
            {
                Customer = customerId,
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                },
                Mode = "subscription",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                AllowPromotionCodes = true,
                BillingAddressCollection = "required",
                CustomerUpdate = new SessionCustomerUpdateOptions
                {
                    Address = "auto"
                }
            };

            var service = new SessionService();
            return await service.CreateAsync(options);
        }

        public async Task<Stripe.Subscription> CreateSubscriptionAsync(string customerId, string priceId)
        {
            var options = new SubscriptionCreateOptions
            {
                Customer = customerId,
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions
                    {
                        Price = priceId
                    }
                },
                PaymentBehavior = "default_incomplete",
                Expand = new List<string> { "latest_invoice.payment_intent" }
            };

            var service = new SubscriptionService();
            return await service.CreateAsync(options);
        }

        public async Task<Stripe.Subscription> CancelSubscriptionAsync(string subscriptionId)
        {
            var service = new SubscriptionService();
            var options = new SubscriptionCancelOptions
            {
                InvoiceNow = true,
                Prorate = true
            };
            
            return await service.CancelAsync(subscriptionId, options);
        }

        public async Task<Stripe.Subscription> GetSubscriptionAsync(string subscriptionId)
        {
            var service = new SubscriptionService();
            return await service.GetAsync(subscriptionId);
        }

        public async Task<Customer> GetCustomerAsync(string customerId)
        {
            var service = new CustomerService();
            return await service.GetAsync(customerId);
        }

        public async Task<Session> GetCheckoutSessionAsync(string sessionId)
        {
            var service = new SessionService();
            return await service.GetAsync(sessionId);
        }

        public async Task<PortalSession> CreateCustomerPortalSessionAsync(string customerId, string returnUrl)
        {
            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = returnUrl
            };

            var service = new Stripe.BillingPortal.SessionService();
            return await service.CreateAsync(options);
        }

        public async Task<List<PaymentMethod>> GetPaymentMethodsAsync(string customerId)
        {
            var service = new PaymentMethodService();
            var options = new PaymentMethodListOptions
            {
                Customer = customerId,
                Type = "card"
            };

            var paymentMethods = await service.ListAsync(options);
            return paymentMethods.Data;
        }

        public async Task<PaymentMethod> AttachPaymentMethodAsync(string paymentMethodId, string customerId)
        {
            var service = new PaymentMethodService();
            var options = new PaymentMethodAttachOptions
            {
                Customer = customerId
            };

            return await service.AttachAsync(paymentMethodId, options);
        }

        public async Task<Customer> UpdateDefaultPaymentMethodAsync(string customerId, string paymentMethodId)
        {
            var service = new CustomerService();
            var options = new CustomerUpdateOptions
            {
                InvoiceSettings = new CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = paymentMethodId
                }
            };

            return await service.UpdateAsync(customerId, options);
        }

        public Event ConstructEvent(string json, string signature)
        {
            try
            {
                return EventUtility.ConstructEvent(json, signature, _webhookSecret);
            }
            catch (StripeException)
            {
                throw;
            }
        }
    }
}