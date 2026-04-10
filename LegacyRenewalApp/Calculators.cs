using System;

namespace LegacyRenewalApp
{
    public interface IDiscountCalculator
    {
        (decimal Amount, string Notes) Calculate(Customer customer, SubscriptionPlan plan, decimal baseAmount, int seatCount, bool useLoyaltyPoints);
    }

    public class DefaultDiscountCalculator : IDiscountCalculator
    {
        public (decimal Amount, string Notes) Calculate(Customer customer, SubscriptionPlan plan, decimal baseAmount, int seatCount, bool useLoyaltyPoints)
        {
            decimal discountAmount = 0m;
            string notes = string.Empty;

            if (customer.Segment == "Silver") { discountAmount += baseAmount * 0.05m; notes += "silver discount; "; }
            else if (customer.Segment == "Gold") { discountAmount += baseAmount * 0.10m; notes += "gold discount; "; }
            else if (customer.Segment == "Platinum") { discountAmount += baseAmount * 0.15m; notes += "platinum discount; "; }
            else if (customer.Segment == "Education" && plan.IsEducationEligible) { discountAmount += baseAmount * 0.20m; notes += "education discount; "; }

            if (customer.YearsWithCompany >= 5) { discountAmount += baseAmount * 0.07m; notes += "long-term loyalty discount; "; }
            else if (customer.YearsWithCompany >= 2) { discountAmount += baseAmount * 0.03m; notes += "basic loyalty discount; "; }

            if (seatCount >= 50) { discountAmount += baseAmount * 0.12m; notes += "large team discount; "; }
            else if (seatCount >= 20) { discountAmount += baseAmount * 0.08m; notes += "medium team discount; "; }
            else if (seatCount >= 10) { discountAmount += baseAmount * 0.04m; notes += "small team discount; "; }

            if (useLoyaltyPoints && customer.LoyaltyPoints > 0)
            {
                int pointsToUse = customer.LoyaltyPoints > 200 ? 200 : customer.LoyaltyPoints;
                discountAmount += pointsToUse;
                notes += $"loyalty points used: {pointsToUse}; ";
            }

            return (discountAmount, notes);
        }
    }

    public interface ISupportFeeCalculator
    {
        (decimal Amount, string Notes) Calculate(string normalizedPlanCode, bool includePremiumSupport);
    }

    public class DefaultSupportFeeCalculator : ISupportFeeCalculator
    {
        public (decimal Amount, string Notes) Calculate(string normalizedPlanCode, bool includePremiumSupport)
        {
            if (!includePremiumSupport) return (0m, string.Empty);

            decimal fee = normalizedPlanCode switch
            {
                "START" => 250m,
                "PRO" => 400m,
                "ENTERPRISE" => 700m,
                _ => 0m
            };
            return (fee, "premium support included; ");
        }
    }

    public interface IPaymentFeeCalculator
    {
        (decimal Amount, string Notes) Calculate(string normalizedPaymentMethod, decimal baseAmountToCalculateOn);
    }

    public class DefaultPaymentFeeCalculator : IPaymentFeeCalculator
    {
        public (decimal Amount, string Notes) Calculate(string normalizedPaymentMethod, decimal baseAmountToCalculateOn)
        {
            return normalizedPaymentMethod switch
            {
                "CARD" => (baseAmountToCalculateOn * 0.02m, "card payment fee; "),
                "BANK_TRANSFER" => (baseAmountToCalculateOn * 0.01m, "bank transfer fee; "),
                "PAYPAL" => (baseAmountToCalculateOn * 0.035m, "paypal fee; "),
                "INVOICE" => (0m, "invoice payment; "),
                _ => throw new ArgumentException("Unsupported payment method")
            };
        }
    }

    public interface ITaxCalculator
    {
        decimal GetTaxRate(string country);
    }

    public class DefaultTaxCalculator : ITaxCalculator
    {
        public decimal GetTaxRate(string country)
        {
            return country switch
            {
                "Poland" => 0.23m,
                "Germany" => 0.19m,
                "Czech Republic" => 0.21m,
                "Norway" => 0.25m,
                _ => 0.20m
            };
        }
    }
}