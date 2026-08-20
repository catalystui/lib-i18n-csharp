using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Catalyst.Internationalization {

    /// <summary>
    /// Represents a provider for internationalization resources.
    /// </summary>
    public interface ILocaleProvider : IReadOnlyDictionary<string, string> {

        /// <summary>
        /// Loads the specified locale's resources asynchronously.
        /// </summary>
        /// <param name="locale">The locale to load.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        Task LoadLocaleAsync(Locale locale, CancellationToken cancellationToken = default);

    }

}
