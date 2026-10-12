using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using InventorySystem.Items.Jailbird;
using Logger = LabApi.Features.Console.Logger;

namespace WD11Susano.Features {
    internal static class JbAccess {
        private static AccessTools.FieldRef<JailbirdItem, Stopwatch> watchRef;
        private static AccessTools.FieldRef<JailbirdItem, float> readyRef;
        private static MethodInfo sendRpc;
        private static int rpcArgs;
        private static MethodInfo setMelee;
        private static MethodInfo setCharges;

        public static bool Init() {
            try {
                watchRef = AccessTools.FieldRefAccess<JailbirdItem, Stopwatch>("_chargeLoadStopwatch");
                readyRef = AccessTools.FieldRefAccess<JailbirdItem, float>("_chargeReadyTime");
                sendRpc = AccessTools.Method(typeof(JailbirdItem), "SendRpc");
                // 0 until SendRpc is found
                rpcArgs = 0;
                if (sendRpc != null) {
                    rpcArgs = sendRpc.GetParameters().Length;
                }
                setMelee = AccessTools.PropertySetter(typeof(JailbirdItem), "TotalMeleeDamageDealt");
                setCharges = AccessTools.PropertySetter(typeof(JailbirdItem), "TotalChargesPerformed");
                bool ok = false;
                if (watchRef != null) {
                    if (sendRpc != null) {
                        // SendRpc needs at least 1 argument, the message type
                        if (rpcArgs >= 1) {
                            if (readyRef != null) {
                                ok = true;
                            }
                        }
                    }
                }
                if (!ok) {
                    Logger.Error($"WD11Susano Access: stopwatch={watchRef != null} sendRpc={sendRpc != null} args={rpcArgs} ready={readyRef != null}");
                }
                return ok;
            } catch (Exception e) {
                Logger.Error("WD11Susano Access.Init failed: " + e);
                return false;
            }
        }

        public static Stopwatch Watch(JailbirdItem jb) {
            return watchRef(jb);
        }

        public static float ReadyTime(JailbirdItem jb) {
            return readyRef(jb);
        }

        public static void SendRpc(JailbirdItem jb, JailbirdMessageType msg) {
            object[] rpcParams = new object[rpcArgs];
            // argument 0 is the message type, the rest stay null
            rpcParams[0] = msg;
            sendRpc.Invoke(jb, rpcParams);
        }

        public static void ClearWear(JailbirdItem jb) {
            try {
                // any melee damage counted at all
                if (jb.TotalMeleeDamageDealt > 0f) {
                    if (setMelee != null) {
                        // back to 0 damage dealt
                        setMelee.Invoke(jb, new object[] { 0f });
                    }
                }
                // any charges counted at all
                if (jb.TotalChargesPerformed > 0) {
                    if (setCharges != null) {
                        // back to 0 charges used
                        setCharges.Invoke(jb, new object[] { 0 });
                    }
                }
            } catch (Exception e) {
                Logger.Error("WD11Susano ClearWear: " + e);
            }
        }
    }
}
