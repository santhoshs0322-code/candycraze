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
        private const int PageSize = 12;
        private const int Columns = 3;
        private const float RowSpacing = 286;
        private const float TileHeight = 244;
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
            CandyTheme.Label(root,"THE CANDY ATLAS",.28f,.925f,.95f,.98f,40);
            var progressCard = CandyTheme.Card(root,"JourneyProgress",.05f,.795f,.95f,.907f,CandyTheme.Ink);
            CandyTheme.Icon(progressCard.transform,CandyGlyph.Kind.Crystal,.035f,.20f,.15f,.80f,Color.white);
            balanceText = CandyTheme.Label(progressCard.transform,"",.16f,.41f,.49f,.90f,38,Color.white);
            CandyTheme.Label(progressCard.transform,"CRYSTALS",.16f,.12f,.49f,.42f,21,CandyTheme.Hex("EADDF5"));
            CandyTheme.SpriteImage(progressCard.transform,Resources.Load<Sprite>("UI/Star"),.53f,.25f,.64f,.75f).color=CandyTheme.Gold;
            starsText = CandyTheme.Label(progressCard.transform,"",.65f,.41f,.96f,.90f,38,Color.white);
            CandyTheme.Label(progressCard.transform,"STARS EARNED",.65f,.12f,.96f,.42f,21,CandyTheme.Hex("EADDF5"));
            chapterText = CandyTheme.Label(root,"",.05f,.748f,.95f,.791f,32);
            completionText = CandyTheme.Label(root,"",.05f,.715f,.95f,.75f,23);
            var track = CandyTheme.Card(root,"ChapterProgressTrack",.07f,.699f,.93f,.707f,CandyTheme.Hex("DCC8E4"),false);
            chapterFill = CandyTheme.Card(track.transform,"ChapterProgressFill",0,0,0,1,CandyTheme.Purple,false).rectTransform;
            var scrollRoot = CandyTheme.Rect(root,"LevelCards",.04f,.225f,.96f,.684f);
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
            float height = Mathf.CeilToInt(count/(float)Columns)*RowSpacing+40;
            content.sizeDelta = new Vector2(0,height);
            chapterText.text = "WORLD " + (chapter+1) + "  /  " + ChapterNames[chapter%ChapterNames.Length];
            starsText.text = data.TotalStars.ToString("N0");
            balanceText.text = data.Coins.ToString("N0");
            completionText.text = current == 1 && completedCount == 0 && chapter == 0
                ? "Begin with level 1. Win to unlock the next sweet challenge."
                : "Levels " + first + " - " + (first+count-1) + "   /   " + completedCount + " of " + count + " complete";
            chapterFill.anchorMax = new Vector2((float)completedCount/count,1);
            previous.interactable = chapter > 0;
            next.interactable = first+count <= total;
            continueText.text = current == 1 && !(data.GetEntry(1)?.Completed ?? false) ? "START LEVEL 1"
                : (data.GetEntry(current)?.Completed ?? false) ? "REPLAY LEVEL " + current : "CONTINUE - LEVEL " + current;
            continueButton.interactable = data.IsLevelUnlocked(current);
            for(int i=0;i<count;i++)
            {
                int level=first+i;
                int row=i/Columns, column=i%Columns;
                float x0=.025f+column*.33f, x1=x0+.29f, y=-(25+TileHeight*.5f+row*RowSpacing);
                bool unlocked=data.IsLevelUnlocked(level), completed=data.GetEntry(level)?.Completed??false;
                bool isCurrent=level==current;
                if(isCurrent)
                {
                    var border=CandyTheme.Card(content,"CurrentLevelFrame",x0,1,x1,1,CandyTheme.Gold,false);
                    border.rectTransform.sizeDelta=new Vector2(14,TileHeight+14);
                    border.rectTransform.anchoredPosition=new Vector2(0,y);
                }
                Color tileColor = !unlocked ? CandyTheme.Hex("E9DDEE") : isCurrent ? CandyTheme.Purple : CandyTheme.Cream;
                Color ink = isCurrent ? Color.white : !unlocked ? CandyTheme.Hex("796485") : CandyTheme.Ink;
                var tile=CandyTheme.Button(content,"",x0,1,x1,1,tileColor,() => OpenLevel(level));
                var rt=(RectTransform)tile.transform; rt.sizeDelta=new Vector2(0,TileHeight); rt.anchoredPosition=new Vector2(0,y);
                tile.interactable=unlocked;
                var colors=tile.colors; colors.disabledColor=Color.white; tile.colors=colors;
                CandyTheme.Label(tile.transform,isCurrent?(level==1?"START HERE":"NEXT LEVEL"):"LEVEL",.05f,.76f,.95f,.94f,22,isCurrent?CandyTheme.Gold:ink);
                CandyTheme.Label(tile.transform,level.ToString(),.12f,.29f,.88f,.76f,70,ink);
                if(completed)
                {
                    for(int star=0;star<3;star++)
                    {
                        var image=CandyTheme.SpriteImage(tile.transform,Resources.Load<Sprite>("UI/Star"),.16f+star*.23f,.08f,.36f+star*.23f,.29f);
                        image.color=star<data.GetStars(level)?CandyTheme.Gold:CandyTheme.Hex("CFBDD8");
                    }
                }
                else if(unlocked)
                    CandyTheme.Label(tile.transform,"LET'S PLAY",.07f,.06f,.93f,.27f,25,ink);
                else
                {
                    CandyTheme.Icon(tile.transform,CandyGlyph.Kind.Lock,.16f,.08f,.30f,.27f,ink);
                    CandyTheme.Label(tile.transform,"LOCKED",.32f,.07f,.90f,.27f,23,ink);
                }
            }
            focus=StartCoroutine(FocusCurrent(first,count,height));
        }
        private IEnumerator FocusCurrent(int first,int count,float height)
        {
            yield return null;
            Canvas.ForceUpdateCanvases(); scroll.StopMovement();
            float index=Mathf.Clamp(SaveManager.Instance.Data.CurrentLevel-first,0,count-1);
            float row=Mathf.Floor(index/Columns);
            float distance=Mathf.Max(0,row*RowSpacing-scroll.viewport.rect.height*.15f);
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
