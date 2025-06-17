using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using UnityEngine;
using UnityEngine.UI;

namespace Test
{
    public class RoomManager : MonoBehaviour
    {
        private static RoomManager _instance;
        public static RoomManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<RoomManager>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("RoomManager");
                        _instance = obj.AddComponent<RoomManager>();
                    }
                }
                return _instance;
            }
        }

        [SerializeField] private Dictionary<string, Player> players = new Dictionary<string, Player>();
        [SerializeField] private GameObject playerPrefab;
        
        [SerializeField] private InputField roomNameInputField;
        
        private HubConnection connection;

        private string curRoom;
        
        private void Awake()
        {
            _instance = this;
        }
        
        public void MethodInit(HubConnection connection)
        {
            connection.On<float, float, float, string>("SyncPlayerPosition", (x, y, z, suid) =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    players[suid]?.SetTargetPosition(x,y,z);
                });
            });
            
            connection.On<string, List<string>, string>("OnEnteredGameRoom", (suidString, roomMembers, roomName) =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    curRoom = roomName;
                    var suid = long.Parse(suidString);
                    if (suid == UserData.Instance.Suid)
                    {
                        foreach (var m in roomMembers)
                        {
                            var g = Instantiate(playerPrefab);
                            players[m] = g.GetComponent<Player>();
                            players[m].suid = long.Parse(m); 
                        }
                    }
                    else
                    {
                        var g = Instantiate(playerPrefab);
                        players[suidString] = g.GetComponent<Player>();
                        players[suidString].suid = suid; 
                    }
                });
            });

            connection.On<string>("OnLeftGameRoom", suidString =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    var suid = long.Parse(suidString);
                    if (suid == UserData.Instance.Suid)
                    {
                        foreach (var m in players)
                        {
                            Destroy(m.Value.gameObject);
                        }
                        players.Clear();
                    }
                    else
                    {
                        foreach (var m in players)
                        {
                            if (m.Value.suid == suid)
                            {
                                Destroy(m.Value.gameObject);
                                players.Remove(m.Key);
                                break;
                            }
                        }
                    }
                });
            });
            
            this.connection = connection;
        }
        
        public async Task SyncPlayerPositionAsync()
        {
            try
            {
                if (connection.State == HubConnectionState.Connected)
                {
                    var player = players[UserData.Instance.Suid.ToString()];
                    await connection.InvokeAsync("SyncPlayerPositionAsync", curRoom, player.transform.position.x, player.transform.position.y, player.transform.position.z);
                }
                else
                {
                    Debug.LogWarning("서버에 연결되어 있지 않습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"메시지 전송 중 오류 발생: {ex.Message}");
            }
        }
        
        private async Task EnterGameRoomAsync()
        {
            try
            {
                if (connection.State == HubConnectionState.Connected)
                {
                    await connection.InvokeAsync("EnterGameRoomAsync", roomNameInputField.text);
                }
                else
                {
                    Debug.LogWarning("서버에 연결되어 있지 않습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"메시지 전송 중 오류 발생: {ex.Message}");
            }
        }
        
        private async Task LeaveGameRoomAsync()
        {
            try
            {
                if (connection.State == HubConnectionState.Connected)
                {
                    await connection.InvokeAsync("LeaveGameRoomAsync", curRoom);
                }
                else
                {
                    Debug.LogWarning("서버에 연결되어 있지 않습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"메시지 전송 중 오류 발생: {ex.Message}");
            }
        }
        
        public void OnClickEnterGameRoom()
        {
            _ = EnterGameRoomAsync();
        }
        
        public void OnClickLeaveGameRoom()
        {
            _ = LeaveGameRoomAsync();
        }
    }
}