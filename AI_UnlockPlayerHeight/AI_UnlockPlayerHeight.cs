using HarmonyLib;

using BepInEx;
using BepInEx.Harmony;
using BepInEx.Configuration;

using System.Collections.Generic;
using System.Reflection.Emit;

using AIProject;
using AIChara;

using UnityEngine;

using JetBrains.Annotations;

namespace AI_UnlockPlayerHeight {
    [BepInPlugin(nameof(AI_UnlockPlayerHeight), nameof(AI_UnlockPlayerHeight), "1.1.0")]
    public class AI_UnlockPlayerHeight : BaseUnityPlugin
    {
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

        private static float GetHeight(ChaControl chaControl)
        {
            if (chaControl == null)
                return 0.75f;

            if (cardHeight.Value)
                return chaControl.chaFile.custom.body.shapeValueBody[0];
            
            if (!cardHeight.Value)
                return (customHeight.Value / 100f);

            return 0.75f;
        }

        private void Awake()
        {
            alignCamera = Config.AddSetting(new ConfigDefinition("AI_UnlockPlayerHeight", "Align camera to height"), true, new ConfigDescription("Aligns camera position according to your height"));
            cardHeight = Config.AddSetting(new ConfigDefinition("AI_UnlockPlayerHeight", "Set height from card"), true, new ConfigDescription("ON->Set height according to your character card OFF->Custom Height"));
            customHeight = Config.AddSetting(new ConfigDefinition("AI_UnlockPlayerHeight", "Custom height"), 75, new ConfigDescription("Works only if 'Set height from card' is OFF", new AcceptableValueRange<int>(-100, 200)));

            HarmonyWrapper.PatchAll(typeof(AI_UnlockPlayerHeight));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerActor), "InitializeIK")][UsedImplicitly]
        public static void PlayerActor_InitializeIK_HeightPostfix(PlayerActor __instance)
        {
            if (!alignCamera.Value || __instance == null) return;

            ChaControl chaControl = __instance.ChaControl;
            PlayerController controller = __instance.PlayerController;

            if (chaControl == null || controller == null) return;

            for (int i = 0; i < controller.transform.childCount; i++)
            {
                Transform child = controller.transform.GetChild(i);
                
                float height = chaControl.GetShapeBodyValue(0);
                float mul = 2f;

                if (child.name.Contains("Lookat"))
                {
                    if (child.name.Contains("Action"))
                        Mathf.Clamp01(height); // Some actions could have the camera too low or too high. Clamp to prevent that.
                    else
                        mul = 2.25f;
                }

                child.localPosition = new Vector3(0f, defaultY[i] + (-0.75f + height) * mul, 0f);
            }
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "Initialize")][UsedImplicitly]
        public static IEnumerable<CodeInstruction> ChaControl_Initialize_HeightTranspile(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.75f)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AI_UnlockPlayerHeight), nameof(GetHeight)));
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "InitShapeBody")][UsedImplicitly]
        public static IEnumerable<CodeInstruction> ChaControl_InitShapeBody_HeightTranspile(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.75f)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AI_UnlockPlayerHeight), nameof(GetHeight)));
                }
                else
                {
                    yield return instruction;
                }
            }
        }
        
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "SetShapeBodyValue")][UsedImplicitly]
        public static IEnumerable<CodeInstruction> ChaControl_SetShapeBodyValue_HeightTranspile(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.75f)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0) {labels = instruction.labels};
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AI_UnlockPlayerHeight), nameof(GetHeight)));
                }
                else
                {
                    yield return instruction;
                }
            }
        }
        
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "UpdateShapeBodyValueFromCustomInfo")][UsedImplicitly]
        public static IEnumerable<CodeInstruction> ChaControl_UpdateShapeBodyValueFromCustomInfo_HeightTranspile(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.75f)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AI_UnlockPlayerHeight), nameof(GetHeight)));
                }
                else
                {
                    yield return instruction;
                }
            }
        }
        
    }
}
