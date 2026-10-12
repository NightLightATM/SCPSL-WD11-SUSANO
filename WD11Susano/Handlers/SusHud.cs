using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using UnityEngine;
using WD11Susano.Features;
using BaseJailbird = InventorySystem.Items.Jailbird.JailbirdItem;
using Logger = LabApi.Features.Console.Logger;

namespace WD11Susano.Handlers {
    public sealed partial class SusHandler {
        private void Hint(Player p, string text, float secs) {
            if (p == null) {
                return;
            }
            flashMsgs[p.PlayerId] = new KeyValuePair<string, float>(text, Time.time + secs);
            bool sendNow = false;
            if (!configurative.ShowHud) {
                sendNow = true;
            }
            if (HeldSus(p, out _) == null) {
                sendNow = true;
            }
            if (sendNow) {
                SendHint(p, text, secs);
            } else {
                GetSpinInfo(p).nextHud = 0f;
            }
        }

        private void UpdateHud(Player p, float now) {
            if (!configurative.ShowHud) {
                return;
            }
            SpinInfo st = GetSpinInfo(p);
            if (now < st.nextHud) {
                return;
            }
            // never resend the hud faster than every 0.1s
            st.nextHud = now + Mathf.Max(0.1f, configurative.HudRefreshSeconds);
            string text = DrawHud(p);
            if (!string.IsNullOrEmpty(text)) {
                // hint lasts 0.35s longer than the refresh so it never blinks out between sends
                SendHint(p, text, configurative.HudRefreshSeconds + 0.35f);
            }
        }

        private static void SendHint(Player p, string text, float secs) {
            try {
                p.SendHint(text, secs);
            } catch (Exception e) {
                Logger.Error("WD11Susano SendHint: " + e);
            }
        }

        private string DrawHud(Player p) {
            if (p == null) {
                return string.Empty;
            }
            if (!p.IsAlive) {
                return string.Empty;
            }
            BaseJailbird jb = HeldSus(p, out SusData sus);
            if (jb == null) {
                if (p.CurrentItem != null) {
                    if (bats.Contains(p.CurrentItem.Serial)) {
                        return $"<size={configurative.HudFontSize}><b>SUSANO BATTERY CELL</b>\n<color=#AAAAAA>Type</color> <b>.replacebattery</b> <color=#AAAAAA>in the console (~)</color></size>";
                    }
                }
                return string.Empty;
            }
            string flash = null;
            if (flashMsgs.TryGetValue(p.PlayerId, out KeyValuePair<string, float> f)) {
                if (Time.time < f.Value) {
                    flash = f.Key;
                } else {
                    flashMsgs.Remove(p.PlayerId);
                }
            }
            bool spinning = false;
            bool charged = false;
            if (sus.bat > 0f) {
                spinning = JbAccess.Watch(jb).IsRunning;
                if (spinning) {
                    charged = JbAccess.Watch(jb).Elapsed.TotalSeconds >= JbAccess.ReadyTime(jb);
                }
            }
            // bar is 5 to 50 segments long
            int segs = Mathf.Clamp(configurative.HudBarSegments, 5, 50);
            // green above 50%, yellow above 20%, red below
            string col = sus.bat > 50f ? "#7CFC00" : sus.bat > 20f ? "#FFD700" : "#FF5555";
            string state = sus.bat <= 0f ? "<color=#FF5555>DEPLETED</color>" : charged ? "<color=#88CCFF>SPINNING</color>" : spinning ? "<color=#88CCFF>CHARGING</color>" : "<color=#AAAAAA>IDLE</color>";
            // filled segments = charge out of 100, rounded up
            string line = $"<size={configurative.HudFontSize}><b>WD-11-SUSANO</b>  <color={col}>{new string('|', Mathf.Clamp(Mathf.CeilToInt(sus.bat / 100f * segs), 0, segs))}</color><color=#444444>{new string('|', segs - Mathf.Clamp(Mathf.CeilToInt(sus.bat / 100f * segs), 0, segs))}</color>  <color={col}>{Mathf.CeilToInt(sus.bat)}%</color>  {state}";
            if (flash != null) {
                line += "\n" + flash;
            }
            line += "</size>";
            int lift = configurative.HudLiftLines;
            if (lift > 0) {
                line += new string('\n', lift);
            } else {
                if (lift < 0) {
                    line = new string('\n', -lift) + line;
                }
            }
            return line;
        }
    }
}
