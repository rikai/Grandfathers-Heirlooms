using System;
using System.Globalization;
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
        public bool directToChest { get; set; }

        public ModConfig()
        {
            triggerDay = 2;
            traceLogging = true;
            weaponStats = DEFAULT_WEAPON_STATS;
            directToChest = false;
        }
    }

    /// <summary>Mod entry point</summary>
    public class GrandfathersHeirlooms : Mod
    {
        /***** Constants *****/
        const string WEAP_ID = "rikai.GrandfathersHeirlooms_PrincessKeepsake";
        const int ELF_BLADE_SPRITE_INDEX = 20;  // reuse the vanilla Elf Blade sprite
        const string MAIL_ID = "rikai.GrandfathersHeirlooms_GrandpaLetter";  // also marks the heirloom as received
        // const int HOLDUP_MSG_DLY = 2000;
        private readonly CultureInfo CULTURE = CultureInfo.InvariantCulture;

        /***** Properteze *****/
        // Set in Entry()
        private ModConfig Config = null!;
        private SDate triggerDate = null!;

        private string letterGrandpaMessage = null!;
        private string narrationMessage1 = null!;
        private string narrationMessage2 = null!;
        private string inventoryFullMessage = null!;

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
            inventoryFullMessage = tran_get("full_inventory", "Inventory Full Message");
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
                    exitFunction = () => spawnChestWeapon(farmer, weapon)
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

        private void spawnChestWeapon(Farmer farmer, MeleeWeapon weapon)
        {
            var cloc = Game1.currentLocation;

            // Drop a chest
            Chest chest = new(true);  // must be "true" or chest won't appear
            Log("created Chest(true)", LogLevel.Trace);
            chest.playerChoiceColor.Set(Microsoft.Xna.Framework.Color.Gold);
            chest.Tint = Microsoft.Xna.Framework.Color.Gold;
            if (farmer.isMarriedOrRoommates())
            {
                cloc.setObject(farmer.relTiles(v: 1), chest);
                Log("Farmer is in love. Chest placed below.", LogLevel.Trace);
            }
            else
            {
                cloc.setObject(farmer.relTiles(h: -1), chest);
                Log("Farmer is alone. Chest placed to the left.", LogLevel.Trace);
            }
            Log("dropped chest in front of farmer", LogLevel.Trace);

            Log($"directToChest == {Config.directToChest.ToString(CULTURE)}", LogLevel.Trace);

            farmer.holdUpItemThenMessage(weapon);
            if (Config.directToChest || farmer.isInventoryFull())
            {
                // TODO: Figure out how to wait out the message above before creating a HUDMessage
                // Game1.addHUDMessage(new HUDMessage(inventoryFullMessage, 2));
                chest.addItem(weapon);
                Log("inserted weapon into chest", LogLevel.Trace);
            }
            else
            {
                farmer.addItemToInventory(weapon);
                Log("inserted weapon into inventory", LogLevel.Trace);
            }
            return;

            // TODO: Instead of a simple chest, make a chest with interaction
            // TODO: Interaction with chest = Farmer lift weapon above head + chimes

        }
    }
}
