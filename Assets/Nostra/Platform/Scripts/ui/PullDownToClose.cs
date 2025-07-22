using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace nostra.core.ui
{
    public class PullDownToClose : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] float dragThreshold = 100f; // Minimum pull-down distance to close
        [SerializeField] float returnSpeed = 10f;     // Speed of snapping back if not enough drag
        [SerializeField] Button closeInvoker;

        private Vector2 startDragPosition;
        private RectTransform rectTransform;
        private Vector2 originalAnchoredPosition;
        private bool isReturning = false;

        void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            originalAnchoredPosition = rectTransform.anchoredPosition;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            startDragPosition = eventData.position;
            isReturning = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 dragDelta = eventData.position - startDragPosition;

            // Only allow vertical movement
            rectTransform.anchoredPosition = originalAnchoredPosition + new Vector2(0, Mathf.Clamp(dragDelta.y, -500f, 0f));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            float verticalDrag = eventData.position.y - startDragPosition.y;

            if (verticalDrag < -dragThreshold)
            {
                ClosePopup();
            }
            else
            {
                // Animate back to original position
                isReturning = true;
            }
        }

        void ClosePopup()
        {
            closeInvoker.onClick.Invoke();
            rectTransform.anchoredPosition = originalAnchoredPosition;
        }

        void Update()
        {
            if (isReturning)
            {
                rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, originalAnchoredPosition, Time.deltaTime * returnSpeed);

                if (Vector2.Distance(rectTransform.anchoredPosition, originalAnchoredPosition) < 0.1f)
                {
                    rectTransform.anchoredPosition = originalAnchoredPosition;
                    isReturning = false;
                }
            }
        }
    }
}