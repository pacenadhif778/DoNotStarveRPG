# Hunger RPG

A TShock plugin for Terraria that adds a level/EXP progression system, hunger, quests and anti-cheat limits on top of the vanilla game.

> Original author: GK. This repository is an English refactor of the original Chinese-language source.

## Features

- **Levels and EXP**: gain EXP from kills, online time, quests, angler quests and PVP. Level caps and daily EXP caps depend on progression (pre-Hardmode, Hardmode, post-Golem).
- **Server (shared) level**: every kill also feeds a server-wide level with its own caps.
- **Hunger**: players starve unless they keep a Well Fed buff active. Low levels, sleeping players and the first seconds after joining are protected.
- **Item level requirements**: items, armor, accessories, vanity and dyes need a minimum level (item rarity x multiplier). Over-level gear is blocked and optionally logged.
- **Quests**: configurable per-level quest chains (own, kill, use, wear, collect, talk, die) with story text and EXP rewards.
- **Summon and invasion limits**: daily charges, stackable charges, and time windows (separate weekend windows).
- **Damage anti-cheat**: per-level damage and DPS limits; players who exceed them repeatedly are kicked.
- **Boss tracking**: kill announcements and a damage ranking, with EXP split by contribution.
- **PVP EXP**: with level protection, level-gap rules and a Blood Moon multiplier.
- **Optional teams**: team EXP, team buffs and team chat prefix.
- **UI**: colored level badge in chat, nine level tiers (Novice to Mythic), colored EXP bars in `/levelinfo`, health-colored hit indicator, and a live info HUD.

## Requirements

- Terraria server running TShock (plugin API version 2.1)
- SQLite or MySQL (whatever TShock is configured to use)
- Build references: `TShockAPI`, `OTAPI`, `TerrariaServer`, `MySql.Data`, `Newtonsoft.Json`

## Installation

1. Build the project against the TShock version you run, using that version's target framework.
2. Copy the resulting `.dll` into `ServerPlugins/`.
3. Start the server once. These files are created in the `tshock/` folder:
   - `HungerRPG.json` - plugin configuration
   - `ServerExp.txt` - server level and EXP state
4. Edit `HungerRPG.json`, then run `/reloadrpg` (or restart).

## Project layout

| File | Purpose |
|---|---|
| `Plugin.cs` | Plugin entry point, hooks, commands, HUD, hunger, NPC kill/strike logic |
| `PacketHandlers.cs` | Packet handlers: spawn, death, slot, item use, summons, invasions |
| `Models.cs` | `RpgPlayer`, `PlayerRecord`, `LevelTask`, `LevelDamageLimit`, `TaskType` |
| `RpgState.cs` | Shared state, server level, database tables |
| `PlayerStore.cs` | Loads and creates player rows in the database |
| `Helpers.cs` | EXP formulas, cap checks, time windows, permission constants (`Perms`) |
| `UiTheme.cs` | Level tier colors, chat tags, progress bars |
| `ConfigFile.cs` | Configuration fields and JSON read/write |
| `MIGRATION.md` | Old-to-new name mapping for config keys, permissions, commands and database |

## Commands

| Command | Description |
|---|---|
| `/levelinfo` (alias `/lvl`) | Show level, EXP bar, quest, server level, team and remaining charges |
| `/completetask` | Complete your current quest |
| `/skiptask` | Skip your current quest (can be disabled in config) |
| `/togglebuffs` | Pause or resume level buffs |
| `/togglehud` | Hide or show the live info display |
| `/itemlevel <name or ID>` | Show the level an item requires |
| `/reloadrpg` | Reload the configuration (needs `hungerrpg.reload`) |

## Permissions

| Permission | Effect |
|---|---|
| `hungerrpg.reload` | Use `/reloadrpg` |
| `hungerrpg.ignore.summonlimit` / `.summontime` | Ignore summon charges / summon time window |
| `hungerrpg.ignore.invasionlimit` / `.invasiontime` | Ignore invasion charges / invasion time window |
| `hungerrpg.ignore.damagelimit` | Ignore damage anti-cheat |
| `hungerrpg.ignore.dyelevel` / `.armorlevel` / `.equiplevel` / `.vanitylevel` / `.uselevel` | Ignore level requirements for that item type |
| `hungerrpg.customaffix` | Show custom chat prefix, color and suffix |
| `hungerrpg.dailysummon.N` | Override daily summon count with `N` |
| `hungerrpg.dailyinvasion.N` | Override daily invasion count with `N` |
| `hungerrpg.summoncap.N` | Override the summon stack cap with `N` |
| `hungerrpg.invasioncap.N` | Override the invasion stack cap with `N` |

## Configuration

`HungerRPG.json` is generated with defaults. The most important groups of settings:

| Group | Keys |
|---|---|
| Hunger | `EnableHunger`, `HungerDelay`, `HungerProtectionLevel`, `JoinWellFedMinutes`, `RespawnWellFedMinutes`, `NoHungerWhileSleeping` |
| Level caps | `MaxLevel`, `PreHardmodeMaxLevel`, `GolemMaxLevel` (`-1` = unlimited) |
| Daily EXP caps | `DailyMaxExp`, `PreHardmodeDailyMaxExp`, `GolemDailyMaxExp`, `UnlimitedExpLevel` |
| Server level | `EnableServerLevel`, `Server*` caps |
| Items | `RarityMultiplier`, `RarityOverrides`, `IgnorePrefixes` |
| Limits | `EnableSummonLimit`, `EnableInvasionLimit`, daily counts, stack caps, time windows in minutes since midnight |
| Anti-cheat | `DamageViolationLimit`, `LevelDamageLimits` |
| Quests | `LevelTasks`, `DisableTaskSkip` |
| PVP | `PvpWinExp`, `PvpLossExp`, `PvpProtectionLevel`, `PvpProtectionLevelGap`, `BloodMoonPvpMultiplier` |
| Display | `HudMode` (0-3), `ShowHitIndicator`, `HitIndicatorMinLife` |
| Teams | `EnableTeams`, `TeamDailyMaxExp`, `TeamKillExp` |

Quest types are numeric: `0` own, `1` kill, `2` use, `3` wear, `4` collect, `5` talk, `6` die.

## Database

Tables are created automatically: `HungerRPG` (one row per account) and, when teams are enabled, `Teams`.
