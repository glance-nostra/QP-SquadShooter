using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace nostra.core.ui
{
    public class VerticalScrollSnap : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Scroll Settings")]
        [SerializeField] private float snapSpeed = 2f;
        [SerializeField] private float minFlickVelocity = 1000f; // Minimum velocity to trigger scroll
        [SerializeField] private Transform contentTransform;
        [SerializeField] private int paginationTriggerOffset = 2;

        private const int CARD_COUNT = 5;
        private const int BUFFER_COUNT = 2;

        private Vector2 lastDragPosition;
        private float lastDragTime;
        private Vector2 dragVelocity;
        private Vector3 currentVelocity;
        private bool isDragging;
        private bool isScrolling;
        private bool isLoadingMore;
        private bool hasMorePosts = true;
        private float screenHeight;
        private int lastSnappedIndex = -1;
        private Vector2 dragStartPosition;

        // Core state
        private int totalPosts;
        private int currentCardIndex;
        private int currentIndex;
        private int scrollCount;
        private readonly RectTransform[] cards = new RectTransform[CARD_COUNT];
        private readonly int[] cardToPostIndex = new int[CARD_COUNT];
        private readonly int[] cardToCardIndex = new int[CARD_COUNT];

        // Events
        public event Action<int, int> OnCardChanged;     // (postIndex, cardIndex)
        public event Action<int> OnScrollStart;          // currentIndex
        public event Action<int> OnScrollEnd;            // finalIndex
        public event Action OnLoadMorePosts;

        private void Awake()
        {
            screenHeight = 1600;
        }

        public void Initialize(int postCount, bool hasMore = true)
        {
            totalPosts = postCount;
            hasMorePosts = hasMore;
            isLoadingMore = false;
            currentIndex = 0;
            scrollCount = 0;
            contentTransform.localPosition = Vector3.zero;
            SetupCards();
        }

        private void SetupCards()
        {
            if (contentTransform.childCount < CARD_COUNT) return;

            // Position cards from top to bottom (0 to -4 * screenHeight)
            int maxCards = Mathf.Min(CARD_COUNT, totalPosts);
            for (int i = 0; i < CARD_COUNT; i++)
            {
                cards[i] = contentTransform.GetChild(i).GetComponent<RectTransform>();
                cards[i].anchoredPosition = new Vector2(0, -i * screenHeight);
                cardToPostIndex[i] = i;
                cardToCardIndex[i] = i;
                if (i >= maxCards)
                {
                    cards[i].gameObject.SetActive(false);
                }
                else
                {
                    cards[i].gameObject.SetActive(true);
                    OnCardChanged?.Invoke(i, i);
                }
            }

            contentTransform.localPosition = new Vector2(0, 0);
            currentCardIndex = 0;
        }

        private void UpdateCardPositions()
        {
            int minBottomCard = Mathf.Min(2, totalPosts - currentIndex - 1);
            int currentBottomCard = 0;
            for (int i = 0; i < CARD_COUNT; i++)
            {
                if (cards[i].anchoredPosition.y < cards[currentCardIndex].anchoredPosition.y)
                {
                    currentBottomCard++;
                }
            }
            for (int i = 0; i < (minBottomCard - currentBottomCard); i++)
            {
                RecycleTopToBottom();
            }
        }

        private void RecycleTopToBottom()
        {
            // Move top card to bottom
            var topCard = cards[0];
            int newPostIndex = cardToPostIndex[0] + CARD_COUNT;
            int nextCardIndex = cardToCardIndex[0];

            if (newPostIndex < totalPosts)
            {
                // Shift array up
                for (int i = 0; i < CARD_COUNT - 1; i++)
                {
                    cards[i] = cards[i + 1];
                    cardToPostIndex[i] = cardToPostIndex[i + 1];
                    cardToCardIndex[i] = cardToCardIndex[i + 1];
                }

                cards[CARD_COUNT - 1] = topCard;
                cardToPostIndex[CARD_COUNT - 1] = newPostIndex;
                cardToCardIndex[CARD_COUNT - 1] = nextCardIndex;
                // Update position and contentsc
                topCard.anchoredPosition = new Vector2(0, topCard.anchoredPosition.y - (CARD_COUNT * screenHeight));
                OnCardChanged?.Invoke(nextCardIndex, newPostIndex);
            }
        }
        private void RecycleBottomToTop()
        {
            // Move bottom card to top
            var bottomCard = cards[CARD_COUNT - 1];
            int newPostIndex = cardToPostIndex[CARD_COUNT - 1] - CARD_COUNT;
            int nextCardIndex = cardToCardIndex[CARD_COUNT - 1];

            if (newPostIndex >= 0)
            {
                // Shift array down
                for (int i = CARD_COUNT - 1; i > 0; i--)
                {
                    cards[i] = cards[i - 1];
                    cardToPostIndex[i] = cardToPostIndex[i - 1];
                    cardToCardIndex[i] = cardToCardIndex[i - 1];
                }

                cards[0] = bottomCard;
                cardToPostIndex[0] = newPostIndex;
                cardToCardIndex[0] = nextCardIndex;
                // Update position and content
                bottomCard.anchoredPosition = new Vector2(0, bottomCard.anchoredPosition.y + (CARD_COUNT * screenHeight));
                OnCardChanged?.Invoke(nextCardIndex, newPostIndex);
            }
        }

        private void Update()
        {
            if (!isDragging)
            {
                UpdateScrollPosition();
            }
        }

        private void UpdateScrollPosition()
        {
            float targetY = currentIndex * screenHeight;
            Vector3 targetPosition = new Vector3(0, targetY, 0);
            Vector3 currentPosition = contentTransform.localPosition;

            // Only animate if not at target
            if (Vector3.Distance(currentPosition, targetPosition) > 0.5f)
            {
                float distance = Mathf.Abs(targetPosition.y - currentPosition.y);
                float dynamicSnapSpeed = snapSpeed;
                if (distance > screenHeight * 0.5f)
                {
                    dynamicSnapSpeed *= 1.5f;
                }

                contentTransform.localPosition = Vector3.SmoothDamp(
                    currentPosition,
                    targetPosition,
                    ref currentVelocity,
                    1f / dynamicSnapSpeed
                );
            }
            else
            {
                // Snap exactly and reset velocity to avoid overshoot/jitter
                contentTransform.localPosition = targetPosition;
                currentVelocity = Vector3.zero;
                if (lastSnappedIndex != currentCardIndex)
                {
                    OnScrollEnd?.Invoke(currentCardIndex);
                    lastSnappedIndex = currentCardIndex;
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            dragStartPosition = eventData.position;
            lastDragPosition = eventData.position;
            lastDragTime = Time.time;
            dragVelocity = Vector2.zero;
            OnScrollStart?.Invoke(currentCardIndex);
        }
        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            Vector2 currentPosition = eventData.position;
            float currentTime = Time.time;
            float deltaTime = currentTime - lastDragTime;

            if (deltaTime > 0)
            {
                Vector2 deltaPosition = currentPosition - lastDragPosition;
                dragVelocity = deltaPosition / deltaTime;
            }

            lastDragPosition = currentPosition;
            lastDragTime = currentTime;

            Vector3 newPosition = contentTransform.localPosition + new Vector3(0, eventData.delta.y, 0);

            // Clamp drag so you can't drag infinitely past bounds
            float minY = 0;
            float maxY = (totalPosts - 1) * screenHeight;
            newPosition.y = Mathf.Clamp(newPosition.y, minY - screenHeight * 0.5f, maxY + screenHeight * 0.5f);

            contentTransform.localPosition = newPosition;
        }
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            Vector2 currentPosition = eventData.position;
            float currentTime = Time.time;
            float deltaTime = currentTime - lastDragTime;
            if (deltaTime > 0)
            {
                Vector2 deltaPosition = currentPosition - lastDragPosition;
                dragVelocity = deltaPosition / deltaTime;
            }
            lastDragPosition = currentPosition;
            lastDragTime = currentTime;
            isDragging = false;
            
            float velocityY = dragVelocity.y;
            float totalDragDistance = eventData.position.y - dragStartPosition.y; // Invert the subtraction
            float dragThreshold = screenHeight * 0.2f; // Reduced threshold for better sensitivity
            
            // Determine scroll direction based on both velocity and total drag distance
            bool shouldScrollToNext = false;
            bool shouldScrollToPrevious = false;
            
            // Check if we have enough velocity or drag distance to trigger scroll
            if (Mathf.Abs(velocityY) > minFlickVelocity)
            {
                // Positive velocity = finger moving down = scroll to next (down in content)
                shouldScrollToNext = velocityY > 0;
                shouldScrollToPrevious = velocityY < 0;
            }
            else if (Mathf.Abs(totalDragDistance) > dragThreshold)
            {
                // Positive distance = dragged down = scroll to next (down in content)
                shouldScrollToNext = totalDragDistance > 0;
                shouldScrollToPrevious = totalDragDistance < 0;
            }
            
            // Execute scroll based on direction
            if (shouldScrollToNext && currentIndex < totalPosts - 1)
            {
                currentIndex++;
                scrollCount++;
                currentCardIndex++;
                if (currentCardIndex >= CARD_COUNT)
                {
                    currentCardIndex = 0;
                }
                // Recycle during second scroll
                if (scrollCount > 2)
                {
                    RecycleTopToBottom();
                }
            }
            else if (shouldScrollToPrevious && currentIndex > 0)
            {
                currentIndex--;
                scrollCount--;
                currentCardIndex--;
                if (currentCardIndex < 0)
                {
                    currentCardIndex = CARD_COUNT - 1;
                }
                // Recycle during second scroll
                if (scrollCount < totalPosts - 3)
                {
                    RecycleBottomToTop();
                }
            }
            else
            {
                // If no scroll should happen, snap back to current position
                // The UpdateScrollPosition will handle this automatically
            }

            CheckPagination();
        }

        private void CheckPagination()
        {
            if (isLoadingMore || !hasMorePosts) return;

            int triggerIndex = totalPosts - paginationTriggerOffset;
            if (currentIndex >= triggerIndex)
            {
                isLoadingMore = true;
                OnLoadMorePosts?.Invoke();
            }
        }

        public void AddPosts(int newPostCount, bool hasMore = true)
        {
            totalPosts += newPostCount;
            hasMorePosts = hasMore;
            isLoadingMore = false;
            UpdateCardPositions();
        }

        public int GetCurrentIndex() => currentIndex;
        public int GetTotalPosts() => totalPosts;
        public void GoToPost(int targetPostIndex)
        {
            if (targetPostIndex < 0 || targetPostIndex >= totalPosts)
                return;

            lastSnappedIndex = -1;
            int indexDiff = targetPostIndex - currentIndex;
            if (indexDiff == 0) 
            {
                return;
            }
            currentIndex = targetPostIndex;
            currentCardIndex = (currentCardIndex + indexDiff) % CARD_COUNT;
            if (currentCardIndex < 0) currentCardIndex += CARD_COUNT;
            scrollCount += indexDiff;
            // Recycle cards as needed
            if (indexDiff > 0)
            {
                for (int i = 0; i < indexDiff; i++)
                {
                    RecycleTopToBottom();
                }
            }
            else
            {
                for (int i = 0; i < -indexDiff; i++)
                {
                    RecycleBottomToTop();
                }
            }

            // Trigger events
            OnScrollStart?.Invoke(currentCardIndex);

            // Check pagination after jump
            CheckPagination();
        }
    }
}