using System;
using System.Collections.Generic;
using System.Text;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("RustStormWelcome", "Milestorme", "1.8.0")]
    [Description("Shows a premium branded welcome popup on join and provides /info to reopen it.")]
    public class RustStormWelcome : RustPlugin
    {
        private const string UI = "RSW_UI";
        private const string CLOSE = "rsw.close";

        private static readonly List<string> DefaultCommands = new List<string>
        {
            "/outpost or /bandit to teleport there",
            "/home to see options",
            "/up 1,2,3 or 4 to upgrade your entire base",
            "/down 1,2,3 or 4 to downgrade your entire base",
            "/skills to view skill tree",
            "/clanhelp to view options",
            "/br for base repair options",
            "/ad to disable or re-enable auto doors",
            "/info to reopen this panel"
        };

        private static readonly List<string> DefaultRules = new List<string>
        {
            "Be respectful in chat and on Discord",
            "No cheating, exploiting, or ban evasion",
            "Follow team-size limits for your server type",
            "Read the full rules in Discord before playing"
        };

        private Configuration config;
        private readonly Dictionary<ulong, Timer> uiRefreshTimers = new Dictionary<ulong, Timer>();

        public class Configuration
        {
            public float WelcomeDelaySeconds = 5f;
            public float PopupDurationSeconds = 20f;
            public bool ShowPopupOnJoin = true;
            public bool ShowChatReminderOnJoin = true;

            public string ServerName = "RustStorm";
            public string DiscordLink = "https://discord.gg/KvsxMpQskd";
            public string BannerUrl = "https://i.ibb.co/9HGbDcWT/Chat-GPT-Image-Mar-31-2026-08-24-03-PM.png";

            public ServerInfoSettings ServerInfo = new ServerInfoSettings();
            public RulesSettings Rules = new RulesSettings();
            public WipeTimerSettings WipeTimer = new WipeTimerSettings();
            public UISettings UI = new UISettings();
        }

        public class ServerInfoSettings
        {
            public string WelcomeLine = "Welcome to RustStorm";
            public string ServerType = "5x | Solo/Duo/Trio";
            public string WipeSchedule = "Map wipes every Friday 3:00 AM GMT+8";
            public string Rates = "5x gather with BetterLoot";
            public List<string> Commands = null;
        }

        public class RulesSettings
        {
            public List<string> SummaryLines = null;
        }

        public class WipeTimerSettings
        {
            public bool EnableDynamicWipeTimer = true;
            public string TimeZoneId = "Asia/Singapore";
            public string DisplayName = "GMT+8";
            public string WipeDay = "Friday";
            public int WipeHour24 = 3;
            public int WipeMinute = 0;
        }

        public class UISettings
        {
            public string OverlayColor = "0.01 0.01 0.02 0.82";
            public string PanelColor = "0.04 0.06 0.10 0.94";
            public string OuterBorderColor = "0.95 0.35 0.08 0.95";
            public string TopGlowColor = "0.16 0.68 1.00 0.98";
            public string TitleColor = "0.98 0.35 0.06 1.00";
            public string DiscordColor = "0.67 0.45 1.00 1.00";
            public string TextColor = "0.95 0.97 1.00 1.00";
            public string MutedTextColor = "0.72 0.78 0.86 1.00";
            public string SectionPanelColor = "0.09 0.12 0.18 0.84";
            public string SectionHeaderColor = "0.82 0.87 0.96 1.00";
            public string SectionHeaderAccentColor = "0.95 0.35 0.08 0.95";
            public string ButtonColor = "0.95 0.35 0.08 1.00";
            public string ButtonTextColor = "1.00 1.00 1.00 1.00";
            public string AnchorMin = "0.04 0.06";
            public string AnchorMax = "0.96 0.96";
        }

        protected override void LoadDefaultConfig()
        {
            config = CreateDefaultConfig();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                    throw new Exception("Config file is empty.");
            }
            catch
            {
                PrintWarning("Config file was invalid. Generating a new one.");
                config = CreateDefaultConfig();
            }

            SanitizeConfig();
            SaveConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        private Configuration CreateDefaultConfig()
        {
            return new Configuration
            {
                ServerInfo = new ServerInfoSettings
                {
                    Commands = new List<string>(DefaultCommands)
                },
                Rules = new RulesSettings
                {
                    SummaryLines = new List<string>(DefaultRules)
                },
                WipeTimer = new WipeTimerSettings(),
                UI = new UISettings()
            };
        }

        private void SanitizeConfig()
        {
            if (config == null)
                config = CreateDefaultConfig();

            if (config.ServerInfo == null)
                config.ServerInfo = new ServerInfoSettings();

            if (config.Rules == null)
                config.Rules = new RulesSettings();

            if (config.WipeTimer == null)
                config.WipeTimer = new WipeTimerSettings();

            if (config.UI == null)
                config.UI = new UISettings();

            config.ServerInfo.Commands = DeduplicateList(config.ServerInfo.Commands, DefaultCommands);
            config.Rules.SummaryLines = DeduplicateList(config.Rules.SummaryLines, DefaultRules);

            if (string.IsNullOrWhiteSpace(config.ServerName))
                config.ServerName = "RustStorm";

            if (string.IsNullOrWhiteSpace(config.DiscordLink))
                config.DiscordLink = "https://discord.gg/KvsxMpQskd";

            if (string.IsNullOrWhiteSpace(config.BannerUrl))
                config.BannerUrl = "https://i.ibb.co/9HGbDcWT/Chat-GPT-Image-Mar-31-2026-08-24-03-PM.png";

            if (string.IsNullOrWhiteSpace(config.WipeTimer.TimeZoneId))
                config.WipeTimer.TimeZoneId = "Asia/Singapore";

            if (string.IsNullOrWhiteSpace(config.WipeTimer.DisplayName))
                config.WipeTimer.DisplayName = "GMT+8";

            if (string.IsNullOrWhiteSpace(config.WipeTimer.WipeDay))
                config.WipeTimer.WipeDay = "Friday";

            config.WipeTimer.WipeHour24 = Mathf.Clamp(config.WipeTimer.WipeHour24, 0, 23);
            config.WipeTimer.WipeMinute = Mathf.Clamp(config.WipeTimer.WipeMinute, 0, 59);
        }

        private List<string> DeduplicateList(List<string> source, List<string> fallback)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var input = source != null && source.Count > 0 ? source : fallback;

            foreach (string entry in input)
            {
                if (string.IsNullOrWhiteSpace(entry))
                    continue;

                string normalized = entry.Trim();
                if (seen.Add(normalized))
                    result.Add(normalized);
            }

            return result;
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null || !player.IsConnected)
                return;

            timer.Once(config.WelcomeDelaySeconds, () =>
            {
                if (player == null || !player.IsConnected)
                    return;

                if (config.ShowPopupOnJoin)
                    ShowUI(player);

                if (config.ShowChatReminderOnJoin)
                    SendChatReminder(player);
            });
        }

        [ChatCommand("info")]
        private void CmdInfo(BasePlayer player, string cmd, string[] args)
        {
            if (player == null || !player.IsConnected)
                return;

            ShowUI(player);
        }

        [ConsoleCommand(CLOSE)]
        private void CmdClose(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg?.Player();
            if (player == null)
                return;

            StopUiRefresh(player.userID);
            CuiHelper.DestroyUi(player, UI);
        }


        private void StartUiRefresh(BasePlayer player)
        {
            if (player == null)
                return;

            StopUiRefresh(player.userID);

            uiRefreshTimers[player.userID] = timer.Every(30f, () =>
            {
                if (player == null || !player.IsConnected)
                {
                    StopUiRefresh(player != null ? player.userID : 0UL);
                    return;
                }

                ShowUI(player, false);
            });
        }

        private void StopUiRefresh(ulong userId)
        {
            Timer existing;
            if (userId == 0UL)
                return;

            if (uiRefreshTimers.TryGetValue(userId, out existing))
            {
                existing?.Destroy();
                uiRefreshTimers.Remove(userId);
            }
        }

        private void SendChatReminder(BasePlayer player)
        {
            player.ChatMessage($"<color=#F25814>{config.ServerInfo.WelcomeLine}</color>");
            player.ChatMessage($"<color=#A772FF>Discord:</color> <color=#FFFFFF>{config.DiscordLink}</color>");

            if (config.WipeTimer.EnableDynamicWipeTimer)
                player.ChatMessage($"<color=#BFD7FF>Next wipe:</color> <color=#FFFFFF>{GetNextWipeCountdownText()}</color>");

            player.ChatMessage("<color=#BFD7FF>Type <color=#FFFFFF>/info</color> to reopen this panel any time.</color>");
        }

        private void ShowUI(BasePlayer player, bool restartRefresh = true)
        {
            CuiHelper.DestroyUi(player, UI);

            var c = new CuiElementContainer();

            c.Add(new CuiPanel
            {
                Image = { Color = config.UI.OverlayColor },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = true
            }, "Overlay", UI);

            c.Add(new CuiPanel
            {
                Image = { Color = config.UI.PanelColor },
                RectTransform = { AnchorMin = config.UI.AnchorMin, AnchorMax = config.UI.AnchorMax },
                CursorEnabled = true
            }, UI, UI + ".panel");

            c.Add(new CuiPanel
            {
                Image = { Color = config.UI.OuterBorderColor },
                RectTransform = { AnchorMin = "0 0.985", AnchorMax = "1 0.992" }
            }, UI + ".panel", UI + ".panel.topouter");

            c.Add(new CuiPanel
            {
                Image = { Color = config.UI.TopGlowColor },
                RectTransform = { AnchorMin = "0 0.972", AnchorMax = "1 0.983" }
            }, UI + ".panel", UI + ".panel.topglow");

            c.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0.40" },
                RectTransform = { AnchorMin = "0.18 0.80", AnchorMax = "0.82 0.98" }
            }, UI + ".panel", UI + ".panel.bannerbg");

            c.Add(new CuiElement
            {
                Parent = UI + ".panel.bannerbg",
                Name = UI + ".panel.bannerimg",
                Components =
                {
                    new CuiRawImageComponent { Url = config.BannerUrl, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.08 0.12", AnchorMax = "0.92 0.88" }
                }
            });

            c.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0.18" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, UI + ".panel.bannerbg", UI + ".panel.banneroverlay");

            c.Add(new CuiLabel
            {
                Text = { Text = config.ServerName.ToUpper(), FontSize = 30, Align = TextAnchor.MiddleCenter, Color = config.UI.TitleColor },
                RectTransform = { AnchorMin = "0.24 0.69", AnchorMax = "0.76 0.76" }
            }, UI + ".panel", UI + ".panel.title");

            c.Add(new CuiLabel
            {
                Text = { Text = $"Discord: {config.DiscordLink}", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = config.UI.DiscordColor },
                RectTransform = { AnchorMin = "0.18 0.64", AnchorMax = "0.82 0.69" }
            }, UI + ".panel", UI + ".panel.discord");

            c.Add(new CuiPanel
            {
                Image = { Color = "0.95 0.35 0.08 0.75" },
                RectTransform = { AnchorMin = "0.22 0.625", AnchorMax = "0.78 0.629" }
            }, UI + ".panel", UI + ".panel.divider");

            AddSection(c, UI + ".panel", "SERVER INFO", BuildServerInfoText(), "0.05 0.40", "0.45 0.61");
            AddSection(c, UI + ".panel", "RULES", BuildRulesText(), "0.05 0.20", "0.45 0.38");
            AddSection(c, UI + ".panel", "COMMANDS", BuildCommandsText(), "0.55 0.20", "0.95 0.61");

            c.Add(new CuiLabel
            {
                Text = { Text = "Type /info any time to open this panel again", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = config.UI.MutedTextColor },
                RectTransform = { AnchorMin = "0.22 0.11", AnchorMax = "0.78 0.15" }
            }, UI + ".panel", UI + ".panel.hint");

            c.Add(new CuiButton
            {
                Button = { Color = config.UI.ButtonColor, Command = CLOSE, Close = UI },
                RectTransform = { AnchorMin = "0.40 0.05", AnchorMax = "0.60 0.10" },
                Text = { Text = "CLOSE", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = config.UI.ButtonTextColor }
            }, UI + ".panel", UI + ".panel.close");

            CuiHelper.AddUi(player, c);

            if (restartRefresh)
                StartUiRefresh(player);

            if (config.PopupDurationSeconds > 0f)
            {
                timer.Once(config.PopupDurationSeconds, () =>
                {
                    if (player != null && player.IsConnected)
                    {
                        StopUiRefresh(player.userID);
                        CuiHelper.DestroyUi(player, UI);
                    }
                });
            }
        }

        private void AddSection(CuiElementContainer c, string parent, string title, string body, string min, string max)
        {
            string panel = parent + "." + title.Replace(" ", string.Empty).ToLowerInvariant();

            c.Add(new CuiPanel
            {
                Image = { Color = config.UI.SectionPanelColor },
                RectTransform = { AnchorMin = min, AnchorMax = max }
            }, parent, panel);

            c.Add(new CuiLabel
            {
                Text = { Text = title, FontSize = 17, Align = TextAnchor.MiddleLeft, Color = config.UI.SectionHeaderColor },
                RectTransform = { AnchorMin = "0.05 0.78", AnchorMax = "0.95 0.98" }
            }, panel, panel + ".header");

            c.Add(new CuiPanel
            {
                Image = { Color = config.UI.SectionHeaderAccentColor },
                RectTransform = { AnchorMin = "0.00 0.74", AnchorMax = "1.00 0.765" }
            }, panel, panel + ".line");

            c.Add(new CuiLabel
            {
                Text = { Text = body, FontSize = 14, Align = TextAnchor.UpperLeft, Color = config.UI.TextColor },
                RectTransform = { AnchorMin = "0.05 0.08", AnchorMax = "0.95 0.70" }
            }, panel, panel + ".body");
        }

        private string BuildServerInfoText()
        {
            var sb = new StringBuilder();
            sb.Append("• Server Type: ").AppendLine(config.ServerInfo.ServerType);
            sb.Append("• Wipe Schedule: ").AppendLine(config.ServerInfo.WipeSchedule);

            if (config.WipeTimer.EnableDynamicWipeTimer)
                sb.Append("• Next Wipe: ").AppendLine(GetNextWipeCountdownText());

            sb.Append("• Rates: ").AppendLine(config.ServerInfo.Rates);
            return sb.ToString().TrimEnd();
        }

        private string BuildRulesText()
        {
            var sb = new StringBuilder();
            foreach (string rule in config.Rules.SummaryLines)
                sb.Append("• ").AppendLine(rule);
            return sb.ToString().TrimEnd();
        }

        private string BuildCommandsText()
        {
            var sb = new StringBuilder();
            foreach (string command in config.ServerInfo.Commands)
                sb.Append("• ").AppendLine(command);
            return sb.ToString().TrimEnd();
        }

        private string GetNextWipeCountdownText()
        {
            DateTime nextWipeLocal;
            TimeZoneInfo tz;

            if (!TryGetNextWipeLocalTime(out nextWipeLocal, out tz))
                return "Unavailable";

            DateTime nextWipeUtc = TimeZoneInfo.ConvertTimeToUtc(nextWipeLocal, tz);
            TimeSpan remaining = nextWipeUtc - DateTime.UtcNow;

            if (remaining.TotalSeconds <= 0)
                return "Wiping now";

            return FormatTimeRemaining(remaining);
        }

        private bool TryGetNextWipeLocalTime(out DateTime nextWipeLocal, out TimeZoneInfo tz)
        {
            nextWipeLocal = DateTime.UtcNow;
            tz = null;

            DayOfWeek wipeDay;
            if (!Enum.TryParse(config.WipeTimer.WipeDay, true, out wipeDay))
                wipeDay = DayOfWeek.Friday;

            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById(config.WipeTimer.TimeZoneId);
            }
            catch
            {
                try
                {
                    tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Singapore");
                }
                catch
                {
                    return false;
                }
            }

            DateTime nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            DateTime scheduledThisWeek = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day, config.WipeTimer.WipeHour24, config.WipeTimer.WipeMinute, 0);

            int daysUntil = ((int)wipeDay - (int)nowLocal.DayOfWeek + 7) % 7;
            scheduledThisWeek = scheduledThisWeek.AddDays(daysUntil);

            if (daysUntil == 0 && nowLocal >= scheduledThisWeek)
                scheduledThisWeek = scheduledThisWeek.AddDays(7);

            nextWipeLocal = scheduledThisWeek;
            return true;
        }

        private string FormatTimeRemaining(TimeSpan span)
        {
            if (span.TotalDays >= 1)
                return string.Format("{0}d {1}h {2}m", Mathf.Max(0, span.Days), Mathf.Max(0, span.Hours), Mathf.Max(0, span.Minutes));

            if (span.TotalHours >= 1)
                return string.Format("{0}h {1}m", Mathf.Max(0, span.Hours), Mathf.Max(0, span.Minutes));

            return string.Format("{0}m", Mathf.Max(0, span.Minutes));
        }

        private void Unload()
        {
            foreach (Timer refreshTimer in uiRefreshTimers.Values)
                refreshTimer?.Destroy();

            uiRefreshTimers.Clear();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, UI);
        }
    }
}
