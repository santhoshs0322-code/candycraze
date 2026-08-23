// ============================================================
// LevelMapController.cs — Runtime level map, no prefabs needed
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCraze
{
    [DisallowMultipleComponent]
    public class LevelMapController : MonoBehaviour
    {
        [Header("Optional — leave null if using RuntimeUIBuilder")]
        [SerializeField] private Button    _backButton;
        [SerializeField] private Transform _nodeContainer;
        [SerializeField] private Text      _totalStarsText;

        private GameConfig _config;
        private bool       _built = false;

        // Card sizing
        float _cardW, _cardH, _colGap, _rowGap;

        // Called by RuntimeUIBuilder
        public void InjectRefs(Button back, Transform container, Text stars)
        {
            if (_backButton    == null) _backButton    = back;
            if (_nodeContainer == null) _nodeContainer = container;
            if (_totalStarsText== null) _totalStarsText= stars;
        }

        void Start()
        {
            _config = Resources.Load<GameConfig>("GameConfig");
            if (_config == null)
                Debug.LogError("[LevelMap] GameConfig not in Resources/ — run CandyCraze → Setup Project");

            if (_backButton != null)
                _backButton.onClick.AddListener(()=>SceneController.NavigateTo(Constants.SCENE_MAIN_MENU));

            AudioManager.Instance?.PlayMenuMusic();
            CalcSizes();
            StartCoroutine(BuildNextFrame());
        }

        IEnumerator BuildNextFrame()
        {
            yield return null;
            if (_nodeContainer == null)
            {
                Debug.LogError("[LevelMap] No node container. Attach RuntimeUIBuilder or assign in Inspector.");
                yield break;
            }
            if (!_built) { BuildMap(); _built=true; }
            UpdateStars();
        }

        void CalcSizes()
        {
            Canvas cv = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
            float refW=1080, refH=2400;
            if (cv != null)
            {
                var cs=cv.GetComponent<CanvasScaler>();
                if (cs!=null){ refW=cs.referenceResolution.x; refH=cs.referenceResolution.y; }
            }
            float hPad=refW*0.04f, gap=refW*0.025f;
            _cardW=(refW-hPad*2f-gap)*0.5f;
            _cardH=Mathf.Clamp(_cardW*1.1f,100,200);
            _colGap=gap; _rowGap=refH*0.012f;
        }

        void BuildMap()
        {
            int total = (_config!=null && _config.TotalLevels>0) ? _config.TotalLevels : 100;
            Debug.Log($"[LevelMap] Building {total} levels.");
            _nodeContainer.DestroyAllChildren();

            int rows = Mathf.CeilToInt(total/2f);
            for (int row=0; row<rows; row++)
            {
                int lL=row*2+1, lR=row*2+2;
                var rowGO=new GameObject($"Row_{row:000}");
                rowGO.transform.SetParent(_nodeContainer,false);
                var rowRt=rowGO.AddComponent<RectTransform>();
                rowRt.sizeDelta=new Vector2(0,_cardH+_rowGap);
                var hlg=rowGO.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing=_colGap; hlg.childAlignment=TextAnchor.MiddleCenter;
                hlg.childControlWidth=false; hlg.childControlHeight=false;
                hlg.childForceExpandWidth=false; hlg.childForceExpandHeight=false;
                hlg.padding=new RectOffset(0,0,Mathf.RoundToInt(_rowGap*0.5f),Mathf.RoundToInt(_rowGap*0.5f));

                SpawnCard(rowGO.transform, lL, total);
                if (lR<=total) SpawnCard(rowGO.transform, lR, total);
                else SpawnPholder(rowGO.transform);
            }
            Canvas.ForceUpdateCanvases();
            Debug.Log($"[LevelMap] Done. {_nodeContainer.childCount} rows.");
        }

        void SpawnCard(Transform row, int n, int total)
        {
            bool unlocked = n==1 || (SaveManager.Instance!=null && SaveManager.Instance.Data.IsLevelUnlocked(n));
            int  stars    = SaveManager.Instance!=null ? SaveManager.Instance.Data.GetStars(n) : 0;
            RuntimeUIBuilder.CreateLevelCard(row, n, unlocked, stars, _cardW, _cardH, OnLevel);
        }

        void SpawnPholder(Transform row)
        {
            var ph=new GameObject("PH"); ph.transform.SetParent(row,false);
            var rt=ph.AddComponent<RectTransform>(); rt.sizeDelta=new Vector2(_cardW,_cardH);
        }

        void OnLevel(int n)
        {
            bool unlocked = n==1 || (SaveManager.Instance!=null && SaveManager.Instance.Data.IsLevelUnlocked(n));
            if (!unlocked) return;
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
            LevelManager.SelectedLevelNumber = n;
            SceneController.NavigateTo(Constants.SCENE_GAME);
        }

        void UpdateStars()
        {
            if (_totalStarsText==null) return;
            int s = SaveManager.Instance!=null ? SaveManager.Instance.Data.TotalStars : 0;
            _totalStarsText.text = $"⭐ {s}";
        }
    }
}
