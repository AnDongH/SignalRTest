using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using MessagePack;
using Protocol.Protocols;
using Protocol.Router;
using Test;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    [SerializeField] TMP_InputField idInputField;
    [SerializeField] TMP_InputField pwInputField;
    [SerializeField] Button loginButton;
    [SerializeField] Button registerButton;
    
    [SerializeField] private bool isAdmin = false;
    
    private void Awake()
    {
        loginButton.onClick.RemoveAllListeners();
        loginButton.onClick.AddListener(OnLoginBtnClicked);
        
        registerButton.onClick.RemoveAllListeners();
        registerButton.onClick.AddListener(OnRegisterBtnClicked);
    }

    private void OnLoginBtnClicked()
    {

        ProtocolReq req = new LoginReq()
        {
            Id = idInputField.text,
            Password = pwInputField.text,
            IsAdmin = isAdmin
        };
        
        HttpManager.PostAsync(req, x =>
        {
            var res = MessagePackSerializer.Deserialize<ProtocolRes>(x) as LoginRes;
            if (res == null || string.IsNullOrEmpty(res.AuthToken) || res.Suid == 0 || res.ProtocolResult == ProtocolResult.Error)
            {
                Debug.Log("Login Failed");
                return;
            }
        
            Debug.Log("Login Success");
        
            UserData.Instance.Suid = res.Suid;
            UserData.Instance.AuthToken = res.AuthToken;
            UserData.Instance.Id = ((LoginReq)req).Id;
            UserData.Instance.Password = ((LoginReq)req).Password;
            UserData.Instance.IsAdmin = ((LoginReq)req).IsAdmin;
            
            SceneManager.LoadScene("Test");
        });
    }
    
    private void OnRegisterBtnClicked()
    {
        ProtocolReq req = new RegisterReq()
        {
            Id = idInputField.text,
            Password = pwInputField.text
        };

        HttpManager.PostAsync(req, x =>
        {
            var res = MessagePackSerializer.Deserialize<ProtocolRes>(x) as RegisterRes;
            if (res == null || res.ProtocolResult == ProtocolResult.Error)
            {
                Debug.Log("Register Failed");
                return;
            }
            
            Debug.Log("Register Success");
        });

    }
    
    private void OnDestroy()
    {
        loginButton.onClick.RemoveAllListeners();
        registerButton.onClick.RemoveAllListeners();
    }
}
