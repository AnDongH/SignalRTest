using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Protocol.Packets;
using Protocol.Protocols;
using Protocol.Router;
using Test;
using UnityEngine;
using UnityEngine.UI;

public class SignalRManager : MonoBehaviour
{
    public HubConnection connection;
    public string serverUrl = "http://localhost/signalR/chatHub";
    [SerializeField] private InputField inputField;
    [SerializeField] private Text receivedMessageText;

    [SerializeField] private string currentGroupName;
    [SerializeField] private List<string> groups;
    
    private static SignalRManager _instance;
    public static SignalRManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SignalRManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("SignalRManager");
                    _instance = obj.AddComponent<SignalRManager>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        _instance = this;
    }
    
    private async void Start()
    {
        try
        {
            await ConnectToServer();
        }
        catch (Exception)
        {
            throw; // TODO 예외 처리
        }
    }

    private async Task ConnectToServer()
    {
        try
        {
            // 서버 연결 설정
            connection = new HubConnectionBuilder()
                .WithUrl(serverUrl, options =>
                {
                    options.Headers["suid"] = UserData.Instance.Suid.ToString();
                    options.AccessTokenProvider = async () =>
                    {
                        await HttpManager.AutoLoginAsync();
                        return UserData.Instance.AuthToken;
                    };
                }) // 이거 주소는 한번 정하면 못바꾼다.
                .WithAutomaticReconnect(new []
                {
                    TimeSpan.Zero, 
                    TimeSpan.FromSeconds(5), 
                    TimeSpan.FromSeconds(10), 
                    TimeSpan.FromSeconds(30)
                })
                .AddMessagePackProtocol(options =>
                {
                    options.SerializerOptions = MessagePackSerializerOptions.Standard
                        .WithSecurity(MessagePackSecurity.UntrustedData);
                })
                .Build();
            
            connection.On<string>("ReceiveAllMessage", (message) =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log($"{message} - All");
                    receivedMessageText.text = $"{message} - All";
                });
            });

            connection.On<string>("ReceiveOthersMessage", (message) =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log($"{message} - Others");
                    receivedMessageText.text = $"{message} - Others";
                });
            });

            connection.On<string>("ReceiveCallerMessage", (message) =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log($"{message} - Caller");
                    receivedMessageText.text = $"{message} - Caller";
                });
            });

            connection.On<string>("ReceiveAllExceptMessage", message =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log($"{message} - AllExcept");
                    receivedMessageText.text = $"{message} - AllExcept";
                });
            });

            connection.On<string>("ReceiveClientMessage", message =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log($"{message} - Client");
                    receivedMessageText.text = $"{message} - Client";
                });
            });

            connection.On<string>("ReceiveGroupMessage", message =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log($"{message} - Group");
                    receivedMessageText.text = $"{message} - Group";
                });
            });

            connection.On<string>("ReceiveGroupsMessage", message =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log($"{message} - Groups");
                    receivedMessageText.text = $"{message} - Groups";
                });
            });

            connection.On<string>("ReceiveGroupExceptMessage", message =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log($"{message} - GroupExcept");
                    receivedMessageText.text = $"{message} - GroupExcept";
                });
            });

            connection.On<string>("ReceiveOthersInGroupMessage", message =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log($"{message} - OthersInGroup");
                    receivedMessageText.text = $"{message} - OthersInGroup";
                });
            });

            connection.On<string>("ReceiveUserMessage", message =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log($"{message} - User");
                    receivedMessageText.text = $"{message} - User";
                });
            });

            connection.On<string>("ReceiveUsersMessage", message =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log($"{message} - Users");
                    receivedMessageText.text = $"{message} - Users";
                });
            });
            
            connection.On<string>("OnEnteredGroupAll", message =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log($"{message}");
                    receivedMessageText.text = $"{message}";
                    _ = GetGroups();
                });
            });
            
            connection.On<string>("OnEnteredGroupCaller", grpName =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    currentGroupName = grpName;
                    _ = GetGroups();
                });
            });
            
            connection.On<string>("OnLeftGroupAll", message =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log($"{message}");
                    receivedMessageText.text = $"{message}";
                    _ = GetGroups();
                });
            });
            
            connection.On("OnLeftGroupCaller", () =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    currentGroupName = "";
                    _ = GetGroups();
                });
            });
            
            connection.On<List<string>>("OnGetGroups", grp =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    groups = grp.ToList();
                });
            });
            
            // 서버에서 클라이언트에게 결과를 요청할때 콜백
            connection.On("OnServerRequestClientResult", () =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log("서버에서 클라이언트 결과 요청");
                });
                return "hello";
            });
            
            connection.On<string>("Notify", message =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log($"{message}");
                    receivedMessageText.text = $"{message}";
                });
            });
            
            connection.On<string>("OnErrored", message =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log($"{message}");
                    receivedMessageText.text = $"{message}";
                });
            });

            connection.On<string>("ShowTime", time =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log(time);
                });
            });
            
            RoomManager.Instance.MethodInit(connection);
            
            connection.Reconnecting += error =>
            {
                Debug.Assert(connection.State == HubConnectionState.Reconnecting);
                Debug.Log("서버와 연결이 끊어졌습니다. 재연결 시도중...");
                return Task.CompletedTask;
            };

            connection.Reconnected += connectionId =>
            {
                Debug.Assert(connection.State == HubConnectionState.Connected);
                Debug.Log("서버에 재연결되었습니다!");
                return Task.CompletedTask;
            };

            connection.Closed += error =>
            {
                Debug.Assert(connection.State == HubConnectionState.Disconnected);
                Debug.Log("서버와 연결이 종료되었습니다.");
                return Task.CompletedTask;
            };

            // 연결 시작
            await connection.StartAsync();
            Debug.Log("서버에 연결되었습니다!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"서버 연결 중 오류 발생: {ex.Message}");
        }
    }

    public static async Task<bool> ConnectWithRetryAsync(HubConnection connection, CancellationToken token)
    {
        // Keep trying to until we can start or the token is canceled.
        while (true)
        {
            try
            {
                await connection.StartAsync(token);
                Debug.Assert(connection.State == HubConnectionState.Connected);
                return true;
            }
            catch when (token.IsCancellationRequested)
            {
                return false;
            }
            catch
            {
                // Failed to connect, trying again in 5000 ms.
                Debug.Assert(connection.State == HubConnectionState.Disconnected);
                await Task.Delay(5000);
            }
        }
    }

    // 메시지 전송 메서드
    private async Task SendAllMessage()
    {
        try
        {
            if (connection.State == HubConnectionState.Connected)
            {
                // signalR 용 MessagePack에서는 다형성을 지원하려면, 커스텀 리졸버를 만들어줘야함..
                var req = new ClientChatPacket()
                {
                    Message = inputField.text
                };

                await connection.InvokeAsync("SendAllMessage", req);
                inputField.text = "";
                StartCoroutine(RefocusInputField());
            }
            else
            {
                Debug.LogWarning("서버에 연결되어 있지 않습니다.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"메시지 전송 중 오류 발생: {ex}");
        }
    }

    // 메시지 전송 메서드
    private async Task SendOthersMessage()
    {
        try
        {
            if (connection.State == HubConnectionState.Connected)
            {
                var req = new ClientChatPacket
                {
                    Message = inputField.text
                };
                await connection.InvokeAsync("SendOthersMessage", req);
                inputField.text = "";
                StartCoroutine(RefocusInputField());
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

    // 메시지 전송 메서드
    private async Task SendCallerMessage()
    {
        try
        {
            if (connection.State == HubConnectionState.Connected)
            {
                var req = new ClientChatPacket
                {
                    Message = inputField.text
                };

                await connection.InvokeAsync("SendCallerMessage", req);
                inputField.text = "";
                StartCoroutine(RefocusInputField());
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

    // 메시지 전송 메서드
    private async Task SendAllExceptMessage()
    {
        try
        {
            if (connection.State == HubConnectionState.Connected)
            {
                var req = new ClientChatPacket
                {
                    Message = inputField.text
                };
                string[] exceps = { connection.ConnectionId };
                await connection.InvokeAsync("SendAllExceptMessage", exceps, req);
                inputField.text = "";
                StartCoroutine(RefocusInputField());
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

    private async Task SendClientMessage()
    {
        try
        {
            if (connection.State == HubConnectionState.Connected)
            {
                var req = new ClientChatPacket
                {
                    Message = inputField.text
                };
                await connection.InvokeAsync("SendClientMessage", connection.ConnectionId, req);
                inputField.text = "";
                StartCoroutine(RefocusInputField());
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

    private async Task SendGroupMessage()
    {
        try
        {
            if (connection.State == HubConnectionState.Connected)
            {
                var req = new ClientChatPacket
                {
                    Message = inputField.text
                };
                await connection.InvokeAsync("SendGroupMessage", currentGroupName, req);
                inputField.text = "";
                StartCoroutine(RefocusInputField());
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

    private async Task SendGroupsMessage()
    {
        try
        {
            if (connection.State == HubConnectionState.Connected)
            {
                var req = new ClientChatPacket
                {
                    Message = inputField.text
                };
                await connection.InvokeAsync("SendGroupsMessage", groups, req);
                inputField.text = "";
                StartCoroutine(RefocusInputField());
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

    private async Task SendGroupExceptMessage()
    {
        try
        {
            if (connection.State == HubConnectionState.Connected)
            {
                var req = new ClientChatPacket
                {
                    Message = inputField.text
                };
                await connection.InvokeAsync("SendGroupExceptMessage", currentGroupName, connection.ConnectionId, req);
                inputField.text = "";
                StartCoroutine(RefocusInputField());
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

    private async Task SendOthersInGroupMessage()
    {
        try
        {
            if (connection.State == HubConnectionState.Connected)
            {
                var req = new ClientChatPacket
                {
                    Message = inputField.text
                };
                await connection.InvokeAsync("SendOthersInGroupMessage", currentGroupName, req);
                inputField.text = "";
                StartCoroutine(RefocusInputField());
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

    private async Task SendUserMessage()
    {
        try
        {
            if (connection.State == HubConnectionState.Connected)
            {
                var req = new ClientChatPacket()
                {
                    Message = inputField.text
                };
                await connection.InvokeAsync("SendUserMessage", req);
                inputField.text = "";
                StartCoroutine(RefocusInputField());
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

    private async Task SendUsersMessage()
    {
        try
        {
            if (connection.State == HubConnectionState.Connected)
            {
                var req = new ClientChatPacket()
                {
                    Message = inputField.text
                };
                
                string[] users = { UserData.Instance.Suid.ToString() };
                
                await connection.InvokeAsync("SendUsersMessage", users, req);
                inputField.text = "";
                StartCoroutine(RefocusInputField());
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

    private async Task EnterGroup()
    {
        try
        {
            if (connection.State == HubConnectionState.Connected)
            {
                await connection.InvokeAsync("EnterGroup", inputField.text);
                StartCoroutine(RefocusInputField());
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

    private async Task LeaveGroup()
    {
        try
        {
            if (connection.State == HubConnectionState.Connected)
            {
                await connection.InvokeAsync("LeaveGroup", currentGroupName);
                StartCoroutine(RefocusInputField());
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

    private async Task GetGroups()
    {
        try
        {
            if (connection.State == HubConnectionState.Connected)
            {
                await connection.InvokeAsync("GetGroups");
                StartCoroutine(RefocusInputField());
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

    private async Task ServerRequestClientResult()
    {
        try
        {
            if (connection.State == HubConnectionState.Connected)
            {
                await connection.InvokeAsync("ServerRequestClientResult");
                StartCoroutine(RefocusInputField());
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

    // S2C == Server to Client
    private async Task S2CStreamingAsIAsyncEnumerable()
    {
        try
        {
            if (connection.State == HubConnectionState.Connected)
            {

                // 구독 취소를 나타내기 위한 CancellationTokenSource
                var cancellationTokenSource = new CancellationTokenSource();
                var stream = connection.StreamAsync<int>("CounterIAsyncEnumerable", 10, 500, cancellationTokenSource.Token);

                await foreach (var count in stream)
                {
                    Debug.Log($"{count}");
                }

                Debug.Log("Streaming completed");
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

    // S2C == Server to Client
    private async Task S2CStreamingAsChannelReader()
    {
        try
        {
            if (connection.State == HubConnectionState.Connected)
            {
                // 구독 취소를 나타내기 위한 CancellationTokenSource
                var cancellationTokenSource = new CancellationTokenSource();
                var channel = await connection.StreamAsChannelAsync<int>("CounterChannelReader", 10, 500, cancellationTokenSource.Token);

                // 스트림 채널에서 데이터를 읽는 루프
                while (await channel.WaitToReadAsync(cancellationTokenSource.Token))
                {
                    while (channel.TryRead(out var count))
                    {
                        Debug.Log($"{count}");
                    }
                }
                Debug.Log("Streaming completed");
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
    
    // C2S == Client to Server
    private async Task C2SStreamingAsIAsyncEnumerable()
    {
        try
        {
            if (connection.State == HubConnectionState.Connected)
            {
                
                // IAsyncEnumerable 반환하는 로컬 메서드 생성
                async IAsyncEnumerable<int> ClientStreamData()
                {
                    for (var i = 0; i < 10; i++)
                    {
                        await Task.Delay(500);
                        yield return i;
                    }
                }
                
                // 로컬 메서드에서 생성한 IAsyncEnumerable을 서버에 전송
                await connection.SendAsync("UploadStreamIAsyncEnumerable", ClientStreamData());
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

    // C2S == Client to Server
    private async Task C2SStreamingAsChannelReader()
    {
        try
        {
            if (connection.State == HubConnectionState.Connected)
            {
                // 채널의 reader를 서버에 전송
                var channel = Channel.CreateBounded<int>(10);
                await connection.SendAsync("UploadStreamChannelReader", channel.Reader);

                // 서버는 해당 작업이 끝날때마다 값을 얻음
                for (var i = 0; i < 10; i++)
                {
                    await channel.Writer.WriteAsync(i);
                    await Task.Delay(500);
                }
                
                channel.Writer.Complete();
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
    
    private IEnumerator RefocusInputField()
    {
        // 한 프레임 기다리기 (InputField가 내부적으로 포커스를 해제하기 때문)
        yield return null;
        
        // 포커스 다시 설정
        inputField.ActivateInputField();
        inputField.Select();
    }
    
    public void OnSendAllMessageBtnClicked()
    {
        _ = SendAllMessage();
    }
    
    public void OnSendOthersMessageBtnClicked()
    {
        _ = SendOthersMessage();
    }
    
    public void OnSendCallerMessageBtnClicked()
    {
        _ = SendCallerMessage();
    }
    
    public void OnSendAllExceptMessageBtnClicked()
    {
        _ = SendAllExceptMessage();
    }
    
    public void OnSendClientMessageBtnClicked()
    {
        _ = SendClientMessage();
    }
    
    public void OnSendGroupMessageBtnClicked()
    {
        _ = SendGroupMessage();
    }
    
    public void OnSendGroupsMessageBtnClicked()
    {
        _ = SendGroupsMessage();
    }
    
    public void OnSendGroupExceptMessageBtnClicked()
    {
        _ = SendGroupExceptMessage();
    }
    
    public void OnSendOthersInGroupMessageBtnClicked()
    {
        _ = SendOthersInGroupMessage();
    }
    
    public void OnSendUserMessageBtnClicked()
    {
        _ = SendUserMessage();
    }

    
    public void OnSendUsersMessageBtnClicked()
    {
        _ = SendUsersMessage();
    }
    
    public void OnEnterGroupBtnClicked()
    {
        _ = EnterGroup();
    }
    
    public void OnLeaveGroupBtnClicked()
    {
        _ = LeaveGroup();
    }
    
    public void OnGetGroupsBtnClicked()
    {
        _ = GetGroups();
    }
    
    public void OnServerRequestClientResultBtnClicked()
    {
        _ = ServerRequestClientResult();
    }

    public void OnS2CStreamingAsIAsyncEnumerableBtnClicked()
    {
        _ = S2CStreamingAsIAsyncEnumerable();
    }
    
    public void OnS2CStreamingAsChannelReaderBtnClicked()
    {
          _ = S2CStreamingAsChannelReader(); 
    }
    
    public void OnC2SStreamingAsIAsyncEnumerableBtnClicked()
    {
        _ = C2SStreamingAsIAsyncEnumerable();
    }
    
    public void OnC2SStreamingAsChannelReaderBtnClicked()
    {
        _ = C2SStreamingAsChannelReader(); 
    }
    
    private async void OnApplicationQuit()
    {
        try
        {
            // 연결 종료
            if (connection != null)
            {
                await connection.StopAsync();
            }
        }
        catch (Exception)
        {
            throw; // TODO 예외 처리
        }
    }
    
    public void GameExit()
    {
        Application.Quit();
        Debug.Log("게임 종료");
    }
}