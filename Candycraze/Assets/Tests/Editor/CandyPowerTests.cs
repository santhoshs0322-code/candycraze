using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CandyCraze;

public class CandyPowerTests
{
    private readonly List<Object> cleanup = new List<Object>();
    private GemDefinition definition;
    [SetUp] public void SetUp()
    {
        definition=ScriptableObject.CreateInstance<GemDefinition>();
        definition.GemTypeID=0;
        cleanup.Add(definition);
    }
    [TearDown] public void TearDown() { foreach(var item in cleanup) if(item!=null) Object.DestroyImmediate(item); cleanup.Clear(); }
    private GemView Candy(int row,int col,GemSpecialType power=GemSpecialType.None,bool vertical=false,int type=0)
    {
        var go=new GameObject("TestCandy"); go.SetActive(false); cleanup.Add(go);
        var gem=go.AddComponent<GemView>(); definition.GemTypeID=type;
        gem.Initialise(definition,row,col,power); gem.LineBlastVertical=vertical; gem.RefreshSpecialOverlay();
        return gem;
    }
    private List<GemView> Group(params int[] coordinates)
    {
        var group=new List<GemView>();
        for(int i=0;i<coordinates.Length;i+=2) group.Add(Candy(coordinates[i],coordinates[i+1]));
        return group;
    }
    [Test] public void MatchThreeIsNotAPower()
    { Assert.AreEqual(GemSpecialType.None,SpecialPieceHandler.DetermineSpecialType(Group(0,0,0,1,0,2))); }
    [Test] public void MatchFourCreatesDirectionalStripe()
    {
        var row=Group(1,0,1,1,1,2,1,3);
        var col=Group(0,1,1,1,2,1,3,1);
        Assert.AreEqual(GemSpecialType.LineBlast,SpecialPieceHandler.DetermineSpecialType(row));
        Assert.AreEqual(GemSpecialType.LineBlast,SpecialPieceHandler.DetermineSpecialType(col));
        Assert.IsFalse(SpecialPieceHandler.IsVerticalMatch(row));
        Assert.IsTrue(SpecialPieceHandler.IsVerticalMatch(col));
    }
    [Test] public void StraightFiveCreatesRainbow()
    { Assert.AreEqual(GemSpecialType.ColorCrystal,SpecialPieceHandler.DetermineSpecialType(Group(0,0,0,1,0,2,0,3,0,4))); }
    [Test] public void LTAndCrossCreateWrappedBombInsteadOfRainbow()
    {
        Assert.AreEqual(GemSpecialType.AreaBomb,SpecialPieceHandler.DetermineSpecialType(Group(0,0,1,0,2,0,2,1,2,2)));
        Assert.AreEqual(GemSpecialType.AreaBomb,SpecialPieceHandler.DetermineSpecialType(Group(0,0,0,1,0,2,1,1,2,1)));
        Assert.AreEqual(GemSpecialType.AreaBomb,SpecialPieceHandler.DetermineSpecialType(Group(0,1,1,0,1,1,1,2,2,1)));
    }
    private SpecialPieceHandler Handler()
    {
        var go=new GameObject("TestHandler"); go.SetActive(false); cleanup.Add(go);
        return go.AddComponent<SpecialPieceHandler>();
    }
    [Test] public void AllSixNormalCandiesAndFourPowerSpritesExist()
    {
        for(int type=0;type<6;type++) Assert.IsNotNull(CandyArtwork.GetNormal(type),"Missing candy "+type);
        Assert.IsNotNull(CandyArtwork.GetPower(GemSpecialType.LineBlast));
        Assert.IsNotNull(CandyArtwork.GetPower(GemSpecialType.LineBlast,true));
        Assert.IsNotNull(CandyArtwork.GetPower(GemSpecialType.AreaBomb));
        Assert.IsNotNull(CandyArtwork.GetPower(GemSpecialType.ColorCrystal));
        Assert.AreNotSame(CandyArtwork.GetPower(GemSpecialType.LineBlast),CandyArtwork.GetPower(GemSpecialType.LineBlast,true));
    }
    [Test] public void StripeBlastTriggersOtherStripeOnce()
    {
        var grid=new GemView[3,3];
        for(int r=0;r<3;r++) for(int c=0;c<3;c++) grid[r,c]=Candy(r,c);
        grid[1,0]=Candy(1,0,GemSpecialType.LineBlast);
        grid[1,2]=Candy(1,2,GemSpecialType.LineBlast,true);
        var result=Handler().ExpandSpecialChain(new List<GemView>{grid[1,0]},grid,3,3);
        Assert.AreEqual(5,result.Count);
        Assert.AreEqual(result.Count,new HashSet<GemView>(result).Count);
        Assert.Contains(grid[0,2],result);
        Assert.Contains(grid[2,2],result);
    }
    [Test] public void AlreadyActivatedRainbowDoesNotClearAnExtraColor()
    {
        var grid=new GemView[2,2];
        grid[0,0]=Candy(0,0,GemSpecialType.ColorCrystal,type:0);
        grid[0,1]=Candy(0,1,type:0);
        grid[1,0]=Candy(1,0,type:1);
        grid[1,1]=Candy(1,1,type:1);
        var result=Handler().ExpandSpecialChain(new List<GemView>{grid[0,0],grid[1,0],grid[1,1]},grid,2,2,grid[0,0]);
        Assert.AreEqual(3,result.Count);
        Assert.IsFalse(result.Contains(grid[0,1]));
    }
    private GemView[,] Board(int size=8)
    {
        var grid=new GemView[size,size];
        for(int r=0;r<size;r++) for(int c=0;c<size;c++) grid[r,c]=Candy(r,c,type:(r+c)%6);
        return grid;
    }
    [Test] public void MissingConfigReferencesResolveTheSameMatchedColor()
    {
        var config=ScriptableObject.CreateInstance<GameConfig>(); cleanup.Add(config);
        config.GemDefinitions=new GemDefinition[6];
        for(int type=0;type<6;type++) Assert.AreEqual(type,config.GetGemDefinition(type).GemTypeID);
    }
    [Test] public void StarterPackAddsOneEachAndNeverRefillsSpentInventory()
    {
        var save=new SaveData { BoosterHammer=3 };
        Assert.IsTrue(save.GrantStarterBoosters());
        Assert.AreEqual(4,save.BoosterHammer);
        Assert.AreEqual(1,save.BoosterRowBlast);
        Assert.AreEqual(1,save.BoosterShuffle);
        Assert.AreEqual(1,save.BoosterExtraMoves);
        Assert.AreEqual(1,save.BoosterColorBlast);
        save.BoosterShuffle=0;
        var restored=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save));
        Assert.IsFalse(restored.GrantStarterBoosters());
        Assert.AreEqual(0,restored.BoosterShuffle);
    }
    [TestCase(false)] [TestCase(true)]
    public void StripePairClearsCrossRegardlessOfDirection(bool vertical)
    {
        var grid=Board();
        var a=grid[3,3]; var b=grid[3,4];
        a.SetPower(GemSpecialType.LineBlast,vertical); b.SetPower(GemSpecialType.LineBlast,vertical);
        var result=SpecialPieceHandler.Combination(a,b,grid,new List<Vector3Int>());
        Assert.AreEqual(15,result.Count);
        Assert.Contains(grid[0,3],result); Assert.Contains(grid[3,7],result);
    }
    [TestCase(false)] [TestCase(true)]
    public void StripeWrappedClearsThreeRowsAndColumns(bool reverse)
    {
        var grid=Board(); var a=grid[3,3]; var b=grid[3,4];
        a.SetPower(reverse?GemSpecialType.AreaBomb:GemSpecialType.LineBlast);
        b.SetPower(reverse?GemSpecialType.LineBlast:GemSpecialType.AreaBomb);
        Assert.AreEqual(39,SpecialPieceHandler.Combination(a,b,grid,new List<Vector3Int>()).Count);
    }
    [Test] public void WrappedPairSchedulesSecondLargeBlast()
    {
        var grid=Board(); var a=grid[3,3]; var b=grid[3,4]; a.SetPower(GemSpecialType.AreaBomb); b.SetPower(GemSpecialType.AreaBomb);
        var repeats=new List<Vector3Int>();
        Assert.AreEqual(25,SpecialPieceHandler.Combination(a,b,grid,repeats).Count);
        Assert.AreEqual(new Vector3Int(3,3,2),repeats[0]);
    }
    [TestCase(GemSpecialType.LineBlast,false)] [TestCase(GemSpecialType.LineBlast,true)]
    [TestCase(GemSpecialType.AreaBomb,false)] [TestCase(GemSpecialType.AreaBomb,true)]
    public void RainbowTransformsPartnerColorInEitherOrder(GemSpecialType power,bool reverse)
    {
        var grid=Board(); var bomb=grid[3,3]; var partner=grid[3,4];
        bomb.SetPower(GemSpecialType.ColorCrystal); partner.SetPower(power);
        var seeds=SpecialPieceHandler.Combination(reverse?partner:bomb,reverse?bomb:partner,grid,new List<Vector3Int>());
        foreach(var gem in grid)
            if(gem!=bomb && gem.GemTypeID==partner.GemTypeID) { Assert.AreEqual(power,gem.SpecialType); Assert.Contains(gem,seeds); }
        Assert.Contains(bomb,seeds);
    }
    [Test] public void RainbowPairClearsWholeBoard()
    {
        var grid=Board(); grid[0,0].SetPower(GemSpecialType.ColorCrystal); grid[0,1].SetPower(GemSpecialType.ColorCrystal);
        Assert.AreEqual(64,SpecialPieceHandler.Combination(grid[0,0],grid[0,1],grid,new List<Vector3Int>()).Count);
    }
    [Test] public void StripeAndNormalRequireAMatch()
    {
        var stripe=Candy(0,0,GemSpecialType.LineBlast); var normal=Candy(0,1);
        Assert.IsFalse(SpecialPieceHandler.CanCombine(stripe,normal));
        stripe.SetPower(GemSpecialType.ColorCrystal);
        Assert.IsTrue(SpecialPieceHandler.CanCombine(stripe,normal));
    }
    [Test] public void SpawnPrefersDraggedCandyThenOtherMatchedCandy()
    {
        var group=Group(0,0,0,1,0,2,0,3,0,4);
        Assert.AreSame(group[0],SpecialPieceHandler.ChooseSpawn(group,group[0],group[3]));
        Assert.AreSame(group[3],SpecialPieceHandler.ChooseSpawn(group,Candy(1,0),group[3]));
    }
    [Test] public void FiveStraightWinsOverIntersectingThree()
    { Assert.AreEqual(GemSpecialType.ColorCrystal,SpecialPieceHandler.DetermineSpecialType(Group(0,0,0,1,0,2,0,3,0,4,1,2,2,2))); }
    [Test] public void RainbowCannotParticipateInOrdinaryMatches()
    {
        var grid=new GemView[1,3];
        for(int c=0;c<3;c++) grid[0,c]=Candy(0,c);
        grid[0,1].SetPower(GemSpecialType.ColorCrystal);
        var go=new GameObject("MatchDetector"); cleanup.Add(go);
        Assert.IsEmpty(go.AddComponent<MatchDetector>().FindAllMatches(grid,1,3));
    }
    [Test] public void WrappedBlastClipsAtBoardEdges()
    {
        var grid=new GemView[6,6];
        for(int r=0;r<6;r++) for(int c=0;c<6;c++) grid[r,c]=Candy(r,c);
        Assert.AreEqual(4,Handler().GetAffectedGems(Candy(0,0,GemSpecialType.AreaBomb),grid,6,6).Count);
        Assert.AreEqual(9,Handler().GetAffectedGems(Candy(2,2,GemSpecialType.AreaBomb),grid,6,6).Count);
    }
}
