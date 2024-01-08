using System.Text;
using TMPro;
using UnityEngine;


namespace NeonLite.GameObjects
{
    internal class InputDisplay : MonoBehaviour
    {
        private TextMeshPro _testText;
        internal static void Initialize()
        {
            if (NeonLite.s_Setting_InputDisplay.Value)
                new GameObject("InputDisplay", typeof(InputDisplay));
        }

        private void Start()
        {
            transform.parent = RM.ui.cardHUDUI.transform;
            transform.localPosition = new Vector3(0, -2f, 0);
            transform.localRotation = Quaternion.identity;
            _testText = Instantiate(RM.ui.cardHUDUI.textAmmo, transform);
            _testText.fontSize = 80;
            _testText.outlineColor = Color.black;
            _testText.outlineWidth = 5;
        }

        private void Update()
        {
            _testText.color = RM.ui.cardHUDUI.abilityIcon[0].abilityIconRenderer.material.GetColor("_TintColor").Alpha(1f);
            
            StringBuilder builder = new("^v<>J12S");
            var input = Singleton<GameInput>.Instance;
            var x = input.GetAxis(GameInput.GameActions.MoveHorizontal);
            var y = input.GetAxis(GameInput.GameActions.MoveVertical);
            var jump = input.GetButton(GameInput.GameActions.Jump);
            var fire = input.GetButton(GameInput.GameActions.FireCard);
            var discard = input.GetButton(GameInput.GameActions.FireCardAlt);
            var swap = input.GetButton(GameInput.GameActions.SwapCard);

            // bad ugly code ahead
            if (y <= 0)
            {
                builder[0] = ' ';
            }
            if (y >= 0)
            {
                builder[1] = ' ';
            }

            if (x <= 0)
            {
                builder[2] = ' ';
            }
            if (x >= 0)
            {
                builder[3] = ' ';
            }
            if (!jump)
            {
                builder[4] = ' ';
            }
            if (!fire)
            {
                builder[5] = ' ';
            }
            if (!discard)
            {
                builder[6] = ' ';
            }
            if (!swap)
            {
                builder[7] = ' ';
            }
            _testText.SetText(builder.ToString());
        }

    }
}
