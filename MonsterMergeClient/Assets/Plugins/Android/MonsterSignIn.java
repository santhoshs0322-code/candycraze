package com.gamixtv.monstermerge.auth;
import android.app.Activity;
import com.unity3d.player.UnityPlayer;
import com.google.android.gms.games.PlayGames;
import org.json.JSONObject;

public final class MonsterSignIn {
    private static void send(String receiver,int operation,String code,String error) {
        try {
            JSONObject result=new JSONObject();result.put("operation",operation);
            result.put("code",code);result.put("error",error);
            UnityPlayer.UnitySendMessage(receiver,"OnNativeSignIn",result.toString());
        } catch(Exception ignored) { }
    }
    public static void signIn(String receiver,String serverClientId,int operation) {
        Activity activity=UnityPlayer.currentActivity;
        activity.runOnUiThread(()->{
            try {
                PlayGames.getGamesSignInClient(activity).signIn().addOnCompleteListener(task->{
                    if(!task.isSuccessful()||task.getResult()==null||!task.getResult().isAuthenticated()) {
                        send(receiver,operation,"","Play Games sign-in was cancelled or unavailable.");return;
                    }
                    PlayGames.getGamesSignInClient(activity).requestServerSideAccess(serverClientId,false).addOnCompleteListener(access->{
                        if(!access.isSuccessful()||access.getResult()==null||access.getResult().isEmpty())
                            send(receiver,operation,"","Play Games server access is not configured. Please contact support.");
                        else send(receiver,operation,access.getResult(),"");
                    });
                });
            } catch(Exception error) { send(receiver,operation,"","Play Games could not start. Check the game configuration."); }
        });
    }
}
