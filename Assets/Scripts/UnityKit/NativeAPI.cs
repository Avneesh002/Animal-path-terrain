using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityKit
{
    /// <summary>
    /// Low-level native communication API.
    /// Routes messages to the correct platform bridge.
    /// </summary>
    public static class NativeAPI
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private static bool _warnedMissingBridge;
#endif
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void SendMessageToFlutter(string message);

        [DllImport("__Internal")]
        private static extern void SendSceneLoadedToFlutter(
            string name, int buildIndex, bool isLoaded, bool isValid);
#endif

        /// <summary>
        /// Send a message string to Flutter.
        /// </summary>
        public static void SendToFlutter(string message)
        {
#if UNITY_IOS && !UNITY_EDITOR
            SendMessageToFlutter(message);
#elif UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var cls = new AndroidJavaClass("com.unity_kit.FlutterBridgeRegistry");
                cls.CallStatic("sendMessageToFlutter", message);
            }
            catch (System.Exception error)
            {
                // A standalone Unity Android build has no Flutter bridge.
                if (!_warnedMissingBridge)
                {
                    _warnedMissingBridge = true;
                    Debug.LogWarning($"[UnityKit] Flutter bridge unavailable: {error.Message}");
                }
            }
#else
            Debug.Log($"[UnityKit] SendToFlutter: {message}");
#endif
        }

        /// <summary>
        /// Notify Flutter that a scene was loaded.
        /// </summary>
        public static void NotifySceneLoaded(string name, int buildIndex, bool isLoaded, bool isValid)
        {
#if UNITY_IOS && !UNITY_EDITOR
            SendSceneLoadedToFlutter(name, buildIndex, isLoaded, isValid);
#elif UNITY_ANDROID && !UNITY_EDITOR
            using var cls = new AndroidJavaClass("com.unity_kit.FlutterBridgeRegistry");
            cls.CallStatic("sendSceneLoadedToFlutter", name, buildIndex, isLoaded, isValid);
#else
            Debug.Log($"[UnityKit] SceneLoaded: {name} (index: {buildIndex})");
#endif
        }
    }
}
