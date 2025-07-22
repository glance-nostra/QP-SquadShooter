using nostra.customisation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace nostra.creation.ui
{
    public class PullToResize : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] float dragThreshold = 100f;
        [SerializeField] float resizeSpeed = 10f;
        [SerializeField] Vector2 minimizedYPos = new Vector2(0f, -860f);
        [SerializeField] Vector2 maximizedYPos = new Vector2(0f, 0f);
        [SerializeField] ModelHandler m_modelHandler;

        private RectTransform rectTransform;
        private Vector2 startDragPosition;
        private Vector2 targetYPos;
        private bool isResizing = false;

        void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            targetYPos = rectTransform.anchoredPosition;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            startDragPosition = eventData.position;
            isResizing = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Optional: Visual feedback while dragging
            // float verticalDelta = eventData.position.y - startDragPosition.y;

            // Vector2 newSize = rectTransform.sizeDelta;
            // newSize.y = Mathf.Clamp(rectTransform.sizeDelta.y + verticalDelta * 0.05f, minimizedSize.y, maximizedSize.y);
            // rectTransform.sizeDelta = newSize;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            float dragAmount = eventData.position.y - startDragPosition.y;

            if (dragAmount > dragThreshold)
            {
                // Pull up → Maximize
                targetYPos = maximizedYPos;
                m_modelHandler.OnUIChanged(false);
            }
            else if (dragAmount < -dragThreshold)
            {
                // Pull down → Minimize
                targetYPos = minimizedYPos;
                m_modelHandler.OnUIChanged(true);
            }
            else
            {
                // Snap to current size (no action)
                targetYPos = rectTransform.anchoredPosition;
            }

            isResizing = true;
        }

        void Update()
        {
            if (isResizing)
            {
                rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetYPos, Time.deltaTime * resizeSpeed);

                if (Vector2.Distance(rectTransform.anchoredPosition, targetYPos) < 0.5f)
                {
                    rectTransform.anchoredPosition = targetYPos;
                    isResizing = false;
                }
            }
        }
    }
}