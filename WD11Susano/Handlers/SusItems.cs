using System;
using LabApi.Features.Wrappers;
using UnityEngine;
using BaseJailbird = InventorySystem.Items.Jailbird.JailbirdItem;
using Logger = LabApi.Features.Console.Logger;

namespace WD11Susano.Handlers {
    public sealed partial class SusHandler {
        public string GiveSus(Player p) {
            if (p == null) {
                return "Target must be an alive human.";
            }
            if (!p.IsAlive) {
                return "Target must be an alive human.";
            }
            if (!p.IsHuman) {
                return "Target must be an alive human.";
            }
            Item item = p.AddItem(ItemType.Jailbird);
            if (item == null) {
                return "Could not add item (inventory full?).";
            }
            susItems[item.Serial] = new SusData();
            Hint(p, "<b>WD-11-SUSANO</b> received. Equip it and hold <b>charge</b> to spin.", 5f);
            return null;
        }

        public string GiveBat(Player p) {
            if (p == null) {
                return "Target must be an alive human.";
            }
            if (!p.IsAlive) {
                return "Target must be an alive human.";
            }
            if (!p.IsHuman) {
                return "Target must be an alive human.";
            }
            Item batItem = p.AddItem(ItemType.GrenadeFlash);
            if (batItem == null) {
                return "Could not add item (inventory full?).";
            }
            bats.Add(batItem.Serial);
            Hint(p, "<b>SUSANO battery cell</b> received. Type <b>.replacebattery</b> in the console (~) to swap it in.", 5f);
            return null;
        }

        public bool SwapBat(Player p, out string resp) {
            if (p == null) {
                resp = "You must be alive.";
                return false;
            }
            if (!p.IsAlive) {
                resp = "You must be alive.";
                return false;
            }
            SusData target = HeldSusData(p);
            bool holding = target != null;
            Item batItem = null;
            foreach (Item item in p.Items) {
                if (item == null) {
                    continue;
                }
                if (batItem == null) {
                    if (bats.Contains(item.Serial)) {
                        batItem = item;
                    }
                }
                if (!holding) {
                    if (susItems.TryGetValue(item.Serial, out SusData sus)) {
                        if (target == null) {
                            target = sus;
                        } else {
                            if (sus.bat < target.bat) {
                                target = sus;
                            }
                        }
                    }
                }
            }
            if (target == null) {
                resp = "You don't have a WD-11-SUSANO.";
                return false;
            }
            if (batItem == null) {
                resp = "You don't have a Susano battery cell.";
                return false;
            }
            bats.Remove(batItem.Serial);
            p.RemoveItem(batItem);
            // bat charge is 0-100
            target.bat = Mathf.Clamp(configurative.ReplacementCharge, 0f, 100f);
            if (configurative.EjectSpentCell) {
                try {
                    // drop it 0.3m in front of the player and 0.5m down (about hip height)
                    Vector3 pos = p.Position + FlatFwd(p) * 0.3f + Vector3.down * 0.5f;
                    TimedGrenadeProjectile proj = TimedGrenadeProjectile.SpawnActive(pos, ItemType.GrenadeFlash, p, configurative.EjectFuseSeconds);
                    if (proj != null) {
                        NoBlindFlashes.Add(proj.Serial);
                    }
                } catch (Exception e) {
                    Logger.Error("WD11Susano eject cell: " + e);
                }
            }
            Hint(p, $"<color=#7CFC00>Battery replaced: {Mathf.CeilToInt(target.bat)}%</color>", 3f);
            GetSpinInfo(p).nextHud = 0f;
            resp = $"Battery replaced. Susano charge: {Mathf.CeilToInt(target.bat)}%.";
            return true;
        }

        private SusData HeldSusData(Player p) {
            if (p == null) {
                return null;
            }
            if (p.CurrentItem == null) {
                return null;
            }
            susItems.TryGetValue(p.CurrentItem.Serial, out SusData sus);
            return sus;
        }

        public bool SetBat(Player p, float pct, out string err) {
            err = null;
            SusData sus = HeldSusData(p);
            if (sus == null) {
                err = "That player is not holding a Susano.";
                return false;
            }
            sus.bat = Mathf.Clamp(pct, 0f, 100f);
            return true;
        }

        private BaseJailbird HeldSus(Player p, out SusData sus) {
            sus = null;
            if (p == null) {
                return null;
            }
            Item item = p.CurrentItem;
            if (item == null) {
                return null;
            }
            if (!susItems.TryGetValue(item.Serial, out sus)) {
                return null;
            }
            return item.Base as BaseJailbird;
        }
    }
}
