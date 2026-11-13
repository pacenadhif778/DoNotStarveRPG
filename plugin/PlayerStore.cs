using System;
using System.Linq;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace HungerRpg
{
    internal static class PlayerStore
    {
        public static PlayerRecord Load(TSPlayer ply)
        {
            var cfg = RpgState.Config;
            var record = new PlayerRecord();

            long startExp = Main.hardMode ? cfg.HardmodeStartExp : cfg.PreHardmodeStartExp;
            int startLevel = Main.hardMode ? cfg.HardmodeStartLevel : cfg.PreHardmodeStartLevel;
            if (startExp < 0) startExp = 0;
            if (startLevel < 0) startLevel = 0;

            try
            {
                bool isNew = false;

                using (var reader = TShock.DB.QueryReader("SELECT * FROM HungerRPG WHERE ID=@0", ply.Account.ID))
                {
                    if (reader.Read())
                    {
                        record.Exp = reader.Get<long>("Exp");
                        record.Level = reader.Get<int>("Level");

                        string buffs = reader.Get<string>("Buffs");
                        if (!string.IsNullOrEmpty(buffs))
                            record.Buffs = buffs.Split(',').Select(int.Parse).ToList();

                        record.BuffsPaused = reader.Get<int>("StopBuffs") == 1;
                        record.HudHidden = reader.Get<int>("HideInfo") == 1;
                        record.TaskId = reader.Get<int>("TaskId");
                        record.TaskProgress = reader.Get<int>("TaskProgress");
                        record.DayExp = reader.Get<long>("DayExp");
                        record.DayStamp = reader.Get<string>("DayText");

                        record.Prefix = reader.Get<string>("Prefix");
                        record.ChatColor = reader.Get<string>("ChatColor");
                        record.Suffix = reader.Get<string>("Suffix");

                        record.BankedSummons = reader.Get<int>("Summons");
                        record.BankedInvasions = reader.Get<int>("Invasions");
                        record.SummonsToday = reader.Get<int>("DaySummons");
                        record.InvasionsToday = reader.Get<int>("DayInvasions");
                        record.OffHourSummons = reader.Get<int>("OverSummons");

                        record.TeamId = reader.Get<int>("Team");
                        record.TeamExp = reader.Get<long>("TeamExp");
                        record.TeamDayExp = reader.Get<long>("TeamDayExp");

                        if (Main.hardMode && cfg.HardmodeExpCatchUp)
                        {
                            if (record.Exp < startExp) record.Exp = startExp;
                            if (record.Level < startLevel) record.Level = startLevel;
                        }
                    }
                    else
                    {
                        isNew = true;
                    }
                }

                if (isNew)
                {
                    int rows = TShock.DB.Query(
                        "INSERT INTO HungerRPG (ID, Exp, Level, Buffs, StopBuffs, HideInfo, TaskId, TaskProgress, DayExp, DayText, Prefix, ChatColor, Suffix, Summons, Invasions, DaySummons, DayInvasions, OverSummons, Team, TeamExp, TeamDayExp) VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13, @14, @15, @16, @17, @18, @19, @20);",
                        ply.Account.ID, startExp, startLevel, "", 0, 0, 0, 0, 0, Helpers.Today(), "", "", "", 0, 0, 0, 0, 0, -1, 0, 0);

                    if (rows == 0)
                    {
                        Console.WriteLine(ply.Name + ": failed to insert HungerRPG database row!");
                    }
                    else
                    {
                        record.Level = startLevel;
                        record.Exp = startExp;
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError("HungerRPG: failed to read database for " + ply.Name + ": " + ex);
            }

            return record;
        }
    }
}
