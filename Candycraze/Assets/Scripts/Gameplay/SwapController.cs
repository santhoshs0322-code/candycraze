// ============================================================
// SwapController.cs
// Handles all player touch/swipe input on the board.
// Converts screen touches into swap requests sent to
// BoardManager.
//
// Features:
//   • Tap to select + tap adjacent to swap
//   • Swipe directly to swap
//   • Prevents input while board is busy
//   • Visual selection feedback via GemView.SetHighlight()
// ============================================================

using UnityEngine;

namespace CandyCraze
{
    public class SwapController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoardManager _boardManager;
        [SerializeField] private Camera       _gameCamera;

        [Header("Swipe Settings")]
        [Tooltip("Minimum swipe distance in screen pixels to register a swipe.")]
        [SerializeField] private float _minSwipeDistance = 20f;

        // ── State ────────────────────────────────────────────
        private bool    _inputBlocked;
        private bool    _isDragging;
        private Vector2 _touchStartScreen;
        private GemView _selectedGem;      // First tap selection
        private GemView _dragGem;          // Gem being dragged from

        // ────────────────────────────────────────────────────
        private void Awake()
        {
            if (_boardManager == null) _boardManager = FindObjectOfType<BoardManager>();
            if (_gameCamera   == null) _gameCamera   = Camera.main;

            // Listen for busy events
            if (GameManager.Instance != null)
                GameManager.Instance.OnBoardBusy.AddListener(SetBlocked);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnBoardBusy.RemoveListener(SetBlocked);
        }

        // ── Unity Input ──────────────────────────────────────

        private void Update()
        {
            if (_inputBlocked) return;
            if (GameManager.Instance != null &&
                GameManager.Instance.State != GameState.Playing) return;

#if UNITY_EDITOR || UNITY_STANDALONE
            HandleMouseInput();
#else
            HandleTouchInput();
#endif
        }

        // ── Mouse (editor) ───────────────────────────────────

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _touchStartScreen = Input.mousePosition;
                _isDragging = false;
                _dragGem = GemAtScreenPos(Input.mousePosition);
            }

            if (Input.GetMouseButton(0) && _dragGem != null)
            {
                Vector2 delta = (Vector2)Input.mousePosition - _touchStartScreen;
                if (!_isDragging && delta.magnitude >= _minSwipeDistance)
                {
                    _isDragging = true;
                    TrySwipeFrom(_dragGem, delta);
                    ClearSelection();
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (!_isDragging)
                {
                    // Pure tap — handle selection
                    GemView tapped = GemAtScreenPos(Input.mousePosition);
                    HandleTap(tapped);
                }
                _isDragging = false;
                _dragGem = null;
            }
        }

        // ── Touch (device) ───────────────────────────────────

        private void HandleTouchInput()
        {
            if (Input.touchCount == 0) return;
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    _touchStartScreen = touch.position;
                    _isDragging = false;
                    _dragGem = GemAtScreenPos(touch.position);
                    break;

                case TouchPhase.Moved:
                    if (_dragGem != null)
                    {
                        Vector2 delta = touch.position - _touchStartScreen;
                        if (!_isDragging && delta.magnitude >= _minSwipeDistance)
                        {
                            _isDragging = true;
                            TrySwipeFrom(_dragGem, delta);
                            ClearSelection();
                        }
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (!_isDragging)
                    {
                        GemView tapped = GemAtScreenPos(touch.position);
                        HandleTap(tapped);
                    }
                    _isDragging = false;
                    _dragGem = null;
                    break;
            }
        }

        // ── Logic ────────────────────────────────────────────

        private void HandleTap(GemView tapped)
        {
            if (tapped == null)
            {
                ClearSelection();
                return;
            }

            if (_selectedGem == null)
            {
                // First tap — select this gem
                _selectedGem = tapped;
                _selectedGem.SetHighlight(true);
            }
            else if (_selectedGem == tapped)
            {
                // Tapped same gem — deselect
                ClearSelection();
            }
            else if (AreAdjacent(_selectedGem, tapped))
            {
                // Tapped adjacent gem — swap
                _boardManager.TrySwap(
                    _selectedGem.Row, _selectedGem.Col,
                    tapped.Row,       tapped.Col);
                ClearSelection();
            }
            else
            {
                // Not adjacent — switch selection
                ClearSelection();
                _selectedGem = tapped;
                _selectedGem.SetHighlight(true);
            }
        }

        private void TrySwipeFrom(GemView gem, Vector2 delta)
        {
            // Determine swipe direction
            int dr = 0, dc = 0;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                dc = delta.x > 0 ? 1 : -1;
            else
                dr = delta.y > 0 ? 1 : -1;   // Screen-up = higher row

            int targetRow = gem.Row + dr;
            int targetCol = gem.Col + dc;

            _boardManager.TrySwap(gem.Row, gem.Col, targetRow, targetCol);
        }

        // ── Helpers ──────────────────────────────────────────

        private GemView GemAtScreenPos(Vector2 screenPos)
        {
            Vector3 world = _gameCamera.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, _gameCamera.nearClipPlane));

            // Small overlap radius to be forgiving on mobile
            Collider2D hit = Physics2D.OverlapCircle(world, Constants.CELL_SIZE * 0.45f);
            return hit != null ? hit.GetComponent<GemView>() : null;
        }

        private bool AreAdjacent(GemView a, GemView b)
        {
            int dr = Mathf.Abs(a.Row - b.Row);
            int dc = Mathf.Abs(a.Col - b.Col);
            return (dr == 1 && dc == 0) || (dr == 0 && dc == 1);
        }

        private void ClearSelection()
        {
            if (_selectedGem != null)
            {
                _selectedGem.SetHighlight(false);
                _selectedGem = null;
            }
        }

        private void SetBlocked(bool blocked)
        {
            _inputBlocked = blocked;
            if (blocked) ClearSelection();
        }
    }
}
