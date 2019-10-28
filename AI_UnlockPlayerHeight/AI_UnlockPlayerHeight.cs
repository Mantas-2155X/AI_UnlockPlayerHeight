using HarmonyLib;

using BepInEx;
using BepInEx.Harmony;
using BepInEx.Configuration;

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

using AIProject;
using AIChara;

using UnityEngine;

using JetBrains.Annotations;

namespace AI_UnlockPlayerHeight {
    [BepInPlugin(nameof(AI_UnlockPlayerHeight), nameof(AI_UnlockPlayerHeight), "1.0.0")]
    public class AI_UnlockPlayerHeight : BaseUnityPlugin
    {
        private static float cardHeightValue = -999f;
        
        private static PlayerActor actor;
        
        private static ConfigEntry<bool> alignCamera { get; set; }
        private static ConfigEntry<bool> cardHeight { get; set; }
        private static ConfigEntry<int> customHeight { get; set; }

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

            float height = cardHeight.Value ? cardHeightValue : customHeight.Value / 100f;
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
            alignCamera = Config.AddSetting(new ConfigDefinition("AI_UnlockPlayerHeight", "Align camera to height"), true, new ConfigDescription("Aligns camera position according to your height"));
            cardHeight = Config.AddSetting(new ConfigDefinition("AI_UnlockPlayerHeight", "Set height from card"), true, new ConfigDescription("ON->Set height according to your character card OFF->Custom Height"));
            customHeight = Config.AddSetting(new ConfigDefinition("AI_UnlockPlayerHeight", "Custom height"), 75, new ConfigDescription("Works only if 'Set height from card' is OFF", new AcceptableValueRange<int>(-100, 200)));

            customHeight.SettingChanged += delegate { if (actor != null) ApplySettings(actor); };
            cardHeight.SettingChanged += delegate { if (actor != null) ApplySettings(actor); };
            alignCamera.SettingChanged += delegate { if (actor != null) ApplySettings(actor); };

            HarmonyWrapper.PatchAll(typeof(AI_UnlockPlayerHeight));
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(PlayerActor), "InitializeIK")][UsedImplicitly]
        public static void PlayerActor_InitializeIK_HeightPostfix(PlayerActor __instance)
        {
            if (__instance != null) 
                ApplySettings(__instance);
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "InitShapeBody")][UsedImplicitly]
        public static void ChaControl_InitShapeBody_HeightPostfix(ChaControl __instance)
        {
            if (__instance != null && __instance.isPlayer) 
                cardHeightValue = __instance.chaFile.custom.body.shapeValueBody[0];
        }

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
