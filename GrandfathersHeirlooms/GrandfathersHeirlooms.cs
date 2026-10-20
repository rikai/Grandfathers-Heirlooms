using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.Weapons;
using StardewValley.Menus;
using StardewValley.Tools;
using StardewValley.Objects;
using pepoHelper;

namespace GrandfathersHeirlooms
{
    class ModConfig
    {
        // minDamage/maxDamage/knockback/speed/precision/defense/type/mineBaseLevel/mineMinLevel/areaOfEffect/critChance/critMultiplier
        public const string DEFAULT_WEAPON_STATS = "3/5/.5/0/5/0/1/-1/-1/0/.20/3";

        public int triggerDay { get; set; }
        public bool traceLogging { get; set; }
        public string weaponStats { get; set; }

        public ModConfig()
        {
            triggerDay = 2;
            traceLogging = true;
            weaponStats = DEFAULT_WEAPON_STATS;
        }
    }

    /// <summary>Mod entry point</summary>
    public class GrandfathersHeirlooms : Mod
    {
        /***** Constants *****/
        const string WEAP_ID = "rikai.GrandfathersHeirlooms_PrincessKeepsake";
        const int ELF_BLADE_SPRITE_INDEX = 20;  // reuse the vanilla Elf Blade sprite
        const string MAIL_ID = "rikai.GrandfathersHeirlooms_GrandpaLetter";  // also marks the heirloom as received
        const string GIFT_BOX_KEY = "rikai.GrandfathersHeirlooms/GiftBox";  // modData key that marks Grandpa's gift box
        const string HOLD_UP_KEY = "rikai.GrandfathersHeirlooms/HoldUp";  // modData key that marks a gift to hold up
        private readonly CultureInfo CULTURE = CultureInfo.InvariantCulture;

        /***** Properteze *****/
        // Set in Entry()
        private ModConfig Config = null!;
        private SDate triggerDate = null!;

        private string letterGrandpaMessage = null!;
        private string narrationMessage1 = null!;
        private string narrationMessage2 = null!;
        private static string fullInventoryMessage = null!;  // static for the Harmony prefix

        /***** Publique Methodes *****/
        public override void Entry(IModHelper helper)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            Config = helper.ReadConfig<ModConfig>();
            if (!Config.traceLogging)
                Monitor.Log("WARNING: Trace logging disabled via config.json", LogLevel.Warn);
            else
                pepoCommon.monitor = Monitor;

            PrepTrigger();
            PrepTranslations();
            PrepWeaponStats();

            new Harmony(ModManifest.UniqueID).Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.checkForAction)),
                prefix: new HarmonyMethod(typeof(GrandfathersHeirlooms), nameof(BeforeChestCheckForAction))
            );

            Helper.Events.Content.AssetRequested += OnAssetRequested;
            Helper.Events.GameLoop.DayStarted += OnDayStarted;
            Log($"registered for AssetRequested and DayStarted events");
        }


        /***** Private Methodes *****/

        private void PrepTrigger()
        {
            int triggerDateDay = Config.triggerDay;
            if (triggerDateDay < 2) triggerDateDay = 2;
            else if (triggerDateDay > 28) triggerDateDay = 28;
            SDate tD = new(triggerDateDay, Season.Spring, 1);
            Log($"triggerDate set to {tD.Day} {tD.Season} {tD.Year}", LogLevel.Info);
            triggerDate = tD;
        }

        private void PrepTranslations()
        {
            var tran = Helper.Translation;

            string tran_get(string item, string? desc = null)
            {
                string s = tran.Get(item);
                string d = desc ?? item;
                Log($"loaded {d} from i18n, {s.Length} chars");
                return s;
            }

            letterGrandpaMessage = tran_get("letter", "Grandpa's Letter");
            narrationMessage1 = tran_get("narration1", "Narration p1");
            narrationMessage2 = tran_get("narration2", "Narration p2");
            fullInventoryMessage = tran_get("full_inventory", "Inventory Full Message");
        }

        private void PrepWeaponStats()
        {
            try
            {
                ParseWeaponStats(Config.weaponStats);
            }
            catch (Exception ex) when (ex is FormatException || ex is IndexOutOfRangeException)
            {
                Log($"weaponStats \"{Config.weaponStats}\" is invalid ({ex.Message}); using default \"{ModConfig.DEFAULT_WEAPON_STATS}\"", LogLevel.Warn);
                Config.weaponStats = ModConfig.DEFAULT_WEAPON_STATS;
            }
        }

        private WeaponData ParseWeaponStats(string? stats)  // nullable: config.json can set it to null
        {
            string[] f = stats?.Split('/') ?? throw new FormatException("value is missing");
            return new WeaponData
            {
                MinDamage = int.Parse(f[0], CULTURE),
                MaxDamage = int.Parse(f[1], CULTURE),
                Knockback = float.Parse(f[2], CULTURE),
                Speed = int.Parse(f[3], CULTURE),
                Precision = int.Parse(f[4], CULTURE),
                Defense = int.Parse(f[5], CULTURE),
                Type = int.Parse(f[6], CULTURE),
                MineBaseLevel = int.Parse(f[7], CULTURE),
                MineMinLevel = int.Parse(f[8], CULTURE),
                AreaOfEffect = int.Parse(f[9], CULTURE),
                CritChance = float.Parse(f[10], CULTURE),
                CritMultiplier = float.Parse(f[11], CULTURE),
            };
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Weapons"))
            {
                e.Edit(asset =>
                {
                    WeaponData weaponData = ParseWeaponStats(Config.weaponStats);
                    weaponData.Name = WEAP_ID;
                    weaponData.DisplayName = Helper.Translation.Get("weapon.name");
                    weaponData.Description = Helper.Translation.Get("weapon.desc");
                    weaponData.Texture = "TileSheets\\weapons";
                    weaponData.SpriteIndex = ELF_BLADE_SPRITE_INDEX;
                    weaponData.CanBeLostOnDeath = false;
                    asset.AsDictionary<string, WeaponData>().Data[WEAP_ID] = weaponData;
                    Log($"weapon {WEAP_ID} set to {weaponData.DisplayName} - {weaponData.Description} - Stats: {Config.weaponStats}", LogLevel.Trace);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Mail"))
            {
                // Lets the letter show up under Collections > Letters once it's in mailReceived
                e.Edit(asset =>
                {
                    asset.AsDictionary<string, string>().Data[MAIL_ID] =
                        $"{Helper.Translation.Get("letter")}[#]{Helper.Translation.Get("letter.title")}";
                    Log($"mail {MAIL_ID} added", LogLevel.Trace);
                });
            }
        }

        private void Log(string message, LogLevel level = LogLevel.Debug)
        {
            if (!Config.traceLogging && level == LogLevel.Trace) return;
            Monitor.Log(message, level);
        }

        private delegate void ExitFunction();

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            var curDate = SDate.Now();
            if (curDate < triggerDate)
            {
                Log($"new day {curDate}, our day is coming.", LogLevel.Trace);
                return;
            }
            else if (curDate > triggerDate)
            {
                Log($"new day {curDate}, our day has passed.", LogLevel.Trace);
                return;
            }
            Log($"new day {curDate}, is our day", LogLevel.Trace);

            // Locals, not fields: in split-screen each screen runs this separately and needs its own copy
            Farmer farmer = Game1.player;
            if (farmer.mailReceived.Contains(MAIL_ID))
            {
                Log("heirloom already received today (e.g. farmhand reconnected), skipping", LogLevel.Trace);
                return;
            }
            farmer.mailReceived.Add(MAIL_ID);
            MeleeWeapon weapon = ItemRegistry.Create<MeleeWeapon>("(W)" + WEAP_ID);

            IClickableMenu narrationDayStart1 =
                new pepoHelper.DialogOnBlack(narrationMessage1)
                {
                    exitFunction = () => { Game1.globalFadeToBlack(); }
                };
            IClickableMenu narrationDayStart2 =
                new pepoHelper.DialogOnBlack(narrationMessage2);
            IClickableMenu letterGrandpa =
                new LetterViewerMenu(letterGrandpaMessage.Replace("@", farmer.Name))
                {
                    exitFunction = () => spawnGiftBox(farmer, weapon)
                };
            Log("built the menus", LogLevel.Trace);

            // Make a chain of menus
            pepoHelper.MenuChainer menuChain = new();
            menuChain.Add(narrationDayStart1, narrationDayStart2, letterGrandpa);
            Log("chained the menus", LogLevel.Trace);
            menuChain.Start(Helper.Events.Display);
            Log("displayed DayStart narration1, continuing doing things in the background");

            // Shift farmer to the left to leave the bed, location dependent on marriage status.

            if (farmer.isMarriedOrRoommates())
            {
                farmer.moveRelTiles(h: -1, faceDir: 2);
                Log("Farmer is married. Moved farmer out of spouse bed", LogLevel.Trace);
            }
            else
            {
                farmer.moveRelTiles(h: -2, faceDir: 3);
                Log("Farmer is not married. Moved farmer out of single bed.", LogLevel.Trace);
            }
        }

        private void spawnGiftBox(Farmer farmer, MeleeWeapon weapon)
        {
            weapon.modData[HOLD_UP_KEY] = "1";
            List<Item> gifts = new() { weapon, ItemRegistry.Create("(BC)130") };
            Microsoft.Xna.Framework.Vector2 tile = farmer.isMarriedOrRoommates()
                ? farmer.relTiles(v: 1)
                : farmer.relTiles(h: -1);
            Chest box = new(gifts, tile, giftbox: true);
            box.modData[GIFT_BOX_KEY] = "1";
            Game1.currentLocation.setObject(tile, box);
            Log($"placed the gift box at ({tile.X},{tile.Y})", LogLevel.Trace);
        }

        /// <summary>Keep Grandpa's gift box closed until the player has room for everything in it, then hold up only the marked gifts.</summary>
        private static bool BeforeChestCheckForAction(Chest __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
        {
            if (justCheckingForActivity || !__instance.modData.ContainsKey(GIFT_BOX_KEY))
                return true;
            if (who.freeSpotsInInventory() < __instance.Items.CountItemStacks())
            {
                Game1.drawObjectDialogue(fullInventoryMessage);
                __result = true;
                return false;
            }

            // The game holds up each item left in the box, and each hold-up cuts off the last, so give the others quietly first
            foreach (Item item in __instance.Items.Where(p => p is not null && !p.modData.ContainsKey(HOLD_UP_KEY)).ToList())
            {
                __instance.Items.Remove(item);
                who.addItemToInventoryBool(item);
            }
            foreach (Item? item in __instance.Items)
                item?.modData.Remove(HOLD_UP_KEY);
            return true;
        }
    }
}
