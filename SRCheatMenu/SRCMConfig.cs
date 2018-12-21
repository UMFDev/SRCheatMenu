using System;
using UModFramework.API;
using UnityEngine;

namespace SRCheatMenu
{
    public class SRCMConfig
    {
        private static readonly string configVersion = "1.0";

        private static string[] KeysCheatMenu;
        public static Color TextColor;
        public static Color GUIColor;

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

                    cfg.Read("LoadPriority", new UMFConfigString("Normal"));
                    cfg.Write("MinVersion", new UMFConfigString("0.48"));
                    cfg.Write("MaxVersion", new UMFConfigString("0.54.99999.99999"));
                    cfg.Write("UpdateURL", new UMFConfigString(@"https://raw.githubusercontent.com/UMFDev/SRCheatMenu/master/version.txt"));
                    cfg.Write("ConfigVersion", new UMFConfigString(configVersion));

                    SRCheatMenu.Log("Finished UMF Settings.");

                    KeysCheatMenu = cfg.Read("KeysCheatMenu", new UMFConfigStringArray(new string[] { "B" }, true), "The key(s) used to toggle the Cheat Menu window.");
                    for (int i = 0; i < KeysCheatMenu.Length; i++) UMFGUI.RegisterBind("KeysCheatMenu" + i.ToString(), KeysCheatMenu[i], SRCheatMenu.Instance.ToggleMenu);

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

        public static SRCMConfig Instance { get; } = new SRCMConfig();
    }
}