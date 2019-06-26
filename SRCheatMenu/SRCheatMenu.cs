using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using MonomiPark.SlimeRancher.DataModel;
using MonomiPark.SlimeRancher.Regions;
using UModFramework.API;
using HarmonyLib;

namespace SRCheatMenu
{
    [UMFHarmony(1)]
    [UMFScript]
    public class SRCheatMenu : MonoBehaviour
    {
        #region Variables
        //GUI
        private static readonly int guiSizeX = 820;
        private static readonly int guiSizeY = 480;
        private static readonly Rect guiRect = new Rect((Screen.width / 2) - (guiSizeX / 2), (Screen.height / 2) - (guiSizeY / 2), guiSizeX, guiSizeY);
        private int toolbarTab = 0;
        private int toolbarTabPrevious = 0;
        private Rect windowRect = guiRect;
        private Vector2 buttonBarScroll = Vector2.zero;
        private Vector2 refineryScroll = Vector2.zero;
        private Vector2 gadgetScroll = Vector2.zero;
        private Vector2 decorizerScroll = Vector2.zero;
        private Vector2 targetScroll = Vector2.zero;
        private static GUIStyle styleSlot = new GUIStyle();
        private static GUIStyle styleNormal = new GUIStyle();
        private static GUIStyle styleUpperCenter = new GUIStyle();
        private static GUIStyle styleUpperCenterShadow = new GUIStyle();
        private static GUIStyle styleDropdown = new GUIStyle();
        private static GUIStyle styleKeybind = new GUIStyle();
        private static GUIStyle styleKeybindShadow = new GUIStyle();

        //Menu
        private static readonly string MenuName = "SR Cheat Menu v" + UMFMod.GetModVersion();
        private static bool MenuEnabled = false;
        private static bool MenuWasEnabled = false;
        private static bool MenuUpdate = false;
        private static string search = string.Empty;
        private static string searchPrevious = string.Empty;
        private static string category = "All";
        private static string categoryPrevious = "All";
        private static List<string> categories = new List<string>() { "All", /*"Allergy Free",*/ "Animal", "Chick", "Craft", "Echo", "Echo Note", "Fashion", "Food", "Fruit", "Gordo", "Largo", /*"Non-Slime Resource",*/ "Ornament", "Plort", "Slime", "Toy", "Veggie" };
        private static Dictionary<string, UMFDropDown> dropDowns = new Dictionary<string, UMFDropDown>();
        private static Dictionary<string, int> slotCounts = new Dictionary<string, int>();
        private static bool KeybindShown = false;
        private static float KeyBindShowStart = 0f;
        private static List<GUIContent> toolbarTabs = new List<GUIContent>();
        //private static int money = 0;
        //private static int keys = 0;

        //Items
        private static List<Identifiable.Id> itemEnum = new List<Identifiable.Id>();
        private static List<Identifiable.Id> itemIds = new List<Identifiable.Id>();
        private static List<Identifiable.Id> itemIdsWater = new List<Identifiable.Id>();
        private static List<Identifiable.Id> itemIdsRefinery = new List<Identifiable.Id>();
        private static List<Gadget.Id> itemIdsGadgets = new List<Gadget.Id>();
        private static List<Identifiable.Id> itemIdsDecorizer = new List<Identifiable.Id>();
        private static Dictionary<Identifiable.Id, Texture2D> itemTextures = new Dictionary<Identifiable.Id, Texture2D>();
        private static readonly Texture2D textureClear = UMFUnity.ColorToTexture2D(2, 2, Color.clear);
        private static int RefineryLimit = 999;
        private static Dictionary<Identifiable.Id, string> refineryNames = new Dictionary<Identifiable.Id, string>();
        private static Dictionary<Gadget.Id, Texture2D> gadgetTextures = new Dictionary<Gadget.Id, Texture2D>();
        private static Dictionary<Gadget.Id, string> gadgetNames = new Dictionary<Gadget.Id, string>();
        private static Dictionary<Identifiable.Id, string> decorizerNames = new Dictionary<Identifiable.Id, string>();
        private static readonly List<Identifiable.Id> blockedIds = new List<Identifiable.Id>() { Identifiable.Id.NONE, Identifiable.Id.PLAYER };
        private static readonly string fileSpawns = Path.Combine(UMFData.ModInfosPath, "SRCheatMenu_Spawns.txt");
        private static readonly string fileItems = Path.Combine(UMFData.ModInfosPath, "SRCheatMenu_Items.txt");
        private static readonly Gadget.Id[] blockedGadgets = { Gadget.Id.FASHION_POD_PIRATEY, Gadget.Id.FASHION_POD_HEROIC };
        private static RaycastHit rayHit = new RaycastHit();
        //private static readonly RanchDirector.Palette[] blockedPalettes = { RanchDirector.Palette.PALETTE27, RanchDirector.Palette.PALETTE28 };

        //Instances
        public static SRCheatMenu Instance { get; set; } = new SRCheatMenu();
        private Ammo ammoPlayer;
        private PlayerState playerState;
        private GameObject player;
        private TimeDirector timeDirector;
        private GameModel gameModel;
        private PlayerModel playerModel;
        private WorldModel worldModel;
        private GadgetsModel gadgetsModel;
        private DecorizerModel decorizerModel;
        //private RanchDirector ranchDirector;
        private GadgetDirector gadgetDirector;
        private DLCDirector dlcDirector;
        private LookupDirector lookupDirector;
        private vp_FPCamera camera;
        private vp_FPController controller;
        private ProgressDirector progressDirector;
        private PediaDirector pediaDirector;
        private TutorialDirector tutorialDirector;
        private TutorialPopupUI tutorialPopupUI;
        private RegionRegistry regionRegistry;
        private SiloStorage target;

        //Toggles
        private static bool noClip = false;
        private Vector3 noClipPos = Vector3.zero;
        internal static bool infiniteHealth = false;
        private static bool infiniteEnergy = false;
        private static bool clearPopups = false;
        #endregion

        #region UMF & Startup
        void Awake()
        {
            Log("Slime Rancher Cheat Menu v" + UMFMod.GetModVersion().ToString(), true);
            UMFGUI.RegisterPauseHandler(Pause);
            SRCMConfig.Instance.Load();
            RegisterCommands();
        }

        [UMFConfig]
        public static void LoadConfig()
        {
            SRCMConfig.Instance.Load();
        }

        internal static void Log(string text, bool clean = false)
        {
            using (UMFLog log = new UMFLog()) log.Log(text, clean);
        }

        private void RegisterCommands()
        {
            UMFGUI.RegisterCommand("srcm_spawn <item name/identifiable.id> (<num>)", "srcm_spawn", new string[] { "spawn" }, 1, "Spawns a item in front of the player.", CommandSpawn);
            UMFGUI.RegisterCommand("srcm_printspawns", "srcm_printspawns", new string[] { "printspawns" }, 0, "Prints a list of spawnable items to a text file and opens it.", CommandPrintSpawns);
            UMFGUI.RegisterCommand("srcm_item <item name/identifiable.id> (<num>) (<slot>)", "srcm_item", new string[] { "item" }, 1, "Adds the specified <num> of <item> into <slot>. If no slot is specified it adds to the first slot.", CommandItem);
            UMFGUI.RegisterCommand("srcm_refillItems", "srcm_refillitems", new string[] { "refill" }, 0, "Refills all slots that have items in them to the max.", CommandRefillItems);
            UMFGUI.RegisterCommand("srcm_printitems", "srcm_printitems", new string[] { "printitems" }, 0, "Prints a list of items that can currently be added to inventory to a text file and opens it.", CommandPrintItems);
            UMFGUI.RegisterCommand("srcm_keys (<num>)", "srcm_keys", new string[] { "keys" }, 0, "Sets or retrieves the current number of keys.", CommandKeys);
            UMFGUI.RegisterCommand("srcm_delete (<radius>)", "srcm_delete", new string[] { "delete" }, 0, "Deletes all items/slimes that are not in a corral, coop or field within a specified radius. Radius is 50 if none is specified.", CommandDeleteRadius);
            UMFGUI.RegisterCommand("srcm_noclip", "srcm_noclip", new string[] { "noclip" }, 0, "Toggles walking and flying through objects at high speeds.", CommandNoClip);
            UMFGUI.RegisterCommand("srcm_infiniteHealth", "srcm_infinitehealth", new string[] { "infhealth", "god" }, 0, "Toggles infinite health.", CommandInfiniteHealth);
            UMFGUI.RegisterCommand("srcm_infiniteEnergy", "srcm_infiniteenergy", new string[] { "infenergy" }, 0, "Toggles infinite energy.", CommandInfiniteEnergy);
            UMFGUI.RegisterCommand("srcm_increaseTime (<minutes>)", "srcm_increasetime", new string[] { "inctime" }, 0, "Increases the world time by 1 hour or the specified minutes.", CommandIncreaseTime);
            UMFGUI.RegisterCommand("srcm_decreaseTime (<minutes>)", "srcm_decreasetime", new string[] { "dectime" }, 0, "Decreases the world time by 1 hour or the specified minutes.", CommandDecreaseTime);
            UMFGUI.RegisterCommand("srcm_sleepwalk", "srcm_sleepwalk", new string[] { "sleepwalk" }, 0, "Toggles the fast forward effect from sleeping.", CommandSleepwalk);
            UMFGUI.RegisterCommand("srcm_unlockUpgrades", "srcm_unlockupgrades", new string[] { "unlockupgrades" }, 0, "Unlocks all player upgrades.", CommandUnlockUpgrades);
            UMFGUI.RegisterCommand("srcm_resetUpgrades", "srcm_resetupgrades", new string[] { "resetupgrades" }, 0, "Resets all player upgrades.", CommandResetUpgrades);
            UMFGUI.RegisterCommand("srcm_unlockProgress", "srcm_unlockprogress", new string[] { "unlockprogress" }, 0, "(Experimental) Unlocks all progress.", CommandUnlockProgress);
            UMFGUI.RegisterCommand("srcm_resetProgress", "srcm_resetprogress", new string[] { "resetprogress" }, 0, "Resets all progress.", CommandResetProgress);
            UMFGUI.RegisterCommand("srcm_listprogress", "srcm_listprogress", new string[] { "listprogress" }, 0, "Lists all internal progress variables and their values. (For Developers)", CommandListProgress);
            UMFGUI.RegisterCommand("srcm_unlockTreasurePods", "srcm_unlocktreasurepods", new string[] { "unlocktreasurepods" }, 0, "(Experimental) Unlocks all treasure pods.", CommandUnlockTreasurePods);
            UMFGUI.RegisterCommand("srcm_resetTreasurePods", "srcm_resettreasurepods", new string[] { "resettreasurepods" }, 0, "(Experimental) Resets all treasure pods.", CommandResetTreasurePods);
        }
        #endregion

        #region Update Instances
        private void UpdateInstances()
        {
            try
            {
                if (!playerState && InGame())
                {
                    Instance = this;
                    playerState = SRSingleton<SceneContext>.Instance.PlayerState;
                    player = SRSingleton<SceneContext>.Instance.Player;
                    timeDirector = SRSingleton<SceneContext>.Instance.TimeDirector;
                    gameModel = SRSingleton<SceneContext>.Instance.GameModel;
                    playerModel = gameModel.GetPlayerModel();
                    worldModel = gameModel.GetWorldModel();
                    gadgetsModel = gameModel.GetGadgetsModel();
                    decorizerModel = gameModel.GetDecorizerModel();
                    gadgetDirector = SRSingleton<SceneContext>.Instance.GadgetDirector;
                    //ranchDirector = SRSingleton<SceneContext>.Instance.RanchDirector;
                    dlcDirector = SRSingleton<GameContext>.Instance.DLCDirector;
                    lookupDirector = FindObjectOfType<LookupDirector>();
                    camera = FindObjectOfType<vp_FPCamera>();
                    controller = FindObjectOfType<vp_FPController>();
                    progressDirector = SRSingleton<SceneContext>.Instance.ProgressDirector;
                    pediaDirector = SRSingleton<SceneContext>.Instance.PediaDirector;
                    tutorialDirector = SRSingleton<SceneContext>.Instance.TutorialDirector;
                    tutorialPopupUI = FindObjectOfType<TutorialPopupUI>();
                    regionRegistry = SRSingleton<SceneContext>.Instance.RegionRegistry;
                    SRCMConfig.Instance.UpdateBinds();
                }
                if (playerState && InGame() && ammoPlayer != playerState.Ammo)
                {
                    ammoPlayer = playerState.Ammo;
                    if (!SRCMConfig.GetAllItems)
                    {
                        itemEnum = playerState.GetPotentialAmmo().ToList();
                        //itemIdsWater = lookupDirector.vacEntries.Where(x => Identifiable.IsLiquid(x.id)).Select(z => z.id).ToList();
                        itemIdsWater = itemEnum.Where(x => Identifiable.IsLiquid(x)).ToList();
                    }
                    else
                    {
                        itemEnum = Enum.GetValues(typeof(Identifiable.Id)).Cast<Identifiable.Id>().Where(x => !Identifiable.IsLiquid(x) && !blockedIds.Contains(x)).ToList();
                        itemIdsWater = Enum.GetValues(typeof(Identifiable.Id)).Cast<Identifiable.Id>().Where(x => Identifiable.IsLiquid(x)).ToList();
                    }
                    if (!itemIdsWater.Contains(Identifiable.Id.NONE)) itemIdsWater.Insert(0, Identifiable.Id.NONE);
                    itemEnum = itemEnum.Where(x => !Identifiable.IsLiquid(x)).ToList();
                    itemEnum = itemEnum.Where(x => !blockedIds.Contains(x)).ToList();
                    if (itemEnum.Count(x => Identifiable.IsGordo(x)) == 0) categories.Remove("Gordo");
                    if (itemEnum.Count(x => Identifiable.IsLargo(x)) == 0) categories.Remove("Largo");
                    if (itemEnum.Count(x => Identifiable.IsToy(x)) == 0) categories.Remove("Toy");
                    itemEnum = SortItemList(itemEnum);
                    itemTextures.Clear();
                    foreach (Identifiable.Id id in Enum.GetValues(typeof(Identifiable.Id))) itemTextures.Add(id, GetIcon(id)?.texture ?? textureClear);
                    RefineryLimit = gadgetDirector.GetRefinerySpaceAvailable(Identifiable.Id.PINK_PLORT) + gadgetDirector.GetRefineryCount(Identifiable.Id.PINK_PLORT);
                    if (RefineryLimit <= 0) RefineryLimit = 999;
                    itemIdsRefinery = Enum.GetValues(typeof(Identifiable.Id)).Cast<Identifiable.Id>().Where(id => GadgetDirector.IsRefineryResource(id)).ToList();
                    itemIdsDecorizer = DecorizerModel.ITEM_CLASSES.SelectMany((HashSet<Identifiable.Id> c) => c).ToList();
                    itemIdsDecorizer = SortDecorizerList(itemIdsDecorizer);
                    //if (refineryUI && refineryUI.listedItems.Length > 0) itemIdsRefinery = itemIdsRefinery.OrderBy(x => refineryUI.listedItems.ToList().FindIndex(y => x == y)).ToList(); //Where does the refinery pull it's sorting from?
                    refineryNames.Clear();
                    foreach (Identifiable.Id id in itemIdsRefinery)
                    {
                        refineryNames.Add(id, GetItemName(id));
                        if (!gadgetsModel.craftMatCounts.ContainsKey(id)) gadgetsModel.craftMatCounts.Add(id, 0);
                    }
                    itemIdsGadgets = Enum.GetValues(typeof(Gadget.Id)).Cast<Gadget.Id>().Where(x => x != Gadget.Id.NONE && lookupDirector.HasGadgetEntry(x)/* && gadgetDirector.HasBlueprint(x)*/).ToList();
                    gadgetTextures.Clear();
                    gadgetNames.Clear();
                    foreach (Gadget.Id id in itemIdsGadgets)
                    {
                        gadgetTextures.Add(id, lookupDirector.GetGadgetEntry(id).icon.texture);
                        gadgetNames.Add(id, GetGadgetName(id));
                        if (!gadgetsModel.gadgets.ContainsKey(id)) gadgetsModel.gadgets.Add(id, 0);
                    }
                    decorizerNames.Clear();
                    foreach (Identifiable.Id id in itemIdsDecorizer)
                    {
                        decorizerNames.Add(id, GetItemName(id));
                        if (!decorizerContents.ContainsKey(id)) decorizerContents.Add(id, 0);
                    }
                    toolbarTabs.Clear();
                    toolbarTabs.Add(new GUIContent("Main", pediaDirector.Get(PediaDirector.Id.UTILITIES).icon.texture));
                    toolbarTabs.Add(new GUIContent("Refinery", pediaDirector.Get(PediaDirector.Id.REFINERY).icon.texture));
                    toolbarTabs.Add(new GUIContent("Gadgets", pediaDirector.Get(PediaDirector.Id.FABRICATOR).icon.texture));
                    toolbarTabs.Add(new GUIContent("Decorizer", pediaDirector.Get(PediaDirector.Id.DECORATIONS).icon.texture));
                    toolbarTabs.Add(new GUIContent("Target", pediaDirector.Get(PediaDirector.Id.SILO).icon.texture));
                }
            }
            catch (Exception e)
            {
                Log("Error: Failed to retrieve game instances: " + e.Message + " (" + e.InnerException?.Message + ") at " + e.Source);
            }
        }
        #endregion

        #region Keys
        public void CommandKeys()
        {
            if (!InGame(true)) return;
            if (UMFGUI.Args.Length > 0)
            {
                int num = int.Parse(UMFGUI.Args[0]);
                if (num >= 0)
                {
                    SetKeys(num);
                    UMFGUI.AddConsoleText("Successfully set Keys to " + num.ToString() + ".");
                }
                else UMFGUI.AddConsoleText("Warning: You must specify a valid number to set your keys to.");
            }
            else
            {
                UMFGUI.AddConsoleText("Current Keys = " + GetKeys().ToString());
            }
        }

        private void SetKeys(int num)
        {
            playerModel.SetKeys(num);
        }

        private int GetKeys()
        {
            return playerModel.keys;
        }
        #endregion

        #region Money
        public void CommandMoney()
        {
            if (!InGame(true)) return;
            if (UMFGUI.Args.Length > 0)
            {
                int num = int.Parse(UMFGUI.Args[0]);
                if (num >= 0)
                {
                    SetMoney(num);
                    UMFGUI.AddConsoleText("Successfully set money to " + num.ToString() + ".");
                }
                else UMFGUI.AddConsoleText("Warning: You must specify a valid number to set your money to.");
            }
            else
            {
                UMFGUI.AddConsoleText("Current Money = " + GetMoney().ToString());
            }
        }

        private void SetMoney(int num)
        {
            playerModel.SetCurrency(num);
        }

        private int GetMoney()
        {
            return playerModel.currency;
        }
        #endregion

        #region Delete Radius
        public void CommandDeleteRadius()
        {
            if (!InGame(true)) return;
            float radius = 50f;
            if (UMFGUI.Args.Length > 0) radius = float.Parse(UMFGUI.Args[0]);
            if (radius <= 0)
            {
                UMFGUI.AddConsoleText("Warning: Invalid radius.");
                return;
            }
            int numDeletedObjects = DeleteRadius(radius);
            if (numDeletedObjects > 0) UMFGUI.AddConsoleText("Successfully removed " + numDeletedObjects.ToString() + " objects in a radius of " + radius.ToString() + ".");
            else UMFGUI.AddConsoleText("There were no objects to remove within a radius of " + radius.ToString() + ".");
        }

        private int DeleteRadius(float radius)
        {
            Vector3 playerPos = player.transform.position;
            int numDeletedObjects = 0;
            foreach (Identifiable identifiable in FindObjectsOfType<Identifiable>())
            {
                try
                {
                    if (itemEnum.Contains(identifiable.id) && identifiable.isActiveAndEnabled && identifiable.GetActorId() > 0)
                    {
                        Vector3 pos = Vector3.zero;
                        if (identifiable.gameObject?.transform != null && identifiable.gameObject?.transform?.position != null) pos = identifiable.gameObject.transform.position;
                        if (pos == Vector3.zero) continue;
                        float distance = (playerPos - pos).magnitude;
                        if (distance <= radius && !CorralRegion.IsWithin(pos) && !CoopRegion.IsWithin(pos) && !VitamizerRegion.IsWithin(pos))
                        {
                            Destroyer.DestroyActor(identifiable.gameObject, "CellDirector.Despawn", false);
                            numDeletedObjects++;
                        }
                    }
                }
                catch (Exception e)
                {
                    Log("Error deleting object: " + e.Message + " (" + e.InnerException?.Message + ")");
                }
            }
            return numDeletedObjects;
        }
        #endregion

        #region Spawn
        public void CommandSpawn()
        {
            if (!InGame(true)) return;
            string item = UMFGUI.Args[0].Trim().Trim('\0');
            int count = 1;
            if (UMFGUI.Args.Length > 1) count = int.Parse(UMFGUI.Args[1]);
            Identifiable.Id itemId = Identifiable.Id.NONE;
            List<Identifiable.Id> ids = Enum.GetValues(typeof(Identifiable.Id)).Cast<Identifiable.Id>().ToList();
            List<string> searchMatches = new List<string>();
            foreach (Identifiable.Id id in ids)
            {
                if (id.ToString().ToLower() == item.ToLower() || GetItemName(id).ToLower() == item.ToLower())
                {
                    itemId = id;
                    break;
                }
            }
            if (itemId == Identifiable.Id.NONE)
            {
                foreach (Identifiable.Id id in ids)
                {
                    if (id.ToString().ToLower().Contains(item.ToLower()) || GetItemName(id).ToLower().Contains(item.ToLower()))
                    {
                        string name = GetItemName(id);
                        if (name.Contains(" ")) name = "\"" + name + "\"";
                        searchMatches.Add(name + " (" + id.ToString() + ")");
                    }
                }
            }
            if (itemId != Identifiable.Id.NONE && !blockedIds.Contains(itemId))
            {
                Spawn(itemId, count);
                UMFGUI.AddConsoleText("Successfully spawned " + count.ToString() + " '" + Identifiable.GetName(itemId) + "' (" + itemId.ToString() + ").");
            }
            else
            {
                if (searchMatches.Count > 0)
                {
                    UMFGUI.AddConsoleText("The item you tried to spawn could not be found.");
                    UMFGUI.AddConsoleText("--- Items with partial match ---");
                    foreach (string match in searchMatches) UMFGUI.AddConsoleText(match);
                }
                else UMFGUI.AddConsoleText("Warning: The item '" + item + "' that you tried to spawn is invalid.");
            }
        }

        private void Spawn(Identifiable.Id itemId, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = player.transform.position + new Vector3(0f, 1.5f, 0f) + (player.transform.forward * 4);
                GameObject gameObject = SRBehaviour.InstantiateActor(lookupDirector.GetPrefab(itemId), regionRegistry.GetCurrentRegionSetId(), pos, Quaternion.identity, true);
                Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
                if (rigidbody) rigidbody.AddForce((player.transform.forward * 200f) + (player.transform.up * 100f));
            }
        }

        public void CommandPrintSpawns()
        {
            List<Identifiable.Id> ids = Enum.GetValues(typeof(Identifiable.Id)).Cast<Identifiable.Id>().ToList();
            List<string> spawns = new List<string>();
            foreach (Identifiable.Id id in ids)
            {
                if (blockedIds.Contains(id)) continue;
                string name = GetItemName(id);
                if (name.Contains(" ")) name = "\"" + name + "\"";
                spawns.Add(name + " (" + id.ToString() + ")");
            }
            File.WriteAllLines(fileSpawns, spawns.ToArray());
            Process.Start(fileSpawns);
        }
        #endregion

        #region Items
        public void CommandItem()
        {
            if (!InGame(true)) return;
            string item = UMFGUI.Args[0].Trim().Trim('\0');
            int count = 1;
            if (UMFGUI.Args.Length > 1) count = int.Parse(UMFGUI.Args[1]);
            int slot = 0;
            if (UMFGUI.Args.Length > 2) slot = int.Parse(UMFGUI.Args[2]);
            if (slot <= 0 || slot > ammoPlayer.GetUsableSlotCount())
            {
                UMFGUI.AddConsoleText("Warning: Invalid slot. Should be a number between 1 and " + ammoPlayer.GetUsableSlotCount().ToString() + ".");
                return;
            }
            if (slot > 0) slot = slot - 1;
            Identifiable.Id itemId = Identifiable.Id.NONE;
            List<string> searchMatches = new List<string>();
            foreach (Identifiable.Id id in itemEnum)
            {
                if (id.ToString().ToLower() == item.ToLower() || GetItemName(id).ToLower() == item.ToLower())
                {
                    itemId = id;
                    break;
                }
            }
            if (itemId == Identifiable.Id.NONE)
            {
                foreach (Identifiable.Id id in itemEnum)
                {
                    if (id.ToString().ToLower().Contains(item.ToLower()) || GetItemName(id).ToLower().Contains(item.ToLower()))
                    {
                        string name = GetItemName(id);
                        if (name.Contains(" ")) name = "\"" + name + "\"";
                        searchMatches.Add(name + " (" + id.ToString() + ")");
                    }
                }
            }
            if (itemId != Identifiable.Id.NONE && !blockedIds.Contains(itemId))
            {
                Item(ammoPlayer, itemId, count, slot);
                UMFGUI.AddConsoleText("Successfully added " + count.ToString() + " '" + GetItemName(itemId) + "' (" + itemId.ToString() + ") to slot " + (slot + 1).ToString() + ".");
            }
            else
            {
                if (searchMatches.Count > 0)
                {
                    UMFGUI.AddConsoleText("The item you tried to add could not be found.");
                    UMFGUI.AddConsoleText("--- Items with partial match ---");
                    foreach (string match in searchMatches) UMFGUI.AddConsoleText(match);
                }
                else UMFGUI.AddConsoleText("Warning: The item '" + item + "' that you tried to add is invalid.");
            }
        }

        private void Item(Ammo ammo, Identifiable.Id itemId, int count, int slot)
        {
            ClearItem(ammo, slot);
            if (itemId != Identifiable.Id.NONE)
            {
                GameObject gameObject = gameModel.InstantiateActor(lookupDirector.GetPrefab(itemId), regionRegistry.GetCurrentRegionSetId(), player.transform.position, Quaternion.identity, true);
                Identifiable identifiable = gameObject.GetComponent<Identifiable>() ?? gameObject.AddComponent<Identifiable>();
                ammo.MaybeAddToSpecificSlot(itemId, identifiable, slot, count, false);
                Destroyer.DestroyActor(gameObject, "Vacuumable.consume", true);
            }
        }

        private void ClearItem(Ammo ammo, int slot)
        {
            ammo.SetAmmoSlot(slot);
            ammo.Clear(slot);
        }

        private Identifiable.Id GetSlotItemId(int slot)
        {
            return ammoPlayer.GetSlotName(slot);
        }

        private int GetSlotCount(int slot)
        {
            return ammoPlayer.GetSlotCount(slot);
        }

        private int GetSlotMaxCount(int slot)
        {
            return ammoPlayer.GetSlotMaxCount(slot);
        }

        private int GetUsableSlotCount()
        {
            return ammoPlayer.GetUsableSlotCount();
        }

        private static string GetItemName(Identifiable.Id id)
        {
            return Identifiable.GetName(id, false) ?? id.ToString();
        }

        private static List<Identifiable.Id> SortItemList(List<Identifiable.Id> list)
        {
            list.Sort();
            List<Identifiable.Id> result = new List<Identifiable.Id>();
            result.AddRange(list.Where(x => Identifiable.IsPlort(x) && !result.Contains(x)));
            result.AddRange(list.Where(x => Identifiable.IsSlime(x) && !result.Contains(x)));
            result.AddRange(list.Where(x => Identifiable.IsFood(x) && !result.Contains(x)));
            result.AddRange(list.Where(x => Identifiable.IsAnimal(x) && !result.Contains(x)));
            result.AddRange(list.Where(x => Identifiable.IsToy(x) && !result.Contains(x)));
            result.AddRange(list.Where(x => Identifiable.IsFashion(x) && !result.Contains(x)));
            result.AddRange(list.Where(x => Identifiable.IsCraft(x) && !result.Contains(x)));
            result.AddRange(list.Where(x => !result.Contains(x)));
            return result;
        }

        private static List<Identifiable.Id> SortDecorizerList(List<Identifiable.Id> list)
        {
            list.Sort();
            List<Identifiable.Id> result = new List<Identifiable.Id>();
            result.AddRange(list.Where(x => Identifiable.ECHO_CLASS.Contains(x) && !result.Contains(x)));
            result.AddRange(list.Where(x => Identifiable.ORNAMENT_CLASS.Contains(x) && !result.Contains(x)));
            result.AddRange(list.Where(x => Identifiable.ECHO_NOTE_CLASS.Contains(x) && !result.Contains(x)));
            result.AddRange(list.Where(x => !result.Contains(x)));
            return result;
        }

        Dictionary<Identifiable.Id, int> decorizerContents
        {
            get
            {
                return Traverse.Create(Traverse.Create(decorizerModel).Field<ReferenceCount<Identifiable.Id>>("contents").Value).Field<Dictionary<Identifiable.Id, int>>("dictionary").Value;
            }
            set
            {
                Traverse.Create(Traverse.Create(decorizerModel).Field<ReferenceCount<Identifiable.Id>>("contents").Value).Field<Dictionary<Identifiable.Id, int>>("dictionary").Value = value;
            }
        }

        private Sprite GetIcon(Identifiable.Id id)
        {
            try
            {
                return Traverse.Create(lookupDirector).Field<Dictionary<Identifiable.Id, LookupDirector.VacEntry>>("vacEntryDict").Value[id].icon;
            }
            catch { }
            return null;
        }

        private static string GetGadgetName(Gadget.Id id)
        {
            return Gadget.GetName(id, false) ?? id.ToString();
        }

        public void CommandRefillItems()
        {
            if (!InGame(true)) return;
            RefillItems(ammoPlayer);
            UMFGUI.AddConsoleText("Successfully refilled all slots that had items in them. (If any)");
        }

        internal void BindRefillItems()
        {
            RefillItems(ammoPlayer);
        }

        internal void RefillItems(Ammo ammo)
        {
            for (int i = 0; i < ammo.GetUsableSlotCount(); i++)
            {
                if (ammo.GetSlotCount(i) > 0 && ammo.GetSlotCount(i) < ammo.GetSlotMaxCount(i) && ammo.GetSlotName(i) != Identifiable.Id.NONE) Item(ammo, ammo.GetSlotName(i), ammo.GetSlotMaxCount(i), i);
            }
        }

        public void CommandPrintItems()
        {
            List<string> items = new List<string>();
            foreach (Identifiable.Id id in itemEnum)
            {
                string name = GetItemName(id);
                if (name.Contains(" ")) name = "\"" + name + "\"";
                items.Add(name + " (" + id.ToString() + ")");
            }
            File.WriteAllLines(fileItems, items.ToArray());
            Process.Start(fileItems);
        }
        #endregion

        #region No Clip
        public void CommandNoClip()
        {
            if (!InGame(true)) return;
            ToggleNoClip();
            UMFGUI.AddConsoleText("Successfully turned NoClip " + (noClip ? "on" : "off") + ".");
        }

        public void ToggleNoClip()
        {
            if (!InGame()) return;
            noClip = !noClip;
            //noClipPos = player.transform.position;
            controller.gameObject.layer = (noClip ? 14 : 8);
            controller.MotorFreeFly = noClip;
        }

        /*private void UpdateNoClip()
        {
            if (noClip && player && camera)
            {
                float speed = 20f * (SRInput.Actions.run.State ? 2f : 1f);
                noClipPos += camera.transform.forward * SRInput.Actions.vertical.RawValue * speed * Time.deltaTime;
                noClipPos += camera.transform.right * SRInput.Actions.horizontal.RawValue * speed * Time.deltaTime;
                controller.Stop();
                controller.SetPosition(noClipPos);
            }
        }*/
        #endregion

        #region Infinite Health
        public void CommandInfiniteHealth()
        {
            if (!InGame(true)) return;
            ToggleInfiniteHealth();
            UMFGUI.AddConsoleText("Successfully turned Infinite Health " + (infiniteHealth ? "on" : "off") + ".");
        }

        internal void ToggleInfiniteHealth()
        {
            infiniteHealth = !infiniteHealth;
        }

        private void UpdateInfiniteHealth()
        {
            if (gameModel && infiniteHealth)
            {
                playerModel.SetHealth(playerModel.maxHealth);
                playerModel.SetRad(0f);
            }
        }
        #endregion

        #region Infinite Energy
        public void CommandInfiniteEnergy()
        {
            if (!InGame(true)) return;
            ToggleInfiniteEnergy();
            UMFGUI.AddConsoleText("Successfully turned Infinite Energy " + (infiniteEnergy ? "on" : "off") + ".");
        }

        internal void ToggleInfiniteEnergy()
        {
            infiniteEnergy = !infiniteEnergy;
        }

        private void UpdateInfiniteEnergy()
        {
            if (gameModel & infiniteEnergy) playerModel.SetEnergy(playerModel.maxEnergy);
        }
        #endregion

        #region Time
        public void CommandIncreaseTime()
        {
            if (!InGame(true)) return;
            double time = SRCMConfig.IncDecTimeDefault * 60d;
            if (UMFGUI.Args.Length > 0) time = double.Parse(UMFGUI.Args[0]) * 60d;
            IncreaseTime(time);
            UMFGUI.AddConsoleText("Successfully increased world time.");
        }

        public void CommandDecreaseTime()
        {
            if (!InGame(true)) return;
            double time = SRCMConfig.IncDecTimeDefault * 60d;
            if (UMFGUI.Args.Length > 0) time = double.Parse(UMFGUI.Args[0]) * 60d;
            DecreaseTime(time);
            UMFGUI.AddConsoleText("Successfully decreased world time.");
        }

        public void CommandSleepwalk()
        {
            if (!InGame(true)) return;
            bool wasSleepwalking = timeDirector.IsFastForwarding();
            ToggleSleepwalk();
            UMFGUI.AddConsoleText("Successfully turned Sleepwalking " + (!wasSleepwalking ? "on" : "off") + ".");
        }

        internal void ToggleSleepwalk()
        {
            if (!timeDirector.IsFastForwarding()) timeDirector.FastForwardTo(999999999999999999d);
            else timeDirector.FastForwardTo(timeDirector.WorldTime());
        }

        internal void BindSleepwalk()
        {
            ToggleSleepwalk();
        }

        internal void BindIncreaseTime()
        {
            IncreaseTime(SRCMConfig.IncDecTimeDefault * 60d);
        }

        internal void BindDecreaseTime()
        {
            DecreaseTime(SRCMConfig.IncDecTimeDefault * 60d);
        }

        private void IncreaseTime(double time)
        {
            SetWorldTime(GetWorldTime() + time);
        }

        private void DecreaseTime(double time)
        {
            SetWorldTime(GetWorldTime() - time);
        }

        private void SetWorldTime(double value)
        {
            worldModel.worldTime = value;
            if (worldModel.worldTime < 0d) worldModel.worldTime = 0d;
        }

        private double GetWorldTime()
        {
            return worldModel.worldTime;
        }
        #endregion

        #region Upgrades
        public void CommandUnlockUpgrades()
        {
            if (!InGame(true)) return;
            List<PlayerState.Upgrade> upgrades = Enum.GetValues(typeof(PlayerState.Upgrade)).Cast<PlayerState.Upgrade>().ToList();
            playerModel.SetUpgrades(upgrades);
            playerModel.SetHealth(playerModel.maxHealth);
            playerModel.SetEnergy(playerModel.maxEnergy);
            UMFGUI.AddConsoleText("Successfully unlocked all player upgrades.");
        }

        public void CommandResetUpgrades()
        {
            if (!InGame(true)) return;
            //playerState.Reset(playerModel);
            playerModel.upgrades.Clear();
            playerModel.maxEnergy = 100;
            playerModel.maxHealth = 100;
            playerModel.maxRads = 100;
            playerModel.maxAmmo = PlayerModel.DEFAULT_MAX_AMMO[0];
            playerModel.currEnergy = playerModel.maxEnergy;
            playerModel.currHealth = playerModel.maxHealth;
            playerModel.currRads = 0f;
            UMFGUI.AddConsoleText("Successfully reset player upgrades.");
        }
        #endregion

        #region Progress
        public void CommandUnlockProgress()
        {
            if (!InGame(true)) return;
            List<PediaDirector.Id> pedia = Enum.GetValues(typeof(PediaDirector.Id)).Cast<PediaDirector.Id>().ToList();
            foreach (PediaDirector.Id pdID in pedia)
            {
                if (!pediaDirector.IsUnlocked(pdID)) pediaDirector.UnlockWithoutPopup(pdID);
            }

            List<ProgressDirector.ProgressType> progs = Enum.GetValues(typeof(ProgressDirector.ProgressType)).Cast<ProgressDirector.ProgressType>().ToList();
            foreach (ProgressDirector.ProgressType prog in progs)
            {
                switch (prog)
                {
                    case ProgressDirector.ProgressType.ENTER_ZONE_SLIMULATION:
                        progressDirector.SetProgress(prog, timeDirector.CurrDay() - 1);
                        break;
                    default:
                        if (prog != ProgressDirector.ProgressType.NONE) progressDirector.SetProgress(prog, 99);
                        break;
                }
            }

            List<ZoneDirector.Zone> zones = Enum.GetValues(typeof(ZoneDirector.Zone)).Cast<ZoneDirector.Zone>().ToList();
            foreach (ZoneDirector.Zone zone in zones)
            {
                if (!playerState.HasUnlockedMap(zone)) playerState.UnlockMap(zone);
            }

            /*List<RanchDirector.Palette> palettes = Enum.GetValues(typeof(RanchDirector.Palette)).Cast<RanchDirector.Palette>().ToList();
            foreach (RanchDirector.Palette palette in palettes)
            {
                if (blockedPalettes.Contains(palette)) continue;
                if (!dlcDirector.HasReached(DLCPackage.Id.PLAYSET_SCIFI, DLCPackage.State.INSTALLED) && palette == RanchDirector.Palette.PALETTE29) continue;
                if (!ranchDirector.HasPalette(palette)) 
            }*/

            List<Gadget.Id> gadgets = Enum.GetValues(typeof(Gadget.Id)).Cast<Gadget.Id>().ToList();
            foreach (Gadget.Id gadget in gadgets)
            {
                if (blockedGadgets.Contains(gadget)) continue;
                if (!dlcDirector.HasReached(DLCPackage.Id.PLAYSET_SCIFI, DLCPackage.State.INSTALLED) && gadget == Gadget.Id.FASHION_POD_SCIFI) continue;
                if (!gadgetsModel.blueprints.Contains(gadget)) gadgetsModel.blueprints.Add(gadget);
                if (!gadgetsModel.availBlueprints.Contains(gadget)) gadgetsModel.availBlueprints.Add(gadget);
            }
            gadgetDirector.CheckAllBlueprintLockers();

            List<TutorialDirector.Id> tuts = Enum.GetValues(typeof(TutorialDirector.Id)).Cast<TutorialDirector.Id>().ToList();
            foreach (TutorialDirector.Id tut in tuts)
            {
                if (gameModel.GetTutorialsModel().completedIds.Contains(tut)) gameModel.GetTutorialsModel().completedIds.Add(tut);
            }
            gameModel.GetTutorialsModel().popupQueue.Clear();
            clearPopups = true;

            foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (gameObject.transform != null)
                {
                    AccessDoor accessDoor = gameObject.GetComponent<AccessDoor>() ?? gameObject.GetComponentInChildren<AccessDoor>();
                    if (accessDoor != null && accessDoor.CurrState != AccessDoor.State.OPEN) accessDoor.CurrState = AccessDoor.State.OPEN;
                }
            }

            playerModel.currHealth = playerModel.maxHealth;
            UMFGUI.AddConsoleText("Successfully unlocked all progress.");
        }

        private void UpdatePopups()
        {
            if (clearPopups && Time.timeScale != 0f && !timeDirector.HasPauser() && !UMFGUI.IsConsoleOpen)
            {
                TutorialPopupUI popup = Traverse.Create(tutorialDirector).Field("currPopup").GetValue<TutorialPopupUI>();
                if (popup != null)
                {
                    tutorialDirector.SuppressTutorials();
                    popup.Complete();
                    tutorialDirector.UnsuppressTutorials();
                }
                else clearPopups = false;
            }
        }

        public void CommandResetProgress()
        {
            if (!InGame(true)) return;
            gameModel.GetPediaModel().ResetUnlocked(new PediaDirector.Id[] { PediaDirector.Id.BASICS });
            gameModel.GetTutorialsModel().completedIds.Clear();
            gameModel.GetProgressModel().Reset();
            playerModel.unlockedZoneMaps.Clear();
            playerModel.unlockedZoneMaps.Add(ZoneDirector.Zone.RANCH);
            gadgetsModel.Reset();
            playerModel.currHealth = playerModel.maxHealth;
            UMFGUI.AddConsoleText("Successfully reset all progress.");
        }

        public void CommandListProgress()
        {
            List<ProgressDirector.ProgressType> progs = Enum.GetValues(typeof(ProgressDirector.ProgressType)).Cast<ProgressDirector.ProgressType>().ToList();
            foreach (ProgressDirector.ProgressType prog in progs)
            {
                Log(prog.ToString() + "=" + progressDirector.GetProgress(prog));
            }
        }
        #endregion

        #region Treasure Pods
        public void CommandUnlockTreasurePods()
        {
            if (!InGame(true)) return;
            foreach (KeyValuePair<string, TreasurePodModel> kvp in SRSingleton<SceneContext>.Instance.GameModel.AllPods())
            {
                if (DLCDirector.SECRET_STYLE_TREASURE_PODS.Contains(kvp.Key) && dlcDirector.GetPackageState(DLCPackage.Id.SECRET_STYLE) != DLCPackage.State.INSTALLED) continue;
                if (kvp.Value.state == TreasurePod.State.LOCKED)
                {
                    GameObject pod = Traverse.Create(kvp.Value).Field<GameObject>("gameObj").Value;
                    TreasurePod treasurePod = pod.GetComponent<TreasurePod>() ?? pod.GetComponentInChildren<TreasurePod>();
                    if (treasurePod != null && treasurePod.CurrState == TreasurePod.State.LOCKED)
                    {
                        Traverse.Create(treasurePod).Method("UpdateImmediate", TreasurePod.State.OPEN).GetValue();
                        StartCoroutine((SRSingleton<SceneContext>.Instance.GameModel.currGameMode != PlayerState.GameMode.TIME_LIMIT_V2) ? AwardPrizesDefault(treasurePod) : AwardPrizesRushMode(treasurePod));
                    }
                }
            }
            UMFGUI.AddConsoleText("Successfully unlocked all treasure pods.");
        }

        private IEnumerator AwardPrizesDefault(TreasurePod treasurePod)
        {
            yield return Traverse.Create(treasurePod).Method("AwardPrizesDefault").GetValue<IEnumerator>();
            yield break;
        }

        private IEnumerator AwardPrizesRushMode(TreasurePod treasurePod)
        {
            yield return Traverse.Create(treasurePod).Method("AwardPrizesRushMode").GetValue<IEnumerator>();
            yield break;
        }

        public void CommandResetTreasurePods()
        {
            if (!InGame(true)) return;
            foreach (KeyValuePair<string, TreasurePodModel> kvp in SRSingleton<SceneContext>.Instance.GameModel.AllPods())
            {
                if (DLCDirector.SECRET_STYLE_TREASURE_PODS.Contains(kvp.Key) && dlcDirector.GetPackageState(DLCPackage.Id.SECRET_STYLE) != DLCPackage.State.INSTALLED) continue;
                if (kvp.Value.state == TreasurePod.State.OPEN)
                {
                    GameObject pod = Traverse.Create(kvp.Value).Field<GameObject>("gameObj").Value;
                    TreasurePod treasurePod = pod.GetComponent<TreasurePod>() ?? pod.GetComponentInChildren<TreasurePod>();
                    if (treasurePod != null && treasurePod.CurrState == TreasurePod.State.OPEN)
                    {
                        Traverse.Create(treasurePod).Method("UpdateImmediate", TreasurePod.State.LOCKED).GetValue();
                    }
                }
            }
            UMFGUI.AddConsoleText("Successfully reset all treasure pods.");
        }
        #endregion

        #region Pause
        public static void Pause(bool pause)
        {
            TimeDirector td = null;
            try
            {
                td = SRSingleton<SceneContext>.Instance.TimeDirector;
            }
            catch { }
            if (!td) return;
            if (pause)
            {
                if (!td.HasPauser()) td.Pause();
            }
            else td.Unpause();
        }

        /*private void Pause()
        {
            if (!timeDirector.HasPauser()) timeDirector.Pause();
        }

        private void Unpause()
        {
            timeDirector.Unpause();
        }*/

        private void UpdateUnpause()
        {
            if (!MenuEnabled && MenuWasEnabled)
            {
                Pause(false);
                MenuWasEnabled = false;
            }
        }
        #endregion

        #region Misc Functions
        private bool InGame(bool consoleText = false)
        {
            if (Levels.isSpecial())
            {
                if (consoleText) UMFGUI.AddConsoleText("Warning: This command can only be used in-game.");
                return false;
            }
            return true;
        }

        internal void ToggleMenu()
        {
            if (!InGame() || UMFGUI.IsMenuOpen || UMFGUI.IsConsoleOpen) return;
            MenuEnabled = !MenuEnabled;
            if (MenuEnabled && timeDirector && timeDirector.HasPauser()) MenuEnabled = false;
            if (MenuEnabled) MenuUpdate = true;
        }

        private void UpdateTarget()
        {
            if (InGame() && !timeDirector.HasPauser())
            {
                if (Physics.Raycast(Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out rayHit, 50f, -1))
                {
                    GameObject gameObject = rayHit.collider.gameObject;
                    SiloStorage siloStorage = gameObject.GetComponentInParent<SiloStorage>();
                    if (siloStorage != null) target = siloStorage;
                    else target = null;
                }
                else target = null;
            }
        }
        #endregion

        #region Update Routine
        public void Update()
        {
            UpdateInstances();
            UpdateUnpause();
            UpdatePopups();
            UpdateInfiniteHealth();
            UpdateInfiniteEnergy();
            UpdateTarget();
            //UpdateNoClip();
        }
        #endregion

        #region GUI
        private void InitStyles()
        {
            styleSlot.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            styleSlot.fontStyle = FontStyle.Bold;
            styleSlot.normal.textColor = SRCMConfig.TextColor;
            styleSlot.alignment = TextAnchor.MiddleLeft;

            styleNormal.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            styleNormal.fontStyle = FontStyle.Normal;
            styleNormal.normal.textColor = Color.white;
            styleNormal.alignment = TextAnchor.MiddleLeft;
            styleNormal.clipping = TextClipping.Overflow;
            styleNormal.wordWrap = true;

            styleUpperCenter.font = Font.CreateDynamicFontFromOSFont("Arial", 18);
            styleUpperCenter.fontStyle = FontStyle.Bold;
            styleUpperCenter.normal.textColor = Color.white;
            styleUpperCenter.alignment = TextAnchor.UpperCenter;

            styleUpperCenterShadow.font = styleUpperCenter.font;
            styleUpperCenterShadow.fontStyle = FontStyle.Bold;
            styleUpperCenterShadow.normal.textColor = Color.black;
            styleUpperCenterShadow.alignment = TextAnchor.UpperCenter;

            styleDropdown.font = Font.CreateDynamicFontFromOSFont("Arial", 18);
            styleDropdown.fontStyle = FontStyle.Bold;
            styleDropdown.normal.textColor = Color.white;
            styleDropdown.alignment = TextAnchor.MiddleLeft;
            styleDropdown.onHover.background = styleDropdown.hover.background = new Texture2D(2, 2);
            styleDropdown.padding.left = styleDropdown.padding.right = styleDropdown.padding.top = styleDropdown.padding.bottom = 4;

            styleKeybind.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            styleKeybind.fontStyle = FontStyle.Bold;
            styleKeybind.normal.textColor = SRCMConfig.GUIColor;
            styleKeybind.alignment = TextAnchor.MiddleRight;

            styleKeybindShadow.font = styleKeybind.font;
            styleKeybindShadow.fontStyle = styleKeybind.fontStyle;
            styleKeybindShadow.normal.textColor = Color.black;
            styleKeybindShadow.alignment = styleKeybind.alignment;
        }

        public void OnGUI()
        {
            if (MenuEnabled && !UMFGUI.IsMenuOpen && !UMFGUI.IsConsoleOpen && playerState && InGame())
            {
                if (!styleSlot.font) InitStyles();
                GUI.backgroundColor = SRCMConfig.GUIColor;
                styleSlot.normal.textColor = SRCMConfig.TextColor;
                windowRect = GUI.Window(221133, windowRect, CheatMenuWindow, "");
                if (MenuUpdate) GUI.BringWindowToFront(221133);
                GUI.FocusWindow(221133);
                Pause(true);
                MenuWasEnabled = true;
            }

            if (!KeybindShown && InGame())
            {
                if (!styleKeybind.font) InitStyles();
                if (KeyBindShowStart == 0f) KeyBindShowStart = Time.realtimeSinceStartup;
                if ((Time.realtimeSinceStartup - KeyBindShowStart) <= 20f)
                {
                    string text = "Press " + SRCMConfig.KeysCheatMenu[0] + " to open the Cheat Menu.";
                    GUI.Label(new Rect(Screen.width - 298, (Screen.height / 2) + 2, 250, 30), text, styleKeybindShadow);
                    GUI.Label(new Rect(Screen.width - 300, Screen.height / 2, 250, 30), text, styleKeybind);
                }
                else KeybindShown = true;
            }
        }

        private void CheatMenuWindow(int idWindow)
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) ToggleMenu();

            GUI.backgroundColor = SRCMConfig.GUIColor;
            GUI.skin.button.fontSize = 16;
            GUI.skin.button.fontStyle = FontStyle.Bold;
            GUI.skin.toggle.fontSize = 16;
            GUI.skin.toggle.fontStyle = FontStyle.Bold;
            GUI.skin.textField.fontSize = 16;
            GUI.skin.textField.fontStyle = FontStyle.Bold;
            GUI.skin.textField.alignment = TextAnchor.MiddleLeft;

            GUI.Box(new Rect(2, 2, guiSizeX - 4, 64), "");
            GUI.Label(new Rect(1, 8, guiSizeX, 30), MenuName, styleUpperCenterShadow);
            GUI.Label(new Rect(0, 7, guiSizeX, 30), MenuName, styleUpperCenter);
            if (GUI.Button(new Rect(guiSizeX - 33, 3, 30, 30), "X")) ToggleMenu();
            toolbarTab = GUI.Toolbar(new Rect(4, 34, guiSizeX - 8, 30), toolbarTab, toolbarTabs.ToArray());
            if (toolbarTab != toolbarTabPrevious)
            {
                if (toolbarTabPrevious == 3) target = null;
                dropDowns.Clear();
                slotCounts.Clear();
                toolbarTabPrevious = toolbarTab;
            }
            int dataHeight = 36;

            //Main Tab
            if (toolbarTabs[toolbarTab].text == "Main")
            {
                //Command Buttons
                int buttonBar = 0;
                int numButtons = 6;
                int buttonBarTotalWidth = 2 + 160 + 10;
                buttonBarScroll = GUI.BeginScrollView(new Rect(2, 68, 160, guiSizeY - 72), buttonBarScroll, new Rect(0, 0, 0, 40 * numButtons + 10), false, true);
                if (GUI.Button(new Rect(8, buttonBar += 10, 130, 30), "Refill Items"))
                {
                    RefillItems(ammoPlayer);
                    MenuUpdate = true;
                }
                if (GUI.Button(new Rect(8, buttonBar += 40, 130, 30), "No Clip [" + (noClip ? "On" : "Off") + "]"))
                {
                    ToggleNoClip();
                    ToggleMenu();
                }
                if (GUI.Button(new Rect(8, buttonBar += 40, 130, 30), "Inf Health [" + (infiniteHealth ? "On" : "Off") + "]"))
                {
                    ToggleInfiniteHealth();
                    ToggleMenu();
                }
                if (GUI.Button(new Rect(8, buttonBar += 40, 130, 30), "Inf Energy [" + (infiniteEnergy ? "On" : "Off") + "]"))
                {
                    ToggleInfiniteEnergy();
                    ToggleMenu();
                }
                if (GUI.Button(new Rect(8, buttonBar += 40, 130, 30), "Inc Time"))
                {
                    SetWorldTime(GetWorldTime() + 3600d);
                }
                if (GUI.Button(new Rect(8, buttonBar += 40, 130, 30), "Dec Time"))
                {
                    SetWorldTime(GetWorldTime() - 3600d);
                }
                if (GUI.Button(new Rect(8, buttonBar += 40, 130, 30), "Sleepwalk [" + (timeDirector.IsFastForwarding() ? "On" : "Off") + "]"))
                {
                    ToggleSleepwalk();
                    ToggleMenu();
                }
                GUI.EndScrollView();

                //Search
                GUI.Label(new Rect(buttonBarTotalWidth, dataHeight += 40, 80, 30), "Search: ", styleSlot);
                search = GUI.TextField(new Rect(buttonBarTotalWidth + 80 + 10, dataHeight, 260, 30), search);
                if (GUI.Button(new Rect(buttonBarTotalWidth + 80 + 10 + 260 + 10, dataHeight, 30, 30), "X")) search = string.Empty;
                if (searchPrevious != search || categoryPrevious != category || MenuUpdate)
                {
                    searchPrevious = search;
                    categoryPrevious = category;
                    dropDowns.Clear();
                    slotCounts.Clear();
                }
                if (search != string.Empty) itemIds = itemEnum.Where(y => GetItemName(y).ToLower().Contains(search.ToLower())).Select(x => x).ToList();
                else itemIds = itemEnum;
                itemIds = SortItemList(itemIds);


                //Category
                GUI.Label(new Rect(buttonBarTotalWidth, dataHeight += 40, 80, 30), "Category: ", styleSlot);
                if (!dropDowns.ContainsKey("Category"))
                {
                    GUIContent[] dropDownList = new GUIContent[categories.Count];
                    for (int j = 0; j < categories.Count; j++) dropDownList[j] = new GUIContent(categories[j]);
                    dropDowns.Add("Category", new UMFDropDown(new Rect(buttonBarTotalWidth + 80 + 10, dataHeight, 260, 30), new GUIContent(category), dropDownList, styleDropdown));
                }
                category = (dropDowns["Category"].SelectedItemIndex != -1 ? categories[dropDowns["Category"].SelectedItemIndex] : category);
                if (GUI.Button(new Rect(buttonBarTotalWidth + 80 + 10 + 260 + 10, dataHeight, 30, 30), "X")) category = "All";
                if (category != "All")
                {
                    //if (category == "Allergy Free") itemIds = itemIds.Where(x => Identifiable.IsAllergyFree(x)).ToList();
                    if (category == "Animal") itemIds = itemIds.Where(x => Identifiable.IsAnimal(x)).ToList();
                    if (category == "Chick") itemIds = itemIds.Where(x => Identifiable.IsChick(x)).ToList();
                    if (category == "Craft") itemIds = itemIds.Where(x => Identifiable.IsCraft(x)).ToList();
                    if (category == "Echo") itemIds = itemIds.Where(x => Identifiable.IsEcho(x)).ToList();
                    if (category == "Echo Note") itemIds = itemIds.Where(x => Identifiable.IsEchoNote(x)).ToList();
                    if (category == "Fashion") itemIds = itemIds.Where(x => Identifiable.IsFashion(x)).ToList();
                    if (category == "Food") itemIds = itemIds.Where(x => Identifiable.IsFood(x)).ToList();
                    if (category == "Fruit") itemIds = itemIds.Where(x => Identifiable.IsFruit(x)).ToList();
                    if (category == "Gordo") itemIds = itemIds.Where(x => Identifiable.IsGordo(x)).ToList();
                    if (category == "Largo") itemIds = itemIds.Where(x => Identifiable.IsLargo(x)).ToList();
                    //if (category == "Non-Slime Resource") itemIds = itemIds.Where(x => Identifiable.IsNonSlimeResource(x)).ToList();
                    if (category == "Ornament") itemIds = itemIds.Where(x => Identifiable.IsOrnament(x)).ToList();
                    if (category == "Plort") itemIds = itemIds.Where(x => Identifiable.IsPlort(x)).ToList();
                    if (category == "Slime") itemIds = itemIds.Where(x => Identifiable.IsSlime(x)).ToList();
                    if (category == "Toy") itemIds = itemIds.Where(x => Identifiable.IsToy(x)).ToList();
                    if (category == "Veggie") itemIds = itemIds.Where(x => Identifiable.IsVeggie(x)).ToList();
                }
                //itemIds.Sort();
                itemIds = SortItemList(itemIds);
                if (!itemIds.Contains(Identifiable.Id.NONE)) itemIds.Insert(0, Identifiable.Id.NONE);
                GUI.Label(new Rect(buttonBarTotalWidth + 80 + 10 + 260 + 10 + 30 + 10 + 35, dataHeight - 20, 100, 30), "Items: " + (itemIds.Count - 1).ToString(), styleNormal);


                //Item Slots
                for (int i = 0; i < ammoPlayer.GetUsableSlotCount(); i++)
                {
                    dataHeight += 40;
                    string slot = "Slot " + (i + 1).ToString();
                    bool slot5 = (i == 4);
                    GUI.Label(new Rect(buttonBarTotalWidth, dataHeight, 80, 30), slot + ": ", styleSlot);
                    if (!dropDowns.ContainsKey(slot))
                    {
                        GUIContent[] dropDownList = new GUIContent[(slot5 ? itemIdsWater.Count : itemIds.Count)];
                        for (int j = 0; j < (slot5 ? itemIdsWater.Count : itemIds.Count); j++) dropDownList[j] = new GUIContent(GetItemName((slot5 ? itemIdsWater[j] : itemIds[j])), itemTextures[(slot5 ? itemIdsWater[j] : itemIds[j])]);
                        dropDowns.Add(slot, new UMFDropDown(new Rect(buttonBarTotalWidth + 80 + 10, dataHeight, 260, 30), new GUIContent(GetItemName(GetSlotItemId(i)), itemTextures[GetSlotItemId(i)]), dropDownList, styleDropdown));
                        dropDowns[slot].EntriesBeforeScroll = 10 - i;
                        slotCounts.Add(slot, GetSlotCount(i));
                    }
                    if (GUI.Button(new Rect(buttonBarTotalWidth + 80 + 10 + 260 + 10, dataHeight, 30, 30), "X"))
                    {
                        ClearItem(ammoPlayer, i);
                        MenuUpdate = true;
                        return;
                    }
                    slotCounts[slot] = int.Parse(GUI.TextField(new Rect(buttonBarTotalWidth + 80 + 10 + 260 + 10 + 30 + 10, dataHeight, 50, 30), (MenuUpdate ? GetSlotCount(i).ToString() : slotCounts[slot].ToString())));
                    slotCounts[slot] = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(buttonBarTotalWidth + 80 + 10 + 260 + 10 + 30 + 10 + 50 + 10, dataHeight + 8, 160, 30), (float)slotCounts[slot], 0f, (float)GetSlotMaxCount(i)));
                    Identifiable.Id selectedItem = dropDowns[slot].SelectedItemIndex != -1 ? (MenuUpdate ? GetSlotItemId(i) : (slot5 ? itemIdsWater[dropDowns[slot].SelectedItemIndex] : itemIds[dropDowns[slot].SelectedItemIndex])) : GetSlotItemId(i);
                    if (selectedItem != Identifiable.Id.NONE && slotCounts[slot] == 0) slotCounts[slot] = 1;
                    if (selectedItem == Identifiable.Id.NONE) slotCounts[slot] = 0;
                    if (categoryPrevious == category && (selectedItem != GetSlotItemId(i) || slotCounts[slot] != GetSlotCount(i))) Item(ammoPlayer, selectedItem, slotCounts[slot], i);
                }
                GUI.Label(new Rect(buttonBarTotalWidth, dataHeight += 40, guiSizeX - 20, 30), "Note: Items that are not listed require a mod to make those items vacuumable.", styleNormal);


                //If any item slot drop downs are activated, disable the controls below them.
                bool isCBClicked = false;
                foreach (UMFDropDown dd in dropDowns.Values)
                {
                    if (dd.IsButtonClicked)
                    {
                        isCBClicked = true;
                        break;
                    }
                }


                //Money
                GUI.Label(new Rect(buttonBarTotalWidth, dataHeight += 40, 80, 30), "Money: ", styleSlot);
                if (isCBClicked) GUI.enabled = false;
                playerModel.currency = int.Parse(GUI.TextField(new Rect(buttonBarTotalWidth + 80 + 10, dataHeight, 80, 30), playerModel.currency.ToString()));
                playerModel.currency = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(buttonBarTotalWidth + 80 + 10 + 80 + 10, dataHeight + 8, 455, 30), (float)playerModel.currency, 0f, 999999f));
                GUI.enabled = true;

                //Keys
                GUI.Label(new Rect(buttonBarTotalWidth, dataHeight += 40, 80, 30), "Keys: ", styleSlot);
                if (isCBClicked) GUI.enabled = false;
                playerModel.keys = int.Parse(GUI.TextField(new Rect(buttonBarTotalWidth + 80 + 10, dataHeight, 80, 30), playerModel.keys.ToString()));
                playerModel.keys = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(buttonBarTotalWidth + 80 + 10 + 80 + 10, dataHeight + 8, 455, 30), (float)playerModel.keys, 0f, 100f));
                GUI.enabled = true;

                //Show dropdowns in reverse order to prevent overlap clipping, and disable any that are below an activated drop down.
                for (int i = dropDowns.Count - 1; i >= 0; --i) dropDowns.Values.ToArray()[i].Show();
                bool isClicked = false;
                foreach (UMFDropDown cb in dropDowns.Values)
                {
                    if (cb.IsButtonClicked)
                    {
                        isClicked = true;
                        continue;
                    }
                    if (isClicked) cb.Disable = true;
                    else cb.Disable = false;
                }
            }


            //Refinery Tab
            if (toolbarTabs[toolbarTab].text == "Refinery")
            {
                //Command Buttons
                int buttonBar = -30;
                int numButtons = 2;
                int buttonBarTotalWidth = 2 + 160 + 10;
                buttonBarScroll = GUI.BeginScrollView(new Rect(2, 68, 160, guiSizeY - 72), buttonBarScroll, new Rect(0, 0, 0, 40 * numButtons + 10), false, true);
                if (GUI.Button(new Rect(8, buttonBar += 40, 130, 30), "Max All"))
                {
                    gadgetsModel.craftMatCounts.Clear();
                    foreach (Identifiable.Id id in itemIdsRefinery)
                    {
                        if (!gadgetsModel.craftMatCounts.ContainsKey(id)) gadgetsModel.craftMatCounts.Add(id, RefineryLimit);
                        else gadgetsModel.craftMatCounts[id] = RefineryLimit;
                    }
                }
                if (GUI.Button(new Rect(8, buttonBar += 40, 130, 30), "Empty All"))
                {
                    foreach (Identifiable.Id id in itemIdsRefinery) gadgetsModel.craftMatCounts[id] = 0;
                }
                GUI.EndScrollView();

                //Refinery Entries
                int numEntries = itemIdsRefinery.Count;
                refineryScroll = GUI.BeginScrollView(new Rect(buttonBarTotalWidth, 68, guiSizeX - buttonBarTotalWidth - 4, guiSizeY - 72), refineryScroll, new Rect(0, 0, 0, 40 * numEntries + 10), false, true);
                dataHeight = -30;
                foreach (Identifiable.Id id in itemIdsRefinery)
                {
                    GUI.Label(new Rect(0, dataHeight += 40, 40, 40), itemTextures[id]);
                    GUI.Label(new Rect(35, dataHeight, 150, 30), refineryNames[id] + ":", styleSlot);
                    gadgetsModel.craftMatCounts[id] = int.Parse(GUI.TextField(new Rect(35 + 150 + 5, dataHeight, 60, 30), gadgetsModel.craftMatCounts[id].ToString()));
                    gadgetsModel.craftMatCounts[id] = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(35 + 150 + 5 + 60 + 5, dataHeight + 8, 320, 30), (float)gadgetsModel.craftMatCounts[id], 0f, (float)RefineryLimit));
                    if (GUI.Button(new Rect(35 + 150 + 5 + 60 + 5 + 320 + 5, dataHeight, 30, 30), "X")) gadgetsModel.craftMatCounts[id] = 0;
                    //if (gadgetsModel.craftMatCounts[id] == 0) gadgetsModel.craftMatCounts.Remove(id);
                }
                GUI.EndScrollView();
            }


            //Gadgets Tab
            if (toolbarTabs[toolbarTab].text == "Gadgets")
            {
                //Command Buttons
                int buttonBar = -30;
                int numButtons = 2;
                int buttonBarTotalWidth = 2 + 160 + 10;
                buttonBarScroll = GUI.BeginScrollView(new Rect(2, 68, 160, guiSizeY - 72), buttonBarScroll, new Rect(0, 0, 0, 40 * numButtons + 10), false, true);
                if (GUI.Button(new Rect(8, buttonBar += 40, 130, 30), "Max All"))
                {
                    gadgetsModel.gadgets.Clear();
                    foreach (Gadget.Id id in itemIdsGadgets)
                    {
                        if (!gadgetsModel.gadgets.ContainsKey(id)) gadgetsModel.gadgets.Add(id, RefineryLimit);
                        else gadgetsModel.gadgets[id] = RefineryLimit;
                    }
                }
                if (GUI.Button(new Rect(8, buttonBar += 40, 130, 30), "Empty All"))
                {
                    foreach (Gadget.Id id in itemIdsGadgets) gadgetsModel.gadgets[id] = 0;
                }
                GUI.EndScrollView();

                //Gadget Entries
                int numEntries = itemIdsGadgets.Count;
                gadgetScroll = GUI.BeginScrollView(new Rect(buttonBarTotalWidth, 68, guiSizeX - buttonBarTotalWidth - 4, guiSizeY - 72), gadgetScroll, new Rect(0, 0, 0, 40 * numEntries + 10), false, true);
                dataHeight = -30;
                foreach (Gadget.Id id in itemIdsGadgets)
                {
                    GUI.Label(new Rect(0, dataHeight += 40, 40, 40), gadgetTextures[id]);
                    GUI.Label(new Rect(35, dataHeight, 220, 30), gadgetNames[id] + ":", styleSlot);
                    gadgetsModel.gadgets[id] = int.Parse(GUI.TextField(new Rect(35 + 220 + 5, dataHeight, 60, 30), gadgetsModel.gadgets[id].ToString()));
                    gadgetsModel.gadgets[id] = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(35 + 220 + 5 + 60 + 5, dataHeight + 8, 250, 30), (float)gadgetsModel.gadgets[id], 0f, (float)RefineryLimit));
                    if (GUI.Button(new Rect(35 + 220 + 5 + 60 + 5 + 250 + 5, dataHeight, 30, 30), "X")) gadgetsModel.gadgets[id] = 0;
                    //if (gadgetsModel.gadgets[id] == 0) gadgetsModel.gadgets.Remove(id);
                }
                GUI.EndScrollView();
            }


            //Decorizer Tab
            if (toolbarTabs[toolbarTab].text == "Decorizer")
            {
                //Command Buttons
                int buttonBar = -30;
                int numButtons = 2;
                int buttonBarTotalWidth = 2 + 160 + 10;
                buttonBarScroll = GUI.BeginScrollView(new Rect(2, 68, 160, guiSizeY - 72), buttonBarScroll, new Rect(0, 0, 0, 40 * numButtons + 10), false, true);
                if (GUI.Button(new Rect(8, buttonBar += 40, 130, 30), "Max All"))
                {
                    decorizerContents.Clear();
                    foreach (Identifiable.Id id in itemIdsDecorizer)
                    {
                        if (!decorizerContents.ContainsKey(id)) decorizerContents.Add(id, RefineryLimit);
                        else decorizerContents[id] = RefineryLimit;
                    }
                }
                if (GUI.Button(new Rect(8, buttonBar += 40, 130, 30), "Empty All"))
                {
                    foreach (Identifiable.Id id in itemIdsDecorizer) decorizerContents[id] = 0;
                }
                GUI.EndScrollView();

                //Decorizer Entries
                int numEntries = itemIdsGadgets.Count;
                decorizerScroll = GUI.BeginScrollView(new Rect(buttonBarTotalWidth, 68, guiSizeX - buttonBarTotalWidth - 4, guiSizeY - 72), decorizerScroll, new Rect(0, 0, 0, 40 * numEntries + 10), false, true);
                dataHeight = -30;
                foreach (Identifiable.Id id in itemIdsDecorizer)
                {
                    GUI.Label(new Rect(0, dataHeight += 40, 40, 40), itemTextures[id]);
                    GUI.Label(new Rect(35, dataHeight, 220, 30), decorizerNames[id] + ":", styleSlot);
                    decorizerContents[id] = int.Parse(GUI.TextField(new Rect(35 + 220 + 5, dataHeight, 60, 30), decorizerContents[id].ToString()));
                    decorizerContents[id] = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(35 + 220 + 5 + 60 + 5, dataHeight + 8, 250, 30), (float)decorizerContents[id], 0f, (float)RefineryLimit));
                    if (GUI.Button(new Rect(35 + 220 + 5 + 60 + 5 + 250 + 5, dataHeight, 30, 30), "X")) decorizerContents[id] = 0;
                }
                GUI.EndScrollView();
            }


            //Target Tab
            if (toolbarTabs[toolbarTab].text == "Target")
            {
                if (target == null)
                {
                    GUI.Label(new Rect(70, 240, guiSizeX - 20, 30), "Look directly at a Silo, Plort Collector or Slime Feeder in order to edit the inventory of it.", styleSlot);
                }
                else
                {
                    Ammo ammo = target.GetRelevantAmmo();

                    //Command Buttons
                    int buttonBar = -30;
                    int numButtons = 2;
                    int buttonBarTotalWidth = 2 + 160 + 10;
                    buttonBarScroll = GUI.BeginScrollView(new Rect(2, 68, 160, guiSizeY - 72), buttonBarScroll, new Rect(0, 0, 0, 40 * numButtons + 10), false, true);
                    if (GUI.Button(new Rect(8, buttonBar += 40, 130, 30), "Refill All"))
                    {
                        RefillItems(ammo);
                        MenuUpdate = true;
                    }
                    if (GUI.Button(new Rect(8, buttonBar += 40, 130, 30), "Empty All"))
                    {
                        for (int i = 0; i < ammo.GetUsableSlotCount(); i++) ammo.Clear(i);
                        MenuUpdate = true;
                    }
                    GUI.EndScrollView();

                    int numEntries = ammo.GetUsableSlotCount() + 2;
                    targetScroll = GUI.BeginScrollView(new Rect(buttonBarTotalWidth, 68, guiSizeX - buttonBarTotalWidth - 4, guiSizeY - 72), targetScroll, new Rect(0, 0, 0, 40 * numEntries + 10 + 100), false, true);
                    dataHeight = -30;

                    //Search
                    GUI.Label(new Rect(0, dataHeight += 40, 80, 30), "Search: ", styleSlot);
                    search = GUI.TextField(new Rect(80 + 10, dataHeight, 260, 30), search);
                    if (GUI.Button(new Rect(80 + 10 + 260 + 10, dataHeight, 30, 30), "X")) search = string.Empty;
                    if (searchPrevious != search || categoryPrevious != category || MenuUpdate)
                    {
                        searchPrevious = search;
                        categoryPrevious = category;
                        dropDowns.Clear();
                        slotCounts.Clear();
                    }
                    if (search != string.Empty) itemIds = itemEnum.Where(y => GetItemName(y).ToLower().Contains(search.ToLower())).Select(x => x).ToList();
                    else itemIds = itemEnum;
                    itemIds = SortItemList(itemIds);


                    //Category
                    GUI.Label(new Rect(0, dataHeight += 40, 80, 30), "Category: ", styleSlot);
                    if (!dropDowns.ContainsKey("Category"))
                    {
                        GUIContent[] dropDownList = new GUIContent[categories.Count];
                        for (int j = 0; j < categories.Count; j++) dropDownList[j] = new GUIContent(categories[j]);
                        dropDowns.Add("Category", new UMFDropDown(new Rect(80 + 10, dataHeight, 260, 30), new GUIContent(category), dropDownList, styleDropdown));
                    }
                    category = (dropDowns["Category"].SelectedItemIndex != -1 ? categories[dropDowns["Category"].SelectedItemIndex] : category);
                    if (GUI.Button(new Rect(80 + 10 + 260 + 10, dataHeight, 30, 30), "X")) category = "All";
                    if (category != "All")
                    {
                        //if (category == "Allergy Free") itemIds = itemIds.Where(x => Identifiable.IsAllergyFree(x)).ToList();
                        if (category == "Animal") itemIds = itemIds.Where(x => Identifiable.IsAnimal(x)).ToList();
                        if (category == "Chick") itemIds = itemIds.Where(x => Identifiable.IsChick(x)).ToList();
                        if (category == "Craft") itemIds = itemIds.Where(x => Identifiable.IsCraft(x)).ToList();
                        if (category == "Echo") itemIds = itemIds.Where(x => Identifiable.IsEcho(x)).ToList();
                        if (category == "Echo Note") itemIds = itemIds.Where(x => Identifiable.IsEchoNote(x)).ToList();
                        if (category == "Fashion") itemIds = itemIds.Where(x => Identifiable.IsFashion(x)).ToList();
                        if (category == "Food") itemIds = itemIds.Where(x => Identifiable.IsFood(x)).ToList();
                        if (category == "Fruit") itemIds = itemIds.Where(x => Identifiable.IsFruit(x)).ToList();
                        if (category == "Gordo") itemIds = itemIds.Where(x => Identifiable.IsGordo(x)).ToList();
                        if (category == "Largo") itemIds = itemIds.Where(x => Identifiable.IsLargo(x)).ToList();
                        //if (category == "Non-Slime Resource") itemIds = itemIds.Where(x => Identifiable.IsNonSlimeResource(x)).ToList();
                        if (category == "Ornament") itemIds = itemIds.Where(x => Identifiable.IsOrnament(x)).ToList();
                        if (category == "Plort") itemIds = itemIds.Where(x => Identifiable.IsPlort(x)).ToList();
                        if (category == "Slime") itemIds = itemIds.Where(x => Identifiable.IsSlime(x)).ToList();
                        if (category == "Toy") itemIds = itemIds.Where(x => Identifiable.IsToy(x)).ToList();
                        if (category == "Veggie") itemIds = itemIds.Where(x => Identifiable.IsVeggie(x)).ToList();
                    }
                    //itemIds.Sort();
                    itemIds = SortItemList(itemIds);
                    if (!itemIds.Contains(Identifiable.Id.NONE)) itemIds.Insert(0, Identifiable.Id.NONE);
                    GUI.Label(new Rect(80 + 10 + 300 + 10 + 30 + 10 + 35, dataHeight - 20, 100, 30), "Items: " + (itemIds.Count - 1).ToString(), styleNormal);

                    //Target Slots
                    for (int i = 0; i < ammo.GetUsableSlotCount(); i++)
                    {
                        dataHeight += 40;
                        string slot = "Slot " + (i + 1).ToString();
                        GUI.Label(new Rect(0, dataHeight, 80, 30), slot + ": ", styleSlot);
                        if (!dropDowns.ContainsKey(slot))
                        {
                            GUIContent[] dropDownList = new GUIContent[itemIds.Count];
                            for (int j = 0; j < itemIds.Count; j++) dropDownList[j] = new GUIContent(GetItemName(itemIds[j]), itemTextures[itemIds[j]]);
                            dropDowns.Add(slot, new UMFDropDown(new Rect(80 + 10, dataHeight, 260, 30), new GUIContent(GetItemName(ammo.GetSlotName(i)), itemTextures[ammo.GetSlotName(i)]), dropDownList, styleDropdown));
                            dropDowns[slot].EntriesBeforeScroll = 6;
                            slotCounts.Add(slot, ammo.GetSlotCount(i));
                        }
                        if (GUI.Button(new Rect(80 + 10 + 260 + 10, dataHeight, 30, 30), "X"))
                        {
                            ammo.Clear(i);
                            MenuUpdate = true;
                            return;
                        }
                        slotCounts[slot] = int.Parse(GUI.TextField(new Rect(80 + 10 + 260 + 10 + 30 + 10, dataHeight, 50, 30), (MenuUpdate ? ammo.GetSlotCount(i).ToString() : slotCounts[slot].ToString())));
                        slotCounts[slot] = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(80 + 10 + 260 + 10 + 30 + 10 + 50 + 10, dataHeight + 8, 160, 30), (float)slotCounts[slot], 0f, (float)ammo.GetSlotMaxCount(i)));
                        Identifiable.Id selectedItem = dropDowns[slot].SelectedItemIndex != -1 ? (MenuUpdate ? ammo.GetSlotName(i) : itemIds[dropDowns[slot].SelectedItemIndex]) : ammo.GetSlotName(i);
                        if (selectedItem != Identifiable.Id.NONE && slotCounts[slot] == 0) slotCounts[slot] = 1;
                        if (selectedItem == Identifiable.Id.NONE) slotCounts[slot] = 0;
                        if (categoryPrevious == category && (selectedItem != ammo.GetSlotName(i) || slotCounts[slot] != ammo.GetSlotCount(i))) Item(ammo, selectedItem, slotCounts[slot], i);
                    }

                    //Show dropdowns in reverse order to prevent overlap clipping, and disable any that are below an activated drop down.
                    for (int i = dropDowns.Count - 1; i >= 0; --i) dropDowns.Values.ToArray()[i].Show();
                    bool isClicked = false;
                    foreach (UMFDropDown cb in dropDowns.Values)
                    {
                        if (cb.IsButtonClicked)
                        {
                            isClicked = true;
                            continue;
                        }
                        if (isClicked) cb.Disable = true;
                        else cb.Disable = false;
                    }
                    GUI.EndScrollView();
                }
            }

            GUI.DragWindow(new Rect(0, 0, guiSizeX, 30));
            MenuUpdate = false;
        }
        #endregion
    }
}
