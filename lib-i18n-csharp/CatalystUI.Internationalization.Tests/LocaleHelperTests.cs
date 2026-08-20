using System.Globalization;

namespace Catalyst.Internationalization.Tests;

[TestFixture]
public sealed class LocaleHelperTests {

    /// <summary>
    /// Verifies that an exactly supported culture name maps to its corresponding locale.
    /// </summary>
    [TestCase("en-US", Locale.en_US)]
    [TestCase("en-GB", Locale.en_GB)]
    [TestCase("pt-BR", Locale.pt_BR)]
    [TestCase("zh-Hans", Locale.zh_Hans)]
    public void FromCultureInfo_MapsExactSupportedCultures(string cultureName, Locale expected) {
        Assert.That(LocaleHelper.FromCultureInfo(CultureInfo.GetCultureInfo(cultureName)), Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that an unsupported regional culture uses the configured language-level fallback locale.
    /// </summary>
    [TestCase("en-CA", Locale.en_US)]
    [TestCase("es-AR", Locale.es_ES)]
    [TestCase("pt-PT", Locale.pt_BR)]
    [TestCase("zh-TW", Locale.zh_Hans)]
    public void FromCultureInfo_UsesLanguageFallbackForUnsupportedRegions(string cultureName, Locale expected) {
        Assert.That(LocaleHelper.FromCultureInfo(CultureInfo.GetCultureInfo(cultureName)), Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that converting a null culture fails with an argument-null exception.
    /// </summary>
    [Test]
    public void FromCultureInfo_RejectsNull() {
        Assert.That(() => LocaleHelper.FromCultureInfo(null!), Throws.ArgumentNullException);
    }

    /// <summary>
    /// Verifies that a culture with no supported locale or fallback is rejected with the correct parameter name.
    /// </summary>
    [Test]
    public void FromCultureInfo_RejectsUnsupportedLanguage() {
        Assert.That(
            () => LocaleHelper.FromCultureInfo(CultureInfo.GetCultureInfo("sv-SE")),
            Throws.ArgumentException.With.Property("ParamName").EqualTo("cultureInfo"));
    }

    /// <summary>
    /// Verifies that locale strings use the standard hyphen separator instead of the enum's underscore.
    /// </summary>
    [TestCase(Locale.en_US, "en-US")]
    [TestCase(Locale.zh_Hans, "zh-Hans")]
    [TestCase(Locale.tl_PH, "tl-PH")]
    public void ToString_UsesStandardCultureSeparator(Locale locale, string expected) {
        Assert.That(LocaleHelper.ToString(locale), Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that locale strings using either hyphens or underscores parse to the expected locale.
    /// </summary>
    [TestCase("en-US", Locale.en_US)]
    [TestCase("en_US", Locale.en_US)]
    [TestCase("zh-Hans", Locale.zh_Hans)]
    public void FromString_AcceptsHyphensAndUnderscores(string value, Locale expected) {
        Assert.That(LocaleHelper.FromString(value), Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that parsing an unknown locale string fails rather than producing an undefined locale.
    /// </summary>
    [Test]
    public void FromString_RejectsUnknownLocale() {
        Assert.That(() => LocaleHelper.FromString("sv-SE"), Throws.TypeOf<ArgumentException>());
    }

    /// <summary>
    /// Verifies that a supported locale converts to a culture with the matching standard culture name.
    /// </summary>
    [TestCase(Locale.en_US, "en-US")]
    [TestCase(Locale.zh_Hans, "zh-Hans")]
    public void ToCultureInfo_ReturnsMatchingCulture(Locale locale, string expectedName) {
        Assert.That(LocaleHelper.ToCultureInfo(locale).Name, Is.EqualTo(expectedName));
    }

    /// <summary>
    /// Verifies that an undefined locale enum value is rejected before culture conversion.
    /// </summary>
    [Test]
    public void ToCultureInfo_RejectsUndefinedEnumValue() {
        Assert.That(
            () => LocaleHelper.ToCultureInfo((Locale)int.MaxValue),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("locale"));
    }

    /// <summary>
    /// Verifies that language fallback lookup is case-insensitive.
    /// </summary>
    [Test]
    public void Fallbacks_AreCaseInsensitive() {
        Assert.That(LocaleHelper.Fallbacks["EN"], Is.EqualTo(Locale.en_US));
    }
}
