using System;
using System.Linq;
using BepInEx;
using XUnity.AutoTranslator.Plugin.Core;

namespace KK_QuickAccessBox
{
    public static class TranslationHelper
    {
        private static readonly Action<string, Action<string>> _translatorCallback;

        static TranslationHelper()
        {
            var xua = Type.GetType("XUnity.AutoTranslator.Plugin.Core.ITranslator, XUnity.AutoTranslator.Plugin.Core", false);
            if (xua != null && xua.GetMethods().Any(x => x.Name == "TranslateAsync"))
            {
                _translatorCallback = (s, action) =>
                {
                    // The lambda doesn't get its types resolved until it's called so this doesn't crash here if the type doesn't exist
                    AutoTranslator.Default.TranslateAsync(s, result => { if (result.Succeeded) action(result.TranslatedText); });
                };
            }
            else
            {
                QuickAccessBox.Logger.LogWarning("Could not find method AutoTranslator.Default.TranslateAsync, item translations will be limited or unavailable");
                _translatorCallback = null;
            }
        }

        public static void Translate(string input, Action<string> updateAction)
        {
            if (updateAction == null) throw new ArgumentNullException(nameof(updateAction));

            if (_translatorCallback != null)
            {
                var didFire = false;
                _translatorCallback(input, s =>
                {
                    updateAction(s);
                    didFire = true;
                    ItemInfoLoader.TriggerCacheSave();
                });
                if (didFire) return;
            }

            // Make sure there's a valid value set
            updateAction(input);
            ItemInfoLoader.TriggerCacheSave();
        }
    }
}
