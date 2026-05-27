using UnityEngine;
using UnityEngine.EventSystems;

namespace AmarionCodex.UI
{
    /// <summary>
    /// Allows dragging the codex window by holding down on the title bar.
    /// Attach to the title bar GameObject; set Target to the window panel RectTransform.
    /// </summary>
    internal class TitleBarDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        public RectTransform Target;

        private Vector2 _dragOffset;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Target == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                Target.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 mousePos);
            _dragOffset = (Vector2)Target.localPosition - mousePos;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Target == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                Target.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 mousePos);
            Target.localPosition = mousePos + _dragOffset;
        }
    }
}
