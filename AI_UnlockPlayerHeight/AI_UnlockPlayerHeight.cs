using HarmonyLib;

using BepInEx;
using BepInEx.Harmony;
using BepInEx.Configuration;

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

using AIChara;
using AIProject;

using UnityEngine;

using JetBrains.Annotations;

namespace AI_UnlockPlayerHeight {
    [BepInPlugin(nameof(AI_UnlockPlayerHeight), nameof(AI_UnlockPlayerHeight), "1.0.0")]
    public class AI_UnlockPlayerHeight : BaseUnityPlugin
    {

        private static ConfigEntry<bool> alignCamera { get; set; }
        
        private static ConfigEntry<bool> cardHeight { get; set; }
        private static ConfigEntry<int> customHeight { get; set; }
        
        private static ConfigEntry<bool> cardHeightDuringH { get; set; }
        private static ConfigEntry<int> customHeightDuringH { get; set; }

        private static PlayerActor actor;

        private static bool inH;
        private static float cardHeightValue;
        private static readonly float[] defaultY =
        {
            0f, 
            0f, 
            10f, 
            15f, 
            15f, 
            16.25f, 
            15f, 
            16f, 
            20.11f
        };

        private static void ApplySettings(PlayerActor __instance)
        {
            if (__instance == null) 
                return;

            actor = __instance;
            
            ChaControl chaControl = actor.ChaControl;
            if (chaControl == null) 
                return;

            PlayerController controller = actor.PlayerController;
            if (controller == null) 
                return;

            float height;
            
            if(inH)
                height = cardHeightDuringH.Value ? cardHeightValue : customHeightDuringH.Value / 100f;
            else
                height = cardHeight.Value ? cardHeightValue : customHeight.Value / 100f;

            chaControl.SetShapeBodyValue(0, height);
            
            for (int i = 0; i < controller.transform.childCount; i++)
            {
                Transform child = controller.transform.GetChild(i);
                Vector3 position = child.localPosition;
                
                if (!alignCamera.Value)
                {
                    child.localPosition = new Vector3(position.x, defaultY[i], position.z);
                    continue;
                }
                
                float mul = 2f;

                if (child.name.Contains("Lookat"))
                {
                    if (child.name.Contains("Action"))
                        height = Mathf.Clamp01(height); // Some actions could have the camera too low or too high. Clamp to prevent that.
                    else
                        mul = 2.25f;
                }

                child.localPosition = new Vector3(position.x, defaultY[i] + (-0.75f + height) * mul, position.z);
            }
        }
        
        private void Awake()
        {
            alignCamera = Config.AddSetting(new ConfigDefinition("Camera", "Align camera to player height"), true, new ConfigDescription("Aligns camera position according to player height"));
            
            cardHeight = Config.AddSetting(new ConfigDefinition("Free Roam & Events", "Height from card"), true, new ConfigDescription("Set players height according to the value in the card", null, new ConfigurationManagerAttributes { Order = 1 }));
            customHeight = Config.AddSetting(new ConfigDefinition("Free Roam & Events", "Custom height"), 75, new ConfigDescription("If 'Height from card' is off, use this value instead'", new AcceptableValueRange<int>(-100, 200), null, new ConfigurationManagerAttributes { Order = 2 }));

            cardHeightDuringH = Config.AddSetting(new ConfigDefinition("H Scene", "Height from card (H)"), false, new ConfigDescription("Set players height according to the value in the card", null, new ConfigurationManagerAttributes { Order = 1 }));
            customHeightDuringH = Config.AddSetting(new ConfigDefinition("H Scene", "Custom height (H)"), 75, new ConfigDescription("If 'Height from card' is off, use this value instead'", new AcceptableValueRange<int>(-100, 200), null, new ConfigurationManagerAttributes { Order = 2 }));
            
            alignCamera.SettingChanged += delegate { ApplySettings(actor); };

            cardHeight.SettingChanged += delegate { ApplySettings(actor); };
            customHeight.SettingChanged += delegate { ApplySettings(actor); };

            cardHeightDuringH.SettingChanged += delegate { ApplySettings(actor); };
            customHeightDuringH.SettingChanged += delegate { ApplySettings(actor); };

            inH = false;
            
            HarmonyWrapper.PatchAll(typeof(AI_UnlockPlayerHeight));
        }

        // Apply height, camera settings for free roam & events (false) //
        [HarmonyPostfix, HarmonyPatch(typeof(PlayerActor), "InitializeIK")][UsedImplicitly]
        public static void PlayerActor_InitializeIK_HeightPostfix(PlayerActor __instance)
        {
            if (__instance != null) 
                ApplySettings(__instance);
        }
        
        // Apply duringH height settings when starting H //
        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "InitCoroutine")][UsedImplicitly]
        public static void HScene_InitCoroutine_HeightPostfix(HScene __instance)
        {
            inH = true;
            
            if (__instance != null && actor != null)
                ApplySettings(actor);
        }
        
        // Apply roam height settings when ending H //
        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "OnDisable")][UsedImplicitly]
        public static void HScene_OnDisable_HeightPostfix(HScene __instance)
        {
            inH = false;
            
            if (__instance != null && actor != null)
                ApplySettings(actor);
        }
        
        // Save players height from card into a cardHeightValue //
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "InitShapeBody")][UsedImplicitly]
        public static void ChaControl_InitShapeBody_HeightPostfix(ChaControl __instance)
        {
            if (__instance != null && __instance.isPlayer) 
                cardHeightValue = __instance.chaFile.custom.body.shapeValueBody[0];
        }

        //--Hard height lock of 75 for the player removal--//
        
        [HarmonyTranspiler, HarmonyPatch(typeof(HScene), "ChangeAnimation")][UsedImplicitly]
        public static IEnumerable<CodeInstruction> HScene_ChangeAnimation_RemoveHeightLock(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.75f);
            if (index <= 0) return il;
            
            il[index - 7].opcode = OpCodes.Nop;
            il[index - 6].opcode = OpCodes.Nop;
            il[index - 5].opcode = OpCodes.Nop;
            il[index - 4].opcode = OpCodes.Nop;
            il[index - 3].opcode = OpCodes.Nop;
            il[index - 2].opcode = OpCodes.Nop;
            il[index - 1].opcode = OpCodes.Nop;
            il[index].opcode = OpCodes.Nop;
            il[index + 1].opcode = OpCodes.Nop;
            il[index + 2].opcode = OpCodes.Nop;

            return il;
        }
        
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "Initialize")][UsedImplicitly]
        public static IEnumerable<CodeInstruction> ChaControl_Initialize_RemoveHeightLock(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.75f);
            if (index <= 0) return il;
            
            il[index - 6].opcode = OpCodes.Nop;
            il[index - 5].opcode = OpCodes.Nop;
            il[index - 4].opcode = OpCodes.Nop;
            il[index - 3].opcode = OpCodes.Nop;
            il[index - 2].opcode = OpCodes.Nop;
            il[index - 1].opcode = OpCodes.Nop;
            il[index].opcode = OpCodes.Nop;
            il[index + 1].opcode = OpCodes.Nop;

            return il;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "InitShapeBody")][UsedImplicitly]
        public static IEnumerable<CodeInstruction> ChaControl_InitShapeBody_RemoveHeightLock(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.75f);
            if (index <= 0) return il;
            
            il[index - 2].opcode = OpCodes.Nop;
            il[index - 1].opcode = OpCodes.Nop;
            il[index].opcode = OpCodes.Nop;
            il[index + 1].opcode = OpCodes.Nop;

            return il;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "SetShapeBodyValue")][UsedImplicitly]
        public static IEnumerable<CodeInstruction> ChaControl_SetShapeBodyValue_RemoveHeightLock(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.75f);
            if (index <= 0) return il;
            
            il[index].opcode = OpCodes.Nop;
            il[index + 1].opcode = OpCodes.Nop;

            return il;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "UpdateShapeBodyValueFromCustomInfo")][UsedImplicitly]
        public static IEnumerable<CodeInstruction> ChaControl_UpdateShapeBodyValueFromCustomInfo_RemoveHeightLock(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.75f);
            if (index <= 0) return il;
            
            il[index - 2].opcode = OpCodes.Nop;
            il[index - 1].opcode = OpCodes.Nop;
            il[index].opcode = OpCodes.Nop;
            il[index + 1].opcode = OpCodes.Nop;

            return il;
        }
    }
}
