using System;
using System.Collections;
using System.Collections.Generic;
using InventorySystem.Items.Jailbird;
using InventorySystem.Items.Scp1509;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Wrappers;
using PlayerStatsSystem;
using UnityEngine;
using WD11Susano.Features;
using BaseJailbird = InventorySystem.Items.Jailbird.JailbirdItem;
using Logger = LabApi.Features.Console.Logger;

namespace WD11Susano.Handlers {
    public sealed partial class SusHandler : CustomEventsHandler {
        public static SusHandler Inst;
        internal static HashSet<ushort> NoBlindFlashes = new HashSet<ushort>();
        private Configurative configurative;
        private System.Random rng = new System.Random();
        private Dictionary<ushort, SusData> susItems = new Dictionary<ushort, SusData>();
        private Dictionary<int, SpinInfo> spins = new Dictionary<int, SpinInfo>();
        private HashSet<ushort> bats = new HashSet<ushort>();
        private Dictionary<int, KeyValuePair<string, float>> flashMsgs = new Dictionary<int, KeyValuePair<string, float>>();
        private float lastTick;

        public SusHandler(Configurative configurative) {
            this.configurative = configurative;
        }

        public override void OnPlayerProcessingJailbirdMessage(PlayerProcessingJailbirdMessageEventArgs ev) {
            if (ev.JailbirdItem == null) {
                return;
            }
            if (!susItems.TryGetValue(ev.JailbirdItem.Serial, out SusData sus)) {
                return;
            }
            switch (ev.Message) {
                case JailbirdMessageType.ChargeLoadTriggered:
                    if (sus.bat <= 0f) {
                        ev.IsAllowed = false;
                        Hint(ev.Player, "<color=#FF5555>SUSANO battery depleted.</color>", 2f);
                        return;
                    }
                    sus.releaseAt = -1f;
                    break;
                case JailbirdMessageType.ChargeStarted:
                    ev.IsAllowed = false;
                    if (sus.bat > 0f) {
                        if (sus.releaseAt < 0f) {
                            sus.releaseAt = Time.time + configurative.ReleaseGraceSeconds;
                        }
                    }
                    break;
                case JailbirdMessageType.ChargeFailed:
                    sus.releaseAt = -1f;
                    break;
            }
        }

        public override void OnPlayerHurting(PlayerHurtingEventArgs ev) {
            Player victim = ev.Player;
            Player attacker = ev.Attacker;
            if (victim == null) {
                return;
            }
            if (ev.DamageHandler is ExplosionDamageHandler boom) {
                if (IsSpinning(victim)) {
                    boom.Damage *= configurative.GrenadeDamageMultiplier;
                }
            }
            if (attacker == null) {
                return;
            }
            if (attacker == victim) {
                return;
            }
            if (ev.DamageHandler is JailbirdDamageHandler jbh) {
                if (attacker.CurrentItem != null) {
                    if (susItems.ContainsKey(attacker.CurrentItem.Serial)) {
                        jbh.Damage = configurative.MeleeDamage;
                    }
                }
            }
            if (attacker.IsSCP) {
                return;
            }
            if (IsChained(attacker)) {
                ev.IsAllowed = false;
                return;
            }
            if (!IsSpinning(victim)) {
                return;
            }
            if (!InCone(victim, attacker.Position, float.MaxValue)) {
                return;
            }
            DamageHandlerBase dh = ev.DamageHandler;
            if (dh is JailbirdDamageHandler) {
                ev.IsAllowed = false;
                return;
            }
            if (dh is Scp1509DamageHandler) {
                ev.IsAllowed = false;
                return;
            }
            if (dh is FirearmDamageHandler) {
                if (rng.NextDouble() < configurative.FirearmDeflectChance) {
                    ev.IsAllowed = false;
                    if (configurative.ShowDeflectHint) {
                        Hint(victim, "<color=#88CCFF>Deflected</color>", 0.6f);
                    }
                }
            }
        }

        public IEnumerator Loop() {
            lastTick = Time.time;
            while (true) {
                // never tick faster than every 0.02s (50 times a second)
                yield return new WaitForSeconds(Mathf.Max(0.02f, configurative.TickSeconds));
                float now = Time.time;
                try {
                    Tick(now, now - lastTick);
                } catch (Exception e) {
                    Logger.Error("WD11Susano tick: " + e);
                }
                lastTick = now;
            }
        }

        private void Tick(float now, float dt) {
            foreach (Player p in Player.List) {
                if (p == null) {
                    continue;
                }
                if (p.IsHost) {
                    continue;
                }
                bool spinning = false;
                SusData sus = null;
                BaseJailbird jb = null;
                if (p.IsAlive) {
                    jb = HeldSus(p, out sus);
                }
                if (jb != null) {
                    JbAccess.ClearWear(jb);
                    var sw = JbAccess.Watch(jb);
                    if (sus.releaseAt > 0f) {
                        if (now >= sus.releaseAt) {
                            StopSpin(jb, sus);
                        }
                    }
                    bool charging = sw.IsRunning;
                    if (charging) {
                        if (sw.Elapsed.TotalSeconds >= JbAccess.ReadyTime(jb)) {
                            spinning = true;
                            charging = false;
                        }
                    }
                    if (charging) {
                        sus.bat -= configurative.ChargeUpDrainPerSecond * dt;
                    }
                    if (spinning) {
                        sus.bat -= configurative.BatDrainPerSecond * dt;
                    }
                    if (sw.IsRunning) {
                        if (sus.bat <= 0f) {
                            sus.bat = 0f;
                            StopSpin(jb, sus);
                            spinning = false;
                            Hint(p, "<color=#FF5555>SUSANO battery depleted.</color>", 3f);
                        }
                    } else {
                        if (configurative.BatRechargePerSecond > 0f) {
                            if (sus.bat < 100f) {
                                sus.bat = Mathf.Min(100f, sus.bat + configurative.BatRechargePerSecond * dt);
                            }
                        }
                    }
                    UpdateHud(p, now);
                } else {
                    if (p.IsAlive) {
                        if (p.CurrentItem != null) {
                            if (bats.Contains(p.CurrentItem.Serial)) {
                                UpdateHud(p, now);
                            }
                        }
                    }
                }
                DoSlow(p, spinning);
                if (spinning) {
                    SpinInfo st = GetSpinInfo(p);
                    if (now >= st.nextAura) {
                        // aura never hits faster than every 0.05s
                        st.nextAura = now + Mathf.Max(0.05f, configurative.AuraIntervalSeconds);
                        Aura(p);
                    }
                    if (configurative.BreakDoors) {
                        BreakDoors(p);
                    }
                }
            }
        }

        public override void OnPlayerPickedUpItem(PlayerPickedUpItemEventArgs ev) {
            if (ev.Player == null) {
                return;
            }
            if (ev.Item == null) {
                return;
            }
            if (susItems.ContainsKey(ev.Item.Serial)) {
                Hint(ev.Player, "You have picked up <b>WD-11-Susano</b>. Hold your charge keybind to use.", 5f);
            } else {
                if (bats.Contains(ev.Item.Serial)) {
                    Hint(ev.Player, "You have picked up a battery. Use <b>.replacebattery</b> to replace a battery in a compatible item.", 5f);
                }
            }
        }

        public override void OnPlayerThrowingProjectile(PlayerThrowingProjectileEventArgs ev) {
            if (ev.ThrowableItem == null) {
                return;
            }
            if (!bats.Contains(ev.ThrowableItem.Serial)) {
                return;
            }
            ev.IsAllowed = false;
            Hint(ev.Player, "That's a Susano battery cell. Type <b>.replacebattery</b> in the console (~).", 3f);
        }

        public override void OnPlayerChangedItem(PlayerChangedItemEventArgs ev) {
            if (ev.OldItem != null) {
                if (susItems.TryGetValue(ev.OldItem.Serial, out SusData oldSus)) {
                    oldSus.releaseAt = -1f;
                }
            }
            if (ev.NewItem != null) {
                if (susItems.TryGetValue(ev.NewItem.Serial, out SusData newSus)) {
                    newSus.releaseAt = -1f;
                    if (ev.Player != null) {
                        GetSpinInfo(ev.Player).nextHud = 0f;
                    }
                }
                if (bats.Contains(ev.NewItem.Serial)) {
                    if (ev.Player != null) {
                        GetSpinInfo(ev.Player).nextHud = 0f;
                    }
                }
            }
        }

        public override void OnPlayerChangedRole(PlayerChangedRoleEventArgs ev) {
            if (ev.Player != null) {
                spins.Remove(ev.Player.PlayerId);
            }
        }

        public override void OnPlayerLeft(PlayerLeftEventArgs ev) {
            if (ev.Player == null) {
                return;
            }
            spins.Remove(ev.Player.PlayerId);
            flashMsgs.Remove(ev.Player.PlayerId);
        }

        public override void OnServerWaitingForPlayers() {
            susItems.Clear();
            bats.Clear();
            NoBlindFlashes.Clear();
            spins.Clear();
        }

        public void RestoreAll() {
            foreach (Player p in Player.List) {
                if (p == null) {
                    continue;
                }
                if (!spins.TryGetValue(p.PlayerId, out SpinInfo st)) {
                    continue;
                }
                if (!st.slowed) {
                    continue;
                }
                st.slowed = false;
                if (p.IsAlive) {
                    SetSlow(p, st.prevSlow);
                }
            }
        }
    }
}
