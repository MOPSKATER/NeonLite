using HarmonyLib;
using MelonLoader;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLite.Modules
{
    [HarmonyPatch]
    internal class OpenGhostDir : Module
    {

        private static MenuButtonHolder ghostButton;

        private static bool jobArchive;
        private static MenuButtonHolder backGhost;

        private static MelonPreferences_Entry<bool> _setting_GhostButton;

        public OpenGhostDir() =>
            _setting_GhostButton = NeonLite.Config_NeonLite.CreateEntry("Open Ghost Directory Button", true, description: "Shows a button at the end to open this level's ghost directory in the file explorer.");


        private static string GetGhostDirectory(LevelData level)
        {
            if (level == null)
                level = Singleton<Game>.Instance.GetCurrentLevel();
            string path = GhostRecorder.GetCompressedSavePathForLevel(level);
            path = Path.GetDirectoryName(path) + "/";
            return path;
        }

        private static void SetupButton(MenuButtonHolder button, Transform parent = null, LevelData level = null)
        {
            if (ghostButton == null || jobArchive)
            {
                if (parent == null)
                {// copy the layout
                    var layout = UnityEngine.Object.Instantiate(button.transform.parent.gameObject, button.transform.parent.parent);
                    layout.name = "Ghost Button Holder";
                    // empty it
                    foreach (Transform child in layout.transform)
                        UnityEngine.Object.Destroy(child.gameObject);
                    parent = layout.transform;
                }

                var ghostObject = UnityEngine.Object.Instantiate(button.gameObject, parent);
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MenuScreenResults), "OnSetVisible")]
        private static void OnMenuScreen(ref MenuScreenResults __instance)
        {
            if (!_setting_GhostButton.Value)
                return;

            var button = __instance._buttonContine;
            SetupButton(button);

            var pos = button.transform.parent.position;
            pos.x = -0.35f; // don't ask me how. the math just ISN'T THERE i had to hardcode it
            ghostButton.transform.parent.position = pos;

            if (!__instance.buttonsToLoad.Contains(ghostButton))
                __instance.buttonsToLoad.Add(ghostButton);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MainMenu), "SetState")]
        private static void SetState(ref MainMenu __instance)
        {
            if (jobArchive)
            {
                backGhost.gameObject.SetActive(false);
                jobArchive = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MainMenu), "SelectMission")]
        public static void SelectMissionScreen(ref MainMenu __instance, ref string missionID, ref bool goToLevelScreen)
        {
            if (!goToLevelScreen || !_setting_GhostButton.Value)
                return;

            var button = __instance._backButton;
            jobArchive = true;
            if (!button.transform.parent.GetComponent<VerticalLayoutGroup>())
            {
                var layoutGroup = button.transform.parent.GetOrAddComponent<VerticalLayoutGroup>(); // setup a vertical layout
                UnityEngine.Object.Destroy(layoutGroup.transform.GetChild(1).gameObject); // remove the random gameobject, possibly just won't work well
                layoutGroup.childForceExpandWidth = false;
                layoutGroup.childControlHeight = false;
                layoutGroup.reverseArrangement = true;
                layoutGroup.childAlignment = TextAnchor.LowerLeft;
                layoutGroup.spacing = -20; // what i found looked best and most accurate
                layoutGroup.transform.position = button.transform.position; // move the position to be at the button pos
                var layoutFitter = layoutGroup.GetOrAddComponent<ContentSizeFitter>(); // add a content fitter to keep the width/height under control
                layoutFitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
                layoutFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var layoutRect = layoutGroup.GetComponent<RectTransform>();
                // run math to set the pivot without changing position
                // courtesy of https://discussions.unity.com/t/139741/4
                Vector2 newPivot = new(0.5f, 0);
                Vector3 deltaPosition = layoutRect.pivot - newPivot;                 // get change in pivot
                deltaPosition.Scale(button.GetComponent<RectTransform>().rect.size); // apply sizing, from the size of the *button*
                deltaPosition.Scale(layoutRect.localScale);                          // apply scaling
                layoutRect.pivot = newPivot;                                         // change the pivot
                layoutRect.localPosition -= deltaPosition;                           // reverse the position
            }

            var ghostTransform = button.transform.parent.Find("Button Ghost"); //reload the reference just 2 b safe
            if (backGhost == null)
            {
                var gbTemp = ghostButton;
                SetupButton(button.GetComponent<MenuButtonHolder>(), button.transform.parent);
                backGhost = ghostButton;
                ghostButton = gbTemp;
            }
            else
                backGhost = ghostTransform.GetComponent<MenuButtonHolder>();
            backGhost.gameObject.SetActive(true);
            SetMissionLevel(Singleton<Game>.Instance.GetGameData().GetMission(missionID).levels[0]);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Singleton<BackButtonAccessor>), "OnDestroy")]
        public static bool OnBackDestroy()
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
        public static void SetMissionLevel(LevelData level)
        {
            if (!backGhost)
                return;

            backGhost.ButtonRef.onClick.RemoveAllListeners();
            backGhost.ButtonRef.onClick.AddListener(() => Process.Start("file://" + GetGhostDirectory(level)));
        }
    }
}
