using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using AIChara;
using System.Linq;
using System;

namespace EnvironmentalLighting
{
    [BepInPlugin(GUID, "Environmental Lighting", VERSION)]
    [BepInProcess("AI-Syoujyo")]
    public class AIEnvironmentalLighting : BaseUnityPlugin
    {
        public const string VERSION = "1.3.0.0";
        internal const string GUID = "animal42069.aienvironmentlighting";
        internal static Harmony harmony;
        internal static ConfigEntry<bool> _dhh_override;
        internal static ConfigEntry<float> _sun_intensity_multipier;
        internal static ConfigEntry<float> _sun_intensity_offset;
        internal static ConfigEntry<float> _sun_shadow_multipier;
        internal static ConfigEntry<float> _moon_intensity_multipier;
        internal static ConfigEntry<float> _moon_intensity_offset;
        internal static ConfigEntry<float> _moon_shadow_multipier;
        internal static ConfigEntry<float> _ambient_day_intensity_multiplier;
        internal static ConfigEntry<float> _ambient_day_sky_intensity_multiplier;
        internal static ConfigEntry<float> _ambient_day_equator_intensity_multiplier;
        internal static ConfigEntry<float> _ambient_day_ground_intensity_multiplier;
        internal static ConfigEntry<float> _ambient_night_intensity_multiplier;
        internal static ConfigEntry<float> _ambient_night_sky_intensity_multiplier;
        internal static ConfigEntry<float> _ambient_night_equator_intensity_multiplier;
        internal static ConfigEntry<float> _ambient_night_ground_intensity_multiplier;

        internal static ConfigEntry<float> _cloud1_intensity_multiplier;
        internal static ConfigEntry<float> _cloud2_intensity_multiplier;
        internal static ConfigEntry<float> _cloud3_intensity_multiplier;
        internal static ConfigEntry<float> _cloud4_intensity_multiplier;
        internal static ConfigEntry<float> _fog_intensity_multiplier;
        internal static ConfigEntry<float> _rain_intensity_multiplier;
        internal static ConfigEntry<float> _storm_intensity_multiplier;

        internal static ConfigEntry<AmbientMode> _ambient_mode;

        internal static ConfigEntry<float> _torchColorRed;
        internal static ConfigEntry<float> _torchColorGreen;
        internal static ConfigEntry<float> _torchColorBlue;
        internal static ConfigEntry<Color> _waistLampColor;
        internal static ConfigEntry<Color> _playerLampColor;
        internal static ConfigEntry<LightShadows> _waistLampShadows;
        internal static ConfigEntry<LightShadows> _playerLampShadows;
        internal static ConfigEntry<float> _waistLampShadowStrength;
        internal static ConfigEntry<float> _playerLampShadowStrength;
        internal static ConfigEntry<float> _waistLampShadowNearPlane;
        internal static ConfigEntry<float> _playerLampShadowNearPlane;
        internal static ConfigEntry<int> _portableLampUpdateRate;

        internal static float directLightIntensity = 0;
        internal static float directLightShadowStrength = 0;
        internal static float ambientIntensity = 0;
        internal static Color ambientSkyColor = new Color(0, 0, 0, 0);
        internal static Color ambientEquatorColor = new Color(0, 0, 0, 0);
        internal static Color ambientGroundColor = new Color(0, 0, 0, 0);

        internal static float nextAmbientSkyUpdate = 0;

        internal static int updateLightCount = 0;

        internal void Awake()
        {
            _dhh_override = Config.Bind("Graphics DHH", "Override", true, "Replace DHH's static light intensity values with the original dynamic environmental lighting");
            _sun_intensity_multipier = Config.Bind("Sunlight", "Intensity Multiplier", 1.0f, new ConfigDescription("Amount to multiply sun direct light intensity by before applying.  Recommend 0.5", new AcceptableValueRange<float>(0, 1)));
            _sun_intensity_offset = Config.Bind("Sunlight", "Intensity Offset", 0.0f, "Amount to offset sun direct light intensity by before applying.  Recommend 0.5");
            _sun_shadow_multipier = Config.Bind("Sunlight", "Shadow Strength Multiplier", 1.0f, new ConfigDescription("Amount to multiply sun direct light shadow strength by before applying.  Recommend 0.8", new AcceptableValueRange<float>(0, 1)));
            _moon_intensity_multipier = Config.Bind("Moonlight", "Intensity Multiplier", 1.0f, new ConfigDescription("Amount to multiply moon direct light intensity by before applying.  Recommend 0.2", new AcceptableValueRange<float>(0, 1)));
            _moon_intensity_offset = Config.Bind("Moonlight", "Intensity Offset", 0.0f, "Amount to offset moon direct light intensity by before applying.  Recommend 0.4");
            _moon_shadow_multipier = Config.Bind("Moonlight", "Shadow Strength Multiplier", 1.0f, new ConfigDescription("Amount to multiply moon direct shadow strength by before applying.  Recommend 1.0", new AcceptableValueRange<float>(0, 1)));
            _ambient_mode = Config.Bind("Ambient Light", "Ambient Mode", AmbientMode.Trilight, "Ambient light mode to use.  WARNING: Skybox mode doesn't properly take into account the weather's effect on ambient light and isn't recommended");
            _ambient_day_intensity_multiplier = Config.Bind("Ambient Daylight", "Intensity Multiplier", 1.0f, new ConfigDescription("Amount to multiply ambient light intensity by before applying.", new AcceptableValueRange<float>(0, 1)));
            _ambient_day_sky_intensity_multiplier = Config.Bind("Ambient Daylight", "Trilight Sky Intensity Multiplier", 1.0f, new ConfigDescription("Amount to multiply Trilight ambient light intensity by before applying.", new AcceptableValueRange<float>(0, 1)));
            _ambient_day_equator_intensity_multiplier = Config.Bind("Ambient Daylight", "Trilight Equator Intensity Multiplier", 1.0f, new ConfigDescription("Amount to multiply Trilight ambient light intensity by before applying.", new AcceptableValueRange<float>(0, 1)));
            _ambient_day_ground_intensity_multiplier = Config.Bind("Ambient Daylight", "Trilight Ground Intensity Multiplier", 1.0f, new ConfigDescription("Amount to multiply Trilight ambient light intensity by before applying.", new AcceptableValueRange<float>(0, 1)));
            _ambient_night_intensity_multiplier = Config.Bind("Ambient Nightlight", "Intensity Multiplier", 1.0f, new ConfigDescription("Amount to multiply ambient light intensity by before applying.  Recommend 0.4", new AcceptableValueRange<float>(0, 1)));
            _ambient_night_sky_intensity_multiplier = Config.Bind("Ambient Nightlight", "Trilight Sky Intensity Multiplier", 1.0f, new ConfigDescription("Amount to multiply Trilight ambient light intensity by before applying.  Recommend 0.4", new AcceptableValueRange<float>(0, 1)));
            _ambient_night_equator_intensity_multiplier = Config.Bind("Ambient Nightlight", "Trilight Equator Intensity Multiplier", 1.0f, new ConfigDescription("Amount to multiply Trilight ambient light intensity by before applying.  Recommend 0.2", new AcceptableValueRange<float>(0, 1)));
            _ambient_night_ground_intensity_multiplier = Config.Bind("Ambient Nightlight", "Trilight Ground Intensity Multiplier", 1.0f, new ConfigDescription("Amount to multiply Trilight ambient light intensity by before applying.  Recommend 0.3", new AcceptableValueRange<float>(0, 1)));
            _cloud1_intensity_multiplier = Config.Bind("Weather", "Light Clouds Intensity Multiplier", 1.0f, new ConfigDescription("Amount to adjust sun/moon light intensity and shadow strength when there are light clouds.  Recommend 0.95", new AcceptableValueRange<float>(0, 1)));
            _cloud2_intensity_multiplier = Config.Bind("Weather", "Partly Cloudy Intensity Multiplier", 1.0f, new ConfigDescription("Amount to adjust sun/moon light intensity and shadow strength when it is partly cloudy.  Recommend 0.9", new AcceptableValueRange<float>(0, 1)));
            _cloud3_intensity_multiplier = Config.Bind("Weather", "Mostly Cloudy Intensity Multiplier", 1.0f, new ConfigDescription("Amount to adjust sun/moon light intensity and shadow strength when it is mostly cloudy.  Recommend 0.85", new AcceptableValueRange<float>(0, 1)));
            _cloud4_intensity_multiplier = Config.Bind("Weather", "Completely Cloudy Intensity Multiplier", 1.0f, new ConfigDescription("Amount to adjust sun/moon light intensity and shadow strength when it is completely cloudy.  Recommend 0.8", new AcceptableValueRange<float>(0, 1)));
            _fog_intensity_multiplier = Config.Bind("Weather", "Foggy Intensity Multiplier", 1.0f, new ConfigDescription("Amount to adjust sun/moon light intensity and shadow strength when it is foggy.  Recommend 0.7", new AcceptableValueRange<float>(0, 1)));
            _rain_intensity_multiplier = Config.Bind("Weather", "Rain Intensity Multiplier", 1.0f, new ConfigDescription("Amount to adjust sun/moon light intensity and shadow strength when it is raining.  Recommend 0.7", new AcceptableValueRange<float>(0, 1)));
            _storm_intensity_multiplier = Config.Bind("Weather", "Storm Intensity Multiplier", 1.0f, new ConfigDescription("Amount to adjust sun/moon light intensity when it is heavily storming.  Recommend 0.5", new AcceptableValueRange<float>(0, 1)));
            _torchColorRed = Config.Bind("Objects", "Fire Light Color Red", 1.0f, new ConfigDescription("Amount to scale the Red color element of fires in the game.  Recommend 0.8", new AcceptableValueRange<float>(0, 2)));
            _torchColorGreen = Config.Bind("Objects", "Fire Light Color Green", 1.0f, new ConfigDescription("Amount to scale the Green color element of fires in the game.  Recommend 1.0", new AcceptableValueRange<float>(0, 2)));
            _torchColorBlue = Config.Bind("Objects", "Fire Light Color Blue", 1.0f, new ConfigDescription("Amount to scale the Blue color element of fires in the game.  Recommend 1.0", new AcceptableValueRange<float>(0, 2)));
            _waistLampColor = Config.Bind("Objects", "Waist Lamp Color", new Color (1.0f, 0.6f, 0.2f, 1.0f), "Color Value to set waist lamps to.  Recommend CC9933FF");
            _playerLampColor = Config.Bind("Objects", "Player Lamp Color", new Color(1.0f, 0.56f, 0.09f, 1.0f), "Color Value to set player lamps and torches to.  Recomment CC8F17FF");
            _waistLampShadows = Config.Bind("Objects", "Waist Lamp Shadows", LightShadows.None, "Should waist lamps cast shadows.  WARNING: can cause a performance drop.  Recommend Soft or None");
            _playerLampShadows = Config.Bind("Objects", "Player Lamp Shadows", LightShadows.None, "Should player lamps cast shadows.  WARNING: can cause a performance drop.  Recommend Soft or None");
            _waistLampShadowStrength = Config.Bind("Objects", "Waist Lamp Shadow Strength", 1.0f, new ConfigDescription("Waist lamp shadow strength if shadows enabled.  Recommend 0.4", new AcceptableValueRange<float>(0, 1)));
            _playerLampShadowStrength = Config.Bind("Objects", "Player Lamp Shadow Strength", 1.0f, new ConfigDescription("Player lamp shadow strength if shadows enabled.  Recommend 0.4", new AcceptableValueRange<float>(0, 1)));
            _waistLampShadowNearPlane = Config.Bind("Objects", "Waist Lamp Shadow Near Plane", 0.2f, "Near clip plane of waist lamp shadows, if enabled.  Recommend 1.5");
            _playerLampShadowNearPlane = Config.Bind("Objects", "Player Lamp Shadow Near Plane", 0.2f, "Near clip plane of player lamp shadows, if enabled.  Recommend 0.4");
            _portableLampUpdateRate = Config.Bind("Objects", "Portable Lamp Update Rate", 180, new ConfigDescription("Controls how often to check for changes to portable lamps.", new AcceptableValueRange<int>(1, 1000)));

            harmony = new Harmony("AIEnvironmentalLighting");
            harmony.PatchAll(typeof(AIEnvironmentalLighting));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(EnviroSky), "Update")]
        internal static void EnviroSky_Update(EnviroSky __instance)
        {
            if (__instance.profile == null || !__instance.started || __instance.serverMode)
                return;

            float solarTime = __instance.GameTime.solarTime;
            if (solarTime > 0.45)
            {
                directLightIntensity = (__instance.MainLight.intensity * _sun_intensity_multipier.Value) + _sun_intensity_offset.Value;
                directLightShadowStrength = __instance.MainLight.shadowStrength * _sun_shadow_multipier.Value;
            }
            else if (solarTime > 0.4)
            {
                float sunIntensity = (solarTime - 0.4f) * 20f;
                float lunarIntensity = 1 - sunIntensity;

                directLightIntensity = (((__instance.lightSettings.directLightSunIntensity.Evaluate(solarTime) * _sun_intensity_multipier.Value) + _sun_intensity_offset.Value) * sunIntensity)
                                     + (((__instance.lightSettings.directLightMoonIntensity.Evaluate(__instance.GameTime.lunarTime) * _moon_intensity_multipier.Value) + _moon_intensity_offset.Value) * lunarIntensity);
                directLightShadowStrength = __instance.MainLight.shadowStrength * (_sun_shadow_multipier.Value * sunIntensity + _moon_shadow_multipier.Value * lunarIntensity);
                __instance.Components.DirectLight.rotation = Quaternion.Lerp(__instance.Components.Moon.transform.rotation, __instance.Components.Sun.transform.rotation, sunIntensity);
            }
            else
            {
                directLightIntensity = (__instance.MainLight.intensity * _moon_intensity_multipier.Value) + _moon_intensity_offset.Value;
                directLightShadowStrength = __instance.MainLight.shadowStrength * _moon_shadow_multipier.Value;
            }

            var currentWeather = Singleton<Manager.Game>.Instance?.Environment?.Weather;
            if (currentWeather != null)
            {
                float intensityMultiplier = 1.0f;

                if (currentWeather == AIProject.Weather.Cloud1)
                    intensityMultiplier = _cloud1_intensity_multiplier.Value;
                else if (currentWeather == AIProject.Weather.Cloud2)
                    intensityMultiplier = _cloud2_intensity_multiplier.Value;
                else if (currentWeather == AIProject.Weather.Cloud3)
                    intensityMultiplier = _cloud3_intensity_multiplier.Value;
                else if (currentWeather == AIProject.Weather.Cloud4)
                    intensityMultiplier = _cloud4_intensity_multiplier.Value;
                else if (currentWeather == AIProject.Weather.Fog)
                    intensityMultiplier = _fog_intensity_multiplier.Value;
                else if (currentWeather == AIProject.Weather.Rain)
                    intensityMultiplier = _rain_intensity_multiplier.Value;
                else if (currentWeather == AIProject.Weather.Storm)
                    intensityMultiplier = _storm_intensity_multiplier.Value;

                directLightIntensity *= intensityMultiplier;
                directLightShadowStrength *= intensityMultiplier;
            }

            if (RenderSettings.ambientMode == AmbientMode.Trilight)
            {
                ambientSkyColor = RenderSettings.ambientSkyColor;
                ambientEquatorColor = RenderSettings.ambientEquatorColor;
                ambientGroundColor = RenderSettings.ambientGroundColor;
            }
            else
            {
                ambientIntensity = RenderSettings.ambientIntensity;
                if (RenderSettings.ambientMode == AmbientMode.Skybox && (solarTime < 0.5 && solarTime > 0.4) &&
                   (nextAmbientSkyUpdate < __instance.internalHour || nextAmbientSkyUpdate > __instance.internalHour + 0.011f))
                {
                    DynamicGI.UpdateEnvironment();
                    nextAmbientSkyUpdate = __instance.internalHour + 0.01f;
                }
            }

            if (!_dhh_override.Value)
            {
                __instance.MainLight.intensity = directLightIntensity;
                __instance.MainLight.shadowStrength = directLightShadowStrength;
                ApplyAmbientIntensities(solarTime);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(EnviroSky), "LateUpdate")]
        internal static void EnviroSky_LateUpdate(EnviroSky __instance, Light ___MainLight)
        {
            if (_ambient_mode.Value != RenderSettings.ambientMode)
            {
                __instance.lightSettings.ambientMode = _ambient_mode.Value;
                RenderSettings.ambientMode = _ambient_mode.Value;
            }

            if (_dhh_override.Value)
            {
                ___MainLight.intensity = directLightIntensity;
                ___MainLight.shadowStrength = directLightShadowStrength;
                ApplyAmbientIntensities(__instance.GameTime.solarTime);
            }

            if (++updateLightCount > _portableLampUpdateRate.Value)
            {
                AdjustPortableLights();
                updateLightCount = 0;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Manager.Map), "InitSearchActorTargetsAll")]
        internal static void MapManager_InitSearchActorTargetsAll()
        {
            AdjustMapTorchLights();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Manager.Housing), "EndHousing")]
        internal static void Housing_EndHousing()
        {
            AdjustMapTorchLights();
        }

        internal static void ApplyAmbientIntensities(float solarTime)
        {
            if (solarTime > 0.45)
            {
                if (RenderSettings.ambientMode == AmbientMode.Trilight)
                {
                    RenderSettings.ambientSkyColor = ambientSkyColor * _ambient_day_sky_intensity_multiplier.Value;
                    RenderSettings.ambientEquatorColor = ambientEquatorColor * _ambient_day_equator_intensity_multiplier.Value;
                    RenderSettings.ambientGroundColor = ambientGroundColor * _ambient_day_ground_intensity_multiplier.Value;
                }
                else
                {
                    RenderSettings.ambientIntensity = ambientIntensity * _ambient_day_intensity_multiplier.Value;
                }
            }
            else if (solarTime > 0.4)
            {
                float sunIntensity = (solarTime - 0.4f) * 20f;
                float lunarIntensity = 1 - sunIntensity;
                if (RenderSettings.ambientMode == AmbientMode.Trilight)
                {
                    RenderSettings.ambientSkyColor = ambientSkyColor * (_ambient_day_sky_intensity_multiplier.Value * sunIntensity + _ambient_night_sky_intensity_multiplier.Value * lunarIntensity);
                    RenderSettings.ambientEquatorColor = ambientEquatorColor * (_ambient_day_equator_intensity_multiplier.Value * sunIntensity + _ambient_night_equator_intensity_multiplier.Value * lunarIntensity);
                    RenderSettings.ambientGroundColor = ambientGroundColor * (_ambient_day_ground_intensity_multiplier.Value * sunIntensity + _ambient_night_ground_intensity_multiplier.Value * lunarIntensity);
                }
                else
                {
                    RenderSettings.ambientIntensity = ambientIntensity * (_ambient_day_intensity_multiplier.Value * sunIntensity + _ambient_night_intensity_multiplier.Value * lunarIntensity);
                }
            }
            else
            {
                if (RenderSettings.ambientMode == AmbientMode.Trilight)
                {
                    RenderSettings.ambientSkyColor = ambientSkyColor * _ambient_night_sky_intensity_multiplier.Value;
                    RenderSettings.ambientEquatorColor = ambientEquatorColor * _ambient_night_equator_intensity_multiplier.Value;
                    RenderSettings.ambientGroundColor = ambientGroundColor * _ambient_night_ground_intensity_multiplier.Value;
                }
                else
                {
                    RenderSettings.ambientIntensity = ambientIntensity * _ambient_night_intensity_multiplier.Value;
                }
            }
        }

        internal static void AdjustMapTorchLights()
        {
            var lights = Resources.FindObjectsOfTypeAll<Light>();
            if (lights.IsNullOrEmpty())
                return;

            foreach (var light in lights)
            {
                if (light.name == null || !light.name.Contains("Point") || light.color.r < 1.0 || light.color.g >= 0.9 || light.color.b >= 0.9)
                    continue;

                light.color = new Color(_torchColorRed.Value * light.color.r, _torchColorGreen.Value * light.color.g, _torchColorBlue.Value * light.color.b);
            }
        }

        internal static void AdjustPortableLights()
        {
            AdjustPortableWaistLights();
            AdjustPortablePlayerLights();
        }

        internal static void AdjustPortableWaistLights()
        {
            var characters = Resources.FindObjectsOfTypeAll<ChaControl>();

            foreach (var character in characters)
            {
                var waist = character.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains("cf_J_Kosi01_s"));

                if (waist == null)
                    continue;

                var portableLight = waist.GetComponentsInChildren<Light>().FirstOrDefault();
                if (portableLight == null || portableLight.type != LightType.Point)
                    continue;

                portableLight.color = _waistLampColor.Value;
                portableLight.shadows = _waistLampShadows.Value;
                portableLight.shadowStrength = _waistLampShadowStrength.Value;
                portableLight.shadowNearPlane = _waistLampShadowNearPlane.Value;          
            }
        }

        internal static void AdjustPortablePlayerLights()
        {
            var characters = Resources.FindObjectsOfTypeAll<ChaControl>();

            foreach (var character in characters)
            {
                var hand = character.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name != null && x.name.Contains("cf_J_Hand_s_R"));

                if (hand == null)
                    continue;

                var portableLight = hand.GetComponentsInChildren<Light>().FirstOrDefault();
                if (portableLight == null || portableLight.type != LightType.Point)
                    continue;

                portableLight.color = _playerLampColor.Value;
                portableLight.shadows = _playerLampShadows.Value;
                portableLight.shadowStrength = _playerLampShadowStrength.Value;
                portableLight.shadowNearPlane = _playerLampShadowNearPlane.Value;
            }
        }
    }
}
