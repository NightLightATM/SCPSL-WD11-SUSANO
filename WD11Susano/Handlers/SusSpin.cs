using System;
using System.Collections;
using InventorySystem.Items.Jailbird;
using LabApi.Features.Wrappers;
using PlayerRoles;
using UnityEngine;
using WD11Susano.Features;
using BaseJailbird = InventorySystem.Items.Jailbird.JailbirdItem;
using Logger = LabApi.Features.Console.Logger;

namespace WD11Susano.Handlers {
    public sealed partial class SusHandler {
        private bool IsSpinning(Player p) {
            if (p == null) {
                return false;
            }
            if (!p.IsAlive) {
                return false;
            }
            BaseJailbird jb = HeldSus(p, out SusData sus);
            if (jb == null) {
                return false;
            }
            if (sus.bat <= 0f) {
                return false;
            }
            var sw = JbAccess.Watch(jb);
            if (!sw.IsRunning) {
                return false;
            }
            return sw.Elapsed.TotalSeconds >= JbAccess.ReadyTime(jb);
        }

        private static void StopSpin(BaseJailbird jb, SusData sus) {
            sus.releaseAt = -1f;
            try {
                var sw = JbAccess.Watch(jb);
                if (!sw.IsRunning) {
                    return;
                }
                sw.Reset();
                JbAccess.SendRpc(jb, JailbirdMessageType.ChargeFailed);
            } catch (Exception e) {
                Logger.Error("WD11Susano EndSpin: " + e);
            }
        }

        private bool IsChained(Player p) {
            if (p == null) {
                return false;
            }
            if (p.IsSCP) {
                return false;
            }
            if (!p.IsAlive) {
                return false;
            }
            foreach (Player user in Player.List) {
                if (user == p) {
                    continue;
                }
                if (!IsSpinning(user)) {
                    continue;
                }
                if (!HitboxIdentity.IsDamageable(user.ReferenceHub, p.ReferenceHub)) {
                    continue;
                }
                if (InCone(user, p.Position, configurative.ChainRadius)) {
                    return true;
                }
            }
            return false;
        }

        private bool InCone(Player user, Vector3 pt, float dist) {
            Vector3 to = pt - user.Position;
            if (to.sqrMagnitude > dist * dist) {
                return false;
            }
            to.y = 0f;
            // 0.0025 = 5cm squared, basically standing inside the user
            if (to.sqrMagnitude < 0.0025f) {
                return true;
            }
            Vector3 fwd = FlatFwd(user);
            return Vector3.Angle(fwd, to) <= configurative.ConeHalfAngle;
        }

        private static Vector3 FlatFwd(Player p) {
            Vector3 f = p.Camera != null ? p.Camera.forward : p.Rotation * Vector3.forward;
            f.y = 0f;
            // 0.0001 = looking almost straight up or down, no usable flat direction
            if (f.sqrMagnitude < 0.0001f) {
                f = p.Rotation * Vector3.forward;
            }
            f.y = 0f;
            return f.sqrMagnitude < 0.0001f ? Vector3.forward : f.normalized;
        }

        private SpinInfo GetSpinInfo(Player p) {
            if (!spins.TryGetValue(p.PlayerId, out SpinInfo st)) {
                st = new SpinInfo();
                spins[p.PlayerId] = st;
            }
            return st;
        }

        private void DoSlow(Player p, bool spinning) {
            spins.TryGetValue(p.PlayerId, out SpinInfo st);
            if (spinning) {
                if (st == null) {
                    st = GetSpinInfo(p);
                }
                if (st.slowed) {
                    return;
                }
                st.prevSlow = CurSlow(p);
                st.slowed = true;
                // 255 = the most the game's slowness value can hold, stops it wrapping back to 0
                SetSlow(p, (byte)Math.Min(255, st.prevSlow + configurative.SpinSlowness));
            } else {
                if (st != null) {
                    if (st.slowed) {
                        st.slowed = false;
                        if (p.IsAlive) {
                            SetSlow(p, st.prevSlow);
                        }
                    }
                }
            }
        }

        private static byte CurSlow(Player p) {
            try {
                var eff = p.ReferenceHub.playerEffectsController.GetEffect<CustomPlayerEffects.Slowness>();
                if (eff != null) {
                    if (eff.IsEnabled) {
                        return eff.Intensity;
                    }
                }
                return 0;
            } catch (Exception e) {
                Logger.Error("WD11Susano CurSlow: " + e);
                return 0;
            }
        }

        private static void SetSlow(Player p, byte amt) {
            try {
                p.ReferenceHub.playerEffectsController.ChangeState("Slowness", amt, 0f, false);
            } catch (Exception e) {
                Logger.Error("WD11Susano SetSlow: " + e);
            }
        }

        private void Aura(Player user) {
            Vector3 fwd = FlatFwd(user);
            foreach (Player t in Player.List) {
                if (t == null) {
                    continue;
                }
                if (t == user) {
                    continue;
                }
                if (t.IsHost) {
                    continue;
                }
                if (!t.IsAlive) {
                    continue;
                }
                if (t.IsSCP) {
                    if (!configurative.AuraAffectsScps) {
                        continue;
                    }
                }
                if (!InCone(user, t.Position, configurative.ChainRadius)) {
                    continue;
                }
                if (!HitboxIdentity.IsDamageable(user.ReferenceHub, t.ReferenceHub)) {
                    continue;
                }
                t.Damage(configurative.AuraDamage, configurative.AuraDeathReason);
                if (!t.IsAlive) {
                    continue;
                }
                Vector3 away = t.Position - user.Position;
                away.y = 0f;
                // 0.0001 = right on top of the user, just push them forward
                away = away.sqrMagnitude < 0.0001f ? fwd : away.normalized;
                Vector3 dir = away + fwd * configurative.KnockbackForwardBias;
                dir.y = 0f;
                dir = dir.sqrMagnitude < 0.0001f ? fwd : dir.normalized;
                Ticker.Run(Knockback(t, dir, configurative.KnockbackDistance, Math.Max(1, configurative.KnockbackSteps)));
                if (configurative.RandomLookOnHit) {
                    try {
                        // x = pitch between -max and +max, y = any yaw from 0 to 360 degrees
                        t.LookRotation = new Vector2((float)((rng.NextDouble() * 2.0 - 1.0) * configurative.RandomLookMaxPitch), (float)(rng.NextDouble() * 360.0));
                    } catch (Exception e) {
                        Logger.Error("WD11Susano random look: " + e);
                    }
                }
            }
        }

        private void BreakDoors(Player user) {
            Vector3 pos = user.Position;
            Vector3 fwd = FlatFwd(user);
            foreach (Door door in Door.List) {
                BreakableDoor bd = door as BreakableDoor;
                if (bd == null) {
                    continue;
                }
                if (bd.IsBroken) {
                    continue;
                }
                Vector3 delta = bd.Position - pos;
                delta.y = 0f;
                if (delta.sqrMagnitude > configurative.DoorBreakRadius * configurative.DoorBreakRadius) {
                    continue;
                }
                // 0.01 = 10cm squared, standing in the doorway so there is no side
                if (delta.sqrMagnitude < 0.01f) {
                    continue;
                }
                // 90 degrees off the facing direction = straight left or right
                if (Mathf.Abs(Vector3.Angle(fwd, delta) - 90f) > configurative.DoorSideArcHalfWidth) {
                    continue;
                }
                bool broke = false;
                try {
                    broke = bd.TryBreak(configurative.DoorBreakDamageType);
                } catch (Exception e) {
                    Logger.Error("WD11Susano door break: " + e);
                }
                if (broke) {
                    if (configurative.DoorExplosionEffect) {
                        try {
                            Utils.ExplosionUtils.ServerSpawnEffect(bd.Position + Vector3.up, ItemType.GrenadeHE);
                        } catch (Exception e) {
                            Logger.Error("WD11Susano door explosion effect: " + e);
                        }
                    }
                }
            }
        }

        private static IEnumerator Knockback(Player t, Vector3 dir, float dist, int steps) {
            float step = dist / steps;
            for (int i = 0; i < steps; i++) {
                yield return null;
                if (t == null) {
                    yield break;
                }
                if (!t.IsAlive) {
                    yield break;
                }
                Vector3 pos = t.Position;
                // look 0.45m past the step so they stop short of walls instead of clipping in
                if (Physics.Raycast(pos, dir, step + 0.45f, PlayerRolesUtils.AttackMask, QueryTriggerInteraction.Ignore)) {
                    yield break;
                }
                // same check 0.6m lower, around knee height
                if (Physics.Raycast(pos + Vector3.down * 0.6f, dir, step + 0.45f, PlayerRolesUtils.AttackMask, QueryTriggerInteraction.Ignore)) {
                    yield break;
                }
                t.Position = pos + dir * step;
            }
        }
    }
}
