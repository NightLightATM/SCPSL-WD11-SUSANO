using System;
using System.Globalization;
using CommandSystem;
using LabApi.Features.Wrappers;
using WD11Susano.Handlers;

namespace WD11Susano.Commands {
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class SusCmd : ICommand {
        public string Command {
            get {
                return "susano";
            }
        }

        public string[] Aliases {
            get {
                return new[] { "wd11" };
            }
        }

        public string Description {
            get {
                return "WD-11-SUSANO. Usage: susano give <player> | susano givebattery <player> | susano battery <player> <0-100>";
            }
        }

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response) {
            if (!sender.CheckPermission(PlayerPermissions.GivingItems, out response)) {
                return false;
            }
            SusHandler h = SusHandler.Inst;
            if (h == null) {
                response = "WD11Susano is not running.";
                return false;
            }
            string[] args = new string[arguments.Count];
            for (int i = 0; i < arguments.Count; i++) {
                args[i] = arguments.Array[arguments.Offset + i];
            }

            // give / givebattery need 2 words: the subcommand and the player
            if (args.Length >= 2) {
                if (args[0].Equals("give", StringComparison.OrdinalIgnoreCase)) {
                    Player p = FindPlayer(args[1]);
                    if (p == null) {
                        response = $"No player '{args[1]}'.";
                        return false;
                    }
                    string err = h.GiveSus(p);
                    response = err ?? $"Gave WD-11-SUSANO to {p.Nickname}.";
                    return err == null;
                }
                bool isBat = false;
                if (args[0].Equals("givebattery", StringComparison.OrdinalIgnoreCase)) {
                    isBat = true;
                }
                if (args[0].Equals("cell", StringComparison.OrdinalIgnoreCase)) {
                    isBat = true;
                }
                if (isBat) {
                    Player p = FindPlayer(args[1]);
                    if (p == null) {
                        response = $"No player '{args[1]}'.";
                        return false;
                    }
                    string err = h.GiveBat(p);
                    response = err ?? $"Gave a Susano battery cell to {p.Nickname}.";
                    return err == null;
                }
            }

            // battery needs 3 words: battery, the player, the percent
            if (args.Length >= 3) {
                if (args[0].Equals("battery", StringComparison.OrdinalIgnoreCase)) {
                    Player p = FindPlayer(args[1]);
                    if (p == null) {
                        response = $"No player '{args[1]}'.";
                        return false;
                    }
                    // word 2 is the percent
                    if (!float.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float num)) {
                        response = "Battery must be a number 0-100.";
                        return false;
                    }
                    if (!h.SetBat(p, num, out string err)) {
                        response = err;
                        return false;
                    }
                    // {num:0} = percent with no decimals
                    response = $"{p.Nickname}'s Susano battery set to {num:0}%.";
                    return true;
                }
            }

            response = "Usage: susano give <player> | susano givebattery <player> | susano battery <player> <0-100>";
            return false;
        }

        private static Player FindPlayer(string arg) {
            if (int.TryParse(arg, out int num)) {
                Player byId = Player.Get(num);
                if (byId != null) {
                    return byId;
                }
            }
            foreach (Player p in Player.List) {
                if (p == null) {
                    continue;
                }
                if (p.IsHost) {
                    continue;
                }
                if (string.Equals(p.Nickname, arg, StringComparison.OrdinalIgnoreCase)) {
                    return p;
                }
            }
            return null;
        }
    }
}
