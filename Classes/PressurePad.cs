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
        public bool SimpleMode = false;
        public TacticalDeviceHelper.DeviceType DeviceType { get; private set; }

        private bool IsHeld = false;
        private bool IsToggled = false;

        private float lastClickTime = 0f;
        private float doubleClickThreshold = 0.3f;

        public event Action<PressurePad, bool, PressurePadInput> OnActiveStateChanged;
        public event Action<PressurePad> OnModeChanged;

        private void EmitIfActiveChanged(PressurePadInput input)
        {
            bool active = IsActive;
            if (active == previousState)
                return;

            previousState = active;
            OnActiveStateChanged?.Invoke(this, active, input);
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
                if (SimpleMode)
                {
                    IsToggled = !IsToggled;
                    EmitIfActiveChanged(PressurePadInput.Pressed);
                    return;
                }

                if (IsToggled)
                {
                    IsToggled = false;
                    EmitIfActiveChanged(PressurePadInput.Pressed);
                }

                float clickTime = Time.time;
                if (clickTime - lastClickTime < doubleClickThreshold)
                {
                    IsToggled = true;
                    EmitIfActiveChanged(PressurePadInput.Pressed);
                }

                IsHeld = true;
                EmitIfActiveChanged(PressurePadInput.Pressed);
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
