using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Full screen on and off: F11 or Alt and Enter from anywhere, and the settings window through
    /// <see cref="Toggle"/>. The Windows build starts in full screen; in the browser the first press asks the browser for it.
    /// </summary>
    public sealed class FullscreenKeys : MonoBehaviour
    {
        public static bool IsFull => Screen.fullScreen;

        public static void Toggle()
        {
            if (Screen.fullScreen)
            {
                Screen.fullScreen = false;
#if !UNITY_WEBGL
                Screen.fullScreenMode = FullScreenMode.Windowed;
#endif
            }
            else
            {
#if !UNITY_WEBGL
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
#endif
                Screen.fullScreen = true;
            }
        }

        private void Update()
        {
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            if (Input.GetKeyDown(KeyCode.F11) || (alt && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))))
            {
                Toggle();
            }
        }
    }
}
