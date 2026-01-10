using System;
using System.Collections.Generic;
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
using PressurePads.ExamplePatches;
using PressurePads.Helpers;
using UnityEngine;
using static HairRenderer;

namespace PressurePads
{
    // first string below is your plugin's GUID, it MUST be unique to any other mod. Read more about it in BepInEx docs. Be sure to update it if you copy this project.
    [BepInPlugin("PressurePads.UniqueGUID", "PressurePads", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;

        // BaseUnityPlugin inherits MonoBehaviour, so you can use base unity functions like Awake() and Update()
        private void Awake()
        {
            // save the Logger to public static field so we can use it elsewhere in the project
            LogSource = Logger;
            LogSource.LogInfo("plugin loaded!");

            // uncomment line(s) below to enable desired example patch, then press F6 to build the project
            // if this solution is properly placed in a YourSPTInstall/Development folder, the compiled plugin will automatically be copied into YourSPTInstall/BepInEx/plugins
            //new SimplePatch().Enable();

            //new OnTogglePatch().Enable();
        }

        private Player.FirearmController cont;

        public KeyCode flashlightKey = KeyCode.Z;
        public bool isHeld = false;
        public bool isToggled = false;

        private float lastClickTime = 0f;
        private float doubleClickThreshold = 0.3f;

        private readonly PressurePad flashlightPad = new PressurePad(KeyCode.Z, TacticalDeviceHelper.DeviceType.Flashlight);
        private readonly PressurePad otherPad = new PressurePad(KeyCode.V, TacticalDeviceHelper.DeviceType.Other);

        public List<TacticalComboVisualController> TacticalDevices;

        private void Update()
        {
            if (!Input.GetKeyDown(flashlightKey) && !Input.GetKeyUp(flashlightKey))
                return;

            if (cont == null && !TryInitController())
                return;

            //manage pressure pad state
            if (Input.GetKeyDown(flashlightKey))
            {
                if (isToggled)
                {
                    isToggled = false;
                    LogSource.LogWarning("Toggle OFF");
                }

                float clickTime = Time.time;
                LogSource.LogWarning($"Gap: {clickTime - lastClickTime}");
                if (clickTime - lastClickTime < doubleClickThreshold)
                {
                    LogSource.LogWarning("Toggle ON");
                    isToggled = true;
                }

                isHeld = true;
                lastClickTime = clickTime;
            }

            if (Input.GetKeyUp(flashlightKey))
            {
                isHeld = false;
            }

            //apply pressure pad state
            LogSource.LogWarning($"Pad is pressed: {computeLightState()}");

            //get all flashlights
            //var tacticals = TacticalDeviceHelper.GetTacticalDevices(cont);
            var tacticals = TacticalDeviceHelper.GetTacticalDevicesOfType(cont, TacticalDeviceHelper.DeviceType.Flashlight);
            List<FirearmLightStateStruct> tacStates = [];
            LogSource.LogInfo($"{tacticals.Count} devices found");
            foreach (var item in tacticals)
            {
                var light = item.LightMod;
                LogSource.LogInfo(
                    $"IsActive={light.IsActive}, \n" +
                    $"Mode={light.SelectedMode}, \n" +
                    $"ModesCount={light.Ginterface396_0.ModesCount}, \n" +
                    $"ID={light.Item.Id}, \n" +
                    $"ShortName={light.Item.ShortName.Localized()}, \n" +
                    $"Name={light.Item.Name.Localized()}, \n" +
                    $"Type={TacticalDeviceHelper.DetectType(item)} \n"

                );
                var state = TacticalDeviceHelper.getTacticalDeviceState(item);
                state.IsActive = computeLightState();
                tacStates.Add(state);
            }

            if (cont.SetLightsState(tacStates.ToArray(), true))
                Logger.LogInfo("Set light state");
            else
                Logger.LogInfo("Failed to set light state");
        }

        private bool computeLightState()
        {
            if (isHeld) return true;
            if (isToggled) return true;
            return false;
        }

        private bool TryInitController()
        {
            LogSource.LogInfo("[PressurePads] Init step 1: Entered TryInitController");

            AbstractGame game = Singleton<AbstractGame>.Instance;
            if (game == null)
            {
                LogSource.LogInfo("[PressurePads] Init step 2 FAILED: LocalGame.Instance is null");
                return false;
            }
            LogSource.LogInfo("[PressurePads] Init step 2 OK: AbstractGame.Instance exists");

            Player player = null;

            if (game is LocalGame lg)
            {
                player = lg.PlayerOwner.Player;
            }
            else if (game is HideoutGame hg)
            {
                player = hg.PlayerOwner.Player;
            }

            if (player == null)
            {
                LogSource.LogInfo("[PressurePads] Init step 4 FAILED: Player is null");
                return false;
            }
            LogSource.LogInfo("[PressurePads] Init step 4 OK: Player exists");

            var hands = player.HandsController;

            if (hands == null)
            {
                LogSource.LogInfo("[PressurePads] Init step 5 FAILED: HandsController is null");
                return false;
            }
            LogSource.LogInfo($"[PressurePads] Init step 5 OK: HandsController type = {hands.GetType().Name}");

            cont = hands as Player.FirearmController;
            if (cont == null)
            {
                LogSource.LogInfo("[PressurePads] Init step 6 FAILED: HandsController is not FirearmController");
                return false;
            }

            LogSource.LogInfo("[PressurePads] Init step 6 OK: FirearmController acquired");
            return true;
        }

    }

}
