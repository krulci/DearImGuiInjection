#if NETSTANDARD2_0 || NET462

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using BepInEx;
using DearImGuiInjection.Windows;
using MonoMod.RuntimeDetour;

namespace DearImGuiInjection.BepInEx;

[BepInPlugin(Metadata.GUID, Metadata.Name, Metadata.Version)]
internal class DearImGuiInjectionBaseUnityPlugin : BaseUnityPlugin
{
    private Type _eventSystemType;
    private MethodInfo _eventSystemUpdate;
    private Hook _eventSystemUpdateHook;

    private void Awake()
    {
        Log.Init(new BepInExLog(Logger));

        var imguiIniConfigDirectoryPath = Paths.ConfigPath;

        var assetsFolder = Path.Combine(Path.GetDirectoryName(Info.Location), "Assets");

        var cursorVisibilityConfig = new BepInExConfigEntry<VirtualKey>(
            Config.Bind("Keybinds", "CursorVisibility",
            DearImGuiInjection.CursorVisibilityToggleDefault,
            "Key for switching the cursor visibility."));

        var chineseSimplifiedFontName = new BepInExConfigEntry<string>(
            Config.Bind("Chinese Simplified Common Font Name", "ChineseSimplifiedFontName",
            DearImGuiInjection.ChineseSimplifiedFontFileNameDefault,
            "File name of the custom Chinese Simplified Common font."));
        var chineseFullFontName = new BepInExConfigEntry<string>(
            Config.Bind("Chinese Full Font Name", "ChineseFullFontName",
            DearImGuiInjection.ChineseFullFontFileNameDefault,
            "File name of the custom Chinese Full font."));
        var japaneseFontName = new BepInExConfigEntry<string>(
            Config.Bind("Japanese Font Name", "JapaneseFontName",
            DearImGuiInjection.JapaneseFontFileNameDefault,
            "File name of the custom Japanese font."));
        DearImGuiInjection.Init(imguiIniConfigDirectoryPath, assetsFolder, cursorVisibilityConfig, chineseSimplifiedFontName, chineseFullFontName, japaneseFontName);

        SetupIgnoreUIObjectsWhenImGuiCursorIsVisible();

        gameObject.AddComponent<UnityMainThreadDispatcher>();
    }

    private void SetupIgnoreUIObjectsWhenImGuiCursorIsVisible()
    {
        try
        {
            var allFlags = (BindingFlags)(-1);
            var unityEngineUIDll = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(ass => ass.GetName().Name == "UnityEngine.UI");
            _eventSystemType = unityEngineUIDll.GetType("UnityEngine.EventSystems.EventSystem");
            _eventSystemUpdate = _eventSystemType.GetMethod("Update", allFlags);
            _eventSystemUpdateHook = new Hook(_eventSystemUpdate, IgnoreUIObjectsWhenImGuiCursorIsVisible);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    private static void IgnoreUIObjectsWhenImGuiCursorIsVisible(Action<object> orig, object self)
    {
        if (DearImGuiInjection.IsCursorVisible)
        {
            return;
        }

        orig(self);
    }

    private void OnDestroy()
    {
        _eventSystemUpdateHook?.Dispose();

        DearImGuiInjection.Dispose();
    }
}

#endif