using System.Globalization;

using Catalyst.Internationalization.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Catalyst.Internationalization.Tests;

[TestFixture]
public sealed class LocalizationServiceTests {

    /// <summary>
    /// Verifies that service lookups use the current UI culture and fall back to the configured default locale.
    /// </summary>
    [Test]
    [NonParallelizable]
    public async Task GetAsync_UsesCurrentUiCultureAndDefaultLocaleFallback() {
        CultureInfo originalCulture = CultureInfo.CurrentUICulture;
        try {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            ServiceCollection services = new();
            services.AddSingleton<ILocaleProvider>(new CultureAwareProvider());
            services.AddSingleton<IOptions<LocalizationOptions>>(
                Options.Create(new LocalizationOptions(Locale.en_US, TimeSpan.FromMinutes(5))));
            services.AddInternationalization();
            await using IAsyncDisposable providerLifetime = (IAsyncDisposable)services.BuildServiceProvider();
            IServiceProvider provider = (IServiceProvider)providerLifetime;
            LocalizationHost host = (LocalizationHost)provider.GetServices<IHostedService>().Single();
            LocalizationService sut = new(host);

            string localized = await sut.GetAsync("hello");
            string fallback = await sut.GetAsync("default-only");

            Assert.Multiple(() => {
                Assert.That(localized, Is.EqualTo("Bonjour"));
                Assert.That(fallback, Is.EqualTo("Default"));
            });
        } finally {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    /// <summary>
    /// Verifies that the localization service forwards cancellation to locale loading.
    /// </summary>
    [Test]
    [NonParallelizable]
    public async Task GetAsync_PropagatesCancellationToken() {
        CultureInfo originalCulture = CultureInfo.CurrentUICulture;
        try {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            ServiceCollection services = new();
            services.AddSingleton<ILocaleProvider>(new CultureAwareProvider());
            services.AddInternationalization();
            await using IAsyncDisposable providerLifetime = (IAsyncDisposable)services.BuildServiceProvider();
            IServiceProvider provider = (IServiceProvider)providerLifetime;
            LocalizationHost host = (LocalizationHost)provider.GetServices<IHostedService>().Single();
            LocalizationService sut = new(host);
            using CancellationTokenSource cancellation = new();
            cancellation.Cancel();

            Assert.That(async () => await sut.GetAsync("hello", cancellation.Token),
                Throws.InstanceOf<OperationCanceledException>());
        } finally {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    private sealed class CultureAwareProvider : Dictionary<string, string>, ILocaleProvider {

        public Task LoadLocaleAsync(Locale locale, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            Clear();
            if (locale == Locale.fr_FR) this["hello"] = "Bonjour";
            if (locale == Locale.en_US) this["default-only"] = "Default";
            return Task.CompletedTask;
        }
    }
}

[TestFixture]
public sealed class ServiceCollectionExtensionsTests {

    /// <summary>
    /// Verifies that internationalization registration adds its cache and hosted service, preserves options, and returns the collection.
    /// </summary>
    [Test]
    public void AddInternationalization_RegistersExpectedServicesAndHonorsOptions() {
        ServiceCollection services = new();
        services.AddSingleton<IOptions<LocalizationOptions>>(Options.Create(new LocalizationOptions(Locale.it_IT)));

        IServiceCollection returned = services.AddInternationalization();
        using IDisposable providerLifetime = services.BuildServiceProvider();
        IServiceProvider provider = (IServiceProvider)providerLifetime;

        Assert.Multiple(() => {
            Assert.That(returned, Is.SameAs(services));
            Assert.That(provider.GetRequiredService<LocalizationCache>(), Is.Not.Null);
            Assert.That(provider.GetServices<IHostedService>().Single(), Is.TypeOf<LocalizationHost>());
            Assert.That(((LocalizationHost)provider.GetServices<IHostedService>().Single()).DefaultLocale, Is.EqualTo(Locale.it_IT));
        });
    }

    /// <summary>
    /// Verifies that a locale provider is registered with scoped lifetime and that the original collection is returned.
    /// </summary>
    [Test]
    public void AddLocaleProvider_RegistersProviderAsScopedAndReturnsCollection() {
        ServiceCollection services = new();

        IServiceCollection returned = services.AddLocaleProvider<FirstProvider>();
        using IDisposable rootLifetime = services.BuildServiceProvider();
        IServiceProvider root = (IServiceProvider)rootLifetime;
        using IServiceScope firstScope = root.CreateScope();
        using IServiceScope secondScope = root.CreateScope();

        ILocaleProvider first = firstScope.ServiceProvider.GetRequiredService<ILocaleProvider>();
        Assert.Multiple(() => {
            Assert.That(returned, Is.SameAs(services));
            Assert.That(first, Is.TypeOf<FirstProvider>());
            Assert.That(firstScope.ServiceProvider.GetRequiredService<ILocaleProvider>(), Is.SameAs(first));
            Assert.That(secondScope.ServiceProvider.GetRequiredService<ILocaleProvider>(), Is.Not.SameAs(first));
        });
    }

    /// <summary>
    /// Verifies that adding another locale provider does not replace an existing provider registration.
    /// </summary>
    [Test]
    public void AddLocaleProvider_DoesNotReplaceExistingRegistration() {
        ServiceCollection services = new();
        services.AddLocaleProvider<FirstProvider>();

        services.AddLocaleProvider<SecondProvider>();
        using IDisposable providerLifetime = services.BuildServiceProvider();
        IServiceProvider provider = (IServiceProvider)providerLifetime;

        Assert.That(provider.GetRequiredService<ILocaleProvider>(), Is.TypeOf<FirstProvider>());
    }

    private abstract class EmptyProvider : Dictionary<string, string>, ILocaleProvider {
        public Task LoadLocaleAsync(Locale locale, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FirstProvider : EmptyProvider;
    private sealed class SecondProvider : EmptyProvider;
}
