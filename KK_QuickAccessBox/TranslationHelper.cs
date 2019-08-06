using System;
using System.Collections.Generic;
using DynamicTranslationLoader;
using DynamicTranslationLoader.Text;
using Harmony;
using KKAPI.Utilities;
using TARC.Compiler;

namespace KK_QuickAccessBox
{
    public class TranslationHelper
    {
        private static readonly Traverse _translatorCallback;
        private static readonly Dictionary<string, CompiledLine> _translatorCompiledLines;
        private static readonly Traverse _translatorRegex;

        static TranslationHelper()
        {
            var translatorTraverse = Traverse.Create<TextTranslator>();
            _translatorCompiledLines = translatorTraverse.Field("Translations").GetValue<Dictionary<string, CompiledLine>>();
            _translatorRegex = translatorTraverse.Method("TryGetRegex", new[] { typeof(string), typeof(string).MakeByRefType(), typeof(bool) });
            _translatorCallback = Traverse.Create<DynamicTranslator>().Method("OnOnUnableToTranslateTextMeshPro", new[] { typeof(object), typeof(string) });
        }

        private readonly Action<string> _onTextChanged;
        private string _text;

        private TranslationHelper(string text, Action<string> onTextChanged)
        {
            _text = text;
            _onTextChanged = onTextChanged;
        }

        /// <summary>
        /// Needed by autotranslator to set the translation
        /// </summary>
        public string text
        {
            get => _text;
            set
            {
                _text = value;
                _onTextChanged?.Invoke(value);
            }
        }

        public static void Translate(string input, Action<string> updateAction)
        {
            if (_translatorCompiledLines.TryGetValue(input, out var line))
            {
                updateAction(line.TranslatedLine);
                return;
            }

            var args = new object[] { input, "", false };
            if (_translatorRegex.GetValue<bool>(args))
            {
                updateAction((string)args[1]);
                return;
            }

            // Make sure there's a valid value set in case we need to wait
            updateAction(input);

            // XUA needs to run on the main thread
            ThreadingHelper.StartSyncInvoke(() => _translatorCallback.GetValue(new TranslationHelper(input, updateAction), input));
        }
    }
}
