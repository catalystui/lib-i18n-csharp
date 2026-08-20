# lib-i18n-csharp ![Static Badge](https://img.shields.io/badge/Powered_by-.NET-blue?style=flat-square&logo=sharp&logoColor=%23ffffff) ![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/catalystui/lib-i18n-csharp/dotnet.yml?branch=main&style=flat-square)

Internationalization support for .NET applications built around a simple provider model.

`Catalyst.Internationalization` handles locale selection, resource caching, fallback behavior, and integration with .NET hosting. It does not define where translations come from. Applications provide an `ILocaleProvider`, which can load resources from JSON, embedded files, a database, an API, or anywhere else that makes sense for the application.

The package is designed for dependency injection and is marked as NativeAOT-compatible.

## Installation

```shell
dotnet add package Catalyst.Internationalization
```

## Getting started

A locale provider represents the resources for one loaded locale. Implement `ILocaleProvider`, populate the dictionary when `LoadLocaleAsync` is called, and register the provider with the service collection.

```csharp
using Catalyst.Internationalization;

public sealed class AppLocaleProvider : Dictionary<string, string>, ILocaleProvider {

    public Task LoadLocaleAsync(
        Locale locale,
        CancellationToken cancellationToken = default) {

        Clear();

        switch (locale) {
            case Locale.en_US:
                this["hello"] = "Hello!";
                break;

            case Locale.es_ES:
                this["hello"] = "¡Hola!";
                break;
        }

        return Task.CompletedTask;
    }

}
```

Register the provider and Catalyst internationalization services:

```csharp
using Catalyst.Internationalization.Extensions;

services.AddLocaleProvider<AppLocaleProvider>();
services.AddInternationalization();
```

`ILocaleProvider` is registered as a scoped service. The localization host creates a new scope when a locale needs to be loaded, snapshots the provider's resources into a `LocaleMap`, and caches that map for later requests.

Only one locale provider is used. `AddLocaleProvider<T>()` uses first-registration-wins behavior, so later locale-provider registrations will not replace an existing provider.

## Fetching localized strings

`LocalizationService` uses `CultureInfo.CurrentUICulture` to select the locale automatically:

```csharp
public sealed class ExampleService {

    private readonly LocalizationService _localization;

    public ExampleService(LocalizationService localization) {
        _localization = localization;
    }

    public async Task<string> GetGreetingAsync(
        CancellationToken cancellationToken = default) {

        return await _localization.GetAsync("hello", cancellationToken);
    }

}
```

If the current UI culture does not contain the requested key, the configured default locale is used as a fallback. If the key still cannot be found, the key itself is returned.

For explicit locale selection or exception behavior, use `LocalizationHost.GetAsync` directly:

```csharp
string value = await host.GetAsync(
    Locale.es_ES,
    "hello",
    fallback: Locale.en_US,
    throwExceptions: true,
    cancellationToken);
```

## Locale providers

`ILocaleProvider` is intentionally small:

```csharp
public interface ILocaleProvider : IReadOnlyDictionary<string, string> {

    Task LoadLocaleAsync(
        Locale locale,
        CancellationToken cancellationToken = default);

}
```

The provider owns resource loading. Catalyst only requires that, after `LoadLocaleAsync` completes, enumerating the provider returns the key/value resources for that locale.

Because providers are scoped, implementations may safely keep state associated with the locale currently being loaded. The host copies that state into its own cached `LocaleMap` before the provider scope is disposed.

## Caching

Loaded locale maps are kept in an `IMemoryCache` through `LocalizationCache`.

The default cache duration is one hour. When an entry expires, the next request for that locale causes the provider to load it again.

Provider failures are also cached temporarily as an empty locale map. This prevents a failing resource source from being hit repeatedly on every localization request. Cancellation is never swallowed and continues to propagate to the caller.

## Locale selection

`LocaleHelper` converts between Catalyst locales and .NET `CultureInfo` values:

```csharp
Locale locale = LocaleHelper.FromCultureInfo(CultureInfo.CurrentUICulture);
CultureInfo culture = LocaleHelper.ToCultureInfo(Locale.en_US);
```

Exact culture names are preferred. When an exact locale is not available, Catalyst falls back by language where a mapping exists. For example, an unsupported English regional culture falls back to `en-US`.

Catalyst currently defines locales for Arabic, Bengali, German, English, Spanish, Persian, French, Hindi, Indonesian, Italian, Japanese, Korean, Dutch, Polish, Portuguese, Russian, Tagalog/Filipino, Turkish, Ukrainian, Urdu, Vietnamese, and Chinese, with regional variants where defined by the `Locale` enum.

## Defaults

Unless configured otherwise:

- Default locale: `en-US`
- Cache duration: 1 hour
- Missing key: returns the requested key
- Provider lifetime: scoped
- Locale maps: cached in memory

## NativeAOT

The package is built with NativeAOT compatibility enabled and verifies AOT compatibility of its references. Locale-provider registration is generic and preserves the public constructors required by .NET dependency injection.

No assembly scanning or runtime provider discovery is required. Providers are registered explicitly:

```csharp
services.AddLocaleProvider<AppLocaleProvider>();
```

## License

CatalystUI Internationalization is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).

Copyright © 2026 CatalystUI LLC.

## Links

- [CatalystUI](https://www.catalystui.org/)
- [Source repository](https://www.github.com/catalystui/lib-i18n-csharp/)
