# KK_QuickAccessBox
Plugin for CharaStudio that adds a quick access list for searching through all of the items, both stock and modded. The plugin comes with thumbnails for the items, unlike the original studio interface. It's also instant and has low resource footprint, no need to wait for menus to open.

To open the quick access box press Left Control + Space (the keybind can be changed in F1 plugin settings, search for "quick"). Video in action: https://www.youtube.com/watch?v=4D3Ibvyxsps

Thanks to essu and Keelhauled for help with using renderer bounding boxes and other stuff.

You can support development of this plugin (and many other) on the [patreon page](https://www.patreon.com/ManlyMarco).

## Installation
1. Make sure your game is updated and has at least [BepInEx v4.1](https://github.com/BepInEx/BepInEx), BepisPlugins r10 and [KKAPI v1.3.8](https://github.com/ManlyMarco/KKAPI) installed. Optionally install [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator) v3.7.0 or higher to fill in any missing translations.
2. Download the latest release from [releases](/../../releases).
3. Extract contents of the archive into your game's directory (the .dll file should end up inside your BepInEx folder).
4. Start CharaStudio.exe. Once fully loaded in try pressing the key combination and the box should appear.

## FAQ
**Q: How to get proper translations of the items into English?**

A: Get the latest BepisPlugins (DynamicTranslator to be specific), [bbepis/KoikatsuTranslation
](https://github.com/bbepis/KoikatsuTranslation) and [DeathWeasel1337/Koikatsu-Plugin-Translations](https://github.com/DeathWeasel1337/Koikatsu-Plugin-Translations). If the item was not translated by these, you will have to get AutoTranslator v3.7.0 or newer to fill in the gaps.

**Q: The box doesn't appear even though I'm pressing the key combination?**

A: Go to plugin settings (F1) and search for "quick". If you see the keybind, change it to something else. If you can't see any settings related to this plugin then check your koikatu_data\output_log.txt for errors as usual.
