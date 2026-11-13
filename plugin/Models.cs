using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;

namespace HungerRpg
{
    public class LocalNpc
    {
        public int Index { get; set; }
        public bool Dead { get; set; }
        public DateTime StartTime { get; set; }
        public Dictionary<string, int> Damage { get; set; }

        public LocalNpc(int index)
        {
            Index = index;
            Dead = false;
            StartTime = DateTime.UtcNow;
            Damage = new Dictionary<string, int>();
        }
    }

    public class DamageEntry
    {
        public string Name { get; set; }
        public int Damage { get; set; }
        public int Proportion { get; set; }

        public DamageEntry(string name, int damage)
        {
            Name = name;
            Damage = damage;
            Proportion = 0;
        }
    }

    public class LevelDamageLimit
    {
        public int FromLevel { get; set; }
        public int ToLevel { get; set; }
        public int DamageLimit { get; set; }
        public int DpsLimit { get; set; }

        public LevelDamageLimit() { }

        public LevelDamageLimit(int fromLevel, int toLevel, int damageLimit, int dpsLimit)
        {
            FromLevel = fromLevel;
            ToLevel = toLevel;
            DamageLimit = damageLimit;
            DpsLimit = dpsLimit;
        }
    }

    public static class TaskType
    {
        public const int Own = 0;
        public const int Kill = 1;
        public const int Use = 2;
        public const int Wear = 3;
        public const int Collect = 4;
        public const int Talk = 5;
        public const int Die = 6;

        public static bool HasCounter(int type)
        {
            return type == Kill || type == Use || type == Talk || type == Die;
        }
    }

    public class LevelTask
    {
        public string Title { get; set; }
        public int Type { get; set; }
        public int TargetId { get; set; }
        public int TargetCount { get; set; }
        public int Exp { get; set; }
        public string Story { get; set; }

        public LevelTask() { }

        public LevelTask(string title, int type, int targetId, int targetCount, int exp, string story = "")
        {
            Title = title;
            Type = type;
            TargetId = targetId;
            TargetCount = targetCount;
            Exp = exp;
            Story = story;
        }
    }

    public class PlayerRecord
    {
        public int Level = -1;
        public long Exp;
        public List<int> Buffs = new List<int>();
        public bool BuffsPaused;
        public bool HudHidden;
        public int TaskId;
        public int TaskProgress;
        public long DayExp;
        public string DayStamp = "";
        public string Prefix = "";
        public string ChatColor = "";
        public string Suffix = "";
        public int BankedSummons;
        public int BankedInvasions;
        public int SummonsToday;
        public int InvasionsToday;
        public int OffHourSummons;
        public int TeamId = -1;
        public long TeamExp;
        public long TeamDayExp;
    }

    public class RpgPlayer
    {
        public int Who { get; set; }
        public int Level { get; set; }
        public long Exp { get; set; }
        public bool BuffsPaused { get; set; }
        public bool HudHidden { get; set; }
        public List<int> Buffs { get; set; }

        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public string ChatColor { get; set; }

        public string CustomPrefix { get; set; }
        public string CustomSuffix { get; set; }
        public string CustomChatColor { get; set; }

        public int TeamId { get; set; }
        public int TeamLevel { get; set; }
        public string TeamName { get; set; }
        public long TeamExp { get; set; }
        public long TeamDayExp { get; set; }
        public List<int> TeamBuffs { get; set; }
        public string TeamPrefix { get; set; }
        public int TeamExpTimer { get; set; }

        public DateTime LastSaveTime { get; set; }
        public DateTime LastStrikeTime { get; set; }
        public DateTime LastExpPopupTime { get; set; }
        public byte SelectedItem { get; set; }
        public int DisableTicks { get; set; }
        public int TaskId { get; set; }
        public int TaskProgress { get; set; }
        public long DayExp { get; set; }
        public string DayStamp { get; set; }
        public string GroupName { get; set; }
        public int OnlineExpTimer { get; set; }
        public int HungerGraceSeconds { get; set; }

        public int BankedSummons { get; set; }
        public int BankedInvasions { get; set; }
        public int SummonsToday { get; set; }
        public int InvasionsToday { get; set; }
        public int OffHourSummons { get; set; }

        public DateTime DamageWindowStart { get; set; }
        public int DamageStrikes { get; set; }
        public long Dps { get; set; }
        public DateTime DpsWindowStart { get; set; }

        public RpgPlayer(int who, PlayerRecord record)
        {
            DateTime now = DateTime.UtcNow;

            Who = who;
            Level = record.Level;
            Exp = record.Exp;
            Buffs = record.Buffs;
            BuffsPaused = record.BuffsPaused;
            HudHidden = record.HudHidden;
            TaskId = record.TaskId;
            TaskProgress = record.TaskProgress;
            DayExp = record.DayExp;
            DayStamp = record.DayStamp;

            CustomPrefix = record.Prefix;
            CustomSuffix = record.Suffix;
            CustomChatColor = record.ChatColor;

            BankedSummons = record.BankedSummons;
            BankedInvasions = record.BankedInvasions;
            SummonsToday = record.SummonsToday;
            InvasionsToday = record.InvasionsToday;
            OffHourSummons = record.OffHourSummons;

            TeamId = record.TeamId;
            TeamExp = record.TeamExp;
            TeamDayExp = record.TeamDayExp;
            TeamName = "";
            TeamLevel = 0;
            TeamBuffs = new List<int>();
            TeamPrefix = "";

            GroupName = "";
            SelectedItem = 0;
            DisableTicks = 5;
            HungerGraceSeconds = Math.Max(3, RpgState.Config.HungerDelay);

            DamageStrikes = 0;
            Dps = 0;
            DamageWindowStart = now;
            DpsWindowStart = now;
            LastSaveTime = now;
            LastStrikeTime = now;
            LastExpPopupTime = now;

            if (Level == -1) return;

            if (EnsureToday())
                TShock.Players[Who].SendSuccessMessage("Welcome back! Your daily limits have been refreshed.");

            if (RpgState.Config.EnableTeams && TeamId > 0)
                LoadTeam();
        }

        public LevelTask CurrentTask
        {
            get
            {
                List<LevelTask> tasks;
                if (RpgState.Config.LevelTasks.TryGetValue(Level, out tasks) && TaskId >= 0 && TaskId < tasks.Count)
                    return tasks[TaskId];
                return null;
            }
        }

        private void LoadTeam()
        {
            try
            {
                using (var reader = TShock.DB.QueryReader("SELECT * FROM Teams WHERE ID=@0", TeamId))
                {
                    if (reader.Read())
                    {
                        TeamName = reader.Get<string>("Name");
                        TeamLevel = reader.Get<int>("Level");
                        string buffs = reader.Get<string>("Buffs");
                        if (!string.IsNullOrEmpty(buffs))
                            TeamBuffs = buffs.Split(',').Select(int.Parse).ToList();
                        TeamPrefix = reader.Get<string>("Prefix");
                    }
                    else
                    {
                        TeamId = -1;
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError("Failed to read team " + TeamId + " from the database: " + ex);
            }
        }

        private bool EnsureToday()
        {
            string today = Helpers.Today();
            if (today == DayStamp) return false;

            DayStamp = today;
            DayExp = 0;
            TeamDayExp = 0;
            RefreshDailyLimits();
            return true;
        }

        private static int RollOver(int banked, int usedToday, int dailyMax, int stackCap)
        {
            int leftover = Math.Max(0, dailyMax - usedToday);
            banked += dailyMax + leftover;
            banked = Math.Min(banked, stackCap);
            return Math.Max(0, banked - dailyMax);
        }

        private void RefreshDailyLimits()
        {
            TSPlayer ply = TShock.Players[Who];

            if (ply.HasPermission(Permissions.summonboss) && !ply.HasPermission(Perms.IgnoreSummonLimit))
                BankedSummons = RollOver(BankedSummons, SummonsToday, Helpers.MaxDailySummons(ply), Helpers.SummonStackCap(ply));

            if (ply.HasPermission(Permissions.startinvasion) && !ply.HasPermission(Perms.IgnoreInvasionLimit))
                BankedInvasions = RollOver(BankedInvasions, InvasionsToday, Helpers.MaxDailyInvasions(ply), Helpers.InvasionStackCap(ply));

            SummonsToday = 0;
            InvasionsToday = 0;
            OffHourSummons = 0;
        }

        public void ProgressTask(int type)
        {
            ProgressTask(type, null);
        }

        public void ProgressTask(int type, int? targetId)
        {
            if (IsMaxLevel()) return;

            LevelTask task = CurrentTask;
            if (task == null || task.Type != type) return;
            if (targetId.HasValue && task.TargetId != targetId.Value) return;

            if (TaskProgress < task.TargetCount)
                TaskProgress++;
        }

        public void AddKillExp(NPC npc, int proportion)
        {
            var cfg = RpgState.Config;
            if (proportion < cfg.BossMinExpPercent) return;

            int exp;
            if (!cfg.ExpOverrides.TryGetValue(npc.netID, out exp))
                exp = Helpers.ExpForKill(npc.lifeMax);

            if (exp == 0) return;
            if (npc.lifeMax < Helpers.MinFullExpLife(Level)) exp = 1;

            AddExp(exp * proportion / 100, false);
        }

        private int ClampToDailyCap(int exp)
        {
            var cfg = RpgState.Config;
            if (Main.hardMode)
            {
                if (!NPC.downedGolemBoss) exp = ClampTo(exp, cfg.GolemDailyMaxExp);
                exp = ClampTo(exp, cfg.DailyMaxExp);
            }
            else
            {
                exp = ClampTo(exp, cfg.PreHardmodeDailyMaxExp);
            }
            return exp;
        }

        private int ClampTo(int exp, long cap)
        {
            if (cap != -1 && cap <= DayExp + exp)
                return (int)(cap - DayExp);
            return exp;
        }

        public void AddExp(int exp, bool ignoreDailyCap = true)
        {
            TSPlayer ply = TShock.Players[Who];
            if (ply == null) return;

            if (Level == -1)
            {
                ply.SendErrorMessage("Failed to load your EXP. Try rejoining the server!");
                return;
            }

            if (exp == 0) return;
            var cfg = RpgState.Config;

            if (exp > 0 && IsMaxLevel()) return;

            if (!ignoreDailyCap && Level < cfg.UnlimitedExpLevel)
            {
                EnsureToday();
                if (exp > 0)
                {
                    exp = ClampToDailyCap(exp);
                    if (exp <= 0) return;
                    DayExp += exp;
                }
            }

            if (exp > 0)
            {
                if (Helpers.ExpForLevel(Level + 1) <= Exp + exp)
                    LevelUp(ply);
            }
            else
            {
                long floor = Helpers.ExpForLevel(Level);
                if (floor > Exp + exp)
                    exp = (int)(floor - Exp);
                if (exp == 0) return;
            }

            Exp += exp;

            DateTime now = DateTime.UtcNow;
            if ((now - LastExpPopupTime).TotalMilliseconds < 50) return;
            LastExpPopupTime = now;

            if (exp > 0)
                Helpers.Popup(ply, "\u2191 +" + exp + " EXP", UiTheme.ExpGainColor);
            else
                Helpers.Popup(ply, "\u2193 -" + Math.Abs(exp) + " EXP", UiTheme.ExpLossColor);

            SaveToDb(false);
        }

        private void LevelUp(TSPlayer ply)
        {
            if (CurrentTask != null)
                ply.SendErrorMessage("Your current quest was skipped because you leveled up!");

            Level++;
            TaskId = 0;
            TaskProgress = 0;

            string hex = UiTheme.LevelHex(Level);
            TShock.Utils.Broadcast(
                ply.Name + " reached " + UiTheme.Tag(hex, "Lv." + Level + " " + UiTheme.LevelTitle(Level)) + "!!!",
                255, 215, 0);

            Helpers.Popup(ply, "\u2191\u2191\u2191 LEVEL UP " + Level + " \u2191\u2191\u2191", UiTheme.LevelColor(Level));
        }

        public void AddTeamExp(int exp, bool ignoreDailyCap = false)
        {
            if (TShock.Players[Who] == null) return;

            var cfg = RpgState.Config;
            if (Level == -1 || !cfg.EnableTeams || TeamId <= 0 || exp <= 0) return;

            if (!ignoreDailyCap)
            {
                EnsureToday();
                if (cfg.TeamDailyMaxExp <= TeamDayExp + exp)
                    exp = (int)(cfg.TeamDailyMaxExp - TeamDayExp);

                if (exp <= 0) return;
                TeamDayExp += exp;
            }

            TeamExp += exp;
            SaveToDb(false);
        }

        public void SaveToDb(bool force)
        {
            TSPlayer ply = TShock.Players[Who];
            if (ply == null) return;

            if (Level == -1)
            {
                TShock.Log.ConsoleError("HungerRPG: database save cancelled for " + ply.Name);
                return;
            }

            if (!force)
            {
                DateTime now = DateTime.UtcNow;
                if ((now - LastSaveTime).TotalMilliseconds < 5000) return;
                if ((now - RpgState.LastSaveTime).TotalMilliseconds < 3000) return;
                LastSaveTime = now;
                RpgState.LastSaveTime = now;
            }

            try
            {
                const string query = "UPDATE HungerRPG SET Exp=@0, Level=@1, StopBuffs=@2, HideInfo=@3, TaskId=@4, TaskProgress=@5, DayExp=@6, DayText=@7, Prefix=@8, ChatColor=@9, Suffix=@10, Summons=@11, Invasions=@12, DaySummons=@13, DayInvasions=@14, OverSummons=@15, Team=@16, TeamExp=@17, TeamDayExp=@18 WHERE ID=@19";
                TShock.DB.Query(query, Exp, Level, BuffsPaused ? 1 : 0, HudHidden ? 1 : 0, TaskId, TaskProgress, DayExp, DayStamp,
                    CustomPrefix, CustomChatColor, CustomSuffix, BankedSummons, BankedInvasions, SummonsToday, InvasionsToday, OffHourSummons,
                    TeamId, TeamExp, TeamDayExp, ply.Account.ID);
            }
            catch (Exception ex)
            {
                TShock.Log.Error("HungerRPG: failed to save to the database: " + ex);
            }
        }

        public bool IsMaxLevel()
        {
            var cfg = RpgState.Config;
            return Helpers.LevelCapReached(Level, cfg.GolemMaxLevel, cfg.MaxLevel, cfg.PreHardmodeMaxLevel);
        }

        public bool IsDailyExpCapped()
        {
            var cfg = RpgState.Config;
            if (Level >= cfg.UnlimitedExpLevel) return false;

            EnsureToday();
            return Helpers.DailyCapReached(DayExp, cfg.GolemDailyMaxExp, cfg.DailyMaxExp, cfg.PreHardmodeDailyMaxExp);
        }

        public string DailyExpRemainingText()
        {
            var cfg = RpgState.Config;
            if (Level >= cfg.UnlimitedExpLevel) return "";
            return Helpers.RemainingDailyText(DayExp, cfg.GolemDailyMaxExp, cfg.DailyMaxExp, cfg.PreHardmodeDailyMaxExp);
        }

        public void RefreshChatStyle()
        {
            TSPlayer user = TShock.Players[Who];
            if (user == null) return;
            if (user.Group.Name == GroupName) return;

            Prefix = "";
            ChatColor = "";
            Suffix = "";

            string prefix = "";
            string chatColor = "";
            string suffix = "";

            try
            {
                using (var reader = TShock.DB.QueryReader("SELECT * FROM GroupList WHERE GroupName=@0", user.Group.Name))
                {
                    if (reader.Read())
                    {
                        prefix = reader.Get<string>("Prefix");
                        chatColor = reader.Get<string>("ChatColor");
                        suffix = reader.Get<string>("Suffix");
                    }
                    else if (user.Group.Name == "superadmin")
                    {
                        var settings = TShock.Config.Settings;
                        prefix = settings.SuperAdminChatPrefix;
                        chatColor = settings.SuperAdminChatRGB[0] + "," + settings.SuperAdminChatRGB[1] + "," + settings.SuperAdminChatRGB[2];
                        suffix = settings.SuperAdminChatSuffix;
                    }
                    else
                    {
                        TShock.Log.ConsoleError("HungerRPG: unknown chat prefix for group " + user.Group.Name + "!");
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError("HungerRPG: failed to read chat style: " + ex);
            }

            GroupName = user.Group.Name;

            if (user.tempGroup != null || !user.HasPermission(Perms.ShowCustomAffix))
            {
                Prefix = prefix;
                ChatColor = chatColor;
                Suffix = suffix;
                return;
            }

            if (!string.IsNullOrEmpty(CustomPrefix)) Prefix = CustomPrefix;
            else if (!string.IsNullOrEmpty(TeamPrefix)) Prefix = TeamPrefix;
            else Prefix = prefix;

            ChatColor = string.IsNullOrEmpty(CustomChatColor) ? chatColor : CustomChatColor;
            Suffix = string.IsNullOrEmpty(CustomSuffix) ? suffix : CustomSuffix;

            if (string.IsNullOrEmpty(ChatColor))
                ChatColor = "255,255,255";
        }

        public void ClearProjectiles()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile == null || projectile.owner != Who) continue;

                projectile.active = false;
                projectile.type = 0;
                TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", i);
            }
        }
    }
}
