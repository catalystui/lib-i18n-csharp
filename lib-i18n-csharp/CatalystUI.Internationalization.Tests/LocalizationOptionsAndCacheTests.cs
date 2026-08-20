using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Catalyst.Internationalization.Tests;

[TestFixture]
public sealed class LocalizationOptionsTests {

    /// <summary>
    /// Verifies that the parameterized options constructor supplies the documented default locale and cache duration.
    /// </summary>
    [Test]
    public void ParameterizedConstructor_UsesDocumentedDefaults() {
        LocalizationOptions options = new(defaultLocale: Locale.en_US);

        Assert.Multiple(() => {
            Assert.That(options.DefaultLocale, Is.EqualTo(Locale.en_US));
            Assert.That(options.CacheDuration, Is.EqualTo(TimeSpan.FromHours(1)));
        });
    }

    /// <summary>
    /// Verifies that explicitly supplied locale and cache duration values are preserved.
    /// </summary>
    [Test]
    public void ParameterizedConstructor_PreservesCustomValues() {
        TimeSpan duration = TimeSpan.FromMinutes(15);

        LocalizationOptions options = new(Locale.fr_FR, duration);

        Assert.Multiple(() => {
            Assert.That(options.DefaultLocale, Is.EqualTo(Locale.fr_FR));
            Assert.That(options.CacheDuration, Is.EqualTo(duration));
        });
    }

    /// <summary>
    /// Verifies that required options can be configured through an object initializer.
    /// </summary>
    [Test]
    public void ObjectInitializer_AllowsExplicitConfiguration() {
        LocalizationOptions options = new() {
            DefaultLocale = Locale.de_DE,
            CacheDuration = TimeSpan.FromSeconds(30),
        };

        Assert.Multiple(() => {
            Assert.That(options.DefaultLocale, Is.EqualTo(Locale.de_DE));
            Assert.That(options.CacheDuration, Is.EqualTo(TimeSpan.FromSeconds(30)));
        });
    }
}

[TestFixture]
public sealed class LocalizationCacheTests {

    private IMemoryCache _memoryCache = null!;
    private LocalizationCache _sut = null!;

    [SetUp]
    public void SetUp() {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _sut = new LocalizationCache(Options.Create(new LocalizationOptions(defaultLocale: Locale.en_US)), _memoryCache);
    }

    [TearDown]
    public void TearDown() => _memoryCache.Dispose();

    /// <summary>
    /// Verifies that all cache lookup APIs consistently report an uncached locale as missing.
    /// </summary>
    [Test]
    public void MissingLocale_IsReportedConsistently() {
        bool found = _sut.TryGetValue(Locale.ja_JP, out LocaleMap? map);

        Assert.Multiple(() => {
            Assert.That(found, Is.False);
            Assert.That(map, Is.Null);
            Assert.That(_sut.Get(Locale.ja_JP), Is.Null);
            Assert.That(_sut.ContainsKey(Locale.ja_JP), Is.False);
        });
    }

    /// <summary>
    /// Verifies that a cached map is returned by reference through every cache read API.
    /// </summary>
    [Test]
    public void Set_MakesSameMapAvailableThroughAllReadMethods() {
        LocaleMap expected = new() { ["greeting"] = "Hello" };

        _sut.Set(Locale.en_US, expected);
        bool found = _sut.TryGetValue(Locale.en_US, out LocaleMap? actual);

        Assert.Multiple(() => {
            Assert.That(found, Is.True);
            Assert.That(actual, Is.SameAs(expected));
            Assert.That(_sut.Get(Locale.en_US), Is.SameAs(expected));
            Assert.That(_sut.ContainsKey(Locale.en_US), Is.True);
        });
    }

    /// <summary>
    /// Verifies that replacing one locale's map does not modify maps cached for other locales.
    /// </summary>
    [Test]
    public void Set_ReplacesExistingMapForLocaleWithoutAffectingOthers() {
        LocaleMap first = new() { ["key"] = "first" };
        LocaleMap replacement = new() { ["key"] = "replacement" };
        LocaleMap french = new() { ["key"] = "français" };
        _sut.Set(Locale.en_US, first);
        _sut.Set(Locale.fr_FR, french);

        _sut.Set(Locale.en_US, replacement);

        Assert.Multiple(() => {
            Assert.That(_sut.Get(Locale.en_US), Is.SameAs(replacement));
            Assert.That(_sut.Get(Locale.fr_FR), Is.SameAs(french));
        });
    }

    /// <summary>
    /// Verifies that the cache rejects a null locale map.
    /// </summary>
    [Test]
    public void Set_RejectsNullMap() {
        Assert.That(() => _sut.Set(Locale.en_US, null!), Throws.ArgumentNullException);
    }
}

[TestFixture]
public sealed class LocaleMapTests {

    /// <summary>
    /// Verifies that a locale map supports normal mutable string-dictionary operations.
    /// </summary>
    [Test]
    public void NewMap_BehavesLikeAMutableStringDictionary() {
        LocaleMap map = new() { ["hello"] = "Hello" };

        map["hello"] = "Hi";
        map.Add("goodbye", "Bye");

        Assert.That(map, Is.EquivalentTo(new Dictionary<string, string> {
            ["hello"] = "Hi",
            ["goodbye"] = "Bye",
        }));
    }
}
