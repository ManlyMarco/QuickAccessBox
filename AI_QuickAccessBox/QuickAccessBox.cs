using BepInEx;

namespace KK_QuickAccessBox
{
    [BepInProcess("StudioNEOV2")]
    [BepInDependency(Screencap.ScreenshotManager.GUID, Screencap.ScreenshotManager.Version)]
    public partial class QuickAccessBox
    {
        public const string GUID = "AI_QuickAccessBox";
    }
}