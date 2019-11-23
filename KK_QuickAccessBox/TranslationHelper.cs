using System;
using System.Linq;
using BepInEx;
using HarmonyLib;
using XUnity.AutoTranslator.Plugin.Core;
using LogLevel = BepInEx.Logging.LogLevel;

namespace KK_QuickAccessBox
{
    public static class TranslationHelper
    {
        private static readonly Action<string, Action<string>> _translatorCallback;
        private static readonly Traverse _translatorGet;

        static TranslationHelper()
        {
            var dtl = Traverse.Create(Type.GetType("DynamicTranslationLoader.Text.TextTranslator, DynamicTranslationLoader", false));
            // public static string TryGetTranslation(string toTranslate)
            _translatorGet = dtl.Method("TryGetTranslation", new[] { typeof(string) });
            if (!_translatorGet.MethodExists())
                QuickAccessBox.Logger.Log(LogLevel.Warning, "[KK_QuickAccessBox] Could not find method DynamicTranslationLoader.Text.TextTranslator.TryGetTranslation, item translations will be limited or unavailable");

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
                QuickAccessBox.Logger.Log(LogLevel.Warning, "[KK_QuickAccessBox] Could not find method AutoTranslator.Default.TranslateAsync, item translations will be limited or unavailable");
                _translatorCallback = null;
            }
        }

        public static void Translate(string input, Action<string> updateAction)
        {
            if (updateAction == null) throw new ArgumentNullException(nameof(updateAction));

            if (_translatorGet.MethodExists())
            {
                var result = _translatorGet.GetValue<string>(input);
                if (result != null)
                {
                    updateAction(result);
                    return;
                }
            }

            // Make sure there's a valid value set in case we need to wait
            updateAction(input);

            if (_translatorCallback != null)
            {
                // XUA needs to run on the main thread
#pragma warning disable 618
                KKAPI.KoikatuAPI.SynchronizedInvoke(() => _translatorCallback(input, updateAction));
#pragma warning restore 618
            }
        }
    }
}
