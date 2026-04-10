using System;

namespace LegacyRenewalApp
{
    public interface ILegacyBillingAdapter
    {
        void SaveInvoice(RenewalInvoice invoice);
        void SendEmail(string to, string subject, string body);
    }

    public class LegacyBillingAdapter : ILegacyBillingAdapter
    {
        public void SaveInvoice(RenewalInvoice invoice)
        {
            LegacyBillingGateway.SaveInvoice(invoice);
        }

        public void SendEmail(string to, string subject, string body)
        {
            LegacyBillingGateway.SendEmail(to, subject, body);
        }
    }
}