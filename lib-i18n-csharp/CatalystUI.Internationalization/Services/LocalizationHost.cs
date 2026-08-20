using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Catalyst.Internationalization {

    /// <summary>
    /// Hosted service for managing localization resources and culture information.
    /// </summary>
    public sealed class LocalizationHost : IHostedService {

        private readonly LocalizationOptions _options;
        private readonly LocalizationCache _cache;
        private readonly IServiceScopeFactory _scopeFactory;

        /// <summary>
        /// Gets the default locale configured for the application.
        /// </summary>
        public Locale DefaultLocale => _options.DefaultLocale;

        /// <summary>
        /// Constructs a new <see cref="LocalizationHost"/>.
        /// </summary>
        public LocalizationHost(IOptions<LocalizationOptions> options, LocalizationCache cache, IServiceScopeFactory scopeFactory) {
            _options = options.Value;
            _cache = cache;
            _scopeFactory = scopeFactory;
        }

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken) {
            await LoadLocale(_options.DefaultLocale, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task StopAsync(CancellationToken cancellationToken) {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Loads a locale mapping for a given culture.
        /// </summary>
        /// <param name="locale">The locale to load.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        public async Task LoadLocale(Locale locale, CancellationToken cancellationToken = default) {
            if (_cache.ContainsKey(locale)) {
                return; // Cache is still valid, locale is loaded
            }

            // Fetch the culture
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            ILocaleProvider localeProvider = scope.ServiceProvider.GetRequiredService<ILocaleProvider>();
            try {
                await localeProvider.LoadLocaleAsync(locale, cancellationToken);
                LocaleMap map = new(localeProvider);
                _cache.Set(locale, map);
            } catch(OperationCanceledException) {
                throw;
            } catch {
                // Cache the failure to prevent repeated attempts to load the same locale
                _cache.Set(locale, new());
                throw;
            }
        }

        /// <summary>
        /// Fetches a localized string for the given locale and key, with an optional fallback locale.
        /// </summary>
        /// <param name="locale">The primary locale to fetch the string from.</param>
        /// <param name="key">The key of the localized string.</param>
        /// <param name="fallback">A fallback locale to use if the primary locale does not contain the key.</param>
        /// <param name="throwExceptions">If <see langword="true"/>, exceptions will be thrown for missing keys; otherwise, the key will be returned.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>The localized string if found; otherwise, the key.</returns>
        public async Task<string> GetAsync(Locale locale, string key, Locale? fallback = null, bool throwExceptions = false, CancellationToken cancellationToken = default) {
            if (!_cache.ContainsKey(locale)) {
                try {
                    await LoadLocale(locale, cancellationToken);
                } catch (OperationCanceledException) {
                    throw;
                } catch {
                    if (throwExceptions) throw;
                }
            }
            if (_cache.TryGetValue(locale, out LocaleMap? map)) {
                if (map.TryGetValue(key, out string? value)) {
                    return value;
                }
            }
            if (fallback != null) {
                if (!_cache.ContainsKey(fallback.Value)) {
                    try {
                        await LoadLocale(fallback.Value, cancellationToken);
                    } catch (OperationCanceledException) {
                        throw;
                    } catch {
                        if (throwExceptions) throw;
                    }
                }
                if (_cache.TryGetValue(fallback.Value, out LocaleMap? fallbackMap)) {
                    if (fallbackMap.TryGetValue(key, out string? value)) {
                        return value;
                    }
                }
            }
            return throwExceptions ? throw new KeyNotFoundException($"Key '{key}' not found in any available locale.") : key;
        }

    }

}
