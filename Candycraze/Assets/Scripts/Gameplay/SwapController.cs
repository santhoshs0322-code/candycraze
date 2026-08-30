// ============================================================
// SwapController.cs
// Unified touch + mouse input. Works on mobile and editor.
// Tap-to-select or swipe-to-swap adjacent gems.
// ============================================================

using UnityEngine;

namespace CandyCraze
{
    public class SwapController : MonoBehaviour
    {
        [SerializeField] private BoardManager _boardManager;
        [SerializeField] private Camera       _gameCamera;
        [SerializeField] private float        _minSwipePixels = 15f;

        private bool    _blocked;
        private bool    _dragging;
        private Vector2 _startScreen;
        private GemView _dragGem;
        private GemView _selected;

        private void Awake()
        {
            if (_boardManager == null) _boardManager = FindObjectOfType<BoardManager>(true);
            if (_gameCamera   == null) _gameCamera   = Camera.main;
        }

        private void Start()
        {
            if (_boardManager == null) _boardManager = FindObjectOfType<BoardManager>(true);
            if (_gameCamera   == null) _gameCamera   = Camera.main;

            if (GameManager.Instance != null)
                GameManager.Instance.OnBoardBusy.AddListener(SetBlocked);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnBoardBusy.RemoveListener(SetBlocked);
        }

        private void Update()
        {
            if (_blocked) return;
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;
            if (_boardManager == null || _gameCamera == null) return;

            // ── TOUCH (mobile) ───────────────────────────────
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                switch (t.phase)
                {
                    case TouchPhase.Began:
                        BeginInput(t.position);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        MoveInput(t.position);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        EndInput(t.position);
                        break;
                }
                return;
            }

            // ── MOUSE (editor / desktop) ─────────────────────
            if (Input.GetMouseButtonDown(0)) BeginInput(Input.mousePosition);
            else if (Input.GetMouseButton(0)) MoveInput(Input.mousePosition);
            else if (Input.GetMouseButtonUp(0)) EndInput(Input.mousePosition);
        }

        // ── Input phases ─────────────────────────────────────

        private void BeginInput(Vector2 screenPos)
        {
            _startScreen = screenPos;
            _dragging = false;
            _dragGem = GemAt(screenPos);
        }

        private void MoveInput(Vector2 screenPos)
        {
            if (_dragGem == null || _dragging) return;
            Vector2 delta = screenPos - _startScreen;
            if (delta.magnitude >= _minSwipePixels)
            {
                _dragging = true;
                SwipeFrom(_dragGem, delta);
                ClearSelection();
            }
        }

        private void EndInput(Vector2 screenPos)
        {
            if (!_dragging)
            {
                GemView tapped = GemAt(screenPos);
                HandleTap(tapped);
            }
            _dragging = false;
            _dragGem = null;
        }

        // ── Tap select ───────────────────────────────────────

        private void HandleTap(GemView tapped)
        {
            if (tapped == null) { ClearSelection(); return; }

            if (_selected == null)
            {
                _selected = tapped;
                _selected.SetHighlight(true);
            }
            else if (_selected == tapped)
            {
                ClearSelection();
            }
            else if (Adjacent(_selected, tapped))
            {
                _boardManager.TrySwap(_selected.Row, _selected.Col, tapped.Row, tapped.Col);
                ClearSelection();
            }
            else
            {
                ClearSelection();
                _selected = tapped;
                _selected.SetHighlight(true);
            }
        }

        private void SwipeFrom(GemView gem, Vector2 delta)
        {
            int dr = 0, dc = 0;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                dc = delta.x > 0 ? 1 : -1;
            else
                dr = delta.y > 0 ? 1 : -1;

            _boardManager.TrySwap(gem.Row, gem.Col, gem.Row + dr, gem.Col + dc);
        }

        // ── Helpers ──────────────────────────────────────────

        private GemView GemAt(Vector2 screenPos)
        {
            // Convert screen to world
            Vector3 world = _gameCamera.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, -_gameCamera.transform.position.z));
            world.z = 0f;

            // Try physics overlap first
            Collider2D hit = Physics2D.OverlapPoint(world);
            if (hit != null)
            {
                var gv = hit.GetComponent<GemView>();
                if (gv != null) return gv;
            }

            // Fallback: find nearest gem by grid position
            int col = Mathf.RoundToInt((world.x - _boardManager.CellToWorld(0,0).x) / Constants.CELL_SIZE);
            int row = Mathf.RoundToInt((world.y - _boardManager.CellToWorld(0,0).y) / Constants.CELL_SIZE);
            return _boardManager.GetGem(row, col);
        }

        private bool Adjacent(GemView a, GemView b)
        {
            int dr = Mathf.Abs(a.Row - b.Row);
            int dc = Mathf.Abs(a.Col - b.Col);
            return (dr == 1 && dc == 0) || (dr == 0 && dc == 1);
        }

        private void ClearSelection()
        {
            if (_selected != null)
            {
                _selected.SetHighlight(false);
                _selected = null;
            }
        }

        private void SetBlocked(bool b)
        {
            _blocked = b;
            if (b) ClearSelection();
        }
    }
}
