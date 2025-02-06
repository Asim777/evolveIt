using UnityEngine;
using UnityEngine.EventSystems;

namespace UI_Panels
{
    public class ObjectScaling : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler
    {
        public float zoomSpeed = 0.1f;
        public float minScale = 0.5f;
        public float maxScale = 2f;

        private Canvas _canvas;
        private Vector2 _dragOffset;

        private void Start()
        {
            _canvas = GetComponent<Canvas>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _dragOffset = Vector2.zero;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _dragOffset = Vector2.zero;
        }
        
        public void OnPointerMove(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _dragOffset += eventData.delta;
                _canvas.transform.position += new Vector3(_dragOffset.x, _dragOffset.y, 0);
            }
            else if (eventData.scrollDelta != Vector2.zero)
            {
                var scale = _canvas.scaleFactor + eventData.scrollDelta.y * zoomSpeed;
                scale = Mathf.Clamp(scale, minScale, maxScale);
                _canvas.scaleFactor = scale;
            }
        }
    }
}