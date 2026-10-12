using System;
using HarmonyLib;
using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Loader.Features.Plugins;
using WD11Susano.Features;
using WD11Susano.Handlers;
using Logger = LabApi.Features.Console.Logger;

namespace WD11Susano {
    public sealed class SusPlugin : Plugin<Configurative> {
        public override string Name {
            get {
                return "WD11Susano";
            }
        }

        public override string Description {
            get {
                return "WD-11-SUSANO custom Jailbird.";
            }
        }

        public override string Author {
            get {
                return "NightLight";
            }
        }

        public override Version Version {
            get {
                // version 0.1.0
                return new Version(0, 1, 0);
            }
        }

        public override Version RequiredApiVersion {
            get {
                return new Version(LabApiProperties.CompiledVersion);
            }
        }

        private SusHandler handler;
        private Harmony harmony;

        public override void Enable() {
            if (!Config.IsEnabled) {
                Logger.Info("WD11Susano is disabled in config.");
                return;
            }
            if (!JbAccess.Init()) {
                Logger.Error("WD11Susano: could not bind Jailbird internals (game update?). Plugin NOT enabled.");
                return;
            }
            handler = new SusHandler(Config);
            SusHandler.Inst = handler;
            CustomHandlersManager.RegisterEventsHandler(handler);
            Ticker.Run(handler.Loop());
            harmony = new Harmony("wd11.susano");
            harmony.PatchAll(typeof(SusPlugin).Assembly);
            Logger.Info("WD11Susano enabled.");
        }

        public override void Disable() {
            if (handler != null) {
                handler.RestoreAll();
                CustomHandlersManager.UnregisterEventsHandler(handler);
            }
            if (harmony != null) {
                harmony.UnpatchAll(harmony.Id);
            }
            harmony = null;
            Ticker.Kill();
            SusHandler.Inst = null;
            handler = null;
            Logger.Info("WD11Susano disabled.");
        }
    }
}
