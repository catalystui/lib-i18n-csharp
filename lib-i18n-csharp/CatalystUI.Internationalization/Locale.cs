// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Catalyst.Internationalization {

    /// <summary>
    /// A list of recognized localization identifiers for supported languages and regions.
    /// </summary>
    public enum Locale {

        /// <summary>
        /// Arabic (Saudi Arabia).
        /// </summary>
        ar_SA,

        /// <summary>
        /// Bengali (Bangladesh).
        /// </summary>
        bn_BD,

        /// <summary>
        /// German (Germany).
        /// </summary>
        de_DE,

        /// <summary>
        /// English (United Kingdom).
        /// </summary>
        en_GB,

        /// <summary>
        /// English (India).
        /// </summary>
        en_IN,

        /// <summary>
        /// English (United States).
        /// </summary>
        en_US,

        /// <summary>
        /// Spanish (Spain).
        /// </summary>
        es_ES,

        /// <summary>
        /// Spanish (Mexico).
        /// </summary>
        es_MX,

        /// <summary>
        /// Persian (Iran).
        /// </summary>
        fa_IR,

        /// <summary>
        /// French (France).
        /// </summary>
        fr_FR,

        /// <summary>
        /// Hindi (India).
        /// </summary>
        hi_IN,

        /// <summary>
        /// Indonesian (Indonesia).
        /// </summary>
        id_ID,

        /// <summary>
        /// Italian (Italy).
        /// </summary>
        it_IT,

        /// <summary>
        /// Japanese (Japan).
        /// </summary>
        ja_JP,

        /// <summary>
        /// Korean (South Korea).
        /// </summary>
        ko_KR,

        /// <summary>
        /// Dutch (Netherlands).
        /// </summary>
        nl_NL,

        /// <summary>
        /// Polish (Poland).
        /// </summary>
        pl_PL,

        /// <summary>
        /// Portuguese (Brazil).
        /// </summary>
        pt_BR,

        /// <summary>
        /// Russian (Russia).
        /// </summary>
        ru_RU,

        /// <summary>
        /// Tagalog (Philippines).
        /// </summary>
        tl_PH,

        /// <summary>
        /// Turkish (Turkey).
        /// </summary>
        tr_TR,

        /// <summary>
        /// Ukrainian (Ukraine).
        /// </summary>
        uk_UA,

        /// <summary>
        /// Urdu (Pakistan).
        /// </summary>
        ur_PK,

        /// <summary>
        /// Vietnamese (Vietnam).
        /// </summary>
        vi_VN,

        /// <summary>
        /// Chinese (China).
        /// </summary>
        zh_CN,

        /// <summary>
        /// Chinese (Simplified).
        /// </summary>
        zh_Hans,

    }

    /// <summary>
    /// Provides extension methods for the <see cref="Locale"/> enumeration.
    /// </summary>
    public static class LocaleHelper {

        /// <summary>
        /// A dictionary mapping two-letter ISO language codes to their corresponding <see cref="Locale"/> values for fallback purposes.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, Locale> Fallbacks = new Dictionary<string, Locale>(StringComparer.OrdinalIgnoreCase) {
            ["ar"] = Locale.ar_SA,
            ["bn"] = Locale.bn_BD,
            ["de"] = Locale.de_DE,
            ["en"] = Locale.en_US,
            ["es"] = Locale.es_ES,
            ["fa"] = Locale.fa_IR,
            ["fr"] = Locale.fr_FR,
            ["hi"] = Locale.hi_IN,
            ["id"] = Locale.id_ID,
            ["it"] = Locale.it_IT,
            ["ja"] = Locale.ja_JP,
            ["ko"] = Locale.ko_KR,
            ["nl"] = Locale.nl_NL,
            ["pl"] = Locale.pl_PL,
            ["pt"] = Locale.pt_BR,
            ["ru"] = Locale.ru_RU,
            ["tl"] = Locale.tl_PH,
            ["fil"] = Locale.tl_PH,
            ["tr"] = Locale.tr_TR,
            ["uk"] = Locale.uk_UA,
            ["ur"] = Locale.ur_PK,
            ["vi"] = Locale.vi_VN,
            ["zh"] = Locale.zh_Hans,
        };

        /// <summary>
        /// Converts a <see cref="CultureInfo"/> object to its corresponding <see cref="Locale"/> value.
        /// </summary>
        /// <param name="cultureInfo">The <see cref="CultureInfo"/> object to convert.</param>
        /// <returns>The corresponding <see cref="Locale"/> value.</returns>
        public static Locale FromCultureInfo(CultureInfo cultureInfo) {
            ArgumentNullException.ThrowIfNull(cultureInfo);
            string name = cultureInfo.Name.Replace('-', '_');
            if (Enum.TryParse(name, out Locale locale)) {
                return locale;
            }
            return Fallbacks.TryGetValue(cultureInfo.TwoLetterISOLanguageName, out Locale fallback) ? fallback : throw new ArgumentException($"Culture '{cultureInfo.Name}' does not map to a supported locale.", nameof(cultureInfo));
        }

        /// <summary>
        /// Converts a <see cref="Locale"/> value to its corresponding <see cref="CultureInfo"/> object.
        /// </summary>
        /// <param name="locale">The <see cref="Locale"/> value to convert.</param>
        /// <returns>The corresponding <see cref="CultureInfo"/> object.</returns>
        public static CultureInfo ToCultureInfo(Locale locale) {
            if (!Enum.IsDefined(locale)) throw new ArgumentOutOfRangeException(nameof(locale));
            string name = locale.ToString().Replace('_', '-');
            return new(name);
        }

        /// <summary>
        /// Converts a <see cref="Locale"/> value to its string representation, replacing underscores with hyphens.
        /// </summary>
        /// <param name="locale">The locale to convert.</param>
        /// <returns>The string representation of the locale.</returns>
        public static string ToString(Locale locale) {
            return locale.ToString().Replace('_', '-');
        }

        /// <summary>
        /// Converts a string representation of a locale to its corresponding <see cref="Locale"/> value, replacing hyphens with underscores.
        /// </summary>
        /// <param name="localeString">The string representation of the locale.</param>
        /// <returns>The corresponding <see cref="Locale"/> value.</returns>
        public static Locale FromString(string localeString) {
            return Enum.Parse<Locale>(localeString.Replace('-', '_'));
        }

    }

}
