namespace LegacyRenewalApp
{
    public interface ICustomerRepository
    {
        Customer GetById(int id);
    }

    public interface ISubscriptionPlanRepository
    {
        SubscriptionPlan GetByCode(string code);
    }
}