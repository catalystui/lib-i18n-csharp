using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Catalyst.Internationalization.Tests;

[TestFixture]
public sealed class LocalizationHostTests {

    /// <summary>
    /// Verifies that host startup loads and caches the configured default locale.
    /// </summary>
    [Test]
    public async Task StartAsync_LoadsConfiguredDefaultLocale() {
        TestContext context = CreateContext(new Dictionary<Locale, Dictionary<string, string>> {
            [Locale.fr_FR] = new() { ["hello"] = "Bonjour" },
        }, defaultLocale: Locale.fr_FR);
        await using (context) {
            await context.Host.StartAsync(CancellationToken.None);

            Assert.Multiple(() => {
                Assert.That(context.Provider.RequestedLocales, Is.EqualTo(new[] { Locale.fr_FR }));
                Assert.That(context.Cache.Get(Locale.fr_FR)!["hello"], Is.EqualTo("Bonjour"));
                Assert.That(context.Host.DefaultLocale, Is.EqualTo(Locale.fr_FR));
            });
        }
    }

    /// <summary>
    /// Verifies that host shutdown completes successfully without additional work.
    /// </summary>
    [Test]
    public async Task StopAsync_CompletesSuccessfully() {
        await using TestContext context = CreateContext();
        Assert.That(context.Host.StopAsync(CancellationToken.None).IsCompleted, Is.True);
        await context.Host.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// Verifies that loading a locale caches a snapshot of the provider's data rather than the mutable provider itself.
    /// </summary>
    [Test]
    public async Task LoadLocale_LoadsProviderDataAndCachesSnapshot() {
        await using TestContext context = CreateContext(new Dictionary<Locale, Dictionary<string, string>> {
            [Locale.en_US] = new() { ["hello"] = "Hello" },
        });

        await context.Host.LoadLocale(Locale.en_US);
        context.Provider["hello"] = "Changed after load";

        Assert.Multiple(() => {
            Assert.That(context.Cache.Get(Locale.en_US)!["hello"], Is.EqualTo("Hello"));
            Assert.That(context.Provider.RequestedLocales, Is.EqualTo(new[] { Locale.en_US }));
        });
    }

    /// <summary>
    /// Verifies that loading an already cached locale does not invoke the provider again.
    /// </summary>
    [Test]
    public async Task LoadLocale_WhenAlreadyCached_DoesNotCallProvider() {
        await using TestContext context = CreateContext();
        context.Cache.Set(Locale.en_US, new LocaleMap { ["hello"] = "Cached" });

        await context.Host.LoadLocale(Locale.en_US);

        Assert.That(context.Provider.RequestedLocales, Is.Empty);
    }

    /// <summary>
    /// Verifies that a provider failure is rethrown and cached as an empty map to prevent repeated load attempts.
    /// </summary>
    [Test]
    public async Task LoadLocale_WhenProviderFails_CachesEmptyMapAndRethrows() {
        await using TestContext context = CreateContext();
        context.Provider.Exception = new InvalidOperationException("Provider unavailable");

        Assert.That(async () => await context.Host.LoadLocale(Locale.en_US),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Provider unavailable"));
        await context.Host.LoadLocale(Locale.en_US);

        Assert.Multiple(() => {
            Assert.That(context.Provider.RequestedLocales, Has.Count.EqualTo(1));
            Assert.That(context.Cache.Get(Locale.en_US), Is.Empty);
        });
    }

    /// <summary>
    /// Verifies that cancellation is propagated without caching an empty failure result.
    /// </summary>
    [Test]
    public async Task LoadLocale_WhenCancelled_DoesNotCacheFailure() {
        await using TestContext context = CreateContext();
        context.Provider.Exception = new OperationCanceledException();

        Assert.That(async () => await context.Host.LoadLocale(Locale.en_US), Throws.InstanceOf<OperationCanceledException>());

        Assert.That(context.Cache.ContainsKey(Locale.en_US), Is.False);
    }

    /// <summary>
    /// Verifies that a primary-locale match is returned without unnecessarily loading the fallback locale.
    /// </summary>
    [Test]
    public async Task GetAsync_ReturnsPrimaryTranslationWithoutLoadingFallback() {
        await using TestContext context = CreateContext(new Dictionary<Locale, Dictionary<string, string>> {
            [Locale.es_MX] = new() { ["hello"] = "Hola" },
            [Locale.en_US] = new() { ["hello"] = "Hello" },
        });

        string result = await context.Host.GetAsync(Locale.es_MX, "hello", Locale.en_US);

        Assert.Multiple(() => {
            Assert.That(result, Is.EqualTo("Hola"));
            Assert.That(context.Provider.RequestedLocales, Is.EqualTo(new[] { Locale.es_MX }));
        });
    }

    /// <summary>
    /// Verifies that the fallback translation is returned when the primary locale does not contain the requested key.
    /// </summary>
    [Test]
    public async Task GetAsync_ReturnsFallbackTranslationWhenPrimaryLacksKey() {
        await using TestContext context = CreateContext(new Dictionary<Locale, Dictionary<string, string>> {
            [Locale.es_MX] = new() { ["goodbye"] = "Adiós" },
            [Locale.en_US] = new() { ["hello"] = "Hello" },
        });

        string result = await context.Host.GetAsync(Locale.es_MX, "hello", Locale.en_US);

        Assert.Multiple(() => {
            Assert.That(result, Is.EqualTo("Hello"));
            Assert.That(context.Provider.RequestedLocales, Is.EqualTo(new[] { Locale.es_MX, Locale.en_US }));
        });
    }

    /// <summary>
    /// Verifies that a missing translation returns its key when exception throwing is disabled.
    /// </summary>
    [Test]
    public async Task GetAsync_WhenKeyIsMissing_ReturnsKeyByDefault() {
        await using TestContext context = CreateContext();

        string result = await context.Host.GetAsync(Locale.en_US, "missing");

        Assert.That(result, Is.EqualTo("missing"));
    }

    /// <summary>
    /// Verifies that a missing translation throws a key-not-found exception when requested.
    /// </summary>
    [Test]
    public async Task GetAsync_WhenKeyIsMissingAndExceptionsRequested_Throws() {
        await using TestContext context = CreateContext();

        Assert.That(
            async () => await context.Host.GetAsync(Locale.en_US, "missing", throwExceptions: true),
            Throws.TypeOf<KeyNotFoundException>().With.Message.Contain("missing"));
    }

    /// <summary>
    /// Verifies that provider load failures are either suppressed or propagated according to the exception setting.
    /// </summary>
    [TestCase(false, ExpectedResult = "hello")]
    [TestCase(true, ExpectedResult = null)]
    public async Task<string?> GetAsync_ControlsProviderFailurePropagation(bool throwExceptions) {
        await using TestContext context = CreateContext();
        context.Provider.Exception = new InvalidOperationException("load failed");

        if (throwExceptions) {
            Assert.That(
                async () => await context.Host.GetAsync(Locale.en_US, "hello", throwExceptions: true),
                Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("load failed"));
            return null;
        }

        return await context.Host.GetAsync(Locale.en_US, "hello");
    }

    /// <summary>
    /// Verifies that cancellation is always propagated even when ordinary provider failures would be suppressed.
    /// </summary>
    [Test]
    public async Task GetAsync_AlwaysPropagatesCancellation() {
        await using TestContext context = CreateContext();
        context.Provider.Exception = new OperationCanceledException();

        Assert.That(
            async () => await context.Host.GetAsync(Locale.en_US, "hello"),
            Throws.InstanceOf<OperationCanceledException>());
    }

    /// <summary>
    /// Verifies that a fallback-provider failure returns the requested key when exception throwing is disabled.
    /// </summary>
    [Test]
    public async Task GetAsync_WhenFallbackProviderFails_ReturnsKeyByDefault() {
        await using TestContext context = CreateContext();
        context.Cache.Set(Locale.es_MX, new LocaleMap());
        context.Provider.Exception = new InvalidOperationException("fallback failed");

        string result = await context.Host.GetAsync(Locale.es_MX, "hello", Locale.en_US);

        Assert.That(result, Is.EqualTo("hello"));
    }

    /// <summary>
    /// Verifies that a fallback-provider failure is rethrown when exception throwing is enabled.
    /// </summary>
    [Test]
    public async Task GetAsync_WhenFallbackProviderFailsAndExceptionsRequested_Rethrows() {
        await using TestContext context = CreateContext();
        context.Cache.Set(Locale.es_MX, new LocaleMap());
        context.Provider.Exception = new InvalidOperationException("fallback failed");

        Assert.That(
            async () => await context.Host.GetAsync(Locale.es_MX, "hello", Locale.en_US, throwExceptions: true),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("fallback failed"));
    }

    private static TestContext CreateContext(
        IReadOnlyDictionary<Locale, Dictionary<string, string>>? resources = null,
        Locale defaultLocale = Locale.en_US) {
        FakeLocaleProvider provider = new(resources);
        ServiceCollection services = new();
        services.AddSingleton<ILocaleProvider>(provider);
        IServiceProvider serviceProvider = services.BuildServiceProvider();
        MemoryCache memoryCache = new(new MemoryCacheOptions());
        LocalizationOptions options = new(defaultLocale);
        LocalizationCache cache = new(Options.Create(options), memoryCache);
        LocalizationHost host = new(Options.Create(options), cache, serviceProvider.GetRequiredService<IServiceScopeFactory>());
        return new(host, cache, provider, memoryCache, serviceProvider);
    }

    private sealed class FakeLocaleProvider : Dictionary<string, string>, ILocaleProvider {

        private readonly IReadOnlyDictionary<Locale, Dictionary<string, string>> _resources;

        public List<Locale> RequestedLocales { get; } = [];
        public Exception? Exception { get; set; }

        public FakeLocaleProvider(IReadOnlyDictionary<Locale, Dictionary<string, string>>? resources) {
            _resources = resources ?? new Dictionary<Locale, Dictionary<string, string>>();
        }

        public Task LoadLocaleAsync(Locale locale, CancellationToken cancellationToken = default) {
            RequestedLocales.Add(locale);
            cancellationToken.ThrowIfCancellationRequested();
            if (Exception is not null) throw Exception;
            Clear();
            if (_resources.TryGetValue(locale, out Dictionary<string, string>? values)) {
                foreach ((string key, string value) in values) this[key] = value;
            }
            return Task.CompletedTask;
        }
    }

    private sealed record TestContext(
        LocalizationHost Host,
        LocalizationCache Cache,
        FakeLocaleProvider Provider,
        MemoryCache MemoryCache,
        IServiceProvider ServiceProvider) : IAsyncDisposable {

        public async ValueTask DisposeAsync() {
            MemoryCache.Dispose();
            if (ServiceProvider is IAsyncDisposable asyncDisposable) {
                await asyncDisposable.DisposeAsync();
            } else if (ServiceProvider is IDisposable disposable) {
                disposable.Dispose();
            }
        }
    }
}
