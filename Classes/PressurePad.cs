using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using PressurePads.Helpers;

namespace PressurePads.Classes
{
    internal class PressurePad
    {
        public PressurePad(KeyCode key, TacticalDeviceHelper.DeviceType type)
        {
            Key = key;
            DeviceType = type;

            IsHeld = false;
            IsToggled = false;
            lastClickTime = 0f;
        }

        public KeyCode Key { get; private set; }
        public TacticalDeviceHelper.DeviceType DeviceType { get; private set; }

        public bool IsHeld { get; private set; }
        public bool IsToggled { get; private set; }

        private float lastClickTime;
    }
}
