# QuickAccessBox
Plugin for Koikatu / Koikatsu Party and AI-Shoujo / AI-Girl studio that adds a quick access list for searching through all of the items, both stock and modded. The plugin comes with thumbnails for the items, unlike the original studio interface. It's also instant and has low resource footprint, no need to wait for menus to open.

![preview](https://user-images.githubusercontent.com/39247311/61983223-d6fdf680-afff-11e9-8a44-95509f681ce0.png)

To open the quick access box press Left Control + Space (the keybind can be changed in F1 plugin settings, search for "quick"). Video in action: https://www.youtube.com/watch?v=4D3Ibvyxsps

Thanks to AutoTranslator developer for implementing a plugin interface, essu and Keelhauled for help with using renderer bounding boxes and other stuff, and DeathWeasel for help with sideloader stuff.

You can support development of this plugin (and many other) on the [patreon page](https://www.patreon.com/ManlyMarco).

## Installation
1. Make sure your game is updated and has at least [BepInEx v5.0](https://github.com/BepInEx/BepInEx), [BepisPlugins r13.0.3](https://github.com/bbepis/BepisPlugins) and [KKAPI v1.4](https://github.com/ManlyMarco/KKAPI) installed.
2. To get proper translations for the items, get the latest translations for your game. This is necessary to be able to search for the items in English.
3. Install [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator) v3.7.0 or higher to fill in any missing translations.
4. Download the latest release from [releases](/../../releases).
5. Extract contents of the archive into your game's directory (the .dll file should end up inside your BepInEx/plugins folder).
6. Start studio. Once fully loaded in try pressing Ctrl+Space combination and the box should appear. There also should be a "Find" button near the left edge of the screen.

## FAQ
**Q: I don't have the studio in Koikatsu or AI-Shoujo**

A: If you dont have studio you can get it by installing the latest HF Patch for your game.

**Q: The box doesn't appear even though I'm pressing the key combination?**

A: If there is a message in top left corner telling you to wait, wait. If there is no message, go to plugin settings (F1) and search for "quick". If you see the keybind, change it to something else. If you can't see any settings related to this plugin then check your output_log.txt for errors as usual. There should also be a new "Search" button at the left side of the studio screen.

**Q: I updated my translations but the search box still uses the old translations?**

A: Close the game and remove the BepInEx/cache/KK_QuickAccessBox.cache file.

## How to add thumbnails for your items
The plugin contains a thumbnail generator that makes it easy and fast to create thumbnails for your studio items. The generator is controlled from plugin settings.

### Generating thumbnails and using them
1. Open plugin settings and search for "Thumbnail generation".
2. Assign a hotkey to the "Generate item thumbnails" setting.
3. Create an empty directory and copy its path into the "Output directory" setting. This is where the new thumbnails will get saved.
4. Close the plugin settings window and reset current studio scene.
5. Press the "Generate item thumbnails" hotkey. You should see the thumbnails getting generated inside your folder.
6. If there are any thumbnails that need to be adjusted, check the "Manual generation" list below.
7. Place the thumbnails inside your mod's .zipmod file. Put them inside an "abdata/studio_thumbnails" folder as loose .png files *without changing their names*.
8. Restart the game with the newly updated .zipmod file and see if your new thumbnails appear in the quick access list.

### Manual generation
Sometimes the generated thumbnails will have wrong orientation or framing. In that case it's necessary to adjust the camera manually. To be able to properly adjust the camera you need to download the [OrthographicCamera](https://github.com/ManlyMarco/Koikatu-Gameplay-Mod) plugin.

1. In plugin settings turn on "Manual mode - adjust by hand".
2. Remove the bad thumbnail files from your folder.
3. Press the generate thumbnails hotkey.
4. Adjust the camera with your mouse. Zoom in and out with mouse wheel.
5. Once you are happy with the result, press Left Shift to advance to the next item.

### Notes
- You can abort thumbnail generation at any time by pressing and holding the Esc key.
- Resulting thumbnail images (64x64 png files) should be included in your .zipmod file inside the "abdata" folder. The path to the file doesn't matter as long as it is inside abdata. It's recommended to put it in a subfolder instead of directly inside the abdata folder.
- You can override thumbnails from other .zipmod files by making the path to your thumbnail longer (use longer or more folder names, do not change the .png file name).
- The generated .png thumbnails have names that represent the item's Group, Category and Item Name. Do not change thumbnail names or they will not work. If you change any of these you will have to re-take the thumbnail to get the new file name.
- If your items are very bright you can use the "Dark background" setting. Do not overuse this setting to keep consistency.

#### How to get best thumbnail load performance and smallest size
These tips are optional and will have minimal effect unless you are creating a lot of thumbnails (100s).
- Use 64x64 resolution, anything higher will not have a positive quality difference.
- Do not use transparency, instead use 245,245,245 as background color.
- Use 256 colors (8 Bits Per Pixel with no alpha channel).
