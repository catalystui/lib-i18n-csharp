using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Catalyst.Internationalization.Extensions {

    /// <summary>
    /// Extensions for the <see cref="ServiceCollection"/> class.
    /// </summary>
    public static class ServiceCollectionExtensions {

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

            // TODO: add main register

        }

    }

}
