using System;

namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly ILegacyBillingAdapter _billingAdapter;
        private readonly IDiscountCalculator _discountCalculator;
        private readonly ISupportFeeCalculator _supportFeeCalculator;
        private readonly IPaymentFeeCalculator _paymentFeeCalculator;
        private readonly ITaxCalculator _taxCalculator;

        public SubscriptionRenewalService()
            : this(new CustomerRepository(), 
                   new SubscriptionPlanRepository(), 
                   new LegacyBillingAdapter(),
                   new DefaultDiscountCalculator(),
                   new DefaultSupportFeeCalculator(),
                   new DefaultPaymentFeeCalculator(),
                   new DefaultTaxCalculator())
        {
        }

        public SubscriptionRenewalService(
            ICustomerRepository customerRepository,
            ISubscriptionPlanRepository planRepository,
            ILegacyBillingAdapter billingAdapter,
            IDiscountCalculator discountCalculator,
            ISupportFeeCalculator supportFeeCalculator,
            IPaymentFeeCalculator paymentFeeCalculator,
            ITaxCalculator taxCalculator)
        {
            _customerRepository = customerRepository;
            _planRepository = planRepository;
            _billingAdapter = billingAdapter;
            _discountCalculator = discountCalculator;
            _supportFeeCalculator = supportFeeCalculator;
            _paymentFeeCalculator = paymentFeeCalculator;
            _taxCalculator = taxCalculator;
        }

        public RenewalInvoice CreateRenewalInvoice(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            ValidateInputs(customerId, planCode, seatCount, paymentMethod);

            string normalizedPlanCode = planCode.Trim().ToUpperInvariant();
            string normalizedPaymentMethod = paymentMethod.Trim().ToUpperInvariant();

            var customer = _customerRepository.GetById(customerId);
            var plan = _planRepository.GetByCode(normalizedPlanCode);

            if (!customer.IsActive)
            {
                throw new InvalidOperationException("Inactive customers cannot renew subscriptions");
            }

            decimal baseAmount = (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;
            string finalNotes = string.Empty;

            var discountResult = _discountCalculator.Calculate(customer, plan, baseAmount, seatCount, useLoyaltyPoints);
            finalNotes += discountResult.Notes;

            decimal subtotalAfterDiscount = baseAmount - discountResult.Amount;
            if (subtotalAfterDiscount < 300m)
            {
                subtotalAfterDiscount = 300m;
                finalNotes += "minimum discounted subtotal applied; ";
            }

            var supportResult = _supportFeeCalculator.Calculate(normalizedPlanCode, includePremiumSupport);
            finalNotes += supportResult.Notes;

            decimal amountForPaymentFee = subtotalAfterDiscount + supportResult.Amount;
            var paymentResult = _paymentFeeCalculator.Calculate(normalizedPaymentMethod, amountForPaymentFee);
            finalNotes += paymentResult.Notes;

            decimal taxRate = _taxCalculator.GetTaxRate(customer.Country);
            decimal taxBase = amountForPaymentFee + paymentResult.Amount;
            decimal taxAmount = taxBase * taxRate;
            decimal finalAmount = taxBase + taxAmount;

            if (finalAmount < 500m)
            {
                finalAmount = 500m;
                finalNotes += "minimum invoice amount applied; ";
            }

            var invoice = BuildInvoice(customerId, normalizedPlanCode, normalizedPaymentMethod, seatCount, customer.FullName, baseAmount, discountResult.Amount, supportResult.Amount, paymentResult.Amount, taxAmount, finalAmount, finalNotes);

            _billingAdapter.SaveInvoice(invoice);
            SendNotification(customer, normalizedPlanCode, invoice.FinalAmount);

            return invoice;
        }

        private void ValidateInputs(int customerId, string planCode, int seatCount, string paymentMethod)
        {
            if (customerId <= 0) throw new ArgumentException("Customer id must be positive");
            if (string.IsNullOrWhiteSpace(planCode)) throw new ArgumentException("Plan code is required");
            if (seatCount <= 0) throw new ArgumentException("Seat count must be positive");
            if (string.IsNullOrWhiteSpace(paymentMethod)) throw new ArgumentException("Payment method is required");
        }

        private RenewalInvoice BuildInvoice(int customerId, string planCode, string paymentMethod, int seatCount, string customerName, decimal baseAmount, decimal discountAmount, decimal supportFee, decimal paymentFee, decimal taxAmount, decimal finalAmount, string notes)
        {
            return new RenewalInvoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customerId}-{planCode}",
                CustomerName = customerName,
                PlanCode = planCode,
                PaymentMethod = paymentMethod,
                SeatCount = seatCount,
                BaseAmount = Math.Round(baseAmount, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero),
                SupportFee = Math.Round(supportFee, 2, MidpointRounding.AwayFromZero),
                PaymentFee = Math.Round(paymentFee, 2, MidpointRounding.AwayFromZero),
                TaxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero),
                FinalAmount = Math.Round(finalAmount, 2, MidpointRounding.AwayFromZero),
                Notes = notes.Trim(),
                GeneratedAt = DateTime.UtcNow
            };
        }

        private void SendNotification(Customer customer, string planCode, decimal finalAmount)
        {
            if (string.IsNullOrWhiteSpace(customer.Email)) return;

            string subject = "Subscription renewal invoice";
            string body = $"Hello {customer.FullName}, your renewal for plan {planCode} has been prepared. Final amount: {finalAmount:F2}.";
            _billingAdapter.SendEmail(customer.Email, subject, body);
        }
    }
}