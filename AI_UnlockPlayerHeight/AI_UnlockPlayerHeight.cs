using System.Collections;
using BepInEx;
using BepInEx.Harmony;
using BepInEx.Logging;
using BepInEx.Configuration;

using AIChara;
using AIProject;

using UnityEngine;

namespace AI_UnlockPlayerHeight {
    [BepInPlugin(nameof(AI_UnlockPlayerHeight), nameof(AI_UnlockPlayerHeight), VERSION)]
    public class AI_UnlockPlayerHeight : BaseUnityPlugin
    {
        public const string VERSION = "1.2.0";
        public new static ManualLogSource Logger;

        private static ConfigEntry<bool> alignCamera { get; set; }
        private static ConfigEntry<float> lookAtOffset { get; set; }
        private static ConfigEntry<float> lookAtPOVOffset { get; set; }

        private static ConfigEntry<bool> cardHeight { get; set; }
        private static ConfigEntry<int> customHeight { get; set; }
        
        private static ConfigEntry<bool> cardHeightDuringH { get; set; }
        private static ConfigEntry<int> customHeightDuringH { get; set; }

        public static PlayerActor actor;

        public static bool inH;
        public static float cardHeightValue;
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

        private static AI_UnlockPlayerHeight instance;
        
        private void Awake()
        {
            instance = this;
            Logger = base.Logger;
            
            alignCamera = Config.Bind(new ConfigDefinition("Camera", "Align camera to player height"), true, new ConfigDescription("Aligns camera position according to player height"));
            lookAtOffset = Config.Bind(new ConfigDefinition("Camera", "Camera y offset"), 0f, new ConfigDescription("Camera lookAt y offset", new AcceptableValueRange<float>(-10f, 10f)));
            lookAtPOVOffset = Config.Bind(new ConfigDefinition("Camera", "Camera POV y offset"), 0f, new ConfigDescription("Camera lookAtPOV y offset", new AcceptableValueRange<float>(-10f, 10f)));

            cardHeight = Config.Bind(new ConfigDefinition("Free Roam & Events", "Height from card"), true, new ConfigDescription("Set players height according to the value in the card", null, new ConfigurationManagerAttributes { Order = 1 }));
            customHeight = Config.Bind(new ConfigDefinition("Free Roam & Events", "Custom height"), 75, new ConfigDescription("If 'Height from card' is off, use this value instead'", new AcceptableValueRange<int>(-100, 200), null, new ConfigurationManagerAttributes { Order = 2 }));

            cardHeightDuringH = Config.Bind(new ConfigDefinition("H Scene", "Height from card (H)"), false, new ConfigDescription("Set players height according to the value in the card", null, new ConfigurationManagerAttributes { Order = 1 }));
            customHeightDuringH = Config.Bind(new ConfigDefinition("H Scene", "Custom height (H)"), 75, new ConfigDescription("If 'Height from card' is off, use this value instead'", new AcceptableValueRange<int>(-100, 200), null, new ConfigurationManagerAttributes { Order = 2 }));

            HarmonyWrapper.PatchAll(typeof(CoreHooks));
            
            if (Application.productName == "AI-Syoujyo")
            {
                alignCamera.SettingChanged += delegate { ApplySettings(actor); };
                lookAtOffset.SettingChanged += delegate { ApplySettings(actor); };

                cardHeight.SettingChanged += delegate { ApplySettings(actor); };
                customHeight.SettingChanged += delegate { ApplySettings(actor); };

                cardHeightDuringH.SettingChanged += delegate { ApplySettings(actor); };
                customHeightDuringH.SettingChanged += delegate { ApplySettings(actor); };
                
                HarmonyWrapper.PatchAll(typeof(GameHooks));
            }

            inH = false;
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
        
        public static void ApplySettings(PlayerActor __instance)
        {
            if (__instance == null) 
                return;

            actor = __instance;
            
            ChaControl chaControl = actor.ChaControl;
            if (chaControl == null) 
                return;

            float height = GetHeight();
            chaControl.SetShapeBodyValue(0, height);
            
            PlayerController controller = actor.PlayerController;
            if (controller == null) 
                return;

            instance.StartCoroutine(ApplySettings_Coroutine(controller, chaControl));
        }

        private static IEnumerator ApplySettings_Coroutine(PlayerController controller, ChaControl chaControl)
        {
            yield return null;
            
            var eyeObjs = chaControl.eyeLookCtrl.eyeLookScript.eyeObjs;
            
            var newEyePos = Vector3.Lerp(eyeObjs[0].eyeTransform.position, eyeObjs[1].eyeTransform.position, 0.5f);
            var newHeadPos = chaControl.objHead.transform.position;
            
            for (int i = 0; i < controller.transform.childCount; i++)
            {
                Transform child = controller.transform.GetChild(i);
                Vector3 position = child.position;
                Vector3 localPosition = child.localPosition;

                if (!alignCamera.Value)
                {
                    child.localPosition = new Vector3(localPosition.x, defaultY[i], localPosition.z);
                    continue;
                }

                if (child.name.Contains("Head"))
                {
                    child.position = new Vector3(position.x, newHeadPos.y, position.z);
                    continue;
                }

                if (child.name.Contains("Action"))
                {
                    child.localPosition = new Vector3(localPosition.x, defaultY[i] + (-0.75f + Mathf.Clamp01(GetHeight())) * 2, localPosition.z);
                    continue;
                }
                
                if (child.name.Contains("Lookat"))
                {
                    float offset = lookAtOffset.Value;

                    if (child.name.Contains("POV"))
                        offset = lookAtPOVOffset.Value;

                    child.position = new Vector3(position.x, newEyePos.y + offset, position.z);
                    continue;
                }
                
                child.localPosition = new Vector3(localPosition.x, defaultY[i] + (-0.75f + GetHeight()) * 2, localPosition.z);
            }
        }
    }
}
