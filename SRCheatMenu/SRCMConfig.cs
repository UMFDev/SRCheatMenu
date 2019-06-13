using System;
using UModFramework.API;
using UnityEngine;

namespace SRCheatMenu
{
    public class SRCMConfig
    {
        private static readonly string configVersion = "1.0";

        private static string[] KeysCheatMenu;
        private static string[] KeysRefill;
        private static string[] KeysNoClip;
        private static string[] KeysInfHealth;
        private static string[] KeysInfEnergy;
        private static string[] KeysIncTime;
        private static string[] KeysDecTime;
        private static string[] KeysSleepwalk;
        internal static double IncDecTimeDefault;
        internal static Color TextColor;
        internal static Color GUIColor;

        public void Load()
        {
            SRCheatMenu.Log("Loading settings.");
            try
            {
                using (UMFConfig cfg = new UMFConfig())
                {
                    string cfgVer = cfg.Read("ConfigVersion", new UMFConfigString());
                    if (cfgVer != string.Empty && cfgVer != configVersion)
                    {
                        cfg.DeleteConfig();
                        SRCheatMenu.Log("The config file was outdated and has been deleted. A new config will be generated.");
                    }

                    //cfg.Write("SupportsHotLoading", new UMFConfigBool(false));
                    cfg.Read("LoadPriority", new UMFConfigString("Normal"));
                    cfg.Write("MinVersion", new UMFConfigString("0.52"));
                    cfg.Write("MaxVersion", new UMFConfigString("0.54.99999.99999"));
                    //cfg.Write("UpdateURL", new UMFConfigString(@"https://raw.githubusercontent.com/UMFDev/SRCheatMenu/master/version.txt"));
                    cfg.Write("UpdateURL", new UMFConfigString(@"https://umodframework.com/updatemod?id=2"));
                    cfg.Write("ConfigVersion", new UMFConfigString(configVersion));

                    SRCheatMenu.Log("Finished UMF Settings.");

                    KeysCheatMenu = cfg.Read("KeysCheatMenu", new UMFConfigStringArray(new string[] { "B" }, true), "The key(s) used to toggle the Cheat Menu window.");
                    for (int i = 0; i < KeysCheatMenu.Length; i++) UMFGUI.RegisterBind("KeysCheatMenu" + i.ToString(), KeysCheatMenu[i], SRCheatMenu.Instance.ToggleMenu);

                    KeysRefill = cfg.Read("KeysRefill", new UMFConfigStringArray(new string[0], true), "The key(s) used to Refilling items.");
                    //for (int i = 0; i < KeysRefill.Length; i++) UMFGUI.RegisterBind("KeysRefill" + i.ToString(), KeysRefill[i], SRCheatMenu.Instance.RefillItems);

                    KeysNoClip = cfg.Read("KeysNoClip", new UMFConfigStringArray(new string[0], true), "The key(s) used to toggle No Clip.");
                    //for (int i = 0; i < KeysNoClip.Length; i++) UMFGUI.RegisterBind("KeysNoClip" + i.ToString(), KeysNoClip[i], SRCheatMenu.Instance.ToggleNoClip);

                    KeysInfHealth = cfg.Read("KeysInfHealth", new UMFConfigStringArray(new string[0], true), "The key(s) used to toggle Infinite Health.");
                    for (int i = 0; i < KeysInfHealth.Length; i++) UMFGUI.RegisterBind("KeysInfHealth" + i.ToString(), KeysInfHealth[i], SRCheatMenu.Instance.ToggleInfiniteHealth);

                    KeysInfEnergy = cfg.Read("KeysInfEnergy", new UMFConfigStringArray(new string[0], true), "The key(s) used to toggle Infinite Energy.");
                    for (int i = 0; i < KeysInfEnergy.Length; i++) UMFGUI.RegisterBind("KeysInfEnergy" + i.ToString(), KeysInfEnergy[i], SRCheatMenu.Instance.ToggleInfiniteEnergy);

                    KeysIncTime = cfg.Read("KeysIncTime", new UMFConfigStringArray(new string[0], true), "The key(s) used to Increase Time.");
                    //for (int i = 0; i < KeysIncTime.Length; i++) UMFGUI.RegisterBind("KeysIncTime" + i.ToString(), KeysIncTime[i], SRCheatMenu.Instance.BindIncreaseTime);

                    KeysDecTime = cfg.Read("KeysDecTime", new UMFConfigStringArray(new string[0], true), "The key(s) used to Decrease Time.");
                    //for (int i = 0; i < KeysDecTime.Length; i++) UMFGUI.RegisterBind("KeysDecTime" + i.ToString(), KeysDecTime[i], SRCheatMenu.Instance.BindDecreaseTime);

                    KeysSleepwalk = cfg.Read("KeysSleepwalk", new UMFConfigStringArray(new string[0], true), "The key(s) used to toggle Sleepwalking.");
                    //for (int i = 0; i < KeysSleepwalk.Length; i++) UMFGUI.RegisterBind("KeysSleepwalk" + i.ToString(), KeysSleepwalk[i], SRCheatMenu.Instance.BindSleepwalk);

                    UpdateInstancedBinds();

                    IncDecTimeDefault = cfg.Read("IncDecTimeDefault", new UMFConfigDouble(60d, 1d, 1440d, 0), "The default value used for increasing and decreasing time in minutes.");

                    TextColor = cfg.Read("TextColor", new UMFConfigColorHexRGBA(new Color(0.604f, 1f, 0.604f, 1f)), "The main text color in Hex RGBA.");
                    GUIColor = cfg.Read("GUIColor", new UMFConfigColorHexRGBA(new Color(0f, 0.7f, 1f, 1f)), "The main GUI color in Hex RGBA.");

                    SRCheatMenu.Log("Finished loading settings.");
                }
            }
            catch (Exception e)
            {
                SRCheatMenu.Log("Error loading mod settings: " + e.Message + " (" + e.InnerException.Message + ")");
            }
        }

        internal void UpdateInstancedBinds()
        {
            for (int i = 0; i < KeysRefill.Length; i++) UMFGUI.RegisterBind("KeysRefill" + i.ToString(), KeysRefill[i], SRCheatMenu.Instance.RefillItems);
            for (int i = 0; i < KeysNoClip.Length; i++) UMFGUI.RegisterBind("KeysNoClip" + i.ToString(), KeysNoClip[i], SRCheatMenu.Instance.ToggleNoClip);
            for (int i = 0; i < KeysIncTime.Length; i++) UMFGUI.RegisterBind("KeysIncTime" + i.ToString(), KeysIncTime[i], SRCheatMenu.Instance.BindIncreaseTime);
            for (int i = 0; i < KeysDecTime.Length; i++) UMFGUI.RegisterBind("KeysDecTime" + i.ToString(), KeysDecTime[i], SRCheatMenu.Instance.BindDecreaseTime);
            for (int i = 0; i < KeysSleepwalk.Length; i++) UMFGUI.RegisterBind("KeysSleepwalk" + i.ToString(), KeysSleepwalk[i], SRCheatMenu.Instance.BindSleepwalk);
        }

        public static SRCMConfig Instance { get; } = new SRCMConfig();
    }
}