using System;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterMerge
{
    public sealed partial class GameUI
    {
        bool viewingGoogleLogin;
        Action afterGoogleLogin;
        Text googleStatus,homeAccountLabel;
        string previewAccount;
        Button googleSignIn,googleCheck;
        bool RequireGoogleLogin(Action next)
        {
            if(app.GoogleLogin && app.GoogleLogin.IsSignedIn)return true;
            ShowGoogleLogin(next);return false;
        }
        void RefreshGoogleUi()
        {
            var login=app.GoogleLogin;if(!login)return;
            if(previewAccount!=app.Save.Data.googleUserId){previewAccount=app.Save.Data.googleUserId;RefreshPreview();}
            if(homeAccountLabel)homeAccountLabel.text=login.IsSignedIn?"ACCOUNT":"SIGN IN";
            if(googleStatus)googleStatus.text=login.Status;
            if(googleSignIn)googleSignIn.interactable=login.Available&&!login.Busy&&!login.Waiting;
            if(googleCheck)googleCheck.interactable=login.Available&&!login.Busy;
            if(viewingGoogleLogin&&login.IsSignedIn&&afterGoogleLogin!=null)
            { var next=afterGoogleLogin;afterGoogleLogin=null;next(); }
            else if(viewingGoogleLogin&&login.IsSignedIn&&googleSignIn)ShowGoogleLogin();
        }
        public void ShowGoogleLogin(Action next=null)
        {
            Page(true);Header("YOUR ADVENTURE",ShowHome);
            viewingGoogleLogin=true;afterGoogleLogin=next;
            var login=app.GoogleLogin;
            TextAt(login.IsSignedIn?"WELCOME BACK, EXPLORER":"KEEP YOUR ADVENTURE",.08f,.65f,.84f,.09f,29,Color.white).fontStyle=FontStyle.Bold;
            TextAt(login.IsSignedIn?login.User.name:"Sign in with Google to save your dragon collection\nand purchase coin packs or upgrades.",.08f,.53f,.84f,.09f,17,Mint);
            googleStatus=TextAt(login.Status,.08f,.405f,.84f,.09f,14,Color.white);
            if(login.IsSignedIn)
            {
                ButtonAt("EXPLORE THE SHOP",.1f,.30f,.8f,.075f,ShowShop,Gold);
                ButtonAt("SIGN OUT",.1f,.205f,.8f,.065f,()=>{login.SignOut();ShowGoogleLogin();},Color.white);
            }
            else
            {
                googleSignIn=ButtonAt("Sign in with Play Games",.1f,.30f,.8f,.075f,login.SignIn,Color.white,null,login.Available&&!login.Busy&&!login.Waiting);
                googleCheck=ButtonAt("CHECK SIGN-IN",.1f,.205f,.8f,.065f,login.Check,Mint,null,!login.Busy);
                ButtonAt("CONTINUE AS GUEST",.1f,.105f,.8f,.06f,()=>{login.Cancel();afterGoogleLogin=null;ShowHome();},Color.white);
            }
            TextAt("Just exploring? You can play and earn coins as a guest.\nGoogle Play Games sign-in stays inside the game.",.075f,.025f,.85f,.06f,12,new Color(.59f,.70f,.80f));
        }
    }
}
