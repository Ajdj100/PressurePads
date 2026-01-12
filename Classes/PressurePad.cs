using System;
using System.Collections.Generic;
using System.Text;
using EFT;
using PressurePads.Helpers;
using UnityEngine;

namespace PressurePads.Classes
{
    public enum PressurePadInput
    {
        Pressed,
        Released
    }

    internal class PressurePad
    {
        public PressurePad(KeyCode key, KeyCode modifier, TacticalDeviceHelper.DeviceType type)
        {
            Key = key;
            Modifier = modifier;
            DeviceType = type;

            IsHeld = false;
            IsToggled = false;
            lastClickTime = 0f;
        }

        public KeyCode Key;
        public KeyCode Modifier;
        public bool AdvancedMode = false;
        public TacticalDeviceHelper.DeviceType DeviceType { get; private set; }

        private bool IsHeld = false;
        private bool IsToggled = false;

        private float lastClickTime = 0f;
        private float doubleClickThreshold = 0.3f;

        /// <summary>
        /// Fired whenever a pressure pad's active state changes.
        /// </summary>
        /// <remarks>
        /// Parameters:
        /// <list type="bullet">
        ///   <item>
        ///     <description><b>PressurePad</b> – The pressure pad instance that triggered the change.</description>
        ///   </item>
        ///   <item>
        ///     <description><b>bool</b> – Whether the pad is now active.</description>
        ///   </item>
        ///   <item>
        ///     <description><b>PressurePadInput</b> – Pressed/released flag.</description>
        ///   </item>
        ///   <item>
        ///     <description><b>bool</b> – Skip click flag</description>
        ///   </item>
        /// </list>
        /// </remarks>
        public event Action<PressurePad, bool, PressurePadInput, bool> OnActiveStateChanged;
        public event Action<PressurePad> OnModeChanged;

        private void EmitIfActiveChanged(PressurePadInput input, bool skipClick  = false)
        {
            bool active = IsActive;
            if (active == previousState)
                return;

            previousState = active;
            OnActiveStateChanged?.Invoke(this, active, input, skipClick);
        }

        public bool IsActive => IsHeld || IsToggled;
        private bool previousState = false;


        public void Update()
        {
            if (!Input.GetKeyDown(Key) && !Input.GetKeyUp(Key))
                return;

            if (Input.GetKeyDown(Key))
            {
                if (Input.GetKey(Modifier))
                {
                    OnModeChanged?.Invoke(this);
                    return;
                }

                //simple mode override
                if (!AdvancedMode)
                {
                    IsToggled = !IsToggled;
                    EmitIfActiveChanged(PressurePadInput.Pressed);
                    return;
                }

                //advanced mode behaviour
                if (IsToggled)
                {
                    IsToggled = false;
                }

                //skip second animation on double click
                bool skipClick = false;
                float clickTime = Time.time;
                if (clickTime - lastClickTime < doubleClickThreshold)
                {
                    IsToggled = true;
                    skipClick = true;
                }
 
                IsHeld = true;
                EmitIfActiveChanged(PressurePadInput.Pressed, skipClick);
                lastClickTime = clickTime;
            }

            if (Input.GetKeyUp(Key))
            {
                //skip on modifier keyup 
                if (Input.GetKey(Modifier))
                {
                    return;
                }

                IsHeld = false;
                EmitIfActiveChanged(PressurePadInput.Released);
            }
        }
    }
}
