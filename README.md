# lib-i18n-csharp ![Static Badge](https://img.shields.io/badge/Powered_by-.NET-blue?style=flat-square&logo=sharp&logoColor=%23ffffff) ![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/catalystui/lib-i18n-csharp/dotnet.yml?branch=main&style=flat-square)

`Catalyst.Internationalization` provides locale loading, in-memory caching, and fallback lookup for .NET applications. It is the internationalization library used by CatalystUI projects, but it does not depend on a particular UI framework or resource format.

The library handles the lifecycle around translations while leaving storage up to the application. A locale provider can read JSON files, query a database, call an API, or use any other source that fits the project.

## Requirements

- .NET 10 or later
- A dependency injection container compatible with `Microsoft.Extensions.DependencyInjection`
- An implementation of `ILocaleProvider`

## Installation

Install the package from NuGet:

```shell
dotnet add package Catalyst.Internationalization --prerelease
```

The `--prerelease` option is required while the package is in beta. It can be removed once a stable release is available.

## Getting started

### 1. Create a locale provider

A provider loads the translations for one locale into its dictionary. The host copies that data into the cache after `LoadLocaleAsync` completes, so a scoped provider can be reused for each load.

This example reads translations from JSON files stored under a `Locales` directory:

```csharp
using System.Text.Json;

using Catalyst.Internationalization;

public sealed class JsonLocaleProvider : Dictionary<string, string>, ILocaleProvider {

    private readonly IWebHostEnvironment _environment;

    public JsonLocaleProvider(IWebHostEnvironment environment) {
        _environment = environment;
    }

    public async Task LoadLocaleAsync(
        Locale locale,
        CancellationToken cancellationToken = default) {
        string localeName = LocaleHelper.ToString(locale);
        string path = Path.Combine(
            _environment.ContentRootPath,
            "Locales",
            $"{localeName}.json");

        await using FileStream stream = File.OpenRead(path);
        Dictionary<string, string> translations =
            await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(
                stream,
                cancellationToken: cancellationToken)
            ?? new Dictionary<string, string>();

        Clear();
        foreach ((string key, string value) in translations) {
            this[key] = value;
        }
    }
}
```

A corresponding `Locales/en-US.json` file might look like this:

```json
{
  "welcome.title": "Welcome",
  "welcome.message": "Good to see you."
}
```

Translation keys are ordinary strings. Dotted names are only a convention and have no special meaning to the library.

### 2. Register the services

Register the provider and internationalization services during application startup:

```csharp
using Catalyst.Internationalization;
using Catalyst.Internationalization.Extensions;

using Microsoft.Extensions.Options;

builder.Services.AddSingleton(
    Options.Create(new LocalizationOptions(
        defaultLocale: Locale.en_US,
        cacheDuration: TimeSpan.FromHours(1))));

builder.Services.AddLocaleProvider<JsonLocaleProvider>();
builder.Services.AddInternationalization();
```

The default locale is loaded when the application host starts. Other locales are loaded on first use and retained for the configured cache duration.

### 3. Look up a translation

`LocalizationService` uses `CultureInfo.CurrentUICulture` for the primary locale and the configured default locale as its fallback:

```csharp
public sealed class WelcomeMessage {

    private readonly LocalizationService _localization;

    public WelcomeMessage(LocalizationService localization) {
        _localization = localization;
    }

    public Task<string> GetTitleAsync(CancellationToken cancellationToken = default) {
        return _localization.GetAsync("welcome.title", cancellationToken);
    }
}
```

For explicit control over the locale and fallback, inject and use `LocalizationHost` directly:

```csharp
string title = await localizationHost.GetAsync(
    locale: Locale.fr_FR,
    key: "welcome.title",
    fallback: Locale.en_US,
    cancellationToken: cancellationToken);
```

Lookup follows this order:

1. Load the requested locale if it is not already cached.
2. Return the value from the requested locale when the key exists.
3. Try the fallback locale when one was supplied.
4. Return the key itself when no translation is found.

Pass `throwExceptions: true` to `LocalizationHost.GetAsync` when a missing key or provider failure should be treated as an error. Cancellation is always propagated.

## Culture and locale conversion

`LocaleHelper` converts between the library's `Locale` values, culture names, and `CultureInfo` instances:

```csharp
Locale locale = LocaleHelper.FromString("pt-BR");
string name = LocaleHelper.ToString(Locale.zh_Hans);
CultureInfo culture = LocaleHelper.ToCultureInfo(Locale.de_DE);
```

When an exact regional match is unavailable, `FromCultureInfo` falls back by language. For example, `en-CA` maps to `en-US`, and `pt-PT` maps to `pt-BR`. A culture whose language is not supported throws an `ArgumentException`.

## Supported locales

| Language | Locale |
| --- | --- |
| Arabic (Saudi Arabia) | `ar-SA` |
| Bengali (Bangladesh) | `bn-BD` |
| Chinese (China) | `zh-CN` |
| Chinese (Simplified) | `zh-Hans` |
| Dutch (Netherlands) | `nl-NL` |
| English (India) | `en-IN` |
| English (United Kingdom) | `en-GB` |
| English (United States) | `en-US` |
| French (France) | `fr-FR` |
| German (Germany) | `de-DE` |
| Hindi (India) | `hi-IN` |
| Indonesian (Indonesia) | `id-ID` |
| Italian (Italy) | `it-IT` |
| Japanese (Japan) | `ja-JP` |
| Korean (South Korea) | `ko-KR` |
| Persian (Iran) | `fa-IR` |
| Polish (Poland) | `pl-PL` |
| Portuguese (Brazil) | `pt-BR` |
| Russian (Russia) | `ru-RU` |
| Spanish (Mexico) | `es-MX` |
| Spanish (Spain) | `es-ES` |
| Tagalog (Philippines) | `tl-PH` |
| Turkish (Turkey) | `tr-TR` |
| Ukrainian (Ukraine) | `uk-UA` |
| Urdu (Pakistan) | `ur-PK` |
| Vietnamese (Vietnam) | `vi-VN` |

## Main types

| Type | Purpose |
| --- | --- |
| `ILocaleProvider` | Loads key/value translations from application-defined storage. |
| `LocalizationHost` | Loads locales, manages fallback lookup, and participates in the application host lifecycle. |
| `LocalizationService` | Looks up strings using the current UI culture and configured default locale. |
| `LocalizationCache` | Stores loaded locale maps for the configured duration. |
| `LocalizationOptions` | Configures the default locale and cache duration. |
| `LocaleHelper` | Converts between locale enum values, strings, and `CultureInfo`. |
| `LocaleMap` | Represents the cached translation dictionary for a locale. |

## Building locally

From the repository root:

```shell
dotnet restore lib-i18n-csharp/lib-i18n-csharp.sln
dotnet test lib-i18n-csharp/lib-i18n-csharp.sln
dotnet pack lib-i18n-csharp/CatalystUI.Internationalization/CatalystUI.Internationalization.csproj
```

## License

CatalystUI Internationalization is available under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
