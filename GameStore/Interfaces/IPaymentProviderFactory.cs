namespace GameStore.Interfaces
{
    public interface IPaymentProviderFactory
    {
        IPaymentProvider GetProvider(string providerName);
        IEnumerable<string> GetAvailableProviders();
    }
}