using HarmonyLib;
using UnityEngine;


namespace NeonLite.GameObjects
{
    [HarmonyPatch]
    internal class InputDisplay : MonoBehaviour
    {
        private static InputDisplay _instance = null;

        private static Texture2D _leftTex;
        private static Texture2D _rightTex;
        private static Texture2D _upTex;
        private static Texture2D _downTex;

        private static Texture2D _fireTex;
        private static Texture2D _discardTex;
        private static Texture2D _jumpTex;
        private static Texture2D _swapTex;
        private static Texture2D _scrollTex;

        private static Texture2D _fake2;
        private static Texture2D _fake3;

        private GameObject _holder;

        private MeshRenderer up;
        private MeshRenderer down;
        private MeshRenderer left;
        private MeshRenderer right;
        private MeshRenderer jump;
        private MeshRenderer fire;
        private MeshRenderer discard;
        private MeshRenderer swap;
        
        private MeshRenderer scroll;
        private float scrollTimer = 0;

        private static Color currentColor;
        private static float invertOff;

        private static Texture2D LoadTexture(byte[] image)
        {
            Texture2D SpriteTexture = new(0, 0);
            SpriteTexture.LoadImage(image);
            return SpriteTexture;
        }
        
        public static void Setup()
        {
            if (NeonLite.s_Setting_InputDisplay.Value)
            {
                // setup the textures

                _upTex = LoadTexture(Properties.Resources.InputUp);
                _downTex = LoadTexture(Properties.Resources.InputDown);
                _leftTex = LoadTexture(Properties.Resources.InputLeft);
                _rightTex = LoadTexture(Properties.Resources.InputRight);

                _fireTex = LoadTexture(Properties.Resources.InputFire);
                _discardTex = LoadTexture(Properties.Resources.InputDiscard);
                _jumpTex = LoadTexture(Properties.Resources.InputJump);
                _swapTex = LoadTexture(Properties.Resources.InputSwap);

                _scrollTex = LoadTexture(Properties.Resources.InputScroll);

                _fake2 = LoadTexture(Properties.Resources.InputFake2);
                _fake3 = LoadTexture(Properties.Resources.InputFake3);
            }
        }

        internal static void Initialize()
        {
            _instance = null;
            if (NeonLite.s_Setting_InputDisplay.Value)
                new GameObject("InputDisplay", typeof(InputDisplay));
        }

        private MeshRenderer SetupIcon(GameObject obj, Texture2D tex, double x, double y, float scale = 1)
        {
            // grab the renderer and set the texture
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.material.SetTexture("_MainTex", tex);
            renderer.material.SetTextureScale("_MainTex", new Vector2(0.5f, 0.5f));
            renderer.material.renderQueue++;

            obj.transform.localPosition = new Vector3((float)x, (float)y, 0);
            obj.transform.localRotation = Quaternion.identity;
            if (scale != 1)
            {
                var t = obj.transform.localScale;
                float pre = t.x;
                t.x *= scale;
                obj.transform.localScale = t;
                // adjust position according to new scale
                obj.transform.localPosition += new Vector3((t.x - pre) / 2, 0, 0);

                var fakeAA = obj.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>();
                if (tex == _scrollTex)
                {
                    // if we're the scrolling texture, we don't use fakeAA and we can just toggle visibility
                    renderer.material.SetTextureScale("_MainTex", new Vector2(0.5f, 1));
                    renderer.material.renderQueue--;
                    renderer.enabled = false;
                    fakeAA.enabled = false;
                }
                else {
                    // jump or fire/discard need a different fakeAA texture because just stretching it looks wrong
                    if (scale > 3)
                        fakeAA.material.SetTexture("_MainTex", _fake3);
                    else
                        fakeAA.material.SetTexture("_MainTex", _fake2);
                }

            }
            obj.SetActive(true);


            return renderer;
        }

        private void Start()
        {
            var cardUI = RM.ui.cardHUDUI;
            transform.SetParent(cardUI.transform, false);
            // we add here so it sits nicely under the ability icon holder
            transform.localPosition = new Vector3(0, -0.666f, 0) + cardUI.abilityIconHolder.transform.localPosition;
            transform.localRotation = Quaternion.identity;

            // copy the existing icon holder
            _holder = Instantiate(cardUI.abilityIconHolder, transform);
            foreach (Transform t in _holder.transform)
            {
                // eliminate all but the first one.
                if (t.GetSiblingIndex() == 0)
                    continue;
                Destroy(t.gameObject);
            }
            // the sole survivor (which will be killed later) will be the base
            var abase = _holder.transform.GetChild(0).gameObject;
            _holder.transform.DetachChildren();
            _holder.transform.localPosition = Vector3.zero;

            const double off = 0.666;

            up = SetupIcon(Instantiate(abase, _holder.transform), _upTex, off * 1, 0);
            down = SetupIcon(Instantiate(abase, _holder.transform), _downTex, off * 1, -off * 1);
            left = SetupIcon(Instantiate(abase, _holder.transform), _leftTex, 0, -off * 1);
            right = SetupIcon(Instantiate(abase, _holder.transform), _rightTex, off * 2, -off * 1);

            // scale floats at end are based on their texture width divided by regular
            fire = SetupIcon(Instantiate(abase, _holder.transform), _fireTex, off * 2, 0, 2.234375f);
            discard = SetupIcon(Instantiate(abase, _holder.transform), _discardTex, off * 4, 0, 2.234375f);
            jump = SetupIcon(Instantiate(abase, _holder.transform), _jumpTex, off * 3, -off * 1, 3.484375f);
            swap = SetupIcon(Instantiate(abase, _holder.transform), _swapTex, 0, 0);

            scroll = SetupIcon(Instantiate(abase, _holder.transform), _scrollTex, off * 3, -off * 1, 3.484375f);

            Destroy(abase);

            // BONUS: the 3rd ability icon is slightly off and is at a weird spot
            // let's move it and make it look even nicer
            cardUI.abilityIconHolder.transform.GetChild(2).localPosition = new Vector3((float)off * 2, 0, 0);


            _instance = this;
            RefreshColor();
            Update();
        }

        private void RefreshColor()
        {
            up?.material.SetColor("_TintColor", currentColor);
            down?.material.SetColor("_TintColor", currentColor);
            left?.material.SetColor("_TintColor", currentColor);
            right?.material.SetColor("_TintColor", currentColor);

            fire?.material.SetColor("_TintColor", currentColor);
            discard?.material.SetColor("_TintColor", currentColor);
            jump?.material.SetColor("_TintColor", currentColor);
            swap?.material.SetColor("_TintColor", currentColor);

            scroll?.material.SetColor("_TintColor", currentColor);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerUICardHUD), "UpdateHUD")]
        private static void PreUpdateHUD(PlayerUICardHUD __instance, ref PlayerCard card)
        {
            if (NeonLite.s_Setting_InputDisplayColor.Value.a != 0)
                currentColor = NeonLite.s_Setting_InputDisplayColor.Value.Alpha(1f);
            else
            {
                if (card.data.discardAbility == PlayerCardData.DiscardAbility.None) // katana/fist
                    currentColor = new Color(0.5f, 0.5f, 0.5f, 1f);
                else 
                    currentColor = card.data.cardColor.Alpha(1f);
            }
            if (_instance)
                _instance.RefreshColor();
        }

        private void Update()
        {
            invertOff = NeonLite.s_Setting_InputDisplayInvert.Value ? 0.5f : 0;
            var input = Singleton<GameInput>.Instance;
            var x = input.GetAxis(GameInput.GameActions.MoveHorizontal);
            var y = input.GetAxis(GameInput.GameActions.MoveVertical);
            var jumpd = input.GetButtonDown(GameInput.GameActions.Jump, GameInput.InputType.Game);
            if (jumpd)
            {
                // if jump was just pushed on this frame, they either pressed spacebar or used a quick frame method like scrolling
                // so we use neon white's code against it
                try
                {
                    // this actually errors on scroll jump and a good chunk of the player's Update isn't actually run because of it in basegame
                    // but since we know that we can use that to our advantage
                    input.GetButton(GameInput.GameActions.Jump);
                }
                catch (Exception) {
                    scrollTimer = 0.1f;
                    jumpd = false;
                }
            }
            else // if we didn't jump on this frame we might be holding it so check it as you normally would without worry
                jumpd = input.GetButton(GameInput.GameActions.Jump);


            var firei = input.GetButton(GameInput.GameActions.FireCard);
            var discardi = input.GetButton(GameInput.GameActions.FireCardAlt);
            var swapi = input.GetButton(GameInput.GameActions.SwapCard);

            // unity does textures kinda weird
            const float on = 0, off = -0.5f;

            up.material.SetTextureOffset("_MainTex", new Vector2(invertOff, y > 0 ? on : off));
            down.material.SetTextureOffset("_MainTex", new Vector2(invertOff, y < 0 ? on : off));

            left.material.SetTextureOffset("_MainTex", new Vector2(invertOff, x < 0 ? on : off));
            right.material.SetTextureOffset("_MainTex", new Vector2(invertOff, x > 0 ? on : off));

            jump.material.SetTextureOffset("_MainTex", new Vector2(invertOff, jumpd ? on : off));

            fire.material.SetTextureOffset("_MainTex", new Vector2(invertOff, firei ? on : off));
            discard.material.SetTextureOffset("_MainTex", new Vector2(invertOff, discardi ? on : off));
            swap.material.SetTextureOffset("_MainTex", new Vector2(invertOff, swapi ? on : off));

            scroll.enabled = false;
            if (scrollTimer > 0)
            {
                scrollTimer -= Time.deltaTime;
                scroll.material.SetTextureOffset("_MainTex", new Vector2(invertOff, 0));
                scroll.enabled = true;
            }
        }
    }
}
