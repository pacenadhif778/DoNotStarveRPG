using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Streams;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Models;
using TShockAPI.Models.PlayerUpdate;
using TShockAPI.Net;

namespace HungerRpg
{
    public delegate bool PacketHandlerDelegate(PacketArgs args);

    public class PacketArgs : EventArgs
    {
        public TSPlayer Player { get; private set; }
        public MemoryStream Data { get; private set; }
        public Player TPlayer { get { return Player.TPlayer; } }

        public PacketArgs(TSPlayer player, MemoryStream data)
        {
            Player = player;
            Data = data;
        }
    }

    public static class PacketHandlers
    {
        private static readonly int[] InvasionItems = { -1, -2, -3, -4, -5, -6, -7, -8, -10, -11 };
        private static readonly int[] BossItems = { 4, 13, 50, 125, 126, 134, 127, 128, 131, 129, 130, 222, 245, 266, 370, 657 };

        private static Dictionary<PacketTypes, PacketHandlerDelegate> _handlers;

        public static void Init()
        {
            _handlers = new Dictionary<PacketTypes, PacketHandlerDelegate>
            {
                { PacketTypes.PlayerSpawn, OnPlayerSpawn },
                { PacketTypes.PlayerDeathV2, OnPlayerDeath },
                { PacketTypes.PlayerUpdate, OnPlayerUpdate },
                { PacketTypes.PlayerSlot, OnPlayerSlot },
                { PacketTypes.NpcTalk, OnNpcTalk },
                { PacketTypes.CompleteAnglerQuest, OnCompleteAnglerQuest },
                { PacketTypes.SpawnBossorInvasion, OnSpawnBoss },
                { PacketTypes.FishOutNPC, OnFishOutNpc },
                { PacketTypes.CrystalInvasionStart, OnOldOnesArmy }
            };
        }

        public static bool Handle(PacketTypes type, TSPlayer player, MemoryStream data)
        {
            PacketHandlerDelegate handler;
            if (_handlers.TryGetValue(type, out handler))
            {
                try
                {
                    return handler(new PacketArgs(player, data));
                }
                catch (Exception ex)
                {
                    TShock.Log.Error("HungerRPG: error while handling packet: " + ex);
                }
            }
            return false;
        }

        private static bool TryBlockInvasion(TSPlayer ply)
        {
            var cfg = RpgState.Config;

            if (!ply.HasPermission(Perms.IgnoreInvasionTime) && !ply.HasPermission(Perms.IgnoreInvasionLimit))
            {
                string restriction = Helpers.GetTimeRestriction("start an invasion",
                    cfg.DailyInvasionStartMinute, cfg.DailyInvasionEndMinute,
                    cfg.WeekendInvasionStartMinute, cfg.WeekendInvasionEndMinute,
                    cfg.DailySummonStartMinute, cfg.DailySummonEndMinute);

                if (restriction != null)
                {
                    ply.SendErrorMessage(restriction);
                    return true;
                }
            }

            if (ply.HasPermission(Perms.IgnoreInvasionLimit)) return false;

            lock (RpgState.Players)
            {
                RpgPlayer rpg = RpgState.Players[ply.Index];
                if (rpg == null) return false;

                int max = Helpers.MaxDailyInvasions(ply);
                if (rpg.InvasionsToday >= max)
                {
                    if (rpg.BankedInvasions <= 0)
                    {
                        ply.SendErrorMessage("You don't have enough invasion charges.");
                        return true;
                    }
                    rpg.BankedInvasions--;
                }
                else
                {
                    rpg.InvasionsToday++;
                }

                rpg.SaveToDb(false);
                if (rpg.InvasionsToday >= max && rpg.BankedInvasions <= 0)
                    ply.SendInfoMessage("You have used up all your invasion charges.");
            }
            return false;
        }

        private static bool TryBlockSummon(TSPlayer ply)
        {
            var cfg = RpgState.Config;
            string restriction = null;

            if (!ply.HasPermission(Perms.IgnoreSummonTime))
            {
                restriction = Helpers.GetTimeRestriction("summon bosses",
                    cfg.DailySummonStartMinute, cfg.DailySummonEndMinute,
                    cfg.WeekendSummonStartMinute, cfg.WeekendSummonEndMinute,
                    cfg.DailySummonStartMinute, cfg.DailySummonEndMinute);
            }

            if (ply.HasPermission(Perms.IgnoreSummonLimit)) return false;

            lock (RpgState.Players)
            {
                RpgPlayer rpg = RpgState.Players[ply.Index];
                if (rpg == null) return false;

                if (restriction != null && rpg.OffHourSummons <= 0)
                {
                    ply.SendErrorMessage(restriction);
                    return true;
                }

                int max = Helpers.MaxDailySummons(ply);
                if (rpg.SummonsToday >= max)
                {
                    if (rpg.BankedSummons <= 0)
                    {
                        ply.SendErrorMessage("You don't have enough summon charges.");
                        return true;
                    }
                    rpg.BankedSummons--;
                }
                else
                {
                    rpg.SummonsToday++;
                }

                if (restriction != null) rpg.OffHourSummons--;

                rpg.SaveToDb(false);
                if (rpg.SummonsToday >= max && rpg.BankedSummons <= 0)
                    ply.SendInfoMessage("You have used up all your summon charges.");
            }
            return false;
        }

        private static bool OnOldOnesArmy(PacketArgs args)
        {
            if (args.Player.IsBouncerThrottled()) return true;

            if (RpgState.Config.EnableInvasionLimit && args.Player.HasPermission(Permissions.startdd2))
                return TryBlockInvasion(args.Player);

            return false;
        }

        private static bool OnFishOutNpc(PacketArgs args)
        {
            args.Data.ReadUInt16();
            args.Data.ReadUInt16();
            short npcType = args.Data.ReadInt16();

            if (RpgState.Config.EnableSummonLimit && npcType == NPCID.DukeFishron && args.Player.HasPermission(Permissions.summonboss))
                return TryBlockSummon(args.Player);

            return false;
        }

        private static bool OnSpawnBoss(PacketArgs args)
        {
            if (args.Player.IsBouncerThrottled()) return true;

            short player = args.Data.ReadInt16();
            short thingType = args.Data.ReadInt16();

            if (player != args.Player.Index) return true;

            if (thingType == 125 || thingType == 126)
                if (Main.npc.Any(n => n != null && n.active && (n.type == 125 || n.type == 126))) return false;

            if (thingType == 128 || thingType == 131 || thingType == 129 || thingType == 130)
                if (Main.npc.Any(n => n != null && n.active && n.type == 127)) return false;

            var cfg = RpgState.Config;

            if (cfg.EnableSummonLimit && BossItems.Contains(thingType) && args.Player.HasPermission(Permissions.summonboss))
                return TryBlockSummon(args.Player);

            if (cfg.EnableInvasionLimit && InvasionItems.Contains(thingType) && args.Player.HasPermission(Permissions.startinvasion))
                return TryBlockInvasion(args.Player);

            return false;
        }

        private static bool OnCompleteAnglerQuest(PacketArgs args)
        {
            if (!RpgState.Config.EnableAnglerQuestExp) return false;

            lock (RpgState.Players)
            {
                RpgPlayer rpg = RpgState.Players[args.Player.Index];
                if (rpg != null) rpg.AddExp(rpg.Level);
            }
            return false;
        }

        private static bool OnNpcTalk(PacketArgs args)
        {
            args.Data.ReadByte();
            short npcId = args.Data.ReadInt16();
            if (npcId < 0) return false;

            NPC npc = Main.npc[npcId];
            if (npc == null) return false;

            lock (RpgState.Players)
            {
                RpgPlayer rpg = RpgState.Players[args.Player.Index];
                if (rpg != null) rpg.ProgressTask(TaskType.Talk, npc.type);
            }
            return false;
        }

        private static bool OnPlayerSpawn(PacketArgs args)
        {
            var cfg = RpgState.Config;

            args.Data.ReadByte();
            args.Data.ReadInt16();
            args.Data.ReadInt16();
            int respawnTimer = args.Data.ReadInt32();
            args.Data.ReadInt16();
            args.Data.ReadInt16();
            PlayerSpawnContext context = (PlayerSpawnContext)args.Data.ReadByte();

            if (context == PlayerSpawnContext.ReviveFromDeath && respawnTimer <= 0)
            {
                if (cfg.EnableHunger && cfg.RespawnWellFedMinutes > 0)
                    args.Player.SetBuff(BuffID.WellFed, 3600 * cfg.RespawnWellFedMinutes);
            }
            else if (context == PlayerSpawnContext.SpawningIntoWorld)
            {
                PlayerRecord record = PlayerStore.Load(args.Player);
                lock (RpgState.Players)
                    RpgState.Players[args.Player.Index] = new RpgPlayer(args.Player.Index, record);
            }

            return false;
        }

        private static bool OnPlayerDeath(PacketArgs args)
        {
            args.Data.ReadByte();
            PlayerDeathReason reason = PlayerDeathReason.FromReader(new BinaryReader(args.Data));
            args.Data.ReadInt16();
            args.Data.ReadByte();
            BitsByte bits = (BitsByte)args.Data.ReadByte();

            if (bits[0])
            {
                HandlePvpDeath(args, reason._sourcePlayerIndex);
                return false;
            }

            lock (RpgState.Players)
            {
                RpgPlayer rpg = RpgState.Players[args.Player.Index];
                if (rpg != null) rpg.ProgressTask(TaskType.Die);
            }
            return false;
        }

        private static void HandlePvpDeath(PacketArgs args, int winnerIndex)
        {
            if (winnerIndex < 0 || winnerIndex >= TShock.Players.Length) return;

            TSPlayer winner = TShock.Players[winnerIndex];
            if (winner == null || winner.Name == args.Player.Name) return;

            var cfg = RpgState.Config;

            lock (RpgState.Players)
            {
                RpgPlayer winnerRpg = RpgState.Players[winner.Index];
                RpgPlayer loserRpg = RpgState.Players[args.Player.Index];
                if (winnerRpg == null || loserRpg == null) return;

                if (winnerRpg.Level <= cfg.PvpProtectionLevel || loserRpg.Level <= cfg.PvpProtectionLevel) return;
                if (Math.Abs(winnerRpg.Level - loserRpg.Level) > cfg.PvpProtectionLevelGap) return;

                int winExp = cfg.PvpWinExp;
                int lossExp = cfg.PvpLossExp;

                if (Main.bloodMoon)
                {
                    winExp *= cfg.BloodMoonPvpMultiplier;
                    lossExp *= cfg.BloodMoonPvpMultiplier;
                }

                winnerRpg.AddExp(winExp);
                loserRpg.AddExp(lossExp);
            }
        }

        private static void NotifyOthers(TSPlayer ply, string text)
        {
            foreach (var other in TShock.Players)
            {
                if (other == null || other.Name == ply.Name) continue;
                other.SendInfoMessage(text);
            }
        }

        private static bool OnPlayerUpdate(PacketArgs args)
        {
            args.Data.ReadInt8();
            ControlSet controls = new ControlSet((BitsByte)args.Data.ReadByte());
            args.Data.ReadByte();
            args.Data.ReadByte();
            args.Data.ReadByte();
            byte selectedItem = args.Data.ReadInt8();

            var cfg = RpgState.Config;
            Item held = args.Player.TPlayer.inventory[selectedItem];
            Item item = Helpers.CreateLevelItem(held.type, held.prefix);
            if (item.type == 0) return false;

            lock (RpgState.Players)
            {
                RpgPlayer rpg = RpgState.Players[args.Player.Index];
                if (rpg == null) return false;

                bool switched = false;
                if (rpg.SelectedItem != selectedItem && selectedItem != 58)
                {
                    rpg.SelectedItem = selectedItem;
                    switched = true;
                }

                int rarity = Helpers.GetRarity(item);
                int level = Helpers.GetEffectiveLevel(rpg);

                if (rarity > level && !args.Player.HasPermission(Perms.IgnoreUseLevel))
                {
                    string kind = Helpers.GetUsageKind(item);
                    string name = Helpers.PrefixedName(item);

                    if (controls.IsUsingItem)
                    {
                        if (cfg.LogOverLevelUse)
                            TShock.Log.Info("{0} used over-level item {1}", args.Player.Name, name);

                        if (kind != null)
                        {
                            args.Player.SendErrorMessage("You are " + UiTheme.LevelTag(level) + " and cannot use a " + UiTheme.LevelTag(rarity) + " " + kind + ": " + name);

                            if (cfg.NotifyOverLevelUse)
                                NotifyOthers(args.Player, string.Format("Player {0} tried to use an over-level item: {1}", args.Player.Name, name));

                            args.Player.Disable("Used over-level item");
                            rpg.DisableTicks = 0;
                            return true;
                        }
                    }
                    else if (switched && kind != null)
                    {
                        args.Player.SendErrorMessage("You switched to a " + UiTheme.LevelTag(rarity) + " " + kind + " [" + name + "]. You are " + UiTheme.LevelTag(level) + " - please don't use it!");
                    }
                }

                if (controls.IsUsingItem)
                    rpg.ProgressTask(TaskType.Use, item.type);
            }

            return false;
        }

        private static bool OnPlayerSlot(PacketArgs args)
        {
            args.Data.ReadInt8();
            short slot = args.Data.ReadInt16();
            args.Data.ReadInt16();
            byte prefix = args.Data.ReadInt8();
            short type = args.Data.ReadInt16();

            if (slot != 58) return false;

            Item item = Helpers.CreateLevelItem(type, prefix);
            if (item.type == 0) return false;

            TSPlayer ply = args.Player;

            lock (RpgState.Players)
            {
                RpgPlayer rpg = RpgState.Players[ply.Index];
                if (rpg == null) return false;

                int rarity = Helpers.GetRarity(item);
                int level = Helpers.GetEffectiveLevel(rpg);
                string name = Helpers.PrefixedName(item);
                string rarityTag = UiTheme.LevelTag(rarity);

                if (rarity > rpg.Level)
                {
                    string own = "You are holding a " + rarityTag + " ";
                    string tail = " but you are only " + UiTheme.LevelTag(rpg.Level) + ". ";

                    if (Helpers.IsDye(item))
                    {
                        if (!ply.HasPermission(Perms.IgnoreDyeLevel))
                            ply.SendErrorMessage(own + "dye [" + item.Name + "]" + tail + "Don't equip it!");
                    }
                    else if (Helpers.IsArmor(item) && !ply.HasPermission(Perms.IgnoreVanityLevel))
                    {
                        ply.SendErrorMessage(own + "vanity item [" + item.Name + "]" + tail + "Don't put it in a vanity slot!");
                    }
                    else if ((item.accessory || item.vanity) && !ply.HasPermission(Perms.IgnoreEquipLevel))
                    {
                        ply.SendErrorMessage(own + "accessory [" + item.Name + "]" + tail + "Don't equip it!");
                    }
                    else if ((item.accessory || item.vanity) && !ply.HasPermission(Perms.IgnoreVanityLevel))
                    {
                        ply.SendErrorMessage(own + "vanity item [" + item.Name + "]" + tail + "Don't put it in a vanity slot!");
                    }
                }

                if (rarity > level)
                {
                    string own = "You are holding a " + rarityTag + " ";
                    string tail = " but your applicable level is " + UiTheme.LevelTag(level) + ". ";

                    if (item.fishingPole > 0 && !ply.HasPermission(Perms.IgnoreUseLevel))
                        ply.SendErrorMessage(own + "fishing rod [" + item.Name + "]" + tail + "Don't use it!");
                    else if (Helpers.IsArmor(item) && !ply.HasPermission(Perms.IgnoreArmorLevel))
                        ply.SendErrorMessage(own + "armor piece [" + item.Name + "]" + tail + "Don't equip it!");
                    else if (Helpers.IsTool(item) && !ply.HasPermission(Perms.IgnoreUseLevel))
                        ply.SendErrorMessage(own + "tool [" + name + "]" + tail + "Don't use it!");
                    else if (Helpers.IsWeapon(item) && !ply.HasPermission(Perms.IgnoreUseLevel))
                        ply.SendErrorMessage(own + "weapon [" + name + "]" + tail + "Don't use it!");
                }
            }

            return false;
        }
    }
}
