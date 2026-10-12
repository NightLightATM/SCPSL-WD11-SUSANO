using System;
using CommandSystem;
using LabApi.Features.Wrappers;
using WD11Susano.Handlers;

namespace WD11Susano.Commands {
    [CommandHandler(typeof(ClientCommandHandler))]
    public sealed class BatCmd : ICommand {
        public string Command {
            get {
                return "replacebattery";
            }
        }

        public string[] Aliases {
            get {
                return new[] { "rb" };
            }
        }

        public string Description {
            get {
                return "Swap a Susano battery cell into your WD-11-SUSANO.";
            }
        }

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response) {
            SusHandler h = SusHandler.Inst;
            if (h == null) {
                response = "WD11Susano is not running.";
                return false;
            }
            Player p = Player.Get(sender);
            if (p == null) {
                response = "Players only.";
                return false;
            }
            return h.SwapBat(p, out response);
        }
    }
}
