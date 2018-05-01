using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleFirebaseUnity.MiniJSON;
using SimpleFirebaseUnity;
using System;

public class NewGame : MonoBehaviour
{
    public Camera cam;
    public GvrReticlePointer g;
    Firebase firebase;
    string queryResult = "";

    public void Start()
    {
        firebase = Firebase.CreateNew("chessvr-sdpd.firebaseio.com");
        firebase.OnGetSuccess += GetOKHandler;
        firebase.OnGetFailed += GetFailHandler;
        g.overridePointerCamera = cam;
        g.transform.position = cam.transform.position;
        g.transform.rotation = cam.transform.rotation;
        g.enabled = true;
    }

    public void CreateNewGameWhite()
    {
        SceneManager.LoadScene("CreateScene", LoadSceneMode.Single);
        TimeSpan t = DateTime.UtcNow - DateTime.Today;
        string secondsSinceToday = ((int)t.TotalSeconds).ToString();
        firebase.Child("Test").Push("{ \"turn\": \"0\", \"datafrom\": \"connect!\", \"datato\": \"connect\", \"time\": " + secondsSinceToday + "}", true);
        // Method signature: void UpdateFailedHandler(Firebase sender, FirebaseError err)
        //SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        //firebase.GetValue("print=pretty");
        SceneManager.LoadScene("GameSceneWhite", LoadSceneMode.Single);
    }

    public void JoinNewGameBlack()
    {
        SceneManager.LoadScene("SearchScene", LoadSceneMode.Single);
        firebase.Child("Test", true).GetValue(FirebaseParam.Empty.OrderByChild("time").LimitToLast(1));
        //firebase.Child("scores", true).GetValue(FirebaseParam.Empty.OrderByChild("rating").LimitToFirst(2));
    }

    void ConfirmConnectionToFB()
    {
        while (queryResult == "")
            continue;

        int a = queryResult.IndexOf("\"datafrom\":");
        int b = queryResult.IndexOf("\",\"datato\":\"");
        string s = queryResult.Substring(a + 12, b - a - 12);     //fetches what is inside "data" (hopefully)
        Debug.Log(a.ToString() + " " + b.ToString() + " " + s);
        if (String.Compare(s, "connect!") == 0)
        {
            SceneManager.LoadScene("GameSceneBlack", LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene("OpenScene", LoadSceneMode.Single);
        }
    }
    void GetOKHandler(Firebase sender, DataSnapshot snapshot)
    {
        DoDebug("[OK] Get from key: <" + sender.FullKey + ">");
        DoDebug("[OK] Raw Json: " + snapshot.RawJson);
        queryResult = snapshot.RawJson;

        Dictionary<string, object> dict = snapshot.Value<Dictionary<string, object>>();
        List<string> keys = snapshot.Keys;

        if (keys != null)
            foreach (string key in keys)
            {
                DoDebug(key + " = " + dict[key].ToString());
            }

        ConfirmConnectionToFB();
    }

    void GetFailHandler(Firebase sender, FirebaseError err)
    {
        DoDebug("[ERR] Get from key: <" + sender.FullKey + ">,  " + err.Message + " (" + (int)err.Status + ")");
    }

    void SetOKHandler(Firebase sender, DataSnapshot snapshot)
    {
        DoDebug("[OK] Set from key: <" + sender.FullKey + ">");
    }

    void SetFailHandler(Firebase sender, FirebaseError err)
    {
        DoDebug("[ERR] Set from key: <" + sender.FullKey + ">, " + err.Message + " (" + (int)err.Status + ")");
    }

    void UpdateOKHandler(Firebase sender, DataSnapshot snapshot)
    {
        DoDebug("[OK] Update from key: <" + sender.FullKey + ">");
    }

    void UpdateFailHandler(Firebase sender, FirebaseError err)
    {
        DoDebug("[ERR] Update from key: <" + sender.FullKey + ">, " + err.Message + " (" + (int)err.Status + ")");
    }

    void DelOKHandler(Firebase sender, DataSnapshot snapshot)
    {
        DoDebug("[OK] Del from key: <" + sender.FullKey + ">");
    }

    void DelFailHandler(Firebase sender, FirebaseError err)
    {
        DoDebug("[ERR] Del from key: <" + sender.FullKey + ">, " + err.Message + " (" + (int)err.Status + ")");
    }

    void PushOKHandler(Firebase sender, DataSnapshot snapshot)
    {
        DoDebug("[OK] Push from key: <" + sender.FullKey + ">");
    }

    void PushFailHandler(Firebase sender, FirebaseError err)
    {
        DoDebug("[ERR] Push from key: <" + sender.FullKey + ">, " + err.Message + " (" + (int)err.Status + ")");
    }

    void GetRulesOKHandler(Firebase sender, DataSnapshot snapshot)
    {
        DoDebug("[OK] GetRules");
        DoDebug("[OK] Raw Json: " + snapshot.RawJson);
    }

    void GetRulesFailHandler(Firebase sender, FirebaseError err)
    {
        DoDebug("[ERR] GetRules,  " + err.Message + " (" + (int)err.Status + ")");
    }

    void GetTimeStamp(Firebase sender, DataSnapshot snapshot)
    {
        long timeStamp = snapshot.Value<long>();
        DateTime dateTime = Firebase.TimeStampToDateTime(timeStamp);

        DoDebug("[OK] Get on timestamp key: <" + sender.FullKey + ">");
        DoDebug("Date: " + timeStamp + " --> " + dateTime.ToString());
    }

    void DoDebug(string str)
    {
        Debug.Log(str);
        /*if (textMesh != null)
        {
            textMesh.text += (++debug_idx + ". " + str) + "\n";
        }*/
    }


    void Update()
    {
        var x = g.GetComponent<GvrPointerPhysicsRaycaster>().GetLastRay().ray;
        //var x = currCam.GetComponentInChildren<GvrPointerPhysicsRaycaster>().GetLastRay().ray;
        RaycastHit hit;
        //Debug.Log("Here");
        if (Physics.Raycast(x, out hit))
        {
            //Debug.Log("I hit " + hit.point.ToString());
            if (hit.point.x >= -90f && hit.point.x <= 90f)
            {
                if (hit.point.y >= -145f && hit.point.y <= -115f)
                {
                    if (hit.point.x <= -10f)
                        CreateNewGameWhite();
                    else if (hit.point.x >= 10f)
                        JoinNewGameBlack();

                }
            }
        }
    }
}