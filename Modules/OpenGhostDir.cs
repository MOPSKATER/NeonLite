using HarmonyLib;
using MelonLoader;
using System.Diagnostics;
using UnityEngine;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    internal class OpenGhostDir : Module
    {

        private static MenuButtonHolder ghostButton;
        private static bool jobArchive;
        private static MelonPreferences_Entry<bool> _setting_GhostButton;

        public OpenGhostDir() =>
            _setting_GhostButton = NeonLite.Config_NeonLite.CreateEntry("Open Ghost Directory Button", true, description: "Shows a button at the end to open this level's ghost directory in the file explorer.");


        public static string GetGhostDirectory(LevelData level = null)
        {
            if (level == null)
                level = Singleton<Game>.Instance.GetCurrentLevel();
            string path = GhostRecorder.GetCompressedSavePathForLevel(level);
            path = Path.GetDirectoryName(path) + "/";
            return path;
        }

        private static void SetupButton(MenuButtonHolder button, LevelData level = null)
        {
            try
            {
                if (ghostButton != null)
                {
                    if (jobArchive)
                    {
                        UnityEngine.Object.Destroy(ghostButton.gameObject);
                        UnityEngine.Object.Destroy(ghostButton.transform.parent.gameObject);
                        ghostButton = null;
                    }
                }

                if (ghostButton == null)
                {
                    // copy the layout
                    var layout = UnityEngine.Object.Instantiate(button.transform.parent.gameObject, button.transform.parent.parent);
                    layout.name = "Ghost Button Holder";
                    // empty it
                    foreach (Transform child in layout.transform)
                        UnityEngine.Object.Destroy(child.gameObject);

                    var ghostObject = UnityEngine.Object.Instantiate(button.gameObject, layout.transform);
                    ghostButton = ghostObject.GetComponent<MenuButtonHolder>();
                    var backComponent = ghostObject.GetComponent<BackButtonAccessor>();
                    if (backComponent)
                        UnityEngine.Object.Destroy(backComponent);

                    ghostButton.ButtonRef.onClick.RemoveAllListeners();
                    ghostButton.ButtonRef.onClick.AddListener(() => Process.Start("file://" + GetGhostDirectory(level)));
                }

                ghostButton.name = "Button Ghost";
                ghostButton.buttonText = "Open Ghost Directory";
                ghostButton.buttonTextRef.text = "Open Ghost Directory";
                ghostButton.gameObject.SetActive(_setting_GhostButton.Value);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("error on setting up the button " + e);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MenuScreenResults), "OnSetVisible")]
        private static void OnMenuScreen(ref MenuScreenResults __instance)
        {
            try
            {
                if (ghostButton && __instance.buttonsToLoad.Contains(ghostButton))
                    __instance.buttonsToLoad.Remove(ghostButton);
                var button = __instance._buttonContine;
                SetupButton(button);

                var pos = button.transform.parent.position;
                pos.x = -0.35f; // don't ask me how. the math just ISN'T THERE i had to hardcode it
                ghostButton.transform.parent.position = pos;

                if (!__instance.buttonsToLoad.Contains(ghostButton))
                    __instance.buttonsToLoad.Add(ghostButton);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("error on OnMenuScreen " + e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MainMenu), "SetState")]
        public static void SetState(ref MainMenu __instance)
        {
            if (ghostButton != null)
            {
                if (jobArchive)
                {
                    UnityEngine.Object.Destroy(ghostButton.gameObject);
                    UnityEngine.Object.Destroy(ghostButton.transform.parent.gameObject);
                    jobArchive = false;
                }
                else if (__instance._screenResults.buttonsToLoad.Contains(ghostButton))
                {
                    __instance._screenResults.buttonsToLoad.Remove(ghostButton);
                }
                ghostButton = null;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MainMenu), "SelectMission")]
        public static void SelectMission(ref MainMenu __instance, ref string missionID, ref bool goToLevelScreen)
        {
            if (!goToLevelScreen)
                return;
            jobArchive = true;
            var button = __instance._backButton;
            SetupButton(button.GetComponent<MenuButtonHolder>(), Singleton<Game>.Instance.GetGameData().GetMission(missionID).levels[0]);

            var pos = button.transform.parent.position;
            pos.x = 7.02f; // same thing here it's stupid
            ghostButton.transform.parent.position = pos;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Singleton<BackButtonAccessor>), "OnDestroy")]
        public static bool OnDestroy()
        {
            // the back button's singleton is EXTREMELY fragile
            // so we just tell it to not worry
            // (this also lets us destroy the copied backbutton without worry, making it safer)
            // will this affect other singletons? idk
            // it *shouldn't* bc this is supposed to never happen in the first place
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuPanelInventoryItem), "SetLevel")]
        public static void SetLevel(LevelData level)
        {
            if (!ghostButton)
                return;

            ghostButton.ButtonRef.onClick.RemoveAllListeners();
            ghostButton.ButtonRef.onClick.AddListener(() => Process.Start("file://" + GetGhostDirectory(level)));
        }
    }
}
