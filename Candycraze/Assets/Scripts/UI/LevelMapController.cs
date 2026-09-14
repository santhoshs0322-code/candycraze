using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace CandyCraze
{
    public class LevelMapController : MonoBehaviour
    {
        private RectTransform root, content;
        private ScrollRect scroll;
        private Text chapterText, starsText;
        private int chapter, total;
        private const int PageSize = 30;
        public void InjectRefs(Button b, Transform c, Text s) { } // Legacy scene compatibility.
        private void Start()
        {
            if (SaveManager.Instance == null) new GameObject("SaveManager").AddComponent<SaveManager>();
            var config = Resources.Load<GameConfig>("GameConfig");
            total = config != null && config.TotalLevels > 0 ? config.TotalLevels : 100;
            chapter = (Mathf.Clamp(SaveManager.Instance.Data.CurrentLevel,1,total)-1)/PageSize;
            root = CandyTheme.Page("PremiumLevelMap");
            CandyTheme.Button(root,"HOME",.055f,.925f,.265f,.978f,CandyTheme.Purple,() => SceneController.NavigateTo(Constants.SCENE_MAIN_MENU),28);
            CandyTheme.Label(root,"Sugar trail",.29f,.925f,.945f,.983f,55);
            chapterText = CandyTheme.Label(root,"",.05f,.859f,.95f,.91f,33);
            var scrollRoot = CandyTheme.Rect(root,"TrailScroll",.04f,.13f,.96f,.84f);
            var hit = scrollRoot.gameObject.AddComponent<Image>(); hit.color = new Color(1,1,1,.01f);
            scroll = scrollRoot.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.scrollSensitivity = 60;
            var viewport = CandyTheme.Rect(scrollRoot,"Viewport",0,0,1,1);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;
            content = CandyTheme.Rect(viewport,"Trail",0,1,1,1); content.pivot = new Vector2(.5f,1);
            scroll.content = content;
            CandyTheme.Button(root,"PREVIOUS",.055f,.047f,.325f,.107f,CandyTheme.Purple,() => ChangeChapter(-1),25);
            CandyTheme.Button(root,"NEXT",.675f,.047f,.945f,.107f,CandyTheme.Purple,() => ChangeChapter(1),25);
            starsText = CandyTheme.Label(root,"",.33f,.05f,.67f,.105f,30);
            SaveManager.Instance.OnDataChanged += Refresh;
            Refresh();
        }
        private void OnDestroy() { if (SaveManager.Instance != null) SaveManager.Instance.OnDataChanged -= Refresh; }
        private void ChangeChapter(int direction)
        {
            int next = Mathf.Clamp(chapter+direction,0,(total-1)/PageSize);
            if (next == chapter) return;
            chapter = next; Refresh();
        }
        private void Refresh()
        {
            if (content == null) return;
            foreach(Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            var data = SaveManager.Instance.Data;
            int first = chapter*PageSize+1, count = Mathf.Min(PageSize,total-first+1);
            float height = count*205+160;
            content.sizeDelta = new Vector2(0,height);
            chapterText.text = "CHAPTER " + (chapter+1) + "  •  Levels " + first + "–" + (first+count-1);
            starsText.text = data.TotalStars+" stars";
            for(int i=0;i<count;i++)
            {
                int level=first+i;
                float x=.5f+Mathf.Sin(i*.95f)*.27f, y=-(110+i*205);
                if(i>0)
                {
                    float previousX=.5f+Mathf.Sin((i-1)*.95f)*.27f;
                    for(int dot=1;dot<=7;dot++)
                    {
                        float t=dot/8f;
                        var bead=CandyTheme.Card(content,"SugarPath",Mathf.Lerp(previousX,x,t),1,Mathf.Lerp(previousX,x,t),1,Color.white,false);
                        bead.rectTransform.sizeDelta=new Vector2(19,19);
                        bead.rectTransform.anchoredPosition=new Vector2(0,y+205*(1-t));
                        bead.transform.SetAsFirstSibling(); // Trail must stay behind every candy node.
                    }
                }
                bool unlocked=data.IsLevelUnlocked(level), completed=data.GetEntry(level)?.Completed??false;
                var node=CandyTheme.Button(content,level.ToString(),x,1,x,1,unlocked?(completed?CandyTheme.Mint:CandyTheme.Pink):CandyTheme.Hex("A992B5"),() => OpenLevel(level),54);
                node.image.sprite=CandyTheme.Circle; node.image.type=Image.Type.Simple;
                var rt=(RectTransform)node.transform; rt.sizeDelta=new Vector2(148,148); rt.anchoredPosition=new Vector2(0,y);
                var shine=CandyTheme.SpriteImage(node.transform,CandyTheme.Circle,.17f,.62f,.83f,.91f);
                shine.preserveAspect=false; shine.color=new Color(1,1,1,.2f); shine.transform.SetAsFirstSibling();
                node.interactable=unlocked;
                if(completed)
                {
                    for(int star=0;star<3;star++)
                    {
                        var image=CandyTheme.SpriteImage(node.transform,Resources.Load<Sprite>("UI/Star"),.14f+star*.24f,-.27f,.38f+star*.24f,.03f);
                        image.color=star<data.GetStars(level)?CandyTheme.Gold:new Color(1,1,1,.35f);
                    }
                }
                else CandyTheme.Label(node.transform,unlocked?(level==Mathf.Min(data.CurrentLevel,total)?"NEXT ADVENTURE":"PLAY"):"LOCKED",-.45f,-.29f,1.45f,-.03f,23,unlocked?CandyTheme.Ink:CandyTheme.Hex("897193"));
            }
            StartCoroutine(FocusCurrent(first,count,height));
        }
        private IEnumerator FocusCurrent(int first,int count,float height)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            float index=Mathf.Clamp(SaveManager.Instance.Data.CurrentLevel-first,0,count-1);
            float distance=Mathf.Max(0,index*205-scroll.viewport.rect.height*.35f);
            float overflow=Mathf.Max(1,height-scroll.viewport.rect.height);
            scroll.verticalNormalizedPosition=1-Mathf.Clamp01(distance/overflow);
        }
        private void OpenLevel(int level)
        {
            if(!SaveManager.Instance.Data.IsLevelUnlocked(level)) return;
            LevelManager.SelectedLevelNumber=level;
            SceneController.NavigateTo(Constants.SCENE_GAME);
        }
    }
}
