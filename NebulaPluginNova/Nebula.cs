﻿global using BepInEx.Unity.IL2CPP.Utils.Collections;
global using Il2CppInterop.Runtime;
global using Nebula.Extensions;
global using Nebula.Utilities;
global using Nebula.Game;
global using Nebula.Player;
global using Nebula.Modules;
global using Nebula.Configuration;
global using UnityEngine;
global using Nebula.Modules.ScriptComponents;
global using System.Collections;
global using HarmonyLib;
global using Timer = Nebula.Modules.ScriptComponents.Timer;
global using Color = UnityEngine.Color;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Nebula;
using Nebula.Roles;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;
using System.Reflection;
using Nebula.Patches;
using Il2CppSystem.Net.NetworkInformation;

namespace Nebula;

[NebulaPreLoad]
public static class ToolsInstaller
{
    public static IEnumerator CoLoad()
    {
        Patches.LoadPatch.LoadingText = "Installing Tools";
        yield return null;

        InstallTool("VoiceChatSupport");
    }
    private static void InstallTool(string name)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream? stream = assembly.GetManifestResourceStream("Nebula.Resources.Tools." + name + ".exe");
        if (stream == null) return;

        var file = File.Create(name + ".exe");
        byte[] data = new byte[stream.Length];
        stream.Read(data);
        file.Write(data);
        stream.Close();
        file.Close();
    }
}

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("Among Us.exe")]
public class NebulaPlugin : BasePlugin
{
    public const string AmongUsVersion = "2023.7.12";
    public const string PluginGuid = "jp.dreamingpig.amongus.nebula";
    public const string PluginName = "NebulaOnTheShip";
    public const string PluginVersion = "0.1.0";

    public const bool IsSnapshot = false;
    public const string MajorCodeName = "Experimental"/*"Haro"*/;
    public const string SnapshotVersion = "23.08.01";
    public const string VisualPluginVersion = "7 hotfix";

    static public HttpClient HttpClient
    {
        get
        {
            if (httpClient == null) httpClient = new HttpClient();
            return httpClient;
        }
    }
    static private HttpClient? httpClient = null;

    public static new NebulaLog Log { get; private set; } = new();

    public static bool FinishedPreload { get; private set; } = false;

    public static string GetNebulaVersionString()
    {
        return "NoS " + MajorCodeName + " " + VisualPluginVersion;
        /*
        if (IsSnapshot)
            return "NoS Snapshot " + SnapshotVersion;
        else
            return "NoS " + MajorCodeName + " v" + VisualPluginVersion;
        */
    }

    public Harmony Harmony = new Harmony(PluginGuid);

    [DllImport("user32.dll", EntryPoint = "SetWindowText")]
    public static extern bool SetWindowText(System.IntPtr hwnd, System.String lpString);
    [DllImport("user32.dll", EntryPoint = "FindWindow")]
    public static extern System.IntPtr FindWindow(System.String className, System.String windowName);

    public bool IsPreferential => Log.IsPreferential;
    public static NebulaPlugin MyPlugin { get; private set; } = null!;
    public IEnumerator CoLoad()
    {
        VanillaAsset.LoadAssetAtInitialize();
        yield return Preload();
    }

    private IEnumerator Preload()
    {
        var types = Assembly.GetAssembly(typeof(RemoteProcessBase))?.GetTypes().Where((type) => type.IsDefined(typeof(NebulaPreLoad)));
        if (types != null)
        {
            List<Type> PostLoad = new();
            HashSet<Type> Loaded = new();

            IEnumerator Preload(Type type, bool isFinalize)
            {
                if (Loaded.Contains(type)) yield break;

                if (type.IsDefined(typeof(NebulaPreLoad)))
                {
                    var myPreType = type.GetCustomAttribute<NebulaPreLoad>()!;
                    var preTypes = myPreType.PreLoadType;
                    foreach (var pretype in preTypes) yield return Preload(pretype, isFinalize);
                    if (!isFinalize && myPreType.MyFinalizerType != NebulaPreLoad.FinalizerType.NotFinalizer)
                    {
                        if (myPreType.MyFinalizerType == NebulaPreLoad.FinalizerType.LoadOnly)
                        {
                            UnityEngine.Debug.Log("Preload (static constructor) " + type.Name);
                            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                        }

                        PostLoad.Add(type);
                        yield break;
                    }
                }

                UnityEngine.Debug.Log("Preload " + type.Name);

                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);

                var loadMethod = type.GetMethod("Load");
                if (loadMethod != null)
                {
                    try
                    {
                        loadMethod.Invoke(null, null);
                        UnityEngine.Debug.Log("Preloaded type " + type.Name + " has Load()");
                    }
                    catch
                    {
                        UnityEngine.Debug.Log("Preloaded type " + type.Name + " has Load with unregulated parameters.");
                    }
                }

                var coloadMethod = type.GetMethod("CoLoad");
                if (coloadMethod != null)
                {
                    IEnumerator? coload = null;
                    try
                    {
                        coload = (IEnumerator)coloadMethod.Invoke(null, null)!;
                        UnityEngine.Debug.Log("Preloaded type " + type.Name + " has CoLoad");
                    }
                    catch 
                    {
                        UnityEngine.Debug.Log("Preloaded type " + type.Name + " has CoLoad with unregulated parameters.");
                    }
                    if (coload != null) yield return coload;
                }

                Loaded.Add(type);
            }

            foreach (var type in types) yield return Preload(type, false);
            foreach (var type in PostLoad) yield return Preload(type, true);
        }
        FinishedPreload = true;
    }

    override public void Load()
    {
        MyPlugin = this;

        Harmony.PatchAll();

        SetWindowText(FindWindow(null!, Application.productName),"Among Us w/ " + GetNebulaVersionString());

        SceneManager.sceneLoaded += (UnityEngine.Events.UnityAction<Scene, LoadSceneMode>)((scene, loadMode) =>
        {
            new GameObject("NebulaManager").AddComponent<NebulaManager>();
        });

        string j;
        j = File.ReadAllText("Json/json1.json");
        Debug.Log(j);
        var json1 = JsonStructure.DeserializeRaw(j);
        j = File.ReadAllText("Json/json2.json");
        Debug.Log(j);
        var json2 = JsonStructure.DeserializeRaw(j);
        Debug.Log("j1:" + (json1 == null ? "null" : json1.Serialize()));
        Debug.Log("j2:" + (json2 == null ? "null" : json2.Serialize()));
        json1.MergeWith(json2);
        File.WriteAllText("Json/merged.json", json1.Serialize());
    }

    public static void Test()
    {
        var obj = UnityHelper.CreateObject("Icon", null, new Vector3(0, 0, -100f), LayerExpansion.GetUILayer());

        var guageLoader = SpriteLoader.FromResource("Nebula.Resources.AttributeGuage.png", 100f);
        var loader= XOnlyDividedSpriteLoader.FromResource("Nebula.Resources.AttributeIcon.png", 100f, 33, true);
        obj.AddComponent<SpriteRenderer>().sprite = loader.GetSprite(0);

        var guage= UnityHelper.CreateObject<SpriteRenderer>("Guage", obj.transform, new Vector3(0, 0, -1f));
        guage.sprite = guageLoader.GetSprite();
        guage.material.shader = NebulaAsset.GuageShader;
        guage.material.SetFloat("_Guage",0.4f);
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Awake))]
public static class AmongUsClientAwakePatch
{
    public static bool IsFirstFlag = true;
    
    public static void Postfix(AmongUsClient __instance)
    {
        if (!IsFirstFlag) return;
        IsFirstFlag = false;

        Language.OnChangeLanguage((uint)AmongUs.Data.DataManager.Settings.Language.CurrentLanguage);

        __instance.StartCoroutine(VanillaAsset.CoLoadAssetOnTitle().WrapToIl2Cpp());

    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class NebulaPreLoad : Attribute
{
    public Type[] PreLoadType { get; private set; }
    public FinalizerType MyFinalizerType { get; private set; }
    public NebulaPreLoad(params Type[] preLoadType)
    {
        PreLoadType = preLoadType;
        MyFinalizerType = FinalizerType.NotFinalizer;
    }

    public NebulaPreLoad(bool isFinalizer, params Type[] preLoadType)
    {
        PreLoadType = preLoadType;
        MyFinalizerType = isFinalizer ? FinalizerType.StaticConstAndLoad : FinalizerType.NotFinalizer;
    }

    public enum FinalizerType
    {
        LoadOnly,
        StaticConstAndLoad,
        NotFinalizer
    }

    public NebulaPreLoad(FinalizerType finalizerType, params Type[] preLoadType)
    {
        PreLoadType = preLoadType;
        MyFinalizerType = finalizerType;
    }

    static public bool FinishedLoading => NebulaPlugin.FinishedPreload;
}

[HarmonyPatch(typeof(StatsManager), nameof(StatsManager.AmBanned), MethodType.Getter)]
public static class AmBannedPatch
{
    public static void Postfix(out bool __result)
    {
        __result = false;
    }
}