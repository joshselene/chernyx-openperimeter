using System;
using UnityEngine;

namespace OpenPerimeter.Core
{
    public enum InputSourceType
    {
        None,
        Touch,
        Keyboard,
        Gamepad
    }

    public static class PerimetryInputHandler
    {
        private static float lastResponseTime = -1f;
        private const float DEBOUNCE_INTERVAL_SEC = 0.150f; // 150 ms refractory debounce

        /// <summary>
        /// Checks whether a valid patient response occurred this frame.
        /// Supports Android touchscreen taps, Editor mouse clicks, keyboard (Space/Enter), and Bluetooth HID clickers.
        /// </summary>
        public static bool CheckResponse(out InputSourceType source)
        {
            float now = Time.realtimeSinceStartup;
            if (lastResponseTime > 0f && (now - lastResponseTime) < DEBOUNCE_INTERVAL_SEC)
            {
                source = InputSourceType.None;
                return false;
            }

            // 1. Android Touch
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    if (Input.GetTouch(i).phase == TouchPhase.Began)
                    {
                        lastResponseTime = now;
                        source = InputSourceType.Touch;
                        return true;
                    }
                }
            }

            // 2. Editor / Mouse Click
            if (Input.GetMouseButtonDown(0))
            {
                lastResponseTime = now;
                source = InputSourceType.Touch;
                return true;
            }

            // 3. Keyboard (Editor / USB / Bluetooth HID)
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                lastResponseTime = now;
                source = InputSourceType.Keyboard;
                return true;
            }

            // 4. Bluetooth Gamepad / Selfie Clicker Button
            if (Input.GetKeyDown(KeyCode.JoystickButton0) || 
                Input.GetKeyDown(KeyCode.Joystick1Button0) ||
                Input.GetKeyDown(KeyCode.JoystickButton1))
            {
                lastResponseTime = now;
                source = InputSourceType.Gamepad;
                return true;
            }

            source = InputSourceType.None;
            return false;
        }

        /// <summary>
        /// Explicitly resets the response debounce state.
        /// </summary>
        public static void ResetDebounce()
        {
            lastResponseTime = -1f;
        }
    }
}
