using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Catalyst.Internationalization {

    /// <summary>
    /// Options for configuring localization behavior.
    /// </summary>
    public record LocalizationOptions {

        /// <summary>
        /// The default locale to use when no specific locale is provided.
        /// </summary>
        public required Locale DefaultLocale { get; init; } = Locale.en_US;

        /// <summary>
        /// The duration for which cached localization resources are considered valid.
        /// </summary>
        public required TimeSpan CacheDuration { get; init; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Constructs a new <see cref="LocalizationOptions"/>
        /// with the specified parameters.
        /// </summary>
        [SetsRequiredMembers]
        public LocalizationOptions(Locale defaultLocale = Locale.en_US, TimeSpan cacheDuration = default) {
            DefaultLocale = defaultLocale;
            CacheDuration = cacheDuration == TimeSpan.Zero ? TimeSpan.FromHours(1) : cacheDuration;
        }

        /// <summary>
        /// Constructs a new <see cref="LocalizationOptions"/>.
        /// </summary>
        public LocalizationOptions() {
            // ...
        }

    }

}
