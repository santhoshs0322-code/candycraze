// ============================================================
// HapticFeedback.cs
// Triggers device vibration on match events.
// Android only — gracefully disabled on other platforms.
// ============================================================

using UnityEngine;

namespace CandyCraze
{
    public static class HapticFeedback
    {
        public static void Light()  => Vibrate(20);
        public static void Medium() => Vibrate(40);
        public static void Heavy()  => Vibrate(80);

        private static void Vibrate(long ms)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var vibrator = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                    .GetStatic<AndroidJavaObject>("currentActivity")
                    .Call<AndroidJavaObject>("getSystemService", "vibrator");
                vibrator.Call("vibrate", ms);
            }
            catch { /* Vibration not available */ }
#endif
        }
    }
}
