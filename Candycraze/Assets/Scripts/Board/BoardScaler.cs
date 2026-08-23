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
        [SerializeField] private float _sidePadding   = 0.3f;  // left/right
        [SerializeField] private float _topPadding    = 1.8f;  // for HUD
        [SerializeField] private float _bottomPadding = 1.2f;  // for booster bar

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
            if (_cam == null) return;

            float boardW = _boardCols * _cellSize;
            float boardH = _boardRows * _cellSize;

            // Total world height needed
            float totalH = boardH + _topPadding + _bottomPadding;
            // Total world width needed
            float totalW = boardW + _sidePadding * 2f;

            // Camera aspect ratio
            float aspect = (float)Screen.width / Screen.height;

            // Fit by height
            float sizeByH = totalH / 2f;
            // Fit by width
            float sizeByW = totalW / (2f * aspect);

            // Use whichever is larger (ensures everything is visible)
            float orthoSize = Mathf.Max(sizeByH, sizeByW);
            _cam.orthographicSize = orthoSize;

            // Centre the board horizontally and vertically
            // Board spans from (0,0) to (boardW, boardH)
            // We want it centred with padding above for HUD
            float centreX = boardW / 2f;
            float centreY = boardH / 2f - (_topPadding - _bottomPadding) / 2f;

            _cam.transform.position = new Vector3(centreX, centreY, -10f);

            // Also position the board root
            if (_boardRoot != null)
                _boardRoot.position = Vector3.zero;

            Debug.Log($"[BoardScaler] OrthoSize={orthoSize:F2} " +
                      $"CamPos=({centreX:F2},{centreY:F2}) " +
                      $"Screen={Screen.width}x{Screen.height} " +
                      $"Aspect={aspect:F2}");
        }

        // Call this if screen rotates (shouldn't in portrait, but just in case)
        private void OnRectTransformDimensionsChange() => FitBoardToScreen();
    }
}
