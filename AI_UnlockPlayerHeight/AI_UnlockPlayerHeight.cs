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
        
        private static ConfigEntry<bool> useOriginalHeight { get; set; }
        private static ConfigEntry<int> customHeight { get; set; }

        private void Awake()
        {
            useOriginalHeight = Config.AddSetting(new ConfigDefinition("AI_UnlockPlayerHeight", "Use original height"), true, new ConfigDescription("ON->Height from chara card OFF->Height from custom height"));
            customHeight = Config.AddSetting(new ConfigDefinition("AI_UnlockPlayerHeight", "Custom height"), 75, new ConfigDescription("Works only if Use Original Height Toggle is OFF", new AcceptableValueRange<int>(-100, 200)));
            
            HarmonyWrapper.PatchAll(typeof(AI_UnlockPlayerHeight));
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(PlayerActor), "InitializeIK")][UsedImplicitly]
        public static void PlayerActor_InitializeIK_HeightPostfix(PlayerActor __instance)
        {
            ChaControl chaControl = __instance.ChaControl;
            if (chaControl == null) return;

            ChaFileBody chaFileBody = chaControl.fileBody;
            if (chaFileBody == null) return;

            ActorCameraControl camControl = __instance.CameraControl;
            if (camControl == null) return;

            float height = chaFileBody.shapeValueBody[0];

            Transform lookAt = camControl.LocomotionSetting.LookAt;
            Transform lookAtPOV = camControl.LocomotionSetting.LookAtPOV;

            Vector3 oldLookAt = lookAt.localPosition;
            Vector3 oldLookAtPOV = lookAtPOV.localPosition;

            lookAt.localPosition = new Vector3(oldLookAt.x, 15f + (-0.75f + height) * 5, oldLookAt.z);
            lookAtPOV.localPosition = new Vector3(oldLookAtPOV.x, 17f + (-0.75f + height) * 5, oldLookAtPOV.z);

            Vector3 oldBodyTarget = __instance.FovTargetPoints[0].localPosition;
            Vector3 oldHeadTarget = __instance.FovTargetPoints[1].localPosition;
            
            __instance.FovTargetPoints[0].localPosition = new Vector3(oldBodyTarget.x, 10f + (-0.75f + height) * 3, oldBodyTarget.z);
            __instance.FovTargetPoints[1].localPosition = new Vector3(oldHeadTarget.x, 15f + (-0.75f + height) * 3, oldHeadTarget.z);
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "Initialize")][UsedImplicitly]
        public static IEnumerable<CodeInstruction> ChaControl_Initialize_HeightTranspile(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            var targetIndex = il.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.75f);
            if (targetIndex <= 0) return il;
            
            if (!useOriginalHeight.Value)
                il[targetIndex].operand = (float)customHeight.Value / 100;
            else
            {
                il[targetIndex - 6].opcode = OpCodes.Nop;
                il[targetIndex - 5].opcode = OpCodes.Nop;
                il[targetIndex - 4].opcode = OpCodes.Nop;
                il[targetIndex - 3].opcode = OpCodes.Nop;
                il[targetIndex - 2].opcode = OpCodes.Nop;
                il[targetIndex - 1].opcode = OpCodes.Nop;
                il[targetIndex].opcode = OpCodes.Nop;
                il[targetIndex + 1].opcode = OpCodes.Nop;
            }
            return il;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "InitShapeBody")][UsedImplicitly]
        public static IEnumerable<CodeInstruction> ChaControl_InitShapeBody_HeightTranspile(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            var targetIndex = il.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.75f);
            if (targetIndex <= 0) return il;
            
            if (!useOriginalHeight.Value)
                il[targetIndex].operand = (float)customHeight.Value / 100;
            else
            {
                il[targetIndex - 2].opcode = OpCodes.Nop;
                il[targetIndex - 1].opcode = OpCodes.Nop;
                il[targetIndex].opcode = OpCodes.Nop;
                il[targetIndex + 1].opcode = OpCodes.Nop;
            }
            return il;
        }
        
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "SetShapeBodyValue")][UsedImplicitly]
        public static IEnumerable<CodeInstruction> ChaControl_SetShapeBodyValue_HeightTranspile(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            var targetIndex = il.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.75f);
            if (targetIndex <= 0) return il;
            
            if (!useOriginalHeight.Value)
                il[targetIndex].operand = (float)customHeight.Value / 100;
            else
            {
                il[targetIndex].opcode = OpCodes.Nop;
                il[targetIndex + 1].opcode = OpCodes.Nop;
            }
            return il;
        }
        
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "UpdateShapeBodyValueFromCustomInfo")][UsedImplicitly]
        public static IEnumerable<CodeInstruction> ChaControl_UpdateShapeBodyValueFromCustomInfo_HeightTranspile(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            var targetIndex = il.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.75f);
            if (targetIndex <= 0) return il;
            
            if (!useOriginalHeight.Value)
                il[targetIndex].operand = (float)customHeight.Value / 100;
            else
            {
                il[targetIndex - 2].opcode = OpCodes.Nop;
                il[targetIndex - 1].opcode = OpCodes.Nop;
                il[targetIndex].opcode = OpCodes.Nop;
                il[targetIndex + 1].opcode = OpCodes.Nop;
            }
            return il;
        }
        
    }
}