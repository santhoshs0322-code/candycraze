using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace CandyCraze
{
    public class LevelMapController : MonoBehaviour
    {
        private RectTransform root, content, chapterFill;
        private ScrollRect scroll;
        private Text chapterText, starsText, balanceText, completionText, continueText;
        private Button previous, next, continueButton;
        private Coroutine focus;
        private int chapter, total;
        private const int PageSize = 30;
        private const float NodeSpacing = 360;
        // Reserve vertical bands for the stars/status below and current-level label above.
        // Connectors stay entirely in the gap, even where the trail becomes nearly vertical.
        private const float PathBelowNode = 150;
        private const float PathAboveNode = 160;
        private static readonly string[] ChapterNames = { "Sugar Meadows", "Jelly Gardens", "Caramel Valley", "Rainbow Peaks" };
        public void InjectRefs(Button b, Transform c, Text s) { } // Legacy scene compatibility.
        private void Start()
        {
            if (SaveManager.Instance == null) new GameObject("SaveManager").AddComponent<SaveManager>();
            var config = Resources.Load<GameConfig>("GameConfig");
            total = config != null && config.TotalLevels > 0 ? config.TotalLevels : 100;
            chapter = (Mathf.Clamp(SaveManager.Instance.Data.CurrentLevel,1,total)-1)/PageSize;
            root = CandyTheme.Page("PremiumLevelMap");
            CandyTheme.Button(root,"HOME",.05f,.925f,.25f,.98f,CandyTheme.Purple,() => SceneController.NavigateTo(Constants.SCENE_MAIN_MENU),28);
            CandyTheme.Label(root,"Your sweet journey",.28f,.925f,.95f,.98f,46);
            var progressCard = CandyTheme.Card(root,"JourneyProgress",.05f,.815f,.95f,.91f,CandyTheme.Cream);
            starsText = CandyTheme.Label(progressCard.transform,"",.03f,.40f,.49f,.95f,36);
            balanceText = CandyTheme.Label(progressCard.transform,"",.51f,.40f,.97f,.95f,34);
            completionText = CandyTheme.Label(progressCard.transform,"",.04f,.02f,.96f,.42f,26);
            chapterText = CandyTheme.Label(root,"",.05f,.767f,.95f,.808f,29);
            var track = CandyTheme.Card(root,"ChapterProgressTrack",.07f,.75f,.93f,.759f,CandyTheme.Hex("DCC8E4"),false);
            chapterFill = CandyTheme.Card(track.transform,"ChapterProgressFill",0,0,0,1,CandyTheme.Purple,false).rectTransform;
            var scrollRoot = CandyTheme.Rect(root,"TrailScroll",.035f,.225f,.965f,.74f);
            var hit = scrollRoot.gameObject.AddComponent<Image>(); hit.color = new Color(1,1,1,.08f);
            scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.scrollSensitivity = 60;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = CandyTheme.Rect(scrollRoot,"Viewport",0,0,1,1);
            viewport.gameObject.AddComponent<RectMask2D>(); scroll.viewport = viewport;
            content = CandyTheme.Rect(viewport,"Trail",0,1,1,1); content.pivot = new Vector2(.5f,1);
            scroll.content = content;
            previous = CandyTheme.Button(root,"PREVIOUS",.05f,.15f,.34f,.21f,CandyTheme.Purple,() => ChangeChapter(-1),25);
            CandyTheme.Button(root,"MY LEVEL",.36f,.15f,.64f,.21f,CandyTheme.Purple,FocusMyLevel,25);
            next = CandyTheme.Button(root,"NEXT",.66f,.15f,.95f,.21f,CandyTheme.Purple,() => ChangeChapter(1),25);
            continueButton = CandyTheme.Button(root,"",.08f,.045f,.92f,.13f,CandyTheme.Pink,
                () => OpenLevel(Mathf.Clamp(SaveManager.Instance.Data.CurrentLevel,1,total)),38);
            continueText = continueButton.GetComponentInChildren<Text>();
            SaveManager.Instance.OnDataChanged += Refresh;
            Refresh();
        }
        private void OnDestroy() { if (SaveManager.Instance != null) SaveManager.Instance.OnDataChanged -= Refresh; }
        private void ChangeChapter(int direction)
        {
            int target = Mathf.Clamp(chapter+direction,0,(total-1)/PageSize);
            if (target == chapter) return;
            chapter = target; Refresh();
        }
        private void FocusMyLevel()
        {
            chapter = (Mathf.Clamp(SaveManager.Instance.Data.CurrentLevel,1,total)-1)/PageSize;
            Refresh();
        }
        private void Refresh()
        {
            if (content == null) return;
            if (focus != null) StopCoroutine(focus);
            foreach(Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            var data = SaveManager.Instance.Data;
            int first = chapter*PageSize+1, count = Mathf.Min(PageSize,total-first+1);
            int current = Mathf.Clamp(data.CurrentLevel,1,total), completedCount = 0;
            for(int level = first; level < first+count; level++)
                if (data.GetEntry(level)?.Completed ?? false) completedCount++;
            float height = count*NodeSpacing+180;
            content.sizeDelta = new Vector2(0,height);
            chapterText.text = "CHAPTER " + (chapter+1) + " / " + ((total+PageSize-1)/PageSize) + "  -  " + ChapterNames[chapter%ChapterNames.Length];
            starsText.text = data.TotalStars.ToString("N0") + " STARS";
            balanceText.text = data.Coins.ToString("N0") + " CRYSTALS";
            completionText.text = "Levels " + first + " - " + (first+count-1) + "   |   " + completedCount + " / " + count + " completed";
            chapterFill.anchorMax = new Vector2((float)completedCount/count,1);
            previous.interactable = chapter > 0;
            next.interactable = first+count <= total;
            continueText.text = (data.GetEntry(current)?.Completed ?? false) ? "REPLAY LEVEL " + current : "PLAY LEVEL " + current;
            continueButton.interactable = data.IsLevelUnlocked(current);
            for(int i=0;i<count;i++)
            {
                int level=first+i;
                float x=.5f+Mathf.Sin(i*.95f)*.27f, y=-(175+i*NodeSpacing);
                bool unlocked=data.IsLevelUnlocked(level), completed=data.GetEntry(level)?.Completed??false;
                bool isCurrent=level==current;
                if(i>0)
                {
                    float previousX=.5f+Mathf.Sin((i-1)*.95f)*.27f;
                    for(int dot=1;dot<=3;dot++)
                    {
                        float gapProgress=dot/4f;
                        float distanceFromPrevious=Mathf.Lerp(PathBelowNode,NodeSpacing-PathAboveNode,gapProgress);
                        float t=distanceFromPrevious/NodeSpacing;
                        var bead=CandyTheme.SpriteImage(content,CandyTheme.Circle,Mathf.Lerp(previousX,x,t),1,Mathf.Lerp(previousX,x,t),1);
                        bead.color=unlocked?CandyTheme.Purple:CandyTheme.Hex("C9B4D2");
                        bead.rectTransform.sizeDelta=new Vector2(12,12);
                        bead.rectTransform.anchoredPosition=new Vector2(0,y+NodeSpacing*(1-t));
                        bead.transform.SetAsFirstSibling();
                    }
                }
                if(isCurrent)
                {
                    var ring=CandyTheme.SpriteImage(content,CandyTheme.Circle,x,1,x,1);
                    ring.color=CandyTheme.Gold; ring.rectTransform.sizeDelta=new Vector2(184,184);
                    ring.rectTransform.anchoredPosition=new Vector2(0,y);
                }
                var node=CandyTheme.Button(content,level.ToString(),x,1,x,1,
                    unlocked?(completed?CandyTheme.Purple:CandyTheme.Pink):CandyTheme.Hex("8B7797"),() => OpenLevel(level),56);
                node.image.sprite=CandyTheme.Circle; node.image.type=Image.Type.Simple;
                var rt=(RectTransform)node.transform; rt.sizeDelta=new Vector2(152,152); rt.anchoredPosition=new Vector2(0,y);
                node.interactable=unlocked;
                var label = node.GetComponentInChildren<Text>();
                label.rectTransform.anchorMin=new Vector2(.05f,.23f); label.rectTransform.anchorMax=new Vector2(.95f,.83f);
                if(isCurrent)
                {
                    var currentBadge=CandyTheme.Card(node.transform,"CurrentLevelLabel",-.48f,1.20f,1.48f,1.44f,CandyTheme.Cream,false);
                    CandyTheme.Label(currentBadge.transform,"YOU ARE HERE",.03f,.02f,.97f,.98f,23,CandyTheme.Ink);
                }
                if(completed)
                {
                    for(int star=0;star<3;star++)
                    {
                        var image=CandyTheme.SpriteImage(node.transform,Resources.Load<Sprite>("UI/Star"),.08f+star*.28f,-.38f,.36f+star*.28f,-.12f);
                        image.color=star<data.GetStars(level)?CandyTheme.Gold:CandyTheme.Hex("9E87AB");
                    }
                }
                else
                {
                    var badge=CandyTheme.Card(node.transform,"LevelStatus",-.27f,-.38f,1.27f,-.12f,CandyTheme.Cream,false);
                    CandyTheme.Label(badge.transform,unlocked?"READY TO PLAY":"LOCKED",.02f,.03f,.98f,.97f,23,CandyTheme.Ink);
                }
            }
            focus=StartCoroutine(FocusCurrent(first,count,height));
        }
        private IEnumerator FocusCurrent(int first,int count,float height)
        {
            yield return null;
            Canvas.ForceUpdateCanvases(); scroll.StopMovement();
            float index=Mathf.Clamp(SaveManager.Instance.Data.CurrentLevel-first,0,count-1);
            float distance=Mathf.Max(0,175+index*NodeSpacing-scroll.viewport.rect.height*.4f);
            float overflow=Mathf.Max(1,height-scroll.viewport.rect.height);
            scroll.verticalNormalizedPosition=1-Mathf.Clamp01(distance/overflow);
            focus=null;
        }
        private void OpenLevel(int level)
        {
            if(level<1 || level>total || !SaveManager.Instance.Data.IsLevelUnlocked(level)) return;
            LevelManager.SelectedLevelNumber=level;
            SceneController.NavigateTo(Constants.SCENE_GAME);
        }
    }
}
