using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;

namespace HungerRpg
{
    [ApiVersion(2, 1)]
    public class HungerRpgPlugin : TerrariaPlugin
    {
        public override string Name { get { return "Hunger RPG"; } }
        public override string Author { get { return "kobo"; } }
        public override string Description { get { return "Hunger + Levels!"; } }
        public override Version Version { get { return new Version(3, 0, 4); } }

        private static readonly System.Timers.Timer UpdateTimer = new System.Timers.Timer(1000);
        private static int _updateBusy;
        private static int _equipTick;

        public HungerRpgPlugin(Main game) : base(game)
        {
            Order = 5;
            RpgState.Initialize();
        }

        public override void Initialize()
        {
            ServerApi.Hooks.NpcSpawn.Register(this, OnNpcSpawn);
            ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilled);
            ServerApi.Hooks.NpcStrike.Register(this, OnNpcStrike);
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
            ServerApi.Hooks.ServerChat.Register(this, OnChat);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                RpgState.SaveServerExp();

                UpdateTimer.Elapsed -= OnUpdate;
                UpdateTimer.Stop();

                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
                ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
                ServerApi.Hooks.NpcSpawn.Deregister(this, OnNpcSpawn);
                ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilled);
                ServerApi.Hooks.NpcStrike.Deregister(this, OnNpcStrike);
            }
            base.Dispose(disposing);
        }

        private void OnInitialize(EventArgs args)
        {
            RpgState.ReloadConfig();
            RpgState.EnsureTables();
            RpgState.LoadServerExp();
            PacketHandlers.Init();

            Commands.ChatCommands.Add(new Command("", OnToggleBuffs, "togglebuffs") { HelpText = "Type /togglebuffs to pause or resume level buff updates." });
            Commands.ChatCommands.Add(new Command("", OnToggleHud, "togglehud") { HelpText = "Type /togglehud to hide or show the live info display." });
            Commands.ChatCommands.Add(new Command("", OnCompleteTask, "completetask") { HelpText = "Type /completetask to complete your current quest." });
            Commands.ChatCommands.Add(new Command("", OnItemLevel, "itemlevel") { HelpText = "Type /itemlevel <item name or ID> to see the level an item requires." });
            Commands.ChatCommands.Add(new Command(Perms.Reload, OnReload, "reloadrpg") { HelpText = "Type /reloadrpg to reload the Hunger RPG config." });
            Commands.ChatCommands.Add(new Command("", OnLevelInfo, "levelinfo", "lvl") { HelpText = "Type /levelinfo to view your level information." });
            Commands.ChatCommands.Add(new Command("", OnSkipTask, "skiptask") { HelpText = "Type /skiptask to skip your current quest." });
        }

        private void OnPostInitialize(EventArgs e)
        {
            UpdateTimer.Elapsed += OnUpdate;
            UpdateTimer.Start();
        }

        private static bool RequireLogin(TSPlayer ply)
        {
            if (Helpers.IsLoggedIn(ply)) return true;
            ply.SendErrorMessage("You must be logged in to use this command.");
            return false;
        }

        private static string TaskLabel(LevelTask task, int progress)
        {
            string label = task.Title;
            if (TaskType.HasCounter(task.Type))
                label += " (" + progress + "/" + task.TargetCount + ")";
            return label;
        }

        private void OnReload(CommandArgs args)
        {
            RpgState.ReloadConfig();
            args.Player.SendSuccessMessage("Hunger RPG config reloaded!");
        }

        private void OnToggleBuffs(CommandArgs args)
        {
            if (!RequireLogin(args.Player)) return;

            lock (RpgState.Players)
            {
                RpgPlayer rpg = RpgState.Players[args.Player.Index];
                if (rpg == null)
                {
                    args.Player.SendErrorMessage("You must be logged in to use this command.");
                    return;
                }

                rpg.BuffsPaused = !rpg.BuffsPaused;
                args.Player.SendSuccessMessage(rpg.BuffsPaused ? "Your level buffs are now paused!" : "Your level buffs are active again!");
                rpg.SaveToDb(false);
            }
        }

        private void OnToggleHud(CommandArgs args)
        {
            if (!RequireLogin(args.Player)) return;

            lock (RpgState.Players)
            {
                RpgPlayer rpg = RpgState.Players[args.Player.Index];
                if (rpg == null)
                {
                    args.Player.SendErrorMessage("You must be logged in to use this command.");
                    return;
                }

                rpg.HudHidden = !rpg.HudHidden;
                if (rpg.HudHidden)
                {
                    args.Player.SendSuccessMessage("Your live info display is now hidden!");
                    args.Player.SendData(PacketTypes.Status, "", 1, 1);
                }
                else
                {
                    args.Player.SendSuccessMessage("Your live info display is back!");
                }
                rpg.SaveToDb(false);
            }
        }

        private void OnSkipTask(CommandArgs args)
        {
            if (!RequireLogin(args.Player)) return;

            if (RpgState.Config.DisableTaskSkip)
            {
                args.Player.SendErrorMessage("This command has been disabled by the server.");
                return;
            }

            lock (RpgState.Players)
            {
                RpgPlayer rpg = RpgState.Players[args.Player.Index];
                if (rpg == null) return;

                if (rpg.IsMaxLevel())
                {
                    args.Player.SendErrorMessage("You have reached the level cap for this stage and cannot skip quests!");
                    return;
                }

                LevelTask task = rpg.CurrentTask;
                if (task == null)
                {
                    args.Player.SendErrorMessage("You have no quest to skip.");
                    return;
                }

                args.Player.SendSuccessMessage("Quest skipped: {0}", task.Title);
                rpg.TaskId++;
                rpg.TaskProgress = 0;
            }
        }

        private void OnCompleteTask(CommandArgs args)
        {
            if (!RequireLogin(args.Player)) return;

            lock (RpgState.Players)
            {
                RpgPlayer rpg = RpgState.Players[args.Player.Index];
                if (rpg == null) return;

                if (rpg.IsMaxLevel())
                {
                    args.Player.SendErrorMessage("You have reached the level cap for this stage and cannot complete quests!");
                    return;
                }

                LevelTask task = rpg.CurrentTask;
                if (task == null)
                {
                    args.Player.SendErrorMessage("You have no quest to complete.");
                    return;
                }

                if (TaskType.HasCounter(task.Type))
                {
                    if (rpg.TaskProgress < task.TargetCount)
                    {
                        args.Player.SendErrorMessage("You can't complete this quest yet - its requirements are not met!");
                        return;
                    }
                    FinishTask(args.Player, rpg, task);
                    return;
                }

                if (task.Type == TaskType.Wear)
                {
                    if (!args.TPlayer.armor.Any(p => p.type == task.TargetId))
                    {
                        args.Player.SendErrorMessage("You can't complete this quest yet - its requirements are not met!");
                        return;
                    }
                    FinishTask(args.Player, rpg, task);
                    return;
                }

                if (task.Type == TaskType.Own || task.Type == TaskType.Collect)
                {
                    for (int i = 0; i < 58; i++)
                    {
                        Item slot = args.TPlayer.inventory[i];
                        if (slot.type != task.TargetId) continue;

                        if (slot.stack < task.TargetCount)
                        {
                            args.Player.SendErrorMessage("You can't complete this quest - you don't have enough in a single stack!");
                            return;
                        }

                        if (task.Type == TaskType.Collect)
                        {
                            slot.stack -= task.TargetCount;
                            args.Player.SendData(PacketTypes.PlayerSlot, "", args.Player.Index, i, slot.prefix);
                        }

                        FinishTask(args.Player, rpg, task);
                        return;
                    }

                    args.Player.SendErrorMessage("You can't complete this quest - you don't have the required item!");
                    return;
                }

                args.Player.SendErrorMessage("You have no quest to complete.");
            }
        }

        private static void FinishTask(TSPlayer ply, RpgPlayer rpg, LevelTask task)
        {
            if (!string.IsNullOrEmpty(task.Story))
                ply.SendMessage(task.Story.Replace("{name}", ply.Name), Color.White);

            ply.SendSuccessMessage("Quest completed: {0}", task.Title);

            int exp = task.Exp;
            rpg.TaskId++;
            rpg.TaskProgress = 0;
            rpg.AddExp(exp);
        }

        private void OnItemLevel(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("Invalid syntax. Usage: /itemlevel <item name or ID>");
                return;
            }

            string itemName = string.Join(" ", args.Parameters);
            List<Item> matches = TShock.Utils.GetItemByIdOrName(itemName);

            if (matches.Count == 0)
            {
                args.Player.SendErrorMessage("That item does not exist!");
                return;
            }
            if (matches.Count > 1)
            {
                args.Player.SendMultipleMatchError(matches.Select(i => string.Format("[i:{0}]:{1}(ID{0})", i.type, i.Name)));
                return;
            }

            Item item = matches[0];
            if (item.type < 1 || item.type >= ItemID.Count)
            {
                args.Player.SendErrorMessage("Item {0} is invalid.", itemName);
                return;
            }

            string label = "[i:" + item.type + "]:" + item.Name;
            int rarity = Helpers.GetRarity(item);
            string levelTag = UiTheme.LevelTag(rarity);
            string kind;

            if (Helpers.IsDye(item)) kind = "Dye";
            else if (item.fishingPole > 0) kind = "Fishing rod";
            else if (Helpers.IsArmor(item)) kind = "Armor";
            else if (item.accessory || item.vanity) kind = "Accessory";
            else if (Helpers.IsTool(item)) kind = RpgState.Config.IgnorePrefixes ? "Tool" : "Unprefixed tool";
            else if (item.summon || item.ranged || item.magic || item.melee) kind = RpgState.Config.IgnorePrefixes ? "Weapon" : "Unprefixed weapon";
            else
            {
                args.Player.SendSuccessMessage("Item " + label + " has no level requirement.");
                return;
            }

            args.Player.SendMessage(kind + " " + label + " requires " + levelTag, Color.White);
        }

        private void OnLevelInfo(CommandArgs args)
        {
            if (!RequireLogin(args.Player)) return;

            lock (RpgState.Players)
            {
                RpgPlayer rpg = RpgState.Players[args.Player.Index];
                if (rpg == null)
                {
                    args.Player.SendErrorMessage("You must be logged in to use this command.");
                    return;
                }

                args.Player.SendMessage(BuildLevelInfo(args.Player, rpg), Color.White);
            }
        }

        private static string BuildLevelInfo(TSPlayer ply, RpgPlayer rpg)
        {
            var cfg = RpgState.Config;
            var lines = new List<string>();
            string hex = UiTheme.LevelHex(rpg.Level);
            int percent = Helpers.ExpPercent(rpg.Level, rpg.Exp);
            bool maxed = rpg.IsMaxLevel();

            lines.Add(UiTheme.Banner("HUNGER RPG", hex));
            lines.Add(UiTheme.Tag(hex, "Lv." + rpg.Level + " " + UiTheme.LevelTitle(rpg.Level)) + "   " + UiTheme.Tag(UiTheme.Muted, "EXP") + " " + rpg.Exp);
            lines.Add(UiTheme.Bar(percent, 20, hex) + " " + percent + "%");

            if (!maxed)
            {
                LevelTask task = rpg.CurrentTask;
                if (task != null) lines.Add(UiTheme.Row("Quest", TaskLabel(task, rpg.TaskProgress)));
            }

            if (maxed)
                lines.Add(UiTheme.Row("Kill EXP", UiTheme.Tag(UiTheme.Warn, "Level cap reached for this stage")));
            else if (rpg.IsDailyExpCapped())
                lines.Add(UiTheme.Row("Kill EXP", UiTheme.Tag(UiTheme.Warn, "Daily limit reached")));
            else
                lines.Add(UiTheme.Row("Kill EXP", "Mobs with " + Helpers.MinFullExpLife(rpg.Level) + "+ HP give full EXP" + rpg.DailyExpRemainingText()));

            if (cfg.EnableServerLevel)
            {
                int serverPercent = Helpers.ExpPercent(RpgState.ServerLevel, RpgState.ServerExp);
                lines.Add(UiTheme.Row("Server", UiTheme.LevelTag(RpgState.ServerLevel) + "  EXP " + RpgState.ServerExp + " (" + serverPercent + "%)"));

                if (RpgState.IsServerLevelCapped())
                    lines.Add(UiTheme.Row("Server EXP", "Server level cap reached for this stage"));
                else if (RpgState.IsServerDailyExpCapped())
                    lines.Add(UiTheme.Row("Server EXP", "Daily server EXP is full"));
                else
                    lines.Add(UiTheme.Row("Server EXP", "Kill monsters to add shared EXP" + RpgState.ServerDailyExpRemainingText()));
            }

            if (cfg.EnableTeams && rpg.TeamId > 0)
            {
                lines.Add(UiTheme.Row("Team", rpg.TeamName + "  (team Lv." + rpg.TeamLevel + ", contributed " + rpg.TeamExp + " EXP)"));

                if (rpg.TeamDayExp < cfg.TeamDailyMaxExp)
                    lines.Add(UiTheme.Row("Team EXP", "Kill monsters to add team EXP, " + (100 - rpg.TeamDayExp * 100 / cfg.TeamDailyMaxExp) + "% left today"));
                else
                    lines.Add(UiTheme.Row("Team EXP", "Daily contribution limit reached"));
            }

            lines.Add(UiTheme.Row("Position", ply.TileX + ", " + ply.TileY));

            var limits = new List<string>();
            if (cfg.EnableSummonLimit && ply.HasPermission(Permissions.summonboss) && !ply.HasPermission(Perms.IgnoreSummonLimit))
            {
                limits.Add("Boss summons: " + (rpg.BankedSummons + Helpers.MaxDailySummons(ply) - rpg.SummonsToday));
                if (!ply.HasPermission(Perms.IgnoreSummonTime) && rpg.OffHourSummons > 0)
                    limits.Add("Anytime summons: " + rpg.OffHourSummons);
            }
            if (cfg.EnableInvasionLimit && ply.HasPermission(Permissions.startinvasion) && !ply.HasPermission(Perms.IgnoreInvasionLimit))
                limits.Add("Invasions: " + (rpg.BankedInvasions + Helpers.MaxDailyInvasions(ply) - rpg.InvasionsToday));

            if (limits.Count > 0)
                lines.Add(UiTheme.Row("Charges", string.Join(" | ", limits)));

            return string.Join("\n", lines);
        }

        private void OnLeave(LeaveEventArgs e)
        {
            lock (RpgState.Players)
            {
                if (RpgState.Players[e.Who] == null) return;
                RpgState.Players[e.Who].SaveToDb(true);
                RpgState.Players[e.Who] = null;
            }
        }

        private void OnChat(ServerChatEventArgs args)
        {
            lock (RpgState.Players)
            {
                RpgPlayer rpg = RpgState.Players[args.Who];
                if (rpg == null || rpg.Level == -1) return;

                rpg.RefreshChatStyle();

                var group = TShock.Players[args.Who].Group;
                group.Prefix = UiTheme.Badge(rpg.Level) + " " + rpg.Prefix;
                group.ChatColor = rpg.ChatColor;
                group.Suffix = rpg.Suffix;
            }
        }

        private void OnGetData(GetDataEventArgs args)
        {
            if (args.Handled) return;

            TSPlayer user = TShock.Players[args.Msg.whoAmI];
            if (user == null || !user.ConnectionAlive) return;

            using (var data = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length))
            {
                try
                {
                    if (PacketHandlers.Handle(args.MsgID, user, data)) args.Handled = true;
                }
                catch (Exception ex)
                {
                    TShock.Log.Error("HungerRPG: error while forwarding packet: " + ex);
                }
            }
        }

        private void OnNpcSpawn(NpcSpawnEventArgs args)
        {
            if (args.Handled) return;
            if (!Main.npc[args.NpcId].active) return;

            lock (RpgState.Npcs)
                RpgState.Npcs[args.NpcId] = new LocalNpc(args.NpcId);
        }

        private void OnNpcKilled(NpcKilledEventArgs args)
        {
            lock (RpgState.Npcs)
            {
                LocalNpc record = RpgState.Npcs[args.npc.whoAmI];
                if (record == null) return;

                TSPlayer killer = null;
                int lastHit = args.npc.lastInteraction;
                if (lastHit >= 0 && lastHit != 255)
                    killer = TShock.Players[lastHit];

                if (args.npc.boss)
                    HandleBossKill(args.npc, record, killer);

                if (killer != null)
                    RewardKiller(args.npc, killer);

                RpgState.Npcs[args.npc.whoAmI] = null;
            }
        }

        private static void RewardKiller(NPC npc, TSPlayer killer)
        {
            lock (RpgState.Players)
            {
                RpgPlayer rpg = RpgState.Players[killer.Index];
                if (rpg == null) return;

                rpg.ProgressTask(TaskType.Kill, npc.type);
                rpg.AddKillExp(npc, 100);
                rpg.AddTeamExp(RpgState.Config.TeamKillExp);
                RpgState.AddServerExp();
            }
        }

        private static void HandleBossKill(NPC npc, LocalNpc record, TSPlayer killer)
        {
            var cfg = RpgState.Config;
            int seconds = (int)(DateTime.UtcNow - record.StartTime).TotalSeconds;
            Dictionary<string, int> damage = record.Damage;

            if (cfg.AnnounceBossKills)
            {
                string who = killer != null ? killer.Name + " finished off" : "Someone finished off";
                TShock.Utils.Broadcast(
                    string.Format("{0} [{1}]! {2} players fought in {3}s.", who, npc.FullName, damage.Count, seconds),
                    255, 255, 100);
            }

            if (cfg.LogBossKills && killer != null)
                TShock.Log.Info("{0} killed {1}", killer.Name, npc.FullName);

            if (damage.Count == 0) return;

            int total = 0;
            int others = 0;
            var entries = new List<DamageEntry>();
            foreach (var pair in damage)
            {
                if (killer == null || pair.Key != killer.Name) others += pair.Value;
                total += pair.Value;
                entries.Add(new DamageEntry(pair.Key, pair.Value));
            }

            entries = entries.OrderByDescending(e => e.Damage).ToList();

            var ranking = new List<string>();
            for (int i = 0; i < entries.Count; i++)
            {
                bool isKiller = killer != null && entries[i].Name == killer.Name;
                if (!isKiller && others > 0)
                    entries[i].Proportion = entries[i].Damage * 100 / others;

                if (i + 1 > cfg.DamageRankingSize) continue;

                int share = total > 0 ? entries[i].Damage * 100 / total : 0;
                string logLine = string.Format("{0}. {1} - {2}% ({3} damage)", i + 1, entries[i].Name, share, entries[i].Damage);
                if (cfg.LogDamageRanking) TShock.Log.Info(logLine);

                string rankHex = i == 0 ? "FACC15" : i == 1 ? "D1D5DB" : i == 2 ? "FB923C" : UiTheme.Muted;
                ranking.Add(UiTheme.Tag(rankHex, (i + 1) + ".") + " " + entries[i].Name + " - " + share + "% (" + entries[i].Damage + " damage)");
            }

            foreach (var ply in TShock.Players)
            {
                if (ply == null || !entries.Exists(e => e.Name == ply.Name)) continue;
                if (!cfg.AnnounceBossKills) continue;

                foreach (var line in ranking)
                    ply.SendInfoMessage(line);
            }

            foreach (var entry in entries)
            {
                bool isKiller = killer != null && entry.Name == killer.Name;
                if (isKiller) continue;

                TSPlayer ply = TShock.Players.FirstOrDefault(p => p != null && p.Name == entry.Name);
                if (ply == null) continue;

                lock (RpgState.Players)
                {
                    RpgPlayer rpg = RpgState.Players[ply.Index];
                    if (rpg != null) rpg.AddKillExp(npc, entry.Proportion);
                }
            }
        }

        private void OnNpcStrike(NpcStrikeEventArgs args)
        {
            if (args.Handled) return;

            TSPlayer user = TShock.Players[args.Player.whoAmI];
            if (user == null || args.Damage < 1 || !args.Npc.active) return;

            int damage = args.Damage;

            if (IsDamageViolation(user, args.Npc, damage))
            {
                args.Handled = true;
                return;
            }

            if (args.Critical) damage *= 2;

            if (args.Npc.boss && !TrackBossDamage(args, damage)) return;

            var cfg = RpgState.Config;
            if (cfg.ShowHitIndicator && args.Npc.life >= cfg.HitIndicatorMinLife && damage < args.Npc.life)
                ShowHitIndicator(user, args.Npc);
        }

        private static bool TrackBossDamage(NpcStrikeEventArgs args, int damage)
        {
            lock (RpgState.Npcs)
            {
                LocalNpc record = RpgState.Npcs[args.Npc.whoAmI];
                if (record == null || record.Dead) return false;

                string name = args.Player.name;
                int current;
                record.Damage.TryGetValue(name, out current);
                record.Damage[name] = current + damage;

                if (damage >= args.Npc.life) record.Dead = true;
            }
            return true;
        }

        private static void ShowHitIndicator(TSPlayer user, NPC npc)
        {
            lock (RpgState.Players)
            {
                RpgPlayer rpg = RpgState.Players[user.Index];
                if (rpg == null) return;

                DateTime now = DateTime.UtcNow;
                if ((now - rpg.LastStrikeTime).TotalMilliseconds < 900) return;
                rpg.LastStrikeTime = now;
            }

            int percent = npc.life * 100 / npc.lifeMax;
            string text = npc.FullName + "\u2595" + UiTheme.PlainBar(percent, 20) + "\u258F" + percent + "%";
            Helpers.Popup(user, text, UiTheme.FromHex(UiTheme.HealthHex(percent)));
        }

        private static bool IsDamageViolation(TSPlayer user, NPC npc, int damage)
        {
            var cfg = RpgState.Config;
            if (user.HasPermission(Perms.IgnoreDamageLimit) || cfg.DamageViolationLimit <= 0) return false;

            int dpsLimit = 0;
            int damageLimit = 0;
            int strikes = 0;
            int level = 0;
            long dps = 0;

            lock (RpgState.Players)
            {
                RpgPlayer rpg = RpgState.Players[user.Index];
                if (rpg != null)
                {
                    DateTime now = DateTime.UtcNow;
                    level = Helpers.GetEffectiveLevel(rpg);
                    dpsLimit = Helpers.GetDpsLimit(level);
                    damageLimit = Helpers.GetDamageLimit(level);

                    if ((now - rpg.DamageWindowStart).TotalMilliseconds > 60000)
                    {
                        rpg.DamageStrikes = 0;
                        rpg.DamageWindowStart = now;
                    }

                    if (dpsLimit > 0)
                    {
                        if ((now - rpg.DpsWindowStart).TotalMilliseconds < 1000)
                        {
                            rpg.Dps += damage;
                        }
                        else
                        {
                            rpg.Dps = damage;
                            rpg.DpsWindowStart = now;
                        }

                        if (rpg.Dps > dpsLimit) rpg.DamageStrikes++;
                    }

                    if (damageLimit > 0 && damage > damageLimit) rpg.DamageStrikes++;

                    strikes = rpg.DamageStrikes;
                    dps = rpg.Dps;
                }
            }

            if (strikes <= 0 || strikes < cfg.DamageViolationLimit) return false;

            TShock.Log.Info("{0} dealt out-of-tier damage {3} (DPS {4}) to {1}({2}); held item {5}({6}); applicable level {7}",
                user.Name, npc.FullName, npc.type, damage, dps, user.SelectedItem.Name, user.SelectedItem.type, level);

            string reason = string.Format("Player {0} exceeded the level {1} damage limit:", user.Name, level);
            if (damage > damageLimit && damageLimit > 0)
                reason += string.Format(" damage {0} > {1}", damage, damageLimit);
            if (dps > dpsLimit && dpsLimit > 0)
                reason += string.Format(" DPS {0} > {1}", dps, dpsLimit);

            user.Kick(reason, true, false, "Server");
            return true;
        }

        private void OnUpdate(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.Exchange(ref _updateBusy, 1) == 1) return;

            try
            {
                DateTime start = DateTime.Now;
                bool checkEquipment = false;

                if (_equipTick < 3)
                {
                    _equipTick++;
                }
                else
                {
                    _equipTick = 0;
                    checkEquipment = true;
                }

                foreach (TSPlayer user in TShock.Players)
                {
                    if (user == null) continue;

                    if ((DateTime.Now - start).TotalMilliseconds >= 500)
                    {
                        TShock.Log.Error("HungerRPG: update timed out, some processing was skipped!");
                        return;
                    }

                    if (!TickPlayer(user, checkEquipment)) continue;
                    if (RpgState.Config.NoHungerWhileSleeping && user.TPlayer.sleeping.isSleeping) continue;

                    ApplyHunger(user);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _updateBusy, 0);
            }
        }

        private static bool TickPlayer(TSPlayer user, bool checkEquipment)
        {
            var cfg = RpgState.Config;

            lock (RpgState.Players)
            {
                RpgPlayer rpg = RpgState.Players[user.Index];
                if (rpg == null) return false;

                if (rpg.DisableTicks < 5)
                {
                    rpg.ClearProjectiles();
                    rpg.DisableTicks++;
                }

                if (cfg.HudMode != 0 && !rpg.HudHidden)
                    SendHud(user, rpg);

                if (user.Dead || user.TPlayer.statLife < 1) return false;

                AwardTimedExp(rpg);

                if (checkEquipment) CheckEquipment(user, rpg);

                ApplyBuffs(user, rpg);

                if (rpg.Level <= cfg.HungerProtectionLevel) return false;

                if (rpg.HungerGraceSeconds > 0)
                {
                    rpg.HungerGraceSeconds--;
                    if (rpg.HungerGraceSeconds == 0 && cfg.EnableHunger && cfg.JoinWellFedMinutes > 0)
                        user.SetBuff(BuffID.WellFed, 3600 * cfg.JoinWellFedMinutes);
                    return false;
                }
            }
            return true;
        }

        private static void ApplyHunger(TSPlayer user)
        {
            if (!RpgState.Config.EnableHunger) return;

            int[] buffs = user.TPlayer.buffType;
            if (buffs.Contains(BuffID.WellFed) || buffs.Contains(BuffID.WellFed2) || buffs.Contains(BuffID.WellFed3) || buffs.Contains(BuffID.PotionSickness))
                return;

            user.SetBuff(BuffID.Suffocation, 60);
            Helpers.Popup(user, "Hunger is suffocating you!", UiTheme.HungerColor);
        }

        private static void AwardTimedExp(RpgPlayer rpg)
        {
            var cfg = RpgState.Config;

            if (rpg.OnlineExpTimer < cfg.OnlineExpIntervalSeconds)
            {
                rpg.OnlineExpTimer++;
                if (rpg.OnlineExpTimer >= cfg.OnlineExpIntervalSeconds)
                {
                    rpg.OnlineExpTimer = 0;
                    rpg.AddExp(rpg.Level, false);
                }
            }

            if (rpg.TeamExpTimer < cfg.OnlineTeamExpIntervalSeconds)
            {
                rpg.TeamExpTimer++;
                if (rpg.TeamExpTimer >= cfg.OnlineTeamExpIntervalSeconds)
                {
                    rpg.TeamExpTimer = 0;
                    rpg.AddTeamExp(rpg.Level);
                }
            }
        }

        private static void ApplyBuffs(TSPlayer user, RpgPlayer rpg)
        {
            if (rpg.BuffsPaused) return;

            foreach (int buff in rpg.Buffs)
                if (buff > 0) user.SetBuff(buff, 333);

            foreach (int buff in rpg.TeamBuffs)
                if (buff > 0 && !rpg.Buffs.Contains(buff)) user.SetBuff(buff, 333);
        }

        private static void SendHud(TSPlayer user, RpgPlayer rpg)
        {
            var cfg = RpgState.Config;
            bool maxed = rpg.IsMaxLevel();

            string header = "Hunger RPG info:";
            if (!maxed)
            {
                LevelTask task = rpg.CurrentTask;
                if (task != null) header = "Quest: " + TaskLabel(task, rpg.TaskProgress);
            }

            int percent = Helpers.ExpPercent(rpg.Level, rpg.Exp);
            string status;
            if (maxed) status = "Level cap reached for this stage";
            else if (rpg.IsDailyExpCapped()) status = "Daily kill EXP is full";
            else status = "Min mob HP: " + Helpers.MinFullExpLife(rpg.Level) + rpg.DailyExpRemainingText();

            var lines = new List<string>
            {
                header,
                "Position: " + user.TileX + "," + user.TileY,
                "Level: " + rpg.Level + "  EXP: " + rpg.Exp + " " + UiTheme.PlainBar(percent, 10) + " " + percent + "%",
                status
            };

            string text;
            if (cfg.HudMode == 1)
                text = string.Join(new string(' ', 75) + "\n", lines);
            else if (cfg.HudMode == 2)
                text = new string('\n', 19) + string.Join("\n", lines);
            else
                text = string.Join("\n", lines);

            user.SendData(PacketTypes.Status, text, 0, 1);
        }

        private static void CheckEquipment(TSPlayer user, RpgPlayer rpg)
        {
            int level = Helpers.GetEffectiveLevel(rpg);
            int slotCount = user.TPlayer.armor.Length;
            bool blocked = false;

            if (!user.HasPermission(Perms.IgnoreDyeLevel))
            {
                blocked = CheckDyes(user, rpg, user.TPlayer.dye, "dye", "Equipped over-level dye");
                if (!blocked)
                    blocked = CheckDyes(user, rpg, user.TPlayer.miscDyes, "equipment dye", "Equipped over-level equipment dye");
            }

            if (!blocked && !user.HasPermission(Perms.IgnoreArmorLevel))
                blocked = CheckEquipSlots(user, rpg, 0, 3, level, "armor", "Wore over-level armor", true);

            if (!blocked && !user.HasPermission(Perms.IgnoreEquipLevel))
                blocked = CheckEquipSlots(user, rpg, 3, 10, rpg.Level, "accessory", "Equipped over-level accessory", true);

            if (!blocked && !user.HasPermission(Perms.IgnoreVanityLevel))
                CheckEquipSlots(user, rpg, 10, slotCount, rpg.Level, "vanity item", "Equipped over-level vanity item", false);
        }

        private static bool CheckDyes(TSPlayer user, RpgPlayer rpg, Item[] dyes, string noun, string reason)
        {
            foreach (Item dye in dyes)
            {
                if (dye == null || dye.type == 0) continue;

                int rarity = Helpers.GetRarity(dye);
                if (rarity <= rpg.Level) continue;

                user.SendErrorMessage("You are " + UiTheme.LevelTag(rpg.Level) + " and cannot wear a " + UiTheme.LevelTag(rarity) + " " + noun + " [i:" + dye.type + "]. Please remove it!");
                user.Disable(reason);
                rpg.DisableTicks = 0;
                return true;
            }
            return false;
        }

        private static bool CheckEquipSlots(TSPlayer user, RpgPlayer rpg, int from, int to, int level, string noun, string reason, bool notifyOthers)
        {
            Item[] slots = user.TPlayer.armor;
            int end = Math.Min(to, slots.Length);

            for (int i = from; i < end; i++)
            {
                Item worn = slots[i];
                if (worn == null || worn.type == 0) continue;

                Item item = Helpers.CreateLevelItem(worn.type, 0);
                int rarity = Helpers.GetRarity(item);
                if (rarity <= level) continue;

                if (RpgState.Config.LogEquipViolations)
                    TShock.Log.Info("{0}: {1} ({2})", user.Name, reason, worn.Name);

                user.SendErrorMessage("You are " + UiTheme.LevelTag(level) + " and cannot wear a " + UiTheme.LevelTag(rarity) + " " + noun + " [i:" + worn.type + "]. Please remove it!");
                user.Disable(reason);
                rpg.DisableTicks = 0;

                if (notifyOthers && RpgState.Config.NotifyOverLevelUse)
                {
                    foreach (var other in TShock.Players)
                    {
                        if (other == null || other.Name == user.Name) continue;
                        other.SendInfoMessage("Player {0} tried to equip an over-level {1}: {2}", user.Name, noun, item.Name);
                    }
                }
                return true;
            }
            return false;
        }
    }
}
