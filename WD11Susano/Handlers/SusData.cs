namespace WD11Susano.Handlers {
    internal sealed class SusData {
        // 100 = full charge
        public float bat = 100f;
        // -1 = no release pending
        public float releaseAt = -1f;
    }

    internal sealed class SpinInfo {
        public bool slowed;
        public byte prevSlow;
        public float nextAura;
        public float nextHud;
    }
}
