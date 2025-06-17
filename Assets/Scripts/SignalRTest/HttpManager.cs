using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using MessagePack;
using Protocol.Protocols;
using Protocol.Router;
using UnityEngine;
using ByteArrayContent = System.Net.Http.ByteArrayContent;

namespace Test
{
    public static class HttpManager
    {
        private static HttpClient Client { get; set; }
        public const string BaseUrl = "http://localhost/";

        static HttpManager()
        {
            Client = new HttpClient();
        }
        
        public static async Task<byte[]> PostAsync(byte[] data, string url)
        {
            try
            {

                if (!string.IsNullOrEmpty(UserData.Instance.AuthToken)) 
                    Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(UserData.Instance.AuthToken);
                if (UserData.Instance.Suid != 0)
                {
                    Client.DefaultRequestHeaders.Remove("suid");
                    Client.DefaultRequestHeaders.Add("suid", UserData.Instance.Suid.ToString());
                }
                
                // 요청 보내기
                using (var m = await Client.PostAsync(url, new ByteArrayContent(data)))
                {
                    if (!m.IsSuccessStatusCode)
                    {
                        Debug.LogError($"Error: {m.StatusCode}");
                        return null;
                    }
                    var res = await m.Content.ReadAsByteArrayAsync();
                    return res;
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            return null;
        }

        public static void PostAsync(ProtocolReq req, Action<byte[]> callback)
        {
            var data = MessagePackSerializer.Serialize(req);
            var url = $"{BaseUrl}{ProtocolRouter.RouterMap[req.ProtocolId]}";
            PostAsync(data, url).ContinueWith(x => UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                if (x.Result == null) return;
                callback?.Invoke(x.Result);
            }));
        }

        public static async Task AutoLoginAsync()
        {
            ProtocolReq req = new LoginReq()
            {
                Id = UserData.Instance.Id,
                Password = UserData.Instance.Password,
                IsAdmin = UserData.Instance.IsAdmin
            };
                        
            var bytes = MessagePackSerializer.Serialize(req);
            var url = $"{BaseUrl}{ProtocolRouter.RouterMap[req.ProtocolId]}";
            var data = await PostAsync(bytes, url);
            var res = MessagePackSerializer.Deserialize<ProtocolRes>(data) as LoginRes;
                        
            UserData.Instance.Suid = res.Suid;
            UserData.Instance.AuthToken = res.AuthToken;
        }
    }
}