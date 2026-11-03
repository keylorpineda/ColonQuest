using ColonQuest.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// Makes the button under the mouse (or selected with the keyboard) easy to tell apart: it grows a little, its outline
    /// lights up and it gives a soft tick. Every button made by <see cref="UiKit.MakeButton"/> has one.
    /// </summary>
    public sealed class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private static readonly Color Lit = new Color(1f, 0.93f, 0.55f, 1f);
        private const float HoverScale = 1.07f;
        private const float Speed = 14f;

        private Image outline;
        private Color rest;
        private Vector3 restScale = Vector3.one;
        private bool hot;

        public void Bind(Image outlineImage)
        {
            outline = outlineImage;
            rest = outlineImage.color;
            restScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Heat();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hot = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            Heat();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            hot = false;
        }

        private void Heat()
        {
            if (!hot)
            {
                hot = true;
                AudioDirector.Tick();
            }
        }

        private void OnDisable()
        {
            hot = false;
            transform.localScale = restScale;
            if (outline != null)
            {
                outline.color = rest;
            }
        }

        private void Update()
        {
            float step = Speed * Time.unscaledDeltaTime;
            var target = hot ? restScale * HoverScale : restScale;
            transform.localScale = Vector3.Lerp(transform.localScale, target, step);
            if (outline != null)
            {
                outline.color = Color.Lerp(outline.color, hot ? Lit : rest, step);
            }
        }
    }
}
