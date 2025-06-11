using GameStore.Interfaces;
using GameStore.Services.PaymentProviders;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.Services
{
    public class PaymentProviderFactory : IPaymentProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, Type> _providers;

        public PaymentProviderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _providers = new Dictionary<string, Type>
            {
                { "test", typeof(TestPaymentProvider) }
                // Здесь можно добавить другие провайдеры
                // { "stripe", typeof(StripePaymentProvider) },
                // { "yookassa", typeof(YooKassaPaymentProvider) }
            };
        }

        public IPaymentProvider GetProvider(string providerName)
        {
            if (!_providers.ContainsKey(providerName.ToLower()))
            {
                throw new NotSupportedException($"Payment provider '{providerName}' is not supported.");
            }

            var providerType = _providers[providerName.ToLower()];
            return (IPaymentProvider)_serviceProvider.GetRequiredService(providerType);
        }

        public IEnumerable<string> GetAvailableProviders()
        {
            return _providers.Keys;
        }
    }
}