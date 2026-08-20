using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Catalyst.Internationalization {

    /// <summary>
    /// Provides localization services for fetching localized strings based on the current UI culture.
    /// </summary>
    public sealed class LocalizationService {

        private readonly LocalizationHost _host;

        /// <summary>
        /// Constructs a new <see cref="LocalizationService"/>.
        /// </summary>
        public LocalizationService(LocalizationHost host) {
            _host = host;
        }

        /// <inheritdoc cref="LocalizationHost.GetAsync"/>
        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default) {
            return await _host.GetAsync(LocaleHelper.FromCultureInfo(CultureInfo.CurrentUICulture), key, _host.DefaultLocale, cancellationToken: cancellationToken);
        }

    }

}
