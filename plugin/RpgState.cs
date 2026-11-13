using System;
using System.IO;
using MySql.Data.MySqlClient;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace HungerRpg
{
    internal static class RpgState
    {
        private static string ConfigPath
        {
            get { return Path.Combine(TShock.SavePath, "HungerRPG.json"); }
        }

        private static string ServerExpPath
        {
            get { return Path.Combine(TShock.SavePath, "ServerExp.txt"); }
        }

        public static ConfigFile Config { get; private set; }
        public static RpgPlayer[] Players { get; private set; }
        public static LocalNpc[] Npcs { get; private set; }
        public static DateTime LastSaveTime { get; set; }

        public static int ServerLevel { get; private set; }
        public static long ServerExp { get; private set; }
        public static long ServerDayExp { get; private set; }
        public static string ServerDayStamp { get; private set; }
        private static DateTime ServerLastSaveTime { get; set; }

        public static void Initialize()
        {
            Npcs = new LocalNpc[201];
            Players = new RpgPlayer[256];
            Config = new ConfigFile();
            LastSaveTime = DateTime.UtcNow;
            ServerLastSaveTime = DateTime.UtcNow;
            ServerDayStamp = "";
        }

        public static void ReloadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                    TShock.Log.ConsoleError("HungerRPG config file not found - a new one has been created!");

                Config = ConfigFile.Read(ConfigPath);
                Config.Write(ConfigPath);
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError("HungerRPG: failed to read config: " + ex);
            }
        }

        private static void RollServerDay()
        {
            string today = Helpers.Today();
            if (today == ServerDayStamp) return;

            ServerDayStamp = today;
            ServerDayExp = 0;
        }

        public static bool IsServerDailyExpCapped()
        {
            RollServerDay();
            var cfg = Config;

            if (Helpers.Reached(ServerDayExp, cfg.ServerDailyMaxExp)) return true;
            return Helpers.DailyCapReached(ServerDayExp, cfg.ServerGolemDailyMaxExp, -1, cfg.ServerPreHardmodeDailyMaxExp);
        }

        public static bool IsServerLevelCapped()
        {
            var cfg = Config;

            if (Helpers.Reached(ServerLevel, cfg.ServerMaxLevel)) return true;
            return Helpers.LevelCapReached(ServerLevel, cfg.ServerGolemMaxLevel, -1, cfg.ServerPreHardmodeMaxLevel);
        }

        public static string ServerDailyExpRemainingText()
        {
            var cfg = Config;
            return Helpers.RemainingDailyText(ServerDayExp, cfg.ServerGolemDailyMaxExp, cfg.ServerDailyMaxExp, cfg.ServerPreHardmodeDailyMaxExp);
        }

        public static void AddServerExp()
        {
            if (!Config.EnableServerLevel) return;
            if (IsServerDailyExpCapped() || IsServerLevelCapped()) return;

            ServerDayExp++;
            ServerExp++;

            if (Helpers.ExpForLevel(ServerLevel + 1) <= ServerExp)
            {
                ServerLevel++;
                TShock.Utils.Broadcast(
                    "Server level has risen to " + UiTheme.Tag(UiTheme.LevelHex(ServerLevel), "Lv." + ServerLevel) + "!!!",
                    255, 215, 0);
            }

            if ((DateTime.UtcNow - ServerLastSaveTime).TotalMilliseconds < 5000) return;
            ServerLastSaveTime = DateTime.UtcNow;
            SaveServerExp();
        }

        public static void SaveServerExp()
        {
            File.WriteAllText(ServerExpPath, ServerLevel + "|" + ServerExp + "|" + ServerDayExp + "|" + ServerDayStamp);
        }

        public static void LoadServerExp()
        {
            ServerLevel = 0;
            ServerExp = 0;
            ServerDayExp = 0;
            ServerDayStamp = "";

            if (!File.Exists(ServerExpPath))
            {
                File.Create(ServerExpPath).Close();
                return;
            }

            string[] parts = File.ReadAllText(ServerExpPath).Split('|');
            if (parts.Length != 4) return;

            int level;
            long exp, dayExp;
            if (int.TryParse(parts[0], out level) && long.TryParse(parts[1], out exp) && long.TryParse(parts[2], out dayExp))
            {
                ServerLevel = level;
                ServerExp = exp;
                ServerDayExp = dayExp;
                ServerDayStamp = parts[3];
            }
        }

        public static void EnsureTables()
        {
            var playerTable = new SqlTable("HungerRPG",
                new SqlColumn("ID", MySqlDbType.Int32) { Primary = true },
                new SqlColumn("Exp", MySqlDbType.Int64),
                new SqlColumn("Level", MySqlDbType.Int32),
                new SqlColumn("Buffs", MySqlDbType.Text),
                new SqlColumn("StopBuffs", MySqlDbType.Int32),
                new SqlColumn("HideInfo", MySqlDbType.Int32),
                new SqlColumn("TaskId", MySqlDbType.Int32),
                new SqlColumn("TaskProgress", MySqlDbType.Int32),
                new SqlColumn("DayExp", MySqlDbType.Int64),
                new SqlColumn("DayText", MySqlDbType.VarChar, 32),
                new SqlColumn("ChatColor", MySqlDbType.Text),
                new SqlColumn("Prefix", MySqlDbType.Text),
                new SqlColumn("Suffix", MySqlDbType.Text),
                new SqlColumn("Summons", MySqlDbType.Int32),
                new SqlColumn("Invasions", MySqlDbType.Int32),
                new SqlColumn("DaySummons", MySqlDbType.Int32),
                new SqlColumn("DayInvasions", MySqlDbType.Int32),
                new SqlColumn("OverSummons", MySqlDbType.Int32),
                new SqlColumn("Team", MySqlDbType.Int32),
                new SqlColumn("TeamExp", MySqlDbType.Int64),
                new SqlColumn("TeamDayExp", MySqlDbType.Int64));
            CreateTable(playerTable);

            if (!Config.EnableTeams) return;

            var teamTable = new SqlTable("Teams",
                new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("Name", MySqlDbType.VarChar, 255) { Unique = true },
                new SqlColumn("Leader", MySqlDbType.Int32),
                new SqlColumn("Exp", MySqlDbType.Int64),
                new SqlColumn("Level", MySqlDbType.Int32),
                new SqlColumn("Buffs", MySqlDbType.Text),
                new SqlColumn("Prefix", MySqlDbType.Text));
            CreateTable(teamTable);
        }

        private static void CreateTable(SqlTable table)
        {
            var creator = new SqlTableCreator(TShock.DB, TShock.DB.GetSqlQueryBuilder());
            creator.EnsureTableStructure(table);
        }
    }
}
