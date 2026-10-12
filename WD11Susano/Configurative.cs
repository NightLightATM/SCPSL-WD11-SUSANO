using Interactables.Interobjects.DoorUtils;

namespace WD11Susano {
    public sealed class Configurative {
        public bool IsEnabled {
            get;
            set;
        } = true;
        public bool Debug {
            get;
            set;
        } = false;

        public float ConeHalfAngle {
            get;
            set;
            // 89 degrees each side of where you look = a 178 degree cone
        } = 89f;
        public float ChainRadius {
            get;
            set;
            // 2m chain zone in front of the user
        } = 2f;
        public float FirearmDeflectChance {
            get;
            set;
            // 0.40 = 40% chance per bullet
        } = 0.40f;
        public bool ShowDeflectHint {
            get;
            set;
        } = true;

        public float GrenadeDamageMultiplier {
            get;
            set;
            // 0.5 = grenade damage halved while spinning
        } = 0.5f;

        public float AuraDamage {
            get;
            set;
            // 5 damage per aura hit
        } = 5f;
        public float AuraIntervalSeconds {
            get;
            set;
            // aura hits every 0.5s
        } = 0.5f;
        public string AuraDeathReason {
            get;
            set;
        } = "Shredded by WD-11-SUSANO.";
        public bool AuraAffectsScps {
            get;
            set;
        } = true;

        public float KnockbackDistance {
            get;
            set;
            // 1.5m push per hit
        } = 1.5f;
        public float KnockbackForwardBias {
            get;
            set;
            // 1.0 = push along the user facing as much as away from them
        } = 1.0f;
        public int KnockbackSteps {
            get;
            set;
            // push spread over 5 frames
        } = 5;
        public bool RandomLookOnHit {
            get;
            set;
        } = true;
        public float RandomLookMaxPitch {
            get;
            set;
            // random look goes up to 35 degrees up or down
        } = 35f;

        public byte SpinSlowness {
            get;
            set;
            // 30% slower while spinning
        } = 30;

        public float BatDrainPerSecond {
            get;
            set;
            // 2% bat per second while fully spinning
        } = 2f;
        public float ChargeUpDrainPerSecond {
            get;
            set;
            // 5% bat per second during the charge-up
        } = 5f;
        public float BatRechargePerSecond {
            get;
            set;
            // 0 = bat never recharges on its own
        } = 0f;

        public bool BreakDoors {
            get;
            set;
        } = true;
        public float DoorBreakRadius {
            get;
            set;
            // doors within 2m
        } = 2f;
        public float DoorSideArcHalfWidth {
            get;
            set;
            // 15 degrees each side of straight left or right = 30 degree wedge per side
        } = 15f;
        public DoorDamageType DoorBreakDamageType {
            get;
            set;
        } = DoorDamageType.Grenade;
        public bool DoorExplosionEffect {
            get;
            set;
        } = true;

        public float MeleeDamage {
            get;
            set;
            // 1 damage per normal swing
        } = 1f;

        public float ReplacementCharge {
            get;
            set;
            // a new bat fills to 100%
        } = 100f;
        public bool EjectSpentCell {
            get;
            set;
        } = true;
        public float EjectFuseSeconds {
            get;
            set;
            // spent bat pops 1.5s after dropping
        } = 1.5f;

        public float ReleaseGraceSeconds {
            get;
            set;
            // wait 0.35s for the next wind-up before counting it as a release
        } = 0.35f;
        public float TickSeconds {
            get;
            set;
            // main loop runs every 0.1s
        } = 0.1f;

        public bool ShowHud {
            get;
            set;
        } = true;
        public float HudRefreshSeconds {
            get;
            set;
            // hud resent every 0.5s
        } = 0.5f;
        public int HudFontSize {
            get;
            set;
            // hud text size 24
        } = 24;
        public int HudBarSegments {
            get;
            set;
            // 20 bars in the charge bar
        } = 20;
        public int HudLiftLines {
            get;
            set;
            // 0 = hud at its normal height
        } = 0;
    }
}
