using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace HungerRpg
{
    /// <summary>JSON config file (HungerRPG.json).</summary>
    public class ConfigFile
    {
        // ---- Hunger ----
        public bool EnableHunger = true;
        public int RespawnWellFedMinutes = 5;
        public int JoinWellFedMinutes = 5;
        public int HungerDelay = 10;

        // ---- Summon / invasion limits ----
        public bool EnableSummonLimit = false;
        public bool EnableInvasionLimit = false;
        public int DailySummonCount = 10;
        public int DailyInvasionCount = 10;
        public int SummonStackCap = 3;
        public int InvasionStackCap = 1;

        // Times are minutes since midnight. Start is inclusive, end is exclusive (midnight = 24:00 = 1440).
        public int DailySummonStartMinute = 1170;
        public int DailySummonEndMinute = 1320;
        public int DailyInvasionStartMinute = 1170;
        public int DailyInvasionEndMinute = 1320;

        // Extra weekend window. If both start and end are 0, the whole day is allowed.
        public int WeekendSummonStartMinute = 810;
        public int WeekendSummonEndMinute = 930;
        public int WeekendInvasionStartMinute = 810;
        public int WeekendInvasionEndMinute = 930;

        // ---- Boss tracking / HUD ----
        public bool LogBossKills = true;
        public bool AnnounceBossKills = true;
        public bool LogDamageRanking = true;
        public int DamageRankingSize = 5;
        public bool ShowHitIndicator = true;
        public int HitIndicatorMinLife = 1500;
        public int HudMode = 2; // 0 = off, 1 = type 1, 2 = type 2, 3 = type 3 (for mobile)

        // ---- Starting values ----
        public int PreHardmodeStartLevel = 0;
        public long PreHardmodeStartExp = 0;
        public bool HardmodeExpCatchUp = false;
        public int HardmodeStartLevel = 15;
        public long HardmodeStartExp = 3150;

        // ---- Level caps (-1 = unlimited; no EXP is gained once the cap is reached) ----
        public int MaxLevel = 100;
        public int PreHardmodeMaxLevel = 40;
        public int GolemMaxLevel = 70;

        // ---- Daily EXP caps (-1 = unlimited) ----
        public long PreHardmodeDailyMaxExp = 1000;
        public long GolemDailyMaxExp = 2000;
        public long DailyMaxExp = 4000;
        public int UnlimitedExpLevel = 20; // players at or above this level have no daily EXP cap

        // ---- Server-wide level ----
        public bool EnableServerLevel = true;
        public int ServerMaxLevel = 30;
        public int ServerPreHardmodeMaxLevel = 18; // -1 = unlimited
        public int ServerGolemMaxLevel = 25;

        public long ServerPreHardmodeDailyMaxExp = 2000; // 700 = 5 days, 900 = 3.5 days
        public long ServerGolemDailyMaxExp = 3000;      // 1100 = 5 days, 1500 = 3.5 days
        public long ServerDailyMaxExp = 4000;           // -1 = unlimited; 1300 = 5 days, 1700 = 3.5 days

        // ---- Teams ----
        public bool EnableTeams = true;
        public int TeamDailyMaxExp = 300;
        public int TeamKillExp = 1;

        // ---- Misc EXP sources ----
        public bool EnableAnglerQuestExp = true;
        public int OnlineExpIntervalSeconds = 390;     // enabled when above 0
        public int OnlineTeamExpIntervalSeconds = 390; // enabled when above 0

        // ---- PvP ----
        public int PvpWinExp = 7;
        public int PvpLossExp = -5;
        public int PvpProtectionLevel = 9;
        // Max level difference at which both players still earn PvP EXP.
        // 0 = levels must be equal, 1 = within 1 level, -1 = PvP EXP disabled.
        public int PvpProtectionLevelGap = 0;

        // ---- Protection / equipment rules ----
        public int HungerProtectionLevel = 2; // players at or below this level are not affected by hunger, -1 = unlimited
        public bool LogEquipViolations = true;
        public bool LogOverLevelUse = true;
        public bool NotifyOverLevelUse = true;
        public int RarityMultiplier = 3; // item rarity is multiplied by this value to get the required level
        public bool IgnorePrefixes = true;

        public Dictionary<int, int> RarityOverrides = new Dictionary<int, int>();
        public bool NoHungerWhileSleeping = true;
        public int BossMinExpPercent = 3;
        public int BloodMoonPvpMultiplier = 3;

        // Violation counter is cleared every minute. 3 = kick on the 3rd violation,
        // 1 = kick on the 1st, 0 = damage limits disabled.
        public int DamageViolationLimit = 3;
        public List<LevelDamageLimit> LevelDamageLimits = new List<LevelDamageLimit>();

        // ---- Quests ----
        public bool DisableTaskSkip = false;
        public Dictionary<int, int> ExpOverrides = new Dictionary<int, int>();
        public Dictionary<int, List<LevelTask>> LevelTasks = new Dictionary<int, List<LevelTask>>();

        public static ConfigFile Read(string path)
        {
            if (!File.Exists(path))
                return CreateDefault();

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Read(fs);
        }

        public static ConfigFile Read(Stream stream)
        {
            using (var sr = new StreamReader(stream))
                return JsonConvert.DeserializeObject<ConfigFile>(sr.ReadToEnd());
        }

        public void Write(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
                Write(fs);
        }

        public void Write(Stream stream)
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            using (var sw = new StreamWriter(stream))
                sw.Write(json);
        }

        private static ConfigFile CreateDefault()
        {
            var config = new ConfigFile();

            config.RarityOverrides = new Dictionary<int, int>
            {
                { 5000, 5 }, { 908, 5 }, { 1309, 2 }, { 1862, 5 }, { 3017, 4 }, { 4989, 8 },
                { 1164, 5 }, { 3999, 3 }, { 4004, 3 }, { 4003, 4 }, { 4881, 5 }
            };

            config.ExpOverrides = new Dictionary<int, int>();
            int[] noExpNpcs =
            {
                5, 25, 30, 33, 112, 115, 116, 117, 118, 119, 139, 210, 211, 261, 264, 265, 267,
                378, 384, 401, 406, 440, 454, 455, 456, 457, 458, 459, 472, 488, 516, 519, 521,
                522, 523, 535, 566, 567, 594, 658, 659, 660
            };
            foreach (int id in noExpNpcs)
                config.ExpOverrides[id] = 0;

            config.LevelDamageLimits = new List<LevelDamageLimit>
            {
                new LevelDamageLimit(0, 8, 95, 1000),
                new LevelDamageLimit(9, 11, 135, 1500),
                new LevelDamageLimit(12, 14, 155, 3000),
                new LevelDamageLimit(15, 20, 375, 30000),
                new LevelDamageLimit(21, 26, 1300, 90000),
                new LevelDamageLimit(27, 30, 1900, 150000)
            };

            config.LevelTasks = new Dictionary<int, List<LevelTask>>();
            DefaultTasks.Populate(config.LevelTasks);

            return config;
        }
    }

    /// <summary>
    /// Default quest chain written to a fresh config file.
    /// Quest types: 0 Own, 1 Kill, 2 Use, 3 Wear, 4 Collect, 5 Talk, 6 Die (see TaskType).
    /// "{name}" in story text is replaced by the player's name.
    /// </summary>
    internal static class DefaultTasks
    {
        public static void Populate(Dictionary<int, List<LevelTask>> tasks)
        {
            List<LevelTask> t;

            // ---- Level 0 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Wear the Giant Bow", 3, 1906, 1, 1));
            tasks.Add(0, t);

            // ---- Level 1 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 1, "{name}: Hello Guide, I'm {name}.\nGuide: Welcome to this world!"));
            t.Add(new LevelTask("Place a Work Bench", 2, 36, 1, 1));
            t.Add(new LevelTask("Wear Wood Breastplate", 3, 728, 1, 1));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "Guide: Fine armor! Looks like you're ready to conquer this world!"));
            tasks.Add(1, t);

            // ---- Level 2 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Swing the Candy Cane Pickaxe 3 times", 2, 1917, 3, 1));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 1, "Guide: Mighty swings! Now we need torches to get through the night. Kill those Blue Slimes!"));
            t.Add(new LevelTask("Kill a Blue Slime", 1, 1, 1, 2));
            t.Add(new LevelTask("Craft/own 1 Torch", 0, 8, 1, 2));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 3, "Guide: Looks like you've mastered the basics of survival!"));
            tasks.Add(2, t);

            // ---- Level 3 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Eat an Apple", 2, 4009, 1, 1, "Tip: When you're hungry, eat food to fix it!"));
            t.Add(new LevelTask("Kill a Blue Slime", 1, 1, 1, 1));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 1, "Guide: Nicely done! Let's see what else we can get!"));
            t.Add(new LevelTask("Wear Shackles", 3, 216, 1, 1));
            t.Add(new LevelTask("Collect 1 Lens", 4, 38, 1, 1));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 1, "Guide: Perfect! Let's go find some fallen stars tonight!"));
            t.Add(new LevelTask("Own 5 Fallen Stars", 0, 75, 5, 1, "Tip: Fallen stars can be crafted into Mana Crystals!"));
            t.Add(new LevelTask("Use a Mana Crystal", 2, 109, 1, 2, "Tip: Mana Crystals permanently raise your maximum mana!"));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 3, "Guide: You look full of magic now!"));
            tasks.Add(3, t);

            // ---- Level 4 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Own 15 Silver Coins", 0, 72, 15, 1));
            t.Add(new LevelTask("Talk to the Merchant", 5, 17, 1, 2, "{name}: Hello Merchant, I'm {name}.\nMerchant: Come see me if you need to buy or sell anything!"));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "{name}: A Merchant has moved into town.\nGuide: We're getting stronger! Let's see what we can get!"));
            t.Add(new LevelTask("Own 15 Rope", 0, 965, 15, 2));
            t.Add(new LevelTask("Own 3 Mushrooms", 0, 5, 3, 2));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "Guide: Looks like you're ready to explore underground. Go find a chest!"));
            t.Add(new LevelTask("Collect 1 Chest", 4, 48, 1, 2));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "Guide: You're truly something else!"));
            t.Add(new LevelTask("Own 1 Bomb", 0, 166, 1, 2));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "Guide: You even found a Bomb... unbelievable.\nGuide: Though the server's mighty power restricts it..."));
            t.Add(new LevelTask("Own 1 Gel", 0, 23, 1, 2));
            t.Add(new LevelTask("Own 1 Sticky Bomb", 0, 235, 1, 2));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 3, "{name}: Like this?\nGuide: Wow, you found a way around it! We're unstoppable now!"));
            tasks.Add(4, t);

            // ---- Level 5 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Talk to the Demolitionist", 5, 38, 1, 2, "{name}: Hello, I'm {name}.\nDemolitionist: Aaah! I'm going to blow everything up!"));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "{name}: A Demolitionist has moved into town.\nGuide: Let's ask the Merchant if he knows him."));
            t.Add(new LevelTask("Talk to the Merchant", 5, 17, 1, 2, "{name}: A Demolitionist has moved into town.\nMerchant: Despite the name, he won't go around wrecking things!"));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "Guide: Sounds like a friend, not an enemy!"));
            t.Add(new LevelTask("Talk to the Demolitionist", 5, 38, 1, 2, "{name}: Welcome, Demolitionist. Griefing the world gets you permanently banned.\nDemolitionist: Understood, thanks. I won't go around destroying things!"));
            t.Add(new LevelTask("Own 3 Grenades", 0, 168, 3, 2));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "{name}: What do we do next?\nGuide: I need a glass bottle, and the first step is building a furnace."));
            t.Add(new LevelTask("Own 30 Stone Blocks", 0, 3, 30, 2));
            t.Add(new LevelTask("Own 1 Furnace", 0, 33, 1, 2));
            t.Add(new LevelTask("Place a Furnace", 2, 33, 1, 2));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "Guide: Now we head to the vast desert."));
            t.Add(new LevelTask("Kill a Vulture", 1, 61, 1, 2));
            t.Add(new LevelTask("Collect 1 Cactus", 4, 276, 1, 2));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "Guide: Er, not the cactus. I need some sand."));
            t.Add(new LevelTask("Own 5 Sand Blocks", 0, 169, 5, 2));
            t.Add(new LevelTask("Craft/own 1 Glass", 0, 170, 1, 2));
            t.Add(new LevelTask("Craft/collect 1 Bottle", 4, 31, 1, 2));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "Guide: Wow, what a perfect bottle - a work of art! Thank you!"));
            tasks.Add(5, t);

            // ---- Level 6 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Own a Life Crystal", 0, 29, 1, 1));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 1, "{name}: I found a Life Crystal. What is it?\nGuide: Use it and you'll become stronger!"));
            t.Add(new LevelTask("Use a Life Crystal", 2, 29, 1, 1));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 1, "Guide: Feeling stronger? Try chopping some trees and see if it's easier!"));
            t.Add(new LevelTask("Own 100 Wood", 0, 9, 100, 1));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 1, "Guide: Remember to plant trees while you chop them~"));
            t.Add(new LevelTask("Plant 3 Acorns", 2, 27, 3, 1));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 1, "Guide: Looks like someone new has arrived. Go see what they want."));
            t.Add(new LevelTask("Talk to the Nurse", 5, 18, 1, 1, "{name}: Hello, I'm {name}.\nNurse: Hello, I'm the Nurse. I can heal your wounds!"));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 1, "{name}: A Nurse has moved into town.\nGuide: Great, now we don't have to fear getting hurt! Let's see if she needs any help."));
            t.Add(new LevelTask("Talk to the Nurse", 5, 18, 1, 1, "Nurse: I need a bed to take in patients."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 1, "{name}: The Nurse needs a bed. How do I make one?\nGuide: First we need a sawmill."));
            t.Add(new LevelTask("Own 1 Chain", 0, 85, 1, 2));
            t.Add(new LevelTask("Own 1 Sawmill", 0, 363, 1, 2));
            t.Add(new LevelTask("Place a Sawmill", 2, 363, 1, 2));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "Guide: Next we need to build a loom."));
            t.Add(new LevelTask("Own 1 Loom", 0, 332, 1, 2));
            t.Add(new LevelTask("Place a Loom", 2, 332, 1, 2));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "Guide: Next we need cobwebs to make silk."));
            t.Add(new LevelTask("Own 50 Cobwebs", 0, 150, 50, 2));
            t.Add(new LevelTask("Own 5 Silk", 0, 225, 5, 2));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "Guide: Now we can make a bed at the sawmill."));
            t.Add(new LevelTask("Own 1 Bed", 0, 224, 1, 2));
            t.Add(new LevelTask("Place a Bed", 2, 224, 1, 2));
            t.Add(new LevelTask("Talk to the Nurse", 5, 18, 1, 5, "Nurse: Now we can take in patients. Thank you~"));
            tasks.Add(6, t);

            // ---- Level 7 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Collect 1 Book", 4, 149, 1, 2));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "{name}: I found a mysterious book?\nGuide: Let's ask someone what it says."));
            t.Add(new LevelTask("Talk to the Demolitionist", 5, 38, 1, 2, "{name}: Do you know what this says?\nDemolitionist: I think the Merchant might."));
            t.Add(new LevelTask("Talk to the Merchant", 5, 17, 1, 2, "{name}: Do you know what this says?\nMerchant: Of course, but I have a condition. I want an Obsidian Rose."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "{name}: The Merchant knows, but he wants an Obsidian Rose.\nGuide: Obsidian Roses are very rare. They're in the Underworld, and you're not ready yet."));
            t.Add(new LevelTask("Own 10 Ice Blocks", 0, 664, 10, 2));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "{name}: How about these ice blocks?\nGuide: Er, ice can't withstand the heat of the Underworld."));
            t.Add(new LevelTask("Collect 1 Ash Block", 4, 172, 1, 2));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "{name}: Look at these Ash Blocks.\nGuide: What, you actually made it to the Underworld? Unbelievable! You really are determined. Now I'll tell you the trick."));
            t.Add(new LevelTask("Own 3 Empty Buckets", 0, 205, 3, 2));
            t.Add(new LevelTask("Own 3 Water Buckets", 0, 206, 3, 2));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "{name}: Why do I need water buckets?\nGuide: Pour it onto lava to form obsidian, then make a helmet out of it."));
            t.Add(new LevelTask("Collect 1 Obsidian", 4, 173, 1, 2));
            t.Add(new LevelTask("Craft/own 1 Obsidian Skull", 0, 193, 1, 2));
            t.Add(new LevelTask("Wear the Obsidian Skull", 3, 193, 1, 3));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "Guide: The Obsidian Skull makes you immune to fire. Now go find the Obsidian Rose - Fire Imps carry it."));
            t.Add(new LevelTask("Kill 3 Fire Imps", 1, 24, 3, 2));
            t.Add(new LevelTask("Own 1 Obsidian Rose", 0, 1323, 1, 10));
            t.Add(new LevelTask("Talk to the Merchant", 5, 17, 1, 2, "{name}: Here, the Obsidian Rose.\nMerchant: What? You actually got it! Thank you so much. Could you give it to the Nurse for me?"));
            t.Add(new LevelTask("Talk to the Nurse", 5, 18, 1, 2, "{name}: Someone asked me to give you this.\nNurse: What a pretty flower! Who sent it?\n{name}: The Merchant.\nNurse: Throw it away.\n{name}: ???"));
            t.Add(new LevelTask("Talk to the Merchant", 5, 17, 1, 2, "{name}: She told me to throw it away. Looks like you'll have to deliver it yourself.\nMerchant: I'll win her over, I swear."));
            t.Add(new LevelTask("Talk to the Nurse", 5, 18, 1, 2, "Merchant: This flower is for you.\nThe Nurse flings the flower away and roars: Get lost!\nMerchant: Waaah..."));
            t.Add(new LevelTask("Talk to the Merchant", 5, 17, 1, 2, "Merchant: Waaah..."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "{name}: Looks like we need to comfort him."));
            t.Add(new LevelTask("Talk to the Merchant", 5, 17, 1, 2, "Guide: Don't be sad.\nMerchant: Waaah..."));
            tasks.Add(7, t);

            // ---- Level 8 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "Guide: I heard you found an Angler?"));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 2, "{name}: Hello, I'm {name}.\nAngler: Hi, I'm the Angler. Thanks for rescuing me! I was only taking a nap, though."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "Guide: He seems fine. You can learn fishing from him."));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 2, "Angler: How do you fish? First you need bait. Ask the Merchant if he has a Bug Net."));
            t.Add(new LevelTask("Talk to the Merchant", 5, 17, 1, 2, "Merchant: A Bug Net? I happen to have one, as long as you have enough money."));
            t.Add(new LevelTask("Own a Bug Net", 0, 1991, 1, 3));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 3, "Angler: Now you need bait. Hit the grass and a grasshopper may jump out - catch it with the Bug Net."));
            t.Add(new LevelTask("Use the Bug Net", 2, 1991, 1, 3));
            t.Add(new LevelTask("Own 3 Grasshoppers", 0, 2740, 3, 3));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 3, "Angler: You can also smash stone piles on the grass to find worms. Catch a few."));
            t.Add(new LevelTask("Use the Bug Net", 2, 1991, 1, 3));
            t.Add(new LevelTask("Own 3 Worms", 0, 2002, 3, 3));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 3, "Angler: Now you need a fishing rod. Gather some metal bars and make a Reinforced Fishing Pole."));
            t.Add(new LevelTask("Own a Reinforced Fishing Pole", 0, 2291, 1, 3));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 2, "Angler: Now you can head to a big pond and fish. Try catching some Bass."));
            t.Add(new LevelTask("Use the Reinforced Fishing Pole", 2, 2291, 1, 3));
            t.Add(new LevelTask("Collect 1 Bass", 4, 2290, 1, 3));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 3, "Angler: Not bad, you've got the hang of fishing.\nAngler: Besides fish, you can also fish up crates. Try collecting some."));
            t.Add(new LevelTask("Use the Reinforced Fishing Pole", 2, 2291, 1, 3));
            t.Add(new LevelTask("Own 3 Wooden Crates", 0, 2334, 3, 3));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 2, "Angler: Great! I hand out a specific quest every day, and you get a reward for finishing it.\nAngler: Complete 5 of my quests and you'll get the Fuzzy Carrot mount."));
            t.Add(new LevelTask("Own a Fuzzy Carrot", 0, 2428, 1, 15));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 3));
            tasks.Add(8, t);

            // ---- Level 9 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 1, "Guide: Running low on food? Maybe the Angler can help us."));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 1, "Angler: Want to learn how to make tasty fish dishes?\nAngler: Let's start with the simplest - sashimi. Bring some fish to the work bench."));
            t.Add(new LevelTask("Own 5 Trout", 0, 2297, 5, 3));
            t.Add(new LevelTask("Own 5 Sashimi", 0, 2427, 5, 3));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 3, "Angler: I didn't expect you to make it this well. Go on, try it!"));
            t.Add(new LevelTask("Use Sashimi", 2, 2427, 1, 3));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 3, "Angler: Delicious, isn't it? Next we'll start cooking.\nAngler: We need a pot. I wonder where we can get one."));
            t.Add(new LevelTask("Talk to the Merchant", 5, 17, 1, 3, "Merchant: A pot? I don't have one. Try asking the Guide."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 3, "Guide: A pot? You'll need some metal bars and wood to make one."));
            t.Add(new LevelTask("Own a Cooking Pot", 0, 345, 1, 3));
            t.Add(new LevelTask("Place a Cooking Pot", 2, 345, 1, 3));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 3, "Guide: Excellent, looks like we can stay clear of famine!"));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 3, "Angler: Time to cook. Prepare some ingredients first."));
            t.Add(new LevelTask("Own 7 Bass", 0, 2290, 7, 3));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 3, "Angler: Now put them in the pot."));
            t.Add(new LevelTask("Own 7 Grilled Fish", 0, 2425, 7, 3));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 3, "Angler: I didn't expect you to make it this well. Go on, try it!"));
            t.Add(new LevelTask("Use Grilled Fish", 2, 2425, 1, 3));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 3, "Angler: Delicious, isn't it? What else do you want to eat?\nAngler: Shrimp? Sure, you just need to prepare..."));
            t.Add(new LevelTask("Own 3 Shrimp", 0, 2316, 3, 3));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 3, "Angler: Now put them in the pot."));
            t.Add(new LevelTask("Own 3 Cooked Shrimp", 0, 2426, 3, 3));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 3, "Angler: I didn't expect you to make it this well. Go on, try it!"));
            t.Add(new LevelTask("Use Cooked Shrimp", 2, 2426, 1, 3));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 3, "Angler: Delicious! A glass of lemonade would make it even better.\nAngler: Where do lemons come from? Trees, of course."));
            t.Add(new LevelTask("Own 3 Bottles", 0, 31, 3, 3));
            t.Add(new LevelTask("Own 3 Lemons", 0, 4291, 3, 3));
            t.Add(new LevelTask("Own 3 Lemonades", 0, 4616, 3, 3));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 3, "Angler: What a treat~"));
            t.Add(new LevelTask("Use Lemonade", 2, 4616, 1, 3));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 3, "Guide: A happy life begins~"));
            tasks.Add(9, t);

            // ---- Level 10 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Talk to the Arms Dealer", 5, 19, 1, 3, "{name}: Hello, I'm {name}.\nArms Dealer: Shh, I've got some big guns here."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 3, "{name}: A mysterious stranger has moved into town.\nGuide: Go ask if anyone knows him."));
            t.Add(new LevelTask("Talk to the Merchant", 5, 17, 1, 3, "Merchant: A fellow tradesman."));
            t.Add(new LevelTask("Talk to the Demolitionist", 5, 38, 1, 3, "Demolitionist: A fellow tradesman."));
            t.Add(new LevelTask("Talk to the Nurse", 5, 18, 1, 3, "Nurse: An arms dealer?"));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 3, "{name}: He's an Arms Dealer.\nGuide: Looks like times are about to change."));
            t.Add(new LevelTask("Talk to the Stylist", 5, 353, 1, 3, "{name}: Hello, I'm {name}.\nStylist: Thank you for saving me."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 3, "{name}: A Stylist has moved into town.\nGuide: Our population is growing."));
            t.Add(new LevelTask("Talk to the Tavernkeep", 5, 550, 1, 3, "{name}: Hello, I'm {name}.\nTavernkeep: Thank you for saving me."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 3, "{name}: A Tavernkeep has moved into town.\nGuide: Our population is growing."));
            t.Add(new LevelTask("Talk to the Golfer", 5, 588, 1, 3, "{name}: Hello, I'm {name}.\nGolfer: Thank you for saving me."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 3, "{name}: A Golfer has moved into town.\nGuide: Now we have a new way to have fun."));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 3, "Angler: Go play a round with the Golfer, I can't wait!"));
            t.Add(new LevelTask("Talk to the Golfer", 5, 588, 1, 3, "{name}: How do I play golf?\nGolfer: This is my specialty. Let's get started.\nGolfer: First you need a tee and a cup. I happen to sell them."));
            t.Add(new LevelTask("Own 1 Golf Tee", 0, 4089, 1, 3));
            t.Add(new LevelTask("Own 1 Golf Cup", 0, 4040, 1, 3));
            t.Add(new LevelTask("Talk to the Golfer", 5, 588, 1, 3, "\nGolfer: Next you need a red flag. I can sell you one of those too."));
            t.Add(new LevelTask("Own 1 Golf Flag (Red)", 0, 4084, 1, 3));
            t.Add(new LevelTask("Talk to the Golfer", 5, 588, 1, 3, "\nGolfer: Now you can place them on the ground. Stick the red flag in the cup to mark the target."));
            t.Add(new LevelTask("Use the Golf Tee", 2, 4089, 1, 3));
            t.Add(new LevelTask("Use the Golf Cup", 2, 4040, 1, 3));
            t.Add(new LevelTask("Use the Golf Flag (Red)", 2, 4084, 1, 3));
            t.Add(new LevelTask("Talk to the Golfer", 5, 588, 1, 3, "\nGolfer: Now you need a club."));
            t.Add(new LevelTask("Own 1 Iron Golf Club", 0, 4587, 1, 3));
            t.Add(new LevelTask("Talk to the Golfer", 5, 588, 1, 3, "\nGolfer: The iron is good for short to medium shots. Give it a try."));
            t.Add(new LevelTask("Use the Iron Golf Club", 2, 4587, 1, 3));
            t.Add(new LevelTask("Talk to the Golfer", 5, 588, 1, 3, "\nGolfer: Swing at the ball on the tee to hit it."));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 3, "Angler: Time for a friendly match."));
            t.Add(new LevelTask("Talk to the Golfer", 5, 588, 1, 3, "\nGolfer: Swing at the ball on the tee to hit it."));
            t.Add(new LevelTask("Own a Bronze Golf Trophy", 0, 4599, 1, 5));
            t.Add(new LevelTask("Talk to the Golfer", 5, 588, 1, 15, "\nGolfer: A great golfer is born."));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 5, "Angler: Beautiful! I knew you could do it."));
            t.Add(new LevelTask("Talk to the Golfer", 5, 588, 1, 10, "\nGolfer: Come play anytime."));
            tasks.Add(10, t);

            // ---- Level 11 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Talk to the Dye Trader", 5, 207, 1, 3, "{name}: Hello, I'm {name}.\nDye Trader: Is there anything you'd like dyed?"));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 3, "{name}: A Dye Trader has moved into town.\nGuide: There's a plant that's been puzzling me - I can ask him about it."));
            t.Add(new LevelTask("Own 1 Yellow Marigold", 0, 1110, 1, 3));
            t.Add(new LevelTask("Talk to the Dye Trader", 5, 207, 1, 3, "Dye Trader: Well, it can be made into dye, but you need a Dye Vat. Luckily, I have one here (evil smile)."));
            t.Add(new LevelTask("Own 1 Dye Vat", 0, 1120, 1, 3));
            t.Add(new LevelTask("Place a Dye Vat", 2, 1120, 1, 3));
            t.Add(new LevelTask("Own 1 Yellow Dye", 0, 1009, 1, 3));
            t.Add(new LevelTask("Talk to the Dye Trader", 5, 207, 1, 3, "Dye Trader: You have quite a talent."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "Guide: Such beautiful dye! Quick, ask if he has a green one."));
            t.Add(new LevelTask("Talk to the Dye Trader", 5, 207, 1, 3, "Dye Trader: Green dye? Easy, you just need a Green Mushroom. It grows in caves and the underground layer."));
            t.Add(new LevelTask("Own 1 Green Mushroom", 0, 1108, 1, 3));
            t.Add(new LevelTask("Own 1 Green Dye", 0, 1011, 1, 3));
            t.Add(new LevelTask("Talk to the Dye Trader", 5, 207, 1, 3, "Dye Trader: You have quite a talent."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "Guide: Wonderful, I love it!"));
            t.Add(new LevelTask("Talk to the Painter", 5, 227, 1, 3, "{name}: Hello, I'm {name}.\nPainter: I'm a house painter."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 3, "{name}: A Painter has moved into town.\nGuide: Oh? Let's see what he can do."));
            t.Add(new LevelTask("Talk to the Painter", 5, 227, 1, 3, "Painter: Actually, I'm also an artist."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 3, "Guide: An artist? Take me to see."));
            t.Add(new LevelTask("Talk to the Painter", 5, 227, 1, 3, "Guide: Wow, so lifelike and beautiful!"));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 3, "Guide: The painting called Dawn is really great, if you could buy it."));
            t.Add(new LevelTask("Own 1 Dawn painting", 0, 1490, 1, 15));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 15, "Guide: Wow, is it for me? Thank you~"));
            tasks.Add(11, t);

            // ---- Level 12 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Talk to the Dryad", 5, 20, 1, 1, "{name}: Hello, I'm {name}.\nDryad: The power of nature be with you."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 1, "{name}: A Dryad has moved into town.\nGuide: The legendary Dryad? Take me to see her."));
            t.Add(new LevelTask("Talk to the Dryad", 5, 20, 1, 1, "Dryad: There is much to be done in this world."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 1, "Guide: We have another powerful ally."));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 1, "{name}: Did you call me?\nAngler: Yes! I heard the Dryad arrived? I know she has pumpkin seeds.\nAngler: After all that seafood I want a change of taste. Can you help me?"));
            t.Add(new LevelTask("Talk to the Dryad", 5, 20, 1, 1, "Dryad: Pumpkin seeds? I have plenty."));
            t.Add(new LevelTask("Own 10 Pumpkin Seeds", 0, 1828, 10, 3));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 5, "Angler: Plant the pumpkin seeds in the ground, then wait patiently."));
            t.Add(new LevelTask("Plant 10 Pumpkin Seeds", 2, 1828, 10, 3));
            t.Add(new LevelTask("Own 30 Pumpkins", 0, 1725, 30, 3));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 3, "Angler: Finally, a harvest! I can't wait. Now put the pumpkins in the oven..."));
            t.Add(new LevelTask("Own 3 Pumpkin Pies", 0, 1787, 3, 3));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 5, "Angler: Wow, this is really delicious!"));
            t.Add(new LevelTask("Use Pumpkin Pie", 2, 1787, 1, 5));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 5, "Angler: You know, pumpkin pie is the best with honey.\nAngler: You can fill a bottle next to some honey, or..."));
            t.Add(new LevelTask("Own 3 Bottled Honey", 0, 1134, 3, 5));
            t.Add(new LevelTask("Use Bottled Honey", 2, 1134, 1, 5));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 5, "Angler: A perfect match, isn't it?"));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "Guide: I sense a bloody power. Could the Blood Moon be coming? Defeat them!"));
            t.Add(new LevelTask("Kill 10 Dripplers", 1, 490, 10, 5));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "Guide: Impressive! If you look closely, you'll find a Money Trough."));
            t.Add(new LevelTask("Own a Money Trough", 0, 3213, 1, 5));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "Guide: It's a portable storage. Use it to keep your items safe."));
            t.Add(new LevelTask("Use the Money Trough", 2, 3213, 1, 5));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "Guide: You've grown even stronger."));
            t.Add(new LevelTask("Talk to the Party Girl", 5, 208, 1, 5, "{name}: Hello, I'm {name}.\nParty Girl: I suddenly feel like throwing a party!"));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "Guide: A party? Of course I like parties, let's get ready~"));
            t.Add(new LevelTask("Talk to the Party Girl", 5, 208, 1, 5, "Party Girl: Confetti guns? Of course I have some."));
            t.Add(new LevelTask("Own 3 Confetti Guns", 0, 1000, 3, 5));
            t.Add(new LevelTask("Use the Confetti Gun 3 times", 2, 1000, 3, 5));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 10, "Guide: What a joyful party!"));
            tasks.Add(12, t);

            // ---- Level 13 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Talk to the Zoologist", 5, 633, 1, 2, "{name}: Hello, I'm {name}.\nZoologist: Hello~"));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "{name}: A Zoologist has moved into town.\nGuide: A Zoologist?"));
            t.Add(new LevelTask("Talk to the Dryad", 5, 20, 1, 2, "Dryad: A companion who loves nature."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "Guide: We're even stronger now!"));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 2, "{name}: Hello, I'm {name}.\nWitch Doctor: The rhythm of nature!"));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "{name}: Another emissary of nature has moved into town.\nGuide: What?"));
            t.Add(new LevelTask("Talk to the Dryad", 5, 20, 1, 2, "Dryad: A mage of nature."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "Guide: Let's see what he can do."));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 2, "Witch Doctor: Voodoo, magic, and potions.\n{name}: Can you teleport?\nWitch Doctor: I have a potion that lets you teleport instantly.\n{name}: How?\nWitch Doctor: You need an alchemy table."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 2, "Guide: An alchemy table? Just place a bottle on a table.\n{name}: That casual?\nGuide: What else did you expect?"));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 2, "{name}: And then?\nWitch Doctor: You need to prepare some ingredients."));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 2, "Angler: Specular Fish? I've seen them in underground caves in the forest or snow biomes."));
            t.Add(new LevelTask("Own 1 Specular Fish", 0, 2309, 1, 2));
            t.Add(new LevelTask("Own 1 Blinkroot", 0, 315, 1, 2));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 2, "{name}: And then?\nWitch Doctor: Dissolve them in a bottle of water and you're done.\nWitch Doctor: Bottled water? Just fill a bottle at the water's edge."));
            t.Add(new LevelTask("Own 1 Bottled Water", 0, 126, 1, 2));
            t.Add(new LevelTask("Own 1 Wormhole Potion", 0, 2997, 1, 5));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 5, "Witch Doctor: What? Already done? Impressive!\nWitch Doctor: How to use it? Click a teammate's icon on the map to teleport to them."));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 5, "Angler: Since you've learned potion making, could you make one for me? Consider it payment for telling you about the Specular Fish.\n{name}: Er, fine..."));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 5, "{name}: Can you teach me to make a Fishing Potion?\nWitch Doctor: It's easy, but the key ingredient, Crispy Honey Blocks, is hard to get. Ask the Guide."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "Guide: Crispy Honey Blocks? First you need to make water meet honey."));
            t.Add(new LevelTask("Own 3 Water Buckets", 0, 206, 3, 5));
            t.Add(new LevelTask("Own 3 Honey Buckets", 0, 1128, 3, 5));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "Guide: Then pour the water into the honey."));
            t.Add(new LevelTask("Use Water Buckets", 2, 206, 3, 5));
            t.Add(new LevelTask("Use Honey Buckets", 2, 1128, 3, 5));
            t.Add(new LevelTask("Own 3 Honey Blocks", 0, 1125, 3, 5));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "Guide: Then bake the Honey Blocks in a furnace."));
            t.Add(new LevelTask("Own 3 Crispy Honey Blocks", 0, 1127, 3, 5));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "Guide: Perfect."));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 5, "{name}: I got the Crispy Honey Blocks.\nWitch Doctor: Impressive. Then let's continue."));
            t.Add(new LevelTask("Own 3 Waterleaf", 0, 317, 3, 5));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 5, "Witch Doctor: Let's repeat the steps from the Wormhole Potion."));
            t.Add(new LevelTask("Own 3 Bottled Water", 0, 126, 3, 5));
            t.Add(new LevelTask("Own 1 Fishing Potion", 0, 2354, 1, 5));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 5, "Witch Doctor: Absolutely perfect!"));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 15, "Angler: Haha, thank you! I really love this potion."));
            tasks.Add(13, t);

            // ---- Level 14 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Talk to the Tavernkeep", 5, 550, 1, 5, "Tavernkeep: Care for a drink? I've got fine Ale here."));
            t.Add(new LevelTask("Use 3 Ales", 2, 353, 3, 5));
            t.Add(new LevelTask("Talk to the Tavernkeep", 5, 550, 1, 5, "{name}: Fine Ale. Cheers!\nTavernkeep: Back home, this is just a soft drink... *hic*..."));
            t.Add(new LevelTask("Use 3 Ales", 2, 353, 3, 5));
            t.Add(new LevelTask("Talk to the Tavernkeep", 5, 550, 1, 5, "{name}: Another round. Cheers!\nTavernkeep: They say you're strong, but I know what real strength is. Let's see if you live up to it... *hic*..."));
            t.Add(new LevelTask("Use 3 Ales", 2, 353, 3, 5));
            t.Add(new LevelTask("Talk to the Tavernkeep", 5, 550, 1, 5, "{name}: Another round. Cheers!\nTavernkeep: Once... the Eternia Crystal... Etherian army... the old general..."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "Guide: The Eternia Crystal? The Etherian army?"));
            t.Add(new LevelTask("Talk to the Tavernkeep", 5, 550, 1, 5, "Tavernkeep: The Eternia Crystal? The Etherian army? You've had too much to drink."));
            t.Add(new LevelTask("Talk to the Demolitionist", 5, 38, 1, 5, "Demolitionist: The Eternia Crystal? The Etherian army? That's the old general's..."));
            t.Add(new LevelTask("Talk to the Tavernkeep", 5, 550, 1, 5, "Tavernkeep: So you already know. It's a powerful army. If you want to..."));
            t.Add(new LevelTask("Own 5 Defender Medals", 0, 3817, 5, 5));
            t.Add(new LevelTask("Kill 10 Etherian Goblins", 1, 552, 10, 5));
            t.Add(new LevelTask("Talk to the Tavernkeep", 5, 550, 1, 15, "Tavernkeep: Looks like you're a mighty hero too... Come, cheers!"));
            t.Add(new LevelTask("Own 1 Tattered Cloth", 0, 362, 1, 5));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "Guide: You found a Tattered Cloth? Looks like the goblins are about to launch a major attack."));
            t.Add(new LevelTask("Kill 10 Goblin Warriors", 1, 28, 10, 15));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 15, "Guide: Great hero, you've successfully held off the goblins."));
            tasks.Add(14, t);

            // ---- Level 15 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Talk to the Goblin Tinkerer", 5, 107, 1, 5, "{name}: Hello, I'm {name}.\nGoblin Tinkerer: Thank you for saving me."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "Guide: What? You saved a goblin? Have you forgotten their invasion?"));
            t.Add(new LevelTask("Talk to the Dye Trader", 5, 207, 1, 5, "Dye Trader: The Goblin Tinkerer? He's an old friend. Thank you for saving him."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "Guide: Not that kind of goblin, then. My apologies."));
            t.Add(new LevelTask("Talk to the Dye Trader", 5, 207, 1, 5, "Dye Trader: He masters cutting-edge technology. He's an engineer."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "Guide: Can he fly?"));
            t.Add(new LevelTask("Talk to the Dye Trader", 5, 207, 1, 5, "Dye Trader: The Rocket Boots he designed will send you soaring."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "Guide: That must be powerful gear."));
            t.Add(new LevelTask("Talk to the Goblin Tinkerer", 5, 107, 1, 5, "Goblin Tinkerer: Rocket Boots? Gladly, just at cost price..."));
            t.Add(new LevelTask("Own Rocket Boots", 0, 128, 1, 5));
            t.Add(new LevelTask("Wear Rocket Boots", 3, 128, 1, 5));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "Guide: Feels like flying, doesn't it?"));
            t.Add(new LevelTask("Talk to the Goblin Tinkerer", 5, 107, 1, 5, "Goblin Tinkerer: How do they feel? I can make them even faster, you just need the Tinkerer's Workshop..."));
            t.Add(new LevelTask("Own Spectre Boots", 0, 405, 1, 5));
            t.Add(new LevelTask("Wear Spectre Boots", 3, 405, 1, 5));
            t.Add(new LevelTask("Talk to the Goblin Tinkerer", 5, 107, 1, 5, "Goblin Tinkerer: Want even more speed? Like lightning? You just need...\n{name}: Er... that's enough..."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 5, "Guide: Your boots look flashy."));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 5, "Angler: Great boots. I know something that can enhance them - you can fish for it...\n{name}: Er... you too... fine..."));
            t.Add(new LevelTask("Own a Frog Leg", 0, 2423, 1, 5));
            t.Add(new LevelTask("Wear a Frog Leg", 3, 2423, 1, 5));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 5, "Angler: A good match, isn't it?\n{name}: Er... there's more..."));
            t.Add(new LevelTask("Talk to the Goblin Tinkerer", 5, 107, 1, 5, "Goblin Tinkerer: I lost some devices while fleeing. If you find them, they're yours."));
            t.Add(new LevelTask("Own a Depth Meter", 0, 18, 1, 10));
            t.Add(new LevelTask("Own a Compass", 0, 393, 1, 10));
            t.Add(new LevelTask("Own a GPS", 0, 395, 1, 15));
            t.Add(new LevelTask("Own Goblin Tech", 0, 3121, 1, 15));
            t.Add(new LevelTask("Talk to the Goblin Tinkerer", 5, 107, 1, 10, "Goblin Tinkerer: There's even more powerful tech, if you're patient enough to find it."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 15, "Guide: Truly impressive high-tech!"));
            t.Add(new LevelTask("Own a Slime Crown", 0, 560, 1, 10));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 15, "Guide: The King Slime? A powerful boss!"));
            t.Add(new LevelTask("Own Royal Gel", 0, 3090, 1, 5));
            t.Add(new LevelTask("Wear Royal Gel", 3, 3090, 1, 15));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 30, "Guide: Wow, the emblem of the King Slime?"));
            tasks.Add(15, t);

            // ---- Level 18 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Use the Pwnhammer", 2, 367, 1, 21, "Nearby: You swung a hammer that holds ancient power!"));
            t.Add(new LevelTask("Talk to the Clothier", 5, 54, 1, 21, "{name}: My power is unmatched now!\nClothier: Ah... not yet, you... I'm about to go berserk... You'll answer for the Guide's death...\n{name}: You can't stop any of this. Die!"));
            t.Add(new LevelTask("Equip the Clothier Voodoo Doll", 3, 1307, 1, 21));
            t.Add(new LevelTask("Own a Skeletron Treasure Bag", 0, 3323, 1, 21));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 21, "{name}: I heard you came back to life?\nGuide: You *Terraria swear word*, do you know what you're doing? Clothier, don't hold me back, I'm going to kill him today......\nGuide: What, you killed the Clothier too?\n{name}: I'm invincible!\nGuide: Invincible? I think you're in trouble. Remember the King Slime you killed once?"));
            t.Add(new LevelTask("Die 3 times", 6, 0, 3, 21));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 21, "Guide: Hahaha, seeing you like this is so satisfying. Forget it, the past doesn't matter anymore. You still have plenty of dangers ahead..."));
            t.Add(new LevelTask("Die 3 times", 6, 0, 3, 21));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 21, "Guide: Hahaha, how about it? Still invincible?...\nGuide: Sigh... but I'm still the good Guide, and I'll still help you get stronger~"));
            t.Add(new LevelTask("Talk to the Clothier", 5, 54, 1, 21, "{name}: ...\nClothier: Hmph~"));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 21, "{name}: The Clothier is ignoring me. What do I do?\nGuide: Bring him something he likes. I heard he wants to eat mushrooms or something."));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 21, "{name}: What the... Yo-ho, Witch Doctor, you're here too!\nWitch Doctor: Mushrooms? The delicious kind?\nWitch Doctor: I once saw a mushroom that moves. It must be delicious."));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 21, "Witch Doctor: I don't just talk casually. You must find 3 Abeemination."));
            t.Add(new LevelTask("Own 3 Abeeminations", 0, 1133, 3, 21));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 21, "Witch Doctor: Not bad. That tasty mushroom lives in a blue area.\nWitch Doctor: But not underground - you may need to build a house to lure it over."));
            t.Add(new LevelTask("Talk to the Truffle", 5, 160, 1, 21, "{name}: Yummy~\nTruffle: Don't come any closer~~~\n{name}: It can talk? Hmph, so that's how it is. Watch me deal with them."));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 21, "Witch Doctor: Ah, don't blame me, I didn't mean it."));
            t.Add(new LevelTask("Talk to the Clothier", 5, 54, 1, 21, "{name}: Hmph, you actually wanted to eat such a cute mushroom person!\nClothier: Ah, what? You found one? Let me taste it..\n{name}: Let me show you my unmatched power. Take this!\nClothier: Ah, no, ah, I won't do it again, ah"));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 21, "Guide: What's wrong?\n{name}: Hmph\nGuide: ...Sorry... this is my fault. I'll tell you how to get stronger..."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 21, "Guide: It's said the Wall of Flesh once sealed away ancient spirits, among them the spirit of sky and flight - the Soul of Flight.\nGuide: Now that the Wall of Flesh has been defeated and the ancient spirits released, the Souls of Flight should have returned to the sky.\nGuide: Go check the floating islands, you might find an unexpected reward."));
            t.Add(new LevelTask("Own 20 Souls of Flight", 0, 575, 20, 21));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 21, "Guide: When these Souls of Flight are infused into certain items, they grant the power of flight, if you can find them."));
            tasks.Add(18, t);

            // ---- Level 19 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Talk to the Wizard", 5, 108, 1, 15, "{name}: Hello, I'm {name}.\nWizard: Thank you for saving me, I'm happy to serve you."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 15, "Guide: A Wizard? Go see what good things he has."));
            t.Add(new LevelTask("Talk to the Wizard", 5, 108, 1, 15, "Wizard: I have a spell tome that records all things, for just a few gold coins."));
            t.Add(new LevelTask("Own a Spell Tome", 0, 531, 1, 15, "Wizard: Generous, I like that."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 21, "Guide: A spell tome? It's written in an unreadable script."));
            t.Add(new LevelTask("Talk to the Wizard", 5, 108, 1, 21, "Wizard: Actually, I can't read this book either."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 21, "Guide: Let's ask the Witch Doctor then, they both do magic."));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 21, "Witch Doctor: Maybe you need some special material, like Werewolf blood."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 21, "Guide: A werewolf? Those are powerful. Maybe the Arms Dealer has an idea."));
            t.Add(new LevelTask("Talk to the Arms Dealer", 5, 19, 1, 21, "Arms Dealer: Perfect timing, come here, I'll show you a treasure!"));
            t.Add(new LevelTask("Own a Boomstick", 0, 534, 1, 30));
            t.Add(new LevelTask("Talk to the Arms Dealer", 5, 19, 1, 30, "Arms Dealer: This is a Boomstick. I barely managed to get it. You'll also need some ammo.\nArms Dealer: What? Werewolves? Then you might need Silver Bullets. Yes, just like in the movies, werewolves fear silver bullets. Buy some!"));
            t.Add(new LevelTask("Own 100 Silver Bullets", 0, 278, 100, 30));
            t.Add(new LevelTask("Talk to the Arms Dealer", 5, 19, 1, 30, "Arms Dealer: Excellent! Now go find your target and blow its head off!"));
            t.Add(new LevelTask("Use the Boomstick", 2, 534, 1, 30));
            t.Add(new LevelTask("Talk to the Arms Dealer", 5, 19, 1, 30, "Arms Dealer: Don't shoot recklessly! Otherwise I'll have the Nurse stick a syringe in you."));
            t.Add(new LevelTask("Kill a Werewolf", 1, 104, 1, 30));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 30, "{name}: Here you go.\nWitch Doctor: Incredible! My great potion is about to be done.\n{name}: ??? What does the book say??\nWitch Doctor: What book?\n{name}: The spell tome.\nWitch Doctor: What calligraphy?\n{name}: The spell tome I showed you yesterday.\nWitch Doctor: What magic art?\n{name}: You *Terraria swear word*, I'll cut you down.\nWitch Doctor: Aaaah, spare me, great warrior! The Dryad must know."));
            t.Add(new LevelTask("Talk to the Dryad", 5, 20, 1, 50, "Dryad: I see. After the Wall of Flesh was defeated, immense power changed the face of the world...\nDryad: Many new ores have been born in the world. Also, be careful... Huh? Where did they go?"));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 50, "Guide: Understood, I'm off to mine~"));
            tasks.Add(19, t);

            // ---- Level 20 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Own 15 Souls of Light", 0, 520, 15, 15, "{name}: Er... how did I get this stuff into my inventory..."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 15, "Guide: As far as I know, it can be made into a certain key."));
            t.Add(new LevelTask("Own a Light Key", 0, 3092, 1, 15));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 15, "{name}: So what is this thing for?\nGuide: Er, maybe you should ask the Dryad."));
            t.Add(new LevelTask("Talk to the Dryad", 5, 20, 1, 15, "Dryad: A Light Key? How did you get it?\n{name}: Is it used to open chests?\nDryad: No, it's the key item of an ancient summoning ritual.\nDryad: Legend says that if you place it in a chest, a mysterious power will take over the chest and give birth to a fearsome creature."));
            t.Add(new LevelTask("Kill a Hallowed Mimic", 1, 475, 1, 35));
            t.Add(new LevelTask("Talk to the Dryad", 5, 20, 1, 35, "Dryad: You've obtained a powerful weapon."));
            t.Add(new LevelTask("Own a Pirate Map", 0, 1315, 1, 35));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 35, "Guide: A mysterious map. Go ask the Merchant."));
            t.Add(new LevelTask("Talk to the Merchant", 5, 17, 1, 35, "Merchant: A map? Let me see. The destination is... right here! I think this is bad news..."));
            t.Add(new LevelTask("Kill 3 Pirate Deadeyes", 1, 214, 3, 35));
            t.Add(new LevelTask("Talk to the Merchant", 5, 17, 1, 35, "Merchant: Phew, false alarm."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 35, "Guide: What just happened?"));
            t.Add(new LevelTask("Talk to the Pirate", 5, 229, 1, 35, "{name}: Are you here to cause trouble again?\nPirate: I bet your gun has no bullets.\nPirate: Hahaha, not bad, kid. You actually beat my crew."));
            t.Add(new LevelTask("Talk to the Merchant", 5, 17, 1, 35, "{name}: A pirate has arrived.\nMerchant: Aaaah, run!"));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 35, "{name}: A pirate has arrived.\nGuide: Maybe he's a talented one."));
            t.Add(new LevelTask("Talk to the Pirate", 5, 229, 1, 35, "Pirate: Look at this cannon - it can blast an invader away in one shot! Well, without ammo, it's the invaders who blast me away.\n{name}: Right...\nPirate: Go explore this vast land, kid. It has changed enormously."));
            tasks.Add(20, t);

            // ---- Level 21 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Kill 35 Harpies", 1, 48, 35, 15));
            t.Add(new LevelTask("Kill 15 Flying Fish", 1, 224, 15, 15));
            t.Add(new LevelTask("Wear the Raincoat", 3, 1136, 1, 15));
            t.Add(new LevelTask("Kill 3 Umbrella Slimes", 1, 225, 3, 15));
            t.Add(new LevelTask("Kill 5 Ice Slimes", 1, 147, 5, 15));
            t.Add(new LevelTask("Kill 35 Meteor Heads", 1, 23, 35, 15));
            t.Add(new LevelTask("Kill 10 Ghosts", 1, 316, 10, 15));
            t.Add(new LevelTask("Kill 3 Red Devils", 1, 156, 3, 15));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 15, "{name}: In the Underworld I met a tormented soul begging me to save it.\nGuide: Maybe you should ask the Dryad."));
            t.Add(new LevelTask("Talk to the Dryad", 5, 20, 1, 15, "{name}: I saw a tormented soul in the Underworld?\nDryad: I have some Purification Powder, take it and try."));
            t.Add(new LevelTask("Own 10 Purification Powder", 0, 66, 10, 15));
            t.Add(new LevelTask("Use Purification Powder", 2, 66, 1, 15));
            t.Add(new LevelTask("Talk to the Tax Collector", 5, 441, 1, 15, "{name}: Hello, I'm {name}.\nTax Collector: Thank you for saving me. I'll be collecting taxes from you."));
            t.Add(new LevelTask("Talk to the Traveling Merchant", 5, 368, 1, 15, "Traveling Merchant: I'm a traveling merchant. This place is quite prosperous. Care to try my sake?"));
            t.Add(new LevelTask("Drink Sake", 2, 2266, 1, 15));
            t.Add(new LevelTask("Talk to the Traveling Merchant", 5, 368, 1, 30, "{name}: It tastes amazing!\nTraveling Merchant: Haha, if fate allows, we'll meet again."));
            t.Add(new LevelTask("Kill 3 Slimed Armed Zombies", 1, 433, 3, 30));
            t.Add(new LevelTask("Own a Zombie Arm", 0, 1304, 1, 30));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 30, "{name}: A Zombie Arm.\nGuide: A curious item."));
            t.Add(new LevelTask("Talk to the Skeleton Merchant", 5, 453, 1, 30, "{name}: Wow, a skeleton that talks?\nSkeleton Merchant: Don't make a fuss. Come see my treasures - these goods are found nowhere but here."));
            t.Add(new LevelTask("Talk to the Party Girl", 5, 208, 1, 30, "{name}: So tired.\nParty Girl: Go play with this!"));
            t.Add(new LevelTask("Wave the Bubble Wand", 2, 1450, 15, 30));
            t.Add(new LevelTask("Talk to the Party Girl", 5, 208, 1, 90, "Party Girl: Remember to get some rest~"));
            tasks.Add(21, t);

            // ---- Level 25 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Own a Truffle Worm", 0, 2673, 1, 35));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 35, "{name}: Can this be used as bait? I just found it.\nAngler: Aah, throw it away! It'll attract monsters!"));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 35, "Guide: Is the ground shaking?"));
            t.Add(new LevelTask("Kill a Probe", 1, 139, 1, 55));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 55, "Guide: What a huge steel worm. I've never seen such technology."));
            t.Add(new LevelTask("Talk to the Mechanic", 5, 124, 1, 55, "Mechanic: I should find a friend of mine."));
            t.Add(new LevelTask("Talk to the Steampunker", 5, 178, 1, 55, "Steampunker: It's ancient technology."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 55, "Guide: We need to find its source of energy."));
            t.Add(new LevelTask("Talk to the Wizard", 5, 108, 1, 55, "Wizard: Beware, some celestial body is about to cover the sun."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 55, "Guide: A solar eclipse? The moment of desperate battle is coming."));
            t.Add(new LevelTask("Kill a Swamp Thing", 1, 166, 1, 55));
            t.Add(new LevelTask("Own a Broken Hero Sword", 0, 1570, 1, 55));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 55, "Guide: I never imagined you were the legendary hero."));
            t.Add(new LevelTask("Own Excalibur", 0, 368, 1, 55));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 55, "Guide: You want to reforge the broken sword?"));
            t.Add(new LevelTask("Own the vanilla True Excalibur", 0, 674, 1, 55));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 55, "Guide: A hero is about to rise."));
            tasks.Add(25, t);

            // ---- Level 26 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Talk to the Tavernkeep", 5, 550, 1, 15, "Tavernkeep: A hero? There's more to the story, want to hear it?"));
            t.Add(new LevelTask("Use 3 Ales", 2, 353, 3, 15));
            t.Add(new LevelTask("Talk to the Tavernkeep", 5, 550, 1, 15, "Tavernkeep: Remember the Eternia Crystal? It seems to have changed under the influence of the mechanical bosses."));
            t.Add(new LevelTask("Talk to the Steampunker", 5, 178, 1, 15, "Steampunker: Ancient technological resonance?"));
            t.Add(new LevelTask("Talk to the Tavernkeep", 5, 550, 1, 15, "Tavernkeep: The Dryad is calling you over there. If you're interested in the crystal, come find me."));
            t.Add(new LevelTask("Talk to the Dryad", 5, 20, 1, 35, "Dryad: Have you ever seen a certain jungle flower?"));
            t.Add(new LevelTask("Collect Nature's Gift", 4, 223, 1, 55));
            t.Add(new LevelTask("Talk to the Dryad", 5, 20, 1, 55, "Dryad: Thank you for the flower, but it's not this kind."));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 55, "Witch Doctor: Could it be the flower of the century? A legendary, extremely rare and terrifying plant."));
            t.Add(new LevelTask("Talk to the Dryad", 5, 20, 1, 55, "Dryad: I sense their coming into the world."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 55, "Guide: Good or bad?"));
            t.Add(new LevelTask("Talk to the Tavernkeep", 5, 550, 1, 55, "Tavernkeep: A crisis looms in the future."));
            t.Add(new LevelTask("Talk to the Pirate", 5, 229, 1, 55, "Pirate: Piece of cake."));
            t.Add(new LevelTask("Own a Plantera Treasure Bag", 0, 3328, 1, 55));
            t.Add(new LevelTask("Talk to the Pirate", 5, 229, 1, 55, "Pirate: Told you so."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 55, "Guide: A true hero."));
            t.Add(new LevelTask("Talk to the Dryad", 5, 20, 1, 55, "Dryad: This is bad."));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 55, "Witch Doctor: Perhaps some things must be faced head-on rather than avoided."));
            t.Add(new LevelTask("Talk to the Clothier", 5, 54, 1, 55, "Clothier: Perhaps this is simply fate."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 55, "Guide: Hero, get ready to meet it."));
            tasks.Add(26, t);

            // ---- Level 27 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Talk to the Cyborg", 5, 209, 1, 15, "Cyborg: Hello, I'm the Cyborg."));
            t.Add(new LevelTask("Talk to the Demolitionist", 5, 38, 1, 15, "Demolitionist: A big shot."));
            t.Add(new LevelTask("Talk to the Cyborg", 5, 209, 1, 15, "Cyborg: If you're interested, be sure to try my high-tech goods."));
            t.Add(new LevelTask("Talk to the Clothier", 5, 54, 1, 15, "Clothier: I feel a terrifying power growing stronger and gathering in the Dungeon."));
            t.Add(new LevelTask("Talk to the Cyborg", 5, 209, 1, 15, "Cyborg: Maybe the Final Bomb can show off its muscle."));
            t.Add(new LevelTask("Kill a Giant Cursed Skull", 1, 289, 1, 30));
            t.Add(new LevelTask("Talk to the Clothier", 5, 54, 1, 30, "Clothier: Sure enough, they've grown even stronger."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 30, "Guide: Hero, there's nothing to fear. Defeat them."));
            t.Add(new LevelTask("Kill a Skeleton Commando", 1, 293, 1, 30));
            t.Add(new LevelTask("Kill 5 Dungeon Spirits", 1, 288, 5, 30));
            t.Add(new LevelTask("Kill a Skeleton Sniper", 1, 291, 1, 30));
            t.Add(new LevelTask("Kill a Blue Armored Bones", 1, 273, 1, 30));
            t.Add(new LevelTask("Kill a Paladin", 1, 290, 1, 30));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 30, "Guide: A Paladin? Why are they in the Dungeon?"));
            t.Add(new LevelTask("Talk to the Clothier", 5, 54, 1, 30, "Clothier: That irresistible power."));
            t.Add(new LevelTask("Talk to the Dryad", 5, 20, 1, 30, "Dryad: The hallowed order has been broken."));
            t.Add(new LevelTask("Talk to the Wizard", 5, 108, 1, 30, "Wizard: Gazing at the stars last night, I faintly saw a giant pumpkin drift by."));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 30, "Angler: You just want to eat pumpkin, don't you?"));
            t.Add(new LevelTask("Talk to the Wizard", 5, 108, 1, 30, "Wizard: You little brat of an Angler, that's an omen of danger."));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 30, "Angler: Food floating in the sky? What's dangerous about that?"));
            t.Add(new LevelTask("Talk to the Wizard", 5, 108, 1, 30, "Wizard: Prepare some materials and find a way to lure it down."));
            t.Add(new LevelTask("Own 30 Pumpkins", 0, 1725, 30, 30));
            t.Add(new LevelTask("Own 5 Ectoplasm", 0, 1508, 5, 30));
            t.Add(new LevelTask("Own 10 Hallowed Bars", 0, 1225, 10, 30));
            t.Add(new LevelTask("Own a Pumpkin Moon Medallion", 0, 1844, 1, 30));
            t.Add(new LevelTask("Talk to the Wizard", 5, 108, 1, 30, "Wizard: Face your enemies."));
            t.Add(new LevelTask("Kill a Hellhound", 1, 329, 1, 30));
            t.Add(new LevelTask("Kill a Headless Horseman", 1, 315, 1, 30));
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 30, "Angler: Terrifying pumpkins."));
            t.Add(new LevelTask("Talk to the Wizard", 5, 108, 1, 30, "Wizard: Truly out of the ordinary."));
            t.Add(new LevelTask("Own 30 Spooky Wood", 0, 1729, 30, 30));
            t.Add(new LevelTask("Talk to the Wizard", 5, 108, 1, 30, "Wizard: This wood holds great power."));
            tasks.Add(27, t);

            // ---- Level 28 ----
            t = new List<LevelTask>();
            t.Add(new LevelTask("Talk to the Angler", 5, 369, 1, 5, "Angler: There seems to be a creepy smiling face on the moon."));
            t.Add(new LevelTask("Talk to the Wizard", 5, 108, 1, 5, "Wizard: ...Hurry and prepare for battle."));
            t.Add(new LevelTask("Kill a Gingerbread Man", 1, 342, 1, 15));
            t.Add(new LevelTask("Kill a Krampus", 1, 351, 1, 15));
            t.Add(new LevelTask("Kill an Elf Copter", 1, 347, 1, 15));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 50, "Witch Doctor: This... isn't normal."));
            t.Add(new LevelTask("Talk to the Dryad", 5, 20, 1, 50, "Dryad: Rare events are becoming more and more frequent."));
            t.Add(new LevelTask("Talk to the Wizard", 5, 108, 1, 50, "Wizard: The events of recent days are truly shocking."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 50, "Guide: This world is about to undergo a great change. I see the stars gathering by the sun, and endless power flowing into the depths of the jungle."));
            t.Add(new LevelTask("Talk to the Dryad", 5, 20, 1, 50, "Dryad: They have begun to grow wildly in the jungle, and will eventually awaken the ancient power."));
            t.Add(new LevelTask("Talk to the Wizard", 5, 108, 1, 50, "Wizard: Magical power is an ancient force. It lies in the jungle, I can see it."));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 50, "Witch Doctor: The time has come. Go to that place and activate that device."));
            t.Add(new LevelTask("Own a Temple Key", 0, 1141, 1, 50));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 50, "Guide: You must face that immensely powerful enemy now. Please be prepared."));
            t.Add(new LevelTask("Talk to the Mechanic", 5, 124, 1, 50, "Mechanic: You'll face many traps. Bringing Wire Cutters will help a lot."));
            t.Add(new LevelTask("Own Wire Cutters", 0, 510, 1, 50));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 50, "Guide: Let's move out and gather the energy source for that device."));
            t.Add(new LevelTask("Own a Lihzahrd Power Cell", 0, 1293, 1, 50));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 50, "Witch Doctor: The Lihzahrd Altar is activated."));
            t.Add(new LevelTask("Own a Golem Treasure Bag", 0, 3329, 1, 50));
            t.Add(new LevelTask("Talk to the Witch Doctor", 5, 228, 1, 50, "Witch Doctor: It can't be stopped now."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 50, "Guide: Are you ready?"));
            t.Add(new LevelTask("Talk to the Clothier", 5, 54, 1, 50, "Clothier: They're coming back. That terrifying power, I can feel it."));
            t.Add(new LevelTask("Talk to the Guide", 5, 22, 1, 50, "Guide: Time to meet the true enemy. Make every preparation, and head to the Dungeon once more!"));
            tasks.Add(28, t);
        }
    }
}