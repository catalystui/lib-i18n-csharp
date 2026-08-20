using System;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Catalyst.Internationalization.Extensions {

    /// <summary>
    /// Extensions for the <see cref="ServiceCollection"/> class.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions {

        /// <param name="services">The service collection to add the services to.</param>
        extension(IServiceCollection services) {

            /// <summary>
            /// Adds a locale provider to the service collection.
            /// </summary>
            /// <typeparam name="T">The type of the locale provider to add.</typeparam>
            /// <returns>The service collection.</returns>
            public IServiceCollection AddLocaleProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : class, ILocaleProvider {
                services.TryAddScoped<ILocaleProvider, T>();
                return services;
            }

            /// <summary>
            /// Adds internationalization services to the service collection.
            /// </summary>
            /// <returns>The service collection.</returns>
            public IServiceCollection AddInternationalization(Action<LocalizationOptions>? configure = null) {
                services.AddOptions<LocalizationOptions>();
                if (configure != null) services.Configure(configure);
                services.AddMemoryCache();
                services.AddSingleton<LocalizationCache>();
                services.AddSingleton<LocalizationHost>();
                services.AddHostedService(provider => provider.GetRequiredService<LocalizationHost>());
                services.AddScoped<LocalizationService>();
                return services;
            }

        }

    }

}
