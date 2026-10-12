using System.Collections;
using UnityEngine;

namespace WD11Susano.Features {
    public sealed class Ticker : MonoBehaviour {
        public static Ticker Inst {
            get;
            private set;
        }

        public static void Init() {
            if (Inst != null) {
                return;
            }
            GameObject go = new GameObject("WD11SusanoScheduler");
            Object.DontDestroyOnLoad(go);
            Inst = go.AddComponent<Ticker>();
        }

        public static void Kill() {
            if (Inst == null) {
                return;
            }
            Inst.StopAllCoroutines();
            Object.Destroy(Inst.gameObject);
            Inst = null;
        }

        public static void Run(IEnumerator routine) {
            Init();
            Inst.StartCoroutine(routine);
        }
    }
}
