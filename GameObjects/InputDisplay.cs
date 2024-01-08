using HarmonyLib;
using MelonLoader;
using Steamworks;
using System.Text;
using TMPro;
using UnityEngine;


namespace NeonLite.GameObjects
{
    [HarmonyPatch]
    internal class InputDisplay : MonoBehaviour
    {
        private static InputDisplay _instance = null;
        private TextMeshPro updown;
        private TextMeshPro leftright;
        private TextMeshPro jump;
        private TextMeshPro fire;
        private TextMeshPro discard;
        private TextMeshPro swap;

        private static Color currentColor;
        internal static void Initialize()
        {
            _instance = null;
            if (NeonLite.s_Setting_InputDisplay.Value)
                new GameObject("InputDisplay", typeof(InputDisplay));
        }

        private void Awake()
        {
            transform.parent = RM.ui.cardHUDUI.transform;
            transform.localPosition = new Vector3(0, -2f, 0);
            transform.localRotation = Quaternion.identity;

            updown = Instantiate(RM.ui.cardHUDUI.textAmmo, transform);
            updown.outlineColor = Color.black;
            updown.outlineWidth = 0.3f;
            updown.alignment = TextAlignmentOptions.Center;

            leftright = Instantiate(updown, transform);
            leftright.transform.localPosition += new Vector3(0.5f, 0, 0);

            jump = Instantiate(updown, transform);
            jump.transform.localPosition += new Vector3(1.0f, 0, 0);

            fire = Instantiate(updown, transform);
            fire.transform.localPosition += new Vector3(1.5f, 0, 0);

            discard = Instantiate(updown, transform);
            discard.transform.localPosition += new Vector3(2.0f, 0, 0);

            swap = Instantiate(updown, transform);
            swap.transform.localPosition += new Vector3(2.5f, 0, 0);

            _instance = this;
            RefreshColor();
        }

        private void RefreshColor()
        {
            _instance.updown.color = currentColor;
            _instance.leftright.color = currentColor;
            _instance.jump.color = currentColor;
            _instance.fire.color = currentColor;
            _instance.discard.color = currentColor;
            _instance.swap.color = currentColor;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerUICardHUD), "UpdateHUD")]
        private static void PreUpdateHUD(PlayerUICardHUD __instance, ref PlayerCard card)
        {
            currentColor = card.data.cardColor.Alpha(1f);
            if (_instance)
                _instance.RefreshColor();
        }

        private void Update()
        {
            var input = Singleton<GameInput>.Instance;
            var x = input.GetAxis(GameInput.GameActions.MoveHorizontal);
            var y = input.GetAxis(GameInput.GameActions.MoveVertical);
            var jumpi = input.GetButton(GameInput.GameActions.Jump);
            var firei = input.GetButton(GameInput.GameActions.FireCard);
            var discardi = input.GetButton(GameInput.GameActions.FireCardAlt);
            var swapi = input.GetButton(GameInput.GameActions.SwapCard);

            // bad ugly code ahead
            updown.text = "-";
            updown.fontStyle = FontStyles.Normal;
            if (y != 0)
            {
                updown.fontStyle = FontStyles.Bold;
                updown.text = "<voffset=-0.55em>^";
                if (y < 0)
                    updown.text = "<voffset=-0.65em><rotate=\"180\">^</rotate>";
            }

            leftright.text = "-";
            leftright.fontStyle = FontStyles.Normal;
            if (x != 0)
            {
                leftright.fontStyle = FontStyles.Bold;

                leftright.text = ">";
                if (x < 0)
                    leftright.text = "<";
            }

            jump.text = "-";
            if (jumpi)
                jump.text = "J";
            fire.text = "-";
            if (firei)
                fire.text = "1";
            discard.text = "-";
            if (discardi)
                discard.text = "2";
            swap.text = "-";
            if (swapi)
                swap.text = "S";
        }

    }
}
