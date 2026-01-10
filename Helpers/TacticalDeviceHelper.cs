using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace PressurePads.Helpers
{
    internal static class TacticalDeviceHelper
    {
        public enum DeviceMode
        {
            None = 0,
            WhiteLight = 1,
            VisibleLaser = 2,
            IRLight = 3,
            IRLaser = 4,
        }

        public enum DeviceType
        {
            None = 0,
            Flashlight = 1,
            Other = 2,
        }

        /// <summary>
        /// Determines the type of a given tactical device (Thanks SAIN!)
        /// </summary>
        /// <param name="device">Device to check</param>
        /// <returns>Type of given device</returns>
        public static DeviceType DetectType(TacticalComboVisualController device)
        {
            List<Transform> tacModes = _tacticalModesField.GetValue(device) as List<Transform>;
            bool foundFlashlight = false;
            bool foundOther = false;

            foreach (var mode in tacModes)
            {

                foreach (var child in mode.GetChildren())
                {
                    string name = child.name.ToLowerInvariant();

                    if (name.StartsWith("light_0")) //flashlights
                    {
                        foundFlashlight = true;
                        continue;
                    }

                    if (name.StartsWith("vis_0") ||   // visible laser
                        name.StartsWith("il_0") ||   // IR illuminator
                        name.StartsWith("ir_0"))      // IR laser
                    {
                        foundOther = true;
                    }
                }

            }

            if (foundFlashlight)
                return DeviceType.Flashlight;

            if (foundOther)
                return DeviceType.Other;

            // Fallback to none
            return DeviceType.None;
        }


        public static List<TacticalComboVisualController> GetTacticalDevices(Player.FirearmController controller)
        {
            Transform root = controller.WeaponRoot;
            return root.GetComponentsInChildrenActiveIgnoreFirstLevel<TacticalComboVisualController>();
        }

        public static List<TacticalComboVisualController> GetTacticalDevicesOfType(Player.FirearmController controller, DeviceType type)
        {
            var allDevices = GetTacticalDevices(controller);
            List<TacticalComboVisualController> outDevices = [];
            Plugin.LogSource.LogInfo("Starting type detection");
            foreach (var device in allDevices)
            {
                Plugin.LogSource.LogInfo($"Device= {device.name.Localized()}\n + Type={DetectType(device)}");
                if (DetectType(device) == type)
                {
                    outDevices.Add(device);
                    Plugin.LogSource.LogInfo("Adding to return");
                }
            }
            Plugin.LogSource.LogInfo($"Returning {outDevices.Count} items");

            return outDevices;
        }

        public static FirearmLightStateStruct getTacticalDeviceState(TacticalComboVisualController device)
        {
            return device.LightMod.GetLightState();
        }

        private static readonly FieldInfo _tacticalModesField = AccessTools.Field(typeof(TacticalComboVisualController), "list_0");
    }
}
