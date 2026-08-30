// ============================================================
// BoardScaler.cs
// Automatically scales the board and camera to fit any
// Android screen size. Attach to the Main Camera GameObject.
// ============================================================

using UnityEngine;

namespace CandyCraze
{
    [RequireComponent(typeof(Camera))]
    public class BoardScaler : MonoBehaviour
    {
        [Header("Board Settings")]
        [SerializeField] private int   _boardCols     = 8;
        [SerializeField] private int   _boardRows     = 8;
        [SerializeField] private float _cellSize      = 1.0f;

        [Header("Margin (world units)")]
        [SerializeField] private float _sidePadding   = 0.6f;  // left/right breathing room
        [SerializeField] private float _topPadding    = 3.0f;  // space for HUD + objectives
        [SerializeField] private float _bottomPadding = 2.2f;  // space for booster bar

        [Header("Board Root")]
        [SerializeField] private Transform _boardRoot;

        private Camera _cam;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_boardRoot == null)
                _boardRoot = GameObject.Find("BoardRoot")?.transform;
        }

        private void Start()
        {
            FitBoardToScreen();
        }

        public void FitBoardToScreen()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            if (_cam == null) return;

            // Gems occupy centres from (0,0) to (cols-1, rows-1).
            // The board visually spans (-0.5) to (cols-0.5) in each axis.
            float boardW = _boardCols * _cellSize;   // 8 units wide
            float boardH = _boardRows * _cellSize;   // 8 units tall

            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);

            // Space reserved for HUD (top) and booster bar (bottom), in world units
            float topUI    = _topPadding;
            float bottomUI = _bottomPadding;

            // Required ortho size to fit board width within screen width
            float sizeForWidth  = (boardW * 0.5f + _sidePadding) / aspect;
            // Required ortho size to fit board height + UI within screen height
            float sizeForHeight = (boardH * 0.5f) + (topUI + bottomUI) * 0.5f;

            float orthoSize = Mathf.Max(sizeForWidth, sizeForHeight);
            _cam.orthographicSize = orthoSize;

            // Board centre in world space:
            // gems span centres 0..(cols-1), so geometric centre = (cols-1)/2
            float boardCentreX = (_boardCols - 1) * _cellSize * 0.5f;
            float boardCentreY = (_boardRows - 1) * _cellSize * 0.5f;

            // Shift camera vertically so board sits between top HUD and bottom bar
            float verticalShift = (topUI - bottomUI) * 0.5f;

            _cam.transform.position = new Vector3(
                boardCentreX,
                boardCentreY + verticalShift,
                -10f);

            if (_boardRoot != null)
                _boardRoot.position = Vector3.zero;

            Debug.Log($"[BoardScaler] size={orthoSize:F2} " +
                      $"cam=({boardCentreX:F1},{boardCentreY+verticalShift:F1}) " +
                      $"aspect={aspect:F2}");
        }

        // Call this if screen rotates (shouldn't in portrait, but just in case)
        private void OnRectTransformDimensionsChange() => FitBoardToScreen();
    }
}
