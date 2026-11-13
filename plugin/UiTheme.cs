using System;
using System.Globalization;
using Microsoft.Xna.Framework;

namespace HungerRpg
{
    internal static class UiTheme
    {
        public const string Muted = "9CA3AF";
        public const string Good = "4ADE80";
        public const string Warn = "FACC15";
        public const string Bad = "F87171";
        public const string Accent = "67E8F9";

        public static readonly Color ExpGainColor = new Color(255, 230, 130);
        public static readonly Color ExpLossColor = new Color(150, 130, 130);
        public static readonly Color HungerColor = new Color(95, 150, 160);

        private sealed class Tier
        {
            public readonly int MinLevel;
            public readonly string Hex;
            public readonly string Title;

            public Tier(int minLevel, string hex, string title)
            {
                MinLevel = minLevel;
                Hex = hex;
                Title = title;
            }
        }

        private static readonly Tier[] Tiers =
        {
            new Tier(0, "B0B8C4", "Novice"),
            new Tier(10, "4ADE80", "Apprentice"),
            new Tier(20, "22D3EE", "Adept"),
            new Tier(30, "60A5FA", "Veteran"),
            new Tier(40, "A78BFA", "Elite"),
            new Tier(50, "F472B6", "Champion"),
            new Tier(60, "FB923C", "Master"),
            new Tier(75, "FACC15", "Legend"),
            new Tier(90, "FF4D6D", "Mythic")
        };

        private static Tier GetTier(int level)
        {
            Tier result = Tiers[0];
            foreach (var tier in Tiers)
                if (level >= tier.MinLevel) result = tier;
            return result;
        }

        public static string LevelHex(int level)
        {
            return GetTier(level).Hex;
        }

        public static string LevelTitle(int level)
        {
            return GetTier(level).Title;
        }

        public static Color LevelColor(int level)
        {
            return FromHex(LevelHex(level));
        }

        public static Color FromHex(string hex)
        {
            int value = int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return new Color((value >> 16) & 255, (value >> 8) & 255, value & 255);
        }

        public static string Tag(string hex, string text)
        {
            return "[c/" + hex + ":" + text + "]";
        }

        public static string LevelTag(int level)
        {
            return Tag(LevelHex(level), "Lv." + level);
        }

        public static string Badge(int level)
        {
            return Tag(LevelHex(level), "LV" + level);
        }

        public static string Banner(string title, string hex)
        {
            return Tag(hex, "======== " + title + " ========");
        }

        public static string Row(string label, string value)
        {
            return Tag(Muted, label + ":") + " " + value;
        }

        public static string HealthHex(int percent)
        {
            if (percent > 60) return Good;
            if (percent > 30) return Warn;
            return Bad;
        }

        public static string PlainBar(int percent, int width)
        {
            int filled = FilledCount(percent, width);
            return new string('\u25AE', filled) + new string('\u25AF', width - filled);
        }

        public static string Bar(int percent, int width, string hex)
        {
            int filled = FilledCount(percent, width);
            string bar = "";
            if (filled > 0) bar += Tag(hex, new string('\u25AE', filled));
            if (filled < width) bar += Tag(Muted, new string('\u25AF', width - filled));
            return bar;
        }

        private static int FilledCount(int percent, int width)
        {
            percent = Math.Max(0, Math.Min(100, percent));
            return (percent * width + 99) / 100;
        }
    }
}
