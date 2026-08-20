using System.Collections.Generic;

namespace Catalyst.Internationalization {

    /// <summary>
    /// A dictionary that maps localization keys to their corresponding localized strings.
    /// </summary>
    public sealed class LocaleMap : Dictionary<string, string> {

        /// <inheritdoc/>
        internal LocaleMap(IEnumerable<KeyValuePair<string, string>> entries) : base(entries) {
            // ...
        }

        /// <inheritdoc/>
        public LocaleMap() {
            // ...
        }

    }

}
