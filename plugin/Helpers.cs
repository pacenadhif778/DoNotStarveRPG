using System;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace HungerRpg
{
    internal static class Perms
    {
        public const string Reload = "hungerrpg.reload";
        public const string IgnoreSummonLimit = "hungerrpg.ignore.summonlimit";
        public const string IgnoreSummonTime = "hungerrpg.ignore.summontime";
        public const string IgnoreInvasionLimit = "hungerrpg.ignore.invasionlimit";
        public const string IgnoreInvasionTime = "hungerrpg.ignore.invasiontime";
        public const string IgnoreDamageLimit = "hungerrpg.ignore.damagelimit";
        public const string IgnoreDyeLevel = "hungerrpg.ignore.dyelevel";
        public const string IgnoreArmorLevel = "hungerrpg.ignore.armorlevel";
        public const string IgnoreEquipLevel = "hungerrpg.ignore.equiplevel";
        public const string IgnoreVanityLevel = "hungerrpg.ignore.vanitylevel";
        public const string IgnoreUseLevel = "hungerrpg.ignore.uselevel";
        public const string ShowCustomAffix = "hungerrpg.customaffix";

        public const string DailySummonPattern = @"^hungerrpg\.dailysummon\.(\d{1,9})$";
        public const string DailyInvasionPattern = @"^hungerrpg\.dailyinvasion\.(\d{1,9})$";
        public const string SummonCapPattern = @"^hungerrpg\.summoncap\.(\d{1,9})$";
        public const string InvasionCapPattern = @"^hungerrpg\.invasioncap\.(\d{1,9})$";
    }

    internal static class Helpers
    {
        public static int GetDpsLimit(int level)
        {
            LevelDamageLimit limit = FindDamageLimit(level);
            return limit == null ? 0 : limit.DpsLimit;
        }

        public static int GetDamageLimit(int level)
        {
            LevelDamageLimit limit = FindDamageLimit(level);
            return limit == null ? 0 : limit.DamageLimit;
        }

        private static LevelDamageLimit FindDamageLimit(int level)
        {
            var limits = RpgState.Config.LevelDamageLimits;
            if (limits == null) return null;

            foreach (var limit in limits)
            {
                if (limit.FromLevel != 0 && level < limit.FromLevel) continue;
                if (limit.ToLevel != 0 && level > limit.ToLevel) continue;
                return limit;
            }
            return null;
        }

        public static long ExpForLevel(int level)
        {
            long exp = (long)((15 * Math.Pow((double)level / 3, 3)) + level);
            return exp < 1 ? 1 : exp;
        }

        public static int ExpPercent(int level, long exp)
        {
            long next = ExpForLevel(level + 1);
            long current = ExpForLevel(level);
            long needed = next - current;
            if (needed == 0) return 100;
            return (int)((exp - current) * 100 / needed);
        }

        public static int ExpForKill(int life)
        {
            if (life <= 15) return 0;
            int exp = (int)(Math.Pow(life, 1 / 3.5) + Math.Log(life, 3.5) - 7.7);
            return exp < 1 ? 1 : exp;
        }

        public static int MinFullExpLife(int level)
        {
            int life = (int)(Math.Atan(level - 27.7) * 16355 + 25100);
            return life < 0 ? 0 : life;
        }

        public static int MaxDailySummons(TSPlayer ply)
        {
            return ReadPermissionValue(ply, Perms.DailySummonPattern, RpgState.Config.DailySummonCount);
        }

        public static int MaxDailyInvasions(TSPlayer ply)
        {
            return ReadPermissionValue(ply, Perms.DailyInvasionPattern, RpgState.Config.DailyInvasionCount);
        }

        public static int SummonStackCap(TSPlayer ply)
        {
            return ReadPermissionValue(ply, Perms.SummonCapPattern, RpgState.Config.SummonStackCap);
        }

        public static int InvasionStackCap(TSPlayer ply)
        {
            return ReadPermissionValue(ply, Perms.InvasionCapPattern, RpgState.Config.InvasionStackCap);
        }

        private static int ReadPermissionValue(TSPlayer ply, string pattern, int fallback)
        {
            foreach (var permission in ply.Group.permissions)
            {
                Match match = Regex.Match(permission, pattern);
                if (match.Success) return Convert.ToInt32(match.Groups[1].Value);
            }
            return fallback;
        }

        public static bool IsLoggedIn(TSPlayer ply)
        {
            return ply.IsLoggedIn && ply.Account != null && ply.Account.ID != 0;
        }

        public static string Today()
        {
            return DateTime.Now.ToString("yyyy-MM-dd");
        }

        public static int GetEffectiveLevel(RpgPlayer rpg)
        {
            if (RpgState.Config.EnableServerLevel && RpgState.ServerLevel > rpg.Level)
                return RpgState.ServerLevel;
            return rpg.Level;
        }

        public static int GetRarity(Item item)
        {
            int rarity;
            if (!RpgState.Config.RarityOverrides.TryGetValue(item.type, out rarity))
                rarity = item.rare;
            return rarity * RpgState.Config.RarityMultiplier;
        }

        public static Item CreateLevelItem(int type, int prefix)
        {
            var item = new Item();
            item.netDefaults(type);

            if (RpgState.Config.IgnorePrefixes || IsArmor(item) || item.accessory || item.vanity)
                item.Prefix(0);
            else
                item.Prefix(prefix);

            return item;
        }

        public static bool IsArmor(Item item)
        {
            return item.headSlot > -1 || item.bodySlot > -1 || item.legSlot > -1;
        }

        public static bool IsDye(Item item)
        {
            return item.dye > 0 || item.Name.EndsWith("Dye", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsTool(Item item)
        {
            return item.hammer > 0 || item.axe > 0 || item.pick > 0;
        }

        public static bool IsWeapon(Item item)
        {
            return item.sentry || item.summon || item.ranged || item.magic || item.melee;
        }

        public static string GetUsageKind(Item item)
        {
            if (item.fishingPole > 0) return "fishing rod";
            if (IsTool(item)) return "tool";
            if (IsWeapon(item)) return "weapon";
            return null;
        }

        public static string PrefixedName(Item item)
        {
            string prefix = TShock.Utils.GetPrefixById(item.prefix);
            return string.IsNullOrEmpty(prefix) ? item.Name : prefix + " " + item.Name;
        }

        public static void Popup(TSPlayer ply, string text, Color color)
        {
            ply.SendData(PacketTypes.CreateCombatTextExtended, text, (int)color.PackedValue, ply.X, ply.Y);
        }

        public static bool Reached(long value, long limit)
        {
            return limit != -1 && limit <= value;
        }

        public static bool LevelCapReached(int level, int golemCap, int hardmodeCap, int preHardmodeCap)
        {
            if (Main.hardMode)
                return (!NPC.downedGolemBoss && Reached(level, golemCap)) || Reached(level, hardmodeCap);
            return Reached(level, preHardmodeCap);
        }

        public static bool DailyCapReached(long used, long golemCap, long hardmodeCap, long preHardmodeCap)
        {
            if (Main.hardMode)
                return (!NPC.downedGolemBoss && Reached(used, golemCap)) || Reached(used, hardmodeCap);
            return Reached(used, preHardmodeCap);
        }

        public static string RemainingDailyText(long used, long golemCap, long hardmodeCap, long preHardmodeCap)
        {
            if (Main.hardMode)
            {
                if (!NPC.downedGolemBoss && golemCap != -1 && golemCap > used) return PercentLeft(used, golemCap);
                if (hardmodeCap != -1 && hardmodeCap > used) return PercentLeft(used, hardmodeCap);
            }
            else if (preHardmodeCap != -1 && preHardmodeCap > used)
            {
                return PercentLeft(used, preHardmodeCap);
            }
            return "";
        }

        private static string PercentLeft(long used, long cap)
        {
            return " (" + (100 - used * 100 / cap) + "% left)";
        }

        public static string GetTimeRestriction(string action, int dayStart, int dayEnd, int weekendStart, int weekendEnd, int fallbackStart, int fallbackEnd)
        {
            DateTime now = DateTime.Now;
            bool weekend = now.DayOfWeek == DayOfWeek.Sunday || now.DayOfWeek == DayOfWeek.Saturday;
            int start = weekend ? weekendStart : dayStart;
            int end = weekend ? weekendEnd : dayEnd;
            if (start <= 0 && end <= 0) return null;

            int minute = now.Hour * 60 + now.Minute;
            if (minute >= start && minute <= end) return null;

            string clock = string.Format("{0:D2}:{1:D2}", now.Hour, now.Minute);
            if (!weekend)
                return string.Format("It is {0} - you cannot {1} right now. Allowed daily between {2}.", clock, action, FormatRange(start, end));

            if (fallbackStart <= 0 && fallbackEnd <= 0) return null;
            if (minute >= fallbackStart && minute <= fallbackEnd) return null;

            return string.Format("It is weekend {0} - you cannot {1} right now. Allowed on weekends between {2} or {3}.",
                clock, action, FormatRange(start, end), FormatRange(fallbackStart, fallbackEnd));
        }

        private static string FormatRange(int startMinute, int endMinute)
        {
            return string.Format("{0:D2}:{1:D2}-{2:D2}:{3:D2}", startMinute / 60, startMinute % 60, endMinute / 60, endMinute % 60);
        }
    }
}
