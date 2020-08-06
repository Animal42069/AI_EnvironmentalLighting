﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;

namespace AIEnvironmentalLighting
{
    [BepInPlugin("animal42069.aidarkernights", "Environmental Lighting", VERSION)]
    [BepInProcess("AI-Syoujyo")]
    public class AIEnvironmentalLighting : BaseUnityPlugin
    {
        public const string VERSION = "1.0.0.0";
        private static Harmony harmony;
        private static ConfigEntry<bool> _dhh_override;
        private static ConfigEntry<float> _main_intensity_multipier;
        private static ConfigEntry<float> _shadow_strength_multiplier;
        private static ConfigEntry<float> _ambient_intensity_multiplier;

        private static float directLightIntensity = 0;
        private static float shadowStrength = 0;
        private static float ambientIntensity = 0;

        private void Awake()
        {
            _dhh_override = Config.Bind<bool>("Direct Light", "Override DHH", true, "Replace DHH's static light intensity values with the original dynamic environmental lighting");
            _main_intensity_multipier = Config.Bind<float>("Direct Light", "Intensity Multiplier", 1.0f, "Amount to multiply direct light intensity by before applying.");
            _shadow_strength_multiplier = Config.Bind<float>("Direct Light", "Shadow Strength Multiplier", 1.0f, "Amount to multiply direct light intensity by before applying.");
            _ambient_intensity_multiplier = Config.Bind<float>("Ambient Light", "Intensity Multiplier", 0.5f, "Amount to multiply ambient light intensity by before applying.");
            harmony = new Harmony("AIEnvironmentalLighting");
            harmony.PatchAll(typeof(AIEnvironmentalLighting));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(EnviroSky), "Update")]
        private static void EnviroSky_EarlyUpdate(EnviroSky __instance)
        {
            if (RenderSettings.ambientMode != UnityEngine.Rendering.AmbientMode.Skybox)
            {
                Console.WriteLine("Setting Sky to SkyBox");
                __instance.lightSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            }

        }

        [HarmonyPostfix, HarmonyPatch(typeof(EnviroSky), "Update")]
        private static void EnviroSky_Update(EnviroSky __instance, Light ___MainLight)
        {
            float solarTime = __instance.GameTime.solarTime;
            if (solarTime <= 0.45 && solarTime >= 0.4)
            {
                float sunIntensity = (solarTime - 0.4f) * 20f;
                float lunarIntensity = 1 - sunIntensity;

                directLightIntensity = (__instance.lightSettings.directLightSunIntensity.Evaluate(solarTime) * sunIntensity) + __instance.lightSettings.directLightMoonIntensity.Evaluate(__instance.GameTime.lunarTime) * lunarIntensity;
                __instance.Components.DirectLight.rotation = Quaternion.Lerp(__instance.Components.Moon.transform.rotation, __instance.Components.Sun.transform.rotation, sunIntensity);
            }
            else
            {
                directLightIntensity = ___MainLight.intensity;
            }

            shadowStrength = ___MainLight.shadowStrength;
            ambientIntensity = RenderSettings.ambientIntensity;

            if (_dhh_override.Value)
                return;

            ___MainLight.intensity = directLightIntensity * _main_intensity_multipier.Value;
            ___MainLight.shadowStrength = shadowStrength * _shadow_strength_multiplier.Value;
            RenderSettings.ambientIntensity = ambientIntensity * _ambient_intensity_multiplier.Value;

            //           Console.WriteLine("Update directLightIntensity " + ___MainLight.intensity + " shadowStrength " + ___MainLight.shadowStrength + " ambientIntensity " + RenderSettings.ambientIntensity);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(EnviroSky), "LateUpdate")]
        private static void EnviroSky_LateUpdate(EnviroSky __instance, Light ___MainLight)
        {
            if (!_dhh_override.Value)
                return;

            ___MainLight.intensity = directLightIntensity * _main_intensity_multipier.Value;
            ___MainLight.shadowStrength = shadowStrength * _shadow_strength_multiplier.Value;
            RenderSettings.ambientIntensity = ambientIntensity * _ambient_intensity_multiplier.Value;

            /*
                        Console.WriteLine("AmbientMode " + __instance.lightSettings.ambientMode + " Intensity " + RenderSettings.ambientIntensity + " Intensity Calc " + __instance.lightSettings.ambientIntensity.Evaluate(__instance.GameTime.solarTime));
                        Console.WriteLine("solarTime " + __instance.GameTime.solarTime + " Intensity " + ___MainLight.intensity + " shadowStrength " + ___MainLight.shadowStrength);
                        Console.WriteLine("globalVolumeLightIntensity " + __instance.globalVolumeLightIntensity + " DirectLight.rotation" + __instance.Components.DirectLight.rotation);*/
        }
    }
}
