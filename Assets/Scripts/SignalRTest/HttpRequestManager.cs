using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Protocol.Protocols;
using Test;
using UnityEngine;

public class HttpRequestManager : MonoBehaviour
{
    public void OnHubContextBtnClicked()
    {   
        ProtocolReq req = new HubTestReq();
        
        HttpManager.PostAsync(req, x =>
        {
            var res = MessagePackSerializer.Deserialize<ProtocolRes>(x) as HubTestRes;
            if (res == null || res.ProtocolResult == ProtocolResult.Error)
            {
                Debug.Log("Hub Test Failed");
                return;
            }
        
            Debug.Log("Hub Test Success");
        });
    }
}
