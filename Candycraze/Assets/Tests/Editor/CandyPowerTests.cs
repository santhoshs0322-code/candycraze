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
    [Test] public void WrappedBlastClipsAtBoardEdges()
    {
        var grid=new GemView[6,6];
        for(int r=0;r<6;r++) for(int c=0;c<6;c++) grid[r,c]=Candy(r,c);
        Assert.AreEqual(9,Handler().GetAffectedGems(Candy(0,0,GemSpecialType.AreaBomb),grid,6,6).Count);
        Assert.AreEqual(25,Handler().GetAffectedGems(Candy(2,2,GemSpecialType.AreaBomb),grid,6,6).Count);
    }
}
