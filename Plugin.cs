using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.Visual;
using HarmonyLib;
using PressurePads.Classes;
using PressurePads.Helpers;
using UnityEngine;
using static HairRenderer;

namespace PressurePads
{
    // first string below is your plugin's GUID, it MUST be unique to any other mod. Read more about it in BepInEx docs. Be sure to update it if you copy this project.
    [BepInPlugin("ajdj100.pressurepads", "PressurePads", "1.1.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;
        public static ClientConfig ClientConfig;

        public static ConfigEntry<KeyboardShortcut> _FlashlightKeybind;
        public static ConfigEntry<KeyboardShortcut> _FlashlightModifier;
        public static ConfigEntry<KeyboardShortcut> _OtherKeybind;
        public static ConfigEntry<KeyboardShortcut> _OtherModifier;
        public static ConfigEntry<bool> _advancedMode;
        public static ConfigEntry<bool> _dontClick;

        public static ConfigEntry<bool> _enableLogging;


        // BaseUnityPlugin inherits MonoBehaviour, so you can use base unity functions like Awake() and Update()
        private void Awake()
        {
            // save the Logger to public static field so we can use it elsewhere in the project
            LogSource = Logger;

            var pluginPath = Path.GetDirectoryName(Info.Location);
            ClientConfig = ConfigLoader.Load(pluginPath);

            flashlightPad.OnActiveStateChanged += handlePadStateChange;
            flashlightPad.OnModeChanged += handlePadModeChange;
            otherPad.OnActiveStateChanged += handlePadStateChange;
            otherPad.OnModeChanged += handlePadModeChange;

            _advancedMode = Config.Bind(
                "General",
                "Advanced Mode",
                false,
                new ConfigDescription(
                    "Hold the hotkey to activate the light temporarily, or double-tap it to toggle the light on or off.")
            );
            _dontClick = Config.Bind(
                "General",
                "Skip toggle animation and sound",
                false,
                new ConfigDescription(
                    "Toggle tactical devices without the click or toggle device animation."
                )
            );
            _FlashlightKeybind = Config.Bind(
                "Flashlight",
                "Flashlight Keybind",
                new KeyboardShortcut(KeyCode.Z),
                new ConfigDescription("Hotkey for controlling white light flashlights",
                null));
            _FlashlightModifier = Config.Bind(
                "Flashlight",
                "Flashlight Modifier",
                new KeyboardShortcut(KeyCode.LeftControl),
                new ConfigDescription("Modifier key for switching flashlight modes",
                null));
            _OtherKeybind = Config.Bind(
                "Other",
                "Other Keybind",
                new KeyboardShortcut(KeyCode.V),
                new ConfigDescription("Hotkey for controlling IR and laser devices",
                null));
            _OtherModifier = Config.Bind(
                "Other",
                "Other Modifier",
                new KeyboardShortcut(KeyCode.LeftControl),
                new ConfigDescription("Modifier key for switching IR and laser device modes",
                null));
            _enableLogging = Config.Bind(
                "Development",
                "Enable Logging",
                false,
                new ConfigDescription("Enables extra logging, doesnt do anything for gameplay.",
                null));

            // load values
            flashlightPad.Key = _FlashlightKeybind.Value.MainKey;
            flashlightPad.Modifier = _FlashlightModifier.Value.MainKey;
            otherPad.Key = _OtherKeybind.Value.MainKey;
            otherPad.Modifier = _OtherModifier.Value.MainKey;
            _enableLogging.Value = _enableLogging.Value;
            _advancedMode.Value = _advancedMode.Value;

            // hot reload support
            _advancedMode.SettingChanged += (_, __) =>
            {
                flashlightPad.AdvancedMode = _advancedMode.Value;
                otherPad.AdvancedMode = _advancedMode.Value;
            };
            _FlashlightKeybind.SettingChanged += (_, __) =>
            {
                flashlightPad.Key = _FlashlightKeybind.Value.MainKey;
            };

            _FlashlightModifier.SettingChanged += (_, __) =>
            { 
                flashlightPad.Modifier = _FlashlightModifier.Value.MainKey;
            };

            _OtherKeybind.SettingChanged += (_, __) =>
            {
                otherPad.Key = _OtherKeybind.Value.MainKey;
            };

            _OtherModifier.SettingChanged += (_, __) => 
            {
                otherPad.Modifier = _OtherModifier.Value.MainKey;
            };

            LogSource.LogInfo("PressurePads initialized");
        }

        private void _OtherModifier_SettingChanged(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private Player.FirearmController cont;

        private readonly PressurePad flashlightPad = new PressurePad(KeyCode.Z, KeyCode.LeftControl, TacticalDeviceHelper.DeviceType.Flashlight);
        private readonly PressurePad otherPad = new PressurePad(KeyCode.V, KeyCode.LeftControl, TacticalDeviceHelper.DeviceType.Other);

        public List<TacticalComboVisualController> TacticalDevices;

        private void Update()
        {
            //I kinda hate this but I didnt see into the future earlier today so this is the optimization for now
            bool padPressed = Input.GetKeyDown(flashlightPad.Key) || Input.GetKeyDown(otherPad.Key)
                           || Input.GetKeyUp(flashlightPad.Key) || Input.GetKeyUp(otherPad.Key);

            if (!padPressed)
                return;

            //try to init controller if it doesnt exist right now
            if (cont == null && !TryInitController())
            {
                LogSource.LogError("Failed to initialize controller. Does player have a weapon in their hands?");
                return;
            }

            // update pad states
            flashlightPad.Update();
            otherPad.Update();
        }

        private void handlePadStateChange(PressurePad pad, bool isActive, PressurePadInput direction, bool skipAnimation)
        {
            if (_enableLogging.Value)
                LogSource.LogInfo($"Pressurepad active change event\nPad: {pad}\nState: {isActive}\nDirection: {direction}\nSkipAnimation: {skipAnimation}");

            if (cont == null && !TryInitController())
                return;

            var tacticals = TacticalDeviceHelper.GetTacticalDevicesOfType(cont, pad.DeviceType);
            List<FirearmLightStateStruct> states = [];

            //cancel if nothing to toggle
            if (tacticals.Count <= 0)
                return;

            foreach (var item in tacticals)
            {
                var state = TacticalDeviceHelper.getTacticalDeviceState(item);
                state.IsActive = pad.IsActive;
                states.Add(state);
            }

            bool animated = true;
            //if dontclick is true (config setting)
            //if skipAnimation is true
            //if direction is released
            if (_dontClick.Value || skipAnimation || direction == PressurePadInput.Released)
                animated = false;   //disable animation

            if (_enableLogging.Value)
                LogSource.LogInfo($"Setting {states.Count} device states, skip animation: {animated}");

            cont.SetLightsState(states.ToArray(), true, animated);
        }

        private void handlePadModeChange(PressurePad pad)
        {
            if (_enableLogging.Value)
                LogSource.LogInfo("Pressurepad mode change event");
            var tacticals = TacticalDeviceHelper.GetTacticalDevicesOfType(cont, pad.DeviceType);
            List<FirearmLightStateStruct> states = [];

            //cancel if nothing to toggle
            if (tacticals.Count <= 0)
                return;

            foreach (var item in tacticals)
            {
                var state = TacticalDeviceHelper.getTacticalDeviceState(item);
                var nextLightMode = item.LightMod.method_0(state.LightMode + 1);
                state.LightMode = nextLightMode;
                states.Add(state);
            }

            if (_enableLogging.Value)
                LogSource.LogInfo($"Setting {states.Count} device states, skip animation: {_dontClick.Value}");

            cont.SetLightsState(states.ToArray(), true, !_dontClick.Value);
        }

        private bool TryInitController()
        {
            AbstractGame game = Singleton<AbstractGame>.Instance;
            
            if (game == null)
            {
                return false;
            }

            Player player = null;

            player = GamePlayerOwner.MyPlayer;

            if (player == null)
            {
                return false;
            }

            var hands = player.HandsController;

            if (hands == null)
            {
                return false;
            }

            cont = hands as Player.FirearmController;
            if (cont == null)
            {
                return false;
            }
            if(_enableLogging.Value)
                LogSource.LogInfo("Initialized firearm controller");
            return true;
        }

    }

}
