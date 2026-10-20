using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ecos.EditorTools
{
    /// <summary>Funções auxiliares para montar a interface (uGUI) por código.</summary>
    public static class UIFactory
    {
        public const float PixelScale = 2.0833f; // 1 pixel do sprite = 3 unidades de canvas
        public static readonly Color Gold = new Color32(0xFF, 0xE0, 0x6D, 0xFF);
        public static readonly Color Mint = new Color32(0x9C, 0xF5, 0xD8, 0xFF);
        public static readonly Color Cream = new Color32(0xF8, 0xF2, 0xE4, 0xFF);
        public static readonly Color Shadow = new Color32(0x14, 0x10, 0x24, 0xFF);

        static Font font;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Canvas CreateCanvas(string name, int sortingOrder = 0)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvas.pixelPerfect = false;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void CreateEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        public static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool sliced = false)
        {
            var rt = Node(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            if (sliced)
            {
                img.type = UnityEngine.UI.Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = PixelScale;
            }
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color, TextAnchor alignment = TextAnchor.MiddleCenter, bool outline = true)
        {
            var rt = Node(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.color = color;
            t.alignment = alignment;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = Shadow;
                o.effectDistance = new Vector2(2f, -2f);
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.5f);
                s.effectDistance = new Vector2(3f, -4f);
            }
            return t;
        }

        public static Button Button(Transform parent, string name, string label, Vector2 size, int fontSize = 26)
        {
            var img = Image(parent, name, AssetGenerator.Sprite("ui_button"), Color.white, true);
            img.raycastTarget = true;
            img.rectTransform.sizeDelta = size;
            var button = img.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.95f, 0.7f);
            colors.selectedColor = new Color(1f, 0.92f, 0.6f);
            colors.pressedColor = new Color(0.75f, 0.85f, 0.8f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.8f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.targetGraphic = img;
            img.gameObject.AddComponent<ButtonFeedback>();
            var layout = img.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
            var text = Label(img.transform, "Texto", label, fontSize, Color.white);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(10f, 4f);
            text.rectTransform.offsetMax = new Vector2(-10f, -2f);
            return button;
        }

        public static Image Panel(Transform parent, string name, Vector2 size)
        {
            var img = Image(parent, name, AssetGenerator.Sprite("ui_panel"), Color.white, true);
            img.raycastTarget = true;
            Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            return img;
        }

        public static RectTransform VerticalGroup(Transform parent, string name, Vector2 position, Vector2 size, float spacing)
        {
            var rt = Place(Node(parent, name), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return rt;
        }

        /// <summary>Configura a navegação vertical por teclado/gamepad entre os botões (em loop).</summary>
        public static void LinkVertical(params Button[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                var nav = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = buttons[(i - 1 + buttons.Length) % buttons.Length],
                    selectOnDown = buttons[(i + 1) % buttons.Length]
                };
                buttons[i].navigation = nav;
            }
        }

        public static Image Dimmer(Transform parent, Color color)
        {
            var img = Image(parent, "Escurecer", null, color);
            img.raycastTarget = true;
            Stretch(img.rectTransform);
            return img;
        }
    }
}
