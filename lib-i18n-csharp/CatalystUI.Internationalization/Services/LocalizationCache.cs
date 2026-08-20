using System;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Caching.Memory;

// ReSharper disable once CheckNamespace
namespace Catalyst.Internationalization {

    /// <summary>
    /// A cache for storing and retrieving localization resources and culture information.
    /// </summary>
    public sealed class LocalizationCache {

        private readonly LocalizationOptions _options;
        private readonly IMemoryCache _cache;

        /// <summary>
        /// Constructs a new <see cref="LocalizationCache"/>.
        /// </summary>
        public LocalizationCache(LocalizationOptions options, IMemoryCache cache) {
            _options = options;
            _cache = cache;
        }

        /// <summary>
        /// Assigns a localization resource map to a specific locale in the cache.
        /// </summary>
        /// <remarks>
        /// Resets the cache duration for the specified locale.
        /// </remarks>
        /// <param name="locale">The locale for which the locale map is being assigned.</param>
        /// <param name="localeMap">The locale map containing localization resources for the specified locale.</param>
        public void Set(Locale locale, LocaleMap localeMap) {
            ArgumentNullException.ThrowIfNull(localeMap);
            _cache.Set(locale, localeMap, _options.CacheDuration);
        }

        /// <summary>
        /// Attempts to retrieve the localization resource map for a specific locale from the cache.
        /// </summary>
        /// <param name="locale">The locale for which the locale map is being retrieved.</param>
        /// <param name="localeMap">The locale map containing localization resources for the specified locale, if found; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the locale map was found in the cache; otherwise, <see langword="false"/>.</returns>
        public bool TryGetValue(Locale locale, [NotNullWhen(true)] out LocaleMap? localeMap) {
            return _cache.TryGetValue(locale, out localeMap);
        }

        /// <summary>
        /// Retrieves the localization resource map for a specific locale from the cache.
        /// </summary>
        /// <param name="locale">The locale for which the locale map is being retrieved.</param>
        /// <returns>The locale map containing localization resources for the specified locale, or <see langword="null"/> if not found in the cache.</returns>
        public LocaleMap? Get(Locale locale) {
            return _cache.TryGetValue(locale, out LocaleMap? localeMap) ? localeMap : null;
        }

        /// <summary>
        /// Checks if the cache contains a localization resource map for a specific locale.
        /// </summary>
        /// <param name="locale">The locale to check for in the cache.</param>
        /// <returns><see langword="true"/> if the cache contains a locale map for the specified locale; otherwise, <see langword="false"/>.</returns>
        public bool ContainsKey(Locale locale) {
            return _cache.TryGetValue(locale, out _);
        }

    }

}
