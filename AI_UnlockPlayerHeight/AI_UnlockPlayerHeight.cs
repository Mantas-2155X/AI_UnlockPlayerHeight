using HarmonyLib;

using BepInEx;
using BepInEx.Harmony;
using BepInEx.Logging;
using BepInEx.Configuration;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using AIChara;
using AIProject;
using CharaCustom;

using UnityEngine;

namespace AI_UnlockPlayerHeight {
    [BepInPlugin(nameof(AI_UnlockPlayerHeight), nameof(AI_UnlockPlayerHeight), VERSION)]
    public class AI_UnlockPlayerHeight : BaseUnityPlugin
    {
        public const string VERSION = "1.1.2";
        private new static ManualLogSource Logger;

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

        private void Awake()
        {
            Logger = base.Logger;
            
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

        private static float GetHeight()
        {
            float height = 0.75f;
            
            if(inH)
                height = cardHeightDuringH.Value ? cardHeightValue : customHeightDuringH.Value / 100f;
            else
                height = cardHeight.Value ? cardHeightValue : customHeight.Value / 100f;

            return height;
        }
        
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

            float height = GetHeight();
            
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
                    if (child.name.Contains("Action"))
                        height = Mathf.Clamp01(height); // Some actions could have the camera too low or too high. Clamp to prevent that.
                    else
                        mul = 2.25f;

                child.localPosition = new Vector3(position.x, defaultY[i] + (-0.75f + height) * mul, position.z);
            }
        }
        
        private static IEnumerable<CodeInstruction> RemoveLock(IEnumerable<CodeInstruction> instructions, int min, int max, string name)
        {
            var il = instructions.ToList();
            
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.75f);
            if (index <= 0)
            {
                Logger.LogMessage("Failed transpiling '" + name + "' 0.75f index not found!");
                Logger.LogWarning("Failed transpiling '" + name + "' 0.75f index not found!");
                return il;
            }
            
            for(int i = min; i < max; i++)
                il[index + i].opcode = OpCodes.Nop;

            return il;
        }
        
        // Apply height, camera settings for free roam & events //
        [HarmonyPostfix, HarmonyPatch(typeof(PlayerActor), "InitializeIK")]
        public static void PlayerActor_InitializeIK_HeightPostfix(PlayerActor __instance) => ApplySettings(__instance);

        // Apply duringH height settings when starting H //
        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "InitCoroutine")]
        public static void HScene_InitCoroutine_HeightPostfix(HScene __instance)
        {
            inH = true;
            
            if (__instance != null && actor != null)
                ApplySettings(actor);
        }
        
        // Apply roam height settings when ending H //
        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "OnDisable")]
        public static void HScene_OnDisable_HeightPostfix(HScene __instance)
        {
            inH = false;
            
            if (__instance != null && actor != null)
                ApplySettings(actor);
        }
        
        // Save players height from card into cardHeightValue //
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "InitShapeBody")]
        public static void ChaControl_InitShapeBody_HeightPostfix(ChaControl __instance)
        {
            if (__instance != null && __instance.isPlayer) 
                cardHeightValue = __instance.chaFile.custom.body.shapeValueBody[0];
        }

        // Enable male height slider in charamaker //
        [HarmonyPrefix, HarmonyPatch(typeof(CustomControl), "Initialize")]
        public static void CustomControl_Initialize_HeightPrefix(CustomControl __instance)
        {
            var trav = Traverse.Create(__instance);
            
            GameObject[] objMale = trav.Field("hideByCondition").Field("objMale").GetValue<GameObject[]>();
            
            var list = new List<GameObject>();
            foreach (GameObject obj in objMale)
            {
                if (obj.GetComponent<CustomSliderSet>() != null) 
                    if(obj.GetComponent<CustomSliderSet>().title.text == "Height")
                        continue;

                list.Add(obj);
            }

            trav.Field("hideByCondition").Field("objMale").SetValue(list.ToArray());
        }

        //--Hard height lock of 75 for the player removal--//
        
        [HarmonyTranspiler, HarmonyPatch(typeof(Les), "setAnimationParamater")]
        public static IEnumerable<CodeInstruction> Les_setAnimationParamater_RemoveHeightLock(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.75f);
            if (index <= 0)
            {
                Logger.LogMessage("Failed transpiling 'Les_setAnimationParamater_RemoveHeightLock' 0.75f index not found!");
                Logger.LogWarning("Failed transpiling 'Les_setAnimationParamater_RemoveHeightLock' 0.75f index not found!");
                return il;
            }

            for (int i = -4; i < 2; i++)
                il[index + i].opcode = OpCodes.Nop;

            if (il[index - 22].opcode != OpCodes.Ldarg)
            {
                Logger.LogMessage("Failed transpiling 'Les_setAnimationParamater_RemoveHeightLock' Ldarg index not found!");
                Logger.LogWarning("Failed transpiling 'Les_setAnimationParamater_RemoveHeightLock' Ldarg index not found!");
                return il;
            }
            
            for (int i = -22; i < -16; i++)
                il[index + i].opcode = OpCodes.Nop;

            return il;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(HScene), "ChangeAnimation")]
        public static IEnumerable<CodeInstruction> HScene_ChangeAnimation_RemoveHeightLock(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();

            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Callvirt && (instruction.operand as MethodInfo)?.Name == "SetShapeBodyValue");
            if (index <= 0)
            {
                Logger.LogMessage("Failed transpiling 'HScene_ChangeAnimation_RemoveHeightLock' SetShapeBodyValue index not found!");
                Logger.LogWarning("Failed transpiling 'HScene_ChangeAnimation_RemoveHeightLock' SetShapeBodyValue index not found!");
                return il;
            }

            for (int i = -8; i < 2; i++)
                il[index + i].opcode = OpCodes.Nop;
            
            return il;
        }
        
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "Initialize")]
        public static IEnumerable<CodeInstruction> ChaControl_Initialize_RemoveHeightLock(IEnumerable<CodeInstruction> instructions) => RemoveLock(instructions, -6, 2, "ChaControl_Initialize_RemoveHeightLock");

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "InitShapeBody")]
        public static IEnumerable<CodeInstruction> ChaControl_InitShapeBody_RemoveHeightLock(IEnumerable<CodeInstruction> instructions) => RemoveLock(instructions, -2, 2, "ChaControl_InitShapeBody_RemoveHeightLock");

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "SetShapeBodyValue")]
        public static IEnumerable<CodeInstruction> ChaControl_SetShapeBodyValue_RemoveHeightLock(IEnumerable<CodeInstruction> instructions) => RemoveLock(instructions, 0, 2, "ChaControl_SetShapeBodyValue_RemoveHeightLock");

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "UpdateShapeBodyValueFromCustomInfo")]
        public static IEnumerable<CodeInstruction> ChaControl_UpdateShapeBodyValueFromCustomInfo_RemoveHeightLock(IEnumerable<CodeInstruction> instructions) => RemoveLock(instructions, -2, 2, "ChaControl_UpdateShapeBodyValueFromCustomInfo_RemoveHeightLock");
    }
}
