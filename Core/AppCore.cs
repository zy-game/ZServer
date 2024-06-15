using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using FixMath.NET;
using ZGame.Networking;
using ZGame.Room;

namespace ZGame;

public static class AppCore
{
    /// <summary>
    /// 服务器名
    /// </summary>
    public static string name { get; set; }

    /// <summary>
    /// 服务器版本
    /// </summary>
    public static string version { get; set; }

    /// <summary>
    /// 帧间隔时间(毫秒)
    /// </summary>
    public static double fixedDeltaTime { get; set; } = 100;

    /// <summary>
    /// 从app启动到现在的时间(毫秒)
    /// </summary>
    public static double timeSinceStartUp => (DateTime.Now - _startUpTimeStamp).TotalMilliseconds;


    private static bool isRunning;
    private static UDPSocket udpSocket;
    private static double _lastTimeStamp;
    private static DateTime _startUpTimeStamp;
    private static double _lastClearClientTime;
    private const double _clearClientInterval = 60000;
    private static List<Client> _clients = new List<Client>();
    private static List<RoomBase> _rooms = new List<RoomBase>();
    private static List<Service> _services = new List<Service>();
    private static Queue<(Client, MSGPacket)> _msgQueue = new Queue<(Client, MSGPacket)>();
    private static Dictionary<int, SubscribeHandle> _subscribes = new Dictionary<int, SubscribeHandle>();

    public static async void Startup(ushort port)
    {
        Log("===================服务器启动===================");
        isRunning = true;
        _startUpTimeStamp = DateTime.Now;

        udpSocket = new UDPSocket();
        udpSocket.OnConnect += client => _clients.Add(Client.Create(client));
        udpSocket.OnRecvie += (c, m) =>
        {
            if (c.isConnected is false)
            {
                return;
            }

            Client client = _clients.Find(x => x.cid == c.cid);
            if (client is null)
            {
                return;
            }

            _msgQueue.Enqueue((client, m));
        };
        udpSocket.OnDisconnect += sender =>
        {
            Client client = _clients.Find(x => x.cid == sender.cid);
            if (client is null)
            {
                return;
            }

            client.LeaveRoom();
            _clients.Remove(client);
        };
        udpSocket.OnStart(port);
        OnRunning();
        Log("===================服务器关闭===================");
    }

    /// <summary>
    /// 通过房间号获取房间
    /// </summary>
    /// <param name="rid"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetRoom<T>(int rid) where T : RoomBase
    {
        return _rooms.Find(x => x.rid == rid) as T;
    }

    /// <summary>
    /// 获取玩家所在的房间
    /// </summary>
    /// <param name="uid"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetRoom<T>(uint uid) where T : RoomBase
    {
        return _rooms.Find(x => x.HasUser(uid)) as T;
    }

    /// <summary>
    /// 获取空闲房间
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetFreeRoom<T>() where T : RoomBase
    {
        return (T)_rooms.Find(x => x is T && x.state == RoomState.None && x.clients.Length < x.maxUserCount);
    }

    /// <summary>
    /// 创建房间
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T CreateRoom<T>() where T : RoomBase
    {
        T room = RefPooled.Alloc<T>();
        room.Awake();
        _rooms.Add(room);
        return room;
    }

    public static void RemoveRoom(RoomBase room)
    {
        _rooms.Remove(room);
        RefPooled.Free(room);
    }

    private static void OnRunning()
    {
        while (isRunning)
        {
            udpSocket.OnUpdate();
            DispatcherMessage();
            ClearOfflineClient();
            OnUpdate();
            OnFixedUpdate();
            Thread.Sleep(1);
        }
    }

    private static void DispatcherMessage()
    {
        while (_msgQueue.Count > 0)
        {
            var (client, msg) = _msgQueue.Dequeue();
            if (client is null || msg is null)
            {
                continue;
            }

            Dispatch(msg.opcode, client, msg);
        }
    }

    private static void ClearOfflineClient()
    {
        if (timeSinceStartUp - _lastClearClientTime < _clearClientInterval)
        {
            return;
        }

        _lastClearClientTime = timeSinceStartUp;
        _clients.RemoveAll(x => x.isOnline is false);
    }

    private static void OnUpdate()
    {
        for (int i = 0; i < _services.Count; i++)
        {
            _services[i].Update();
        }

        for (int i = 0; i < _rooms.Count; i++)
        {
            _rooms[i].Update();
        }
    }

    private static void OnFixedUpdate()
    {
        if (timeSinceStartUp - _lastTimeStamp < fixedDeltaTime)
        {
            return;
        }

        _lastTimeStamp = timeSinceStartUp;
        for (int i = 0; i < _services.Count; i++)
        {
            _services[i].FixedUpdate();
        }

        for (int i = 0; i < _rooms.Count; i++)
        {
            _rooms[i].FixedUpdate();
        }
    }

    /// <summary>
    /// 关闭服务器
    /// </summary>
    public static void Shutdown()
    {
        isRunning = false;
        RefPooled.Free(udpSocket);
        udpSocket = null;
        _rooms.Clear();
        _services.Clear();
        _clients.Clear();
        _subscribes.Clear();
        _msgQueue.Clear();
        Log("服务器关闭");
    }

    public static T GetService<T>() where T : Service
    {
        Service service = _services.Find(x => x is T);
        if (service is null)
        {
            _services.Add(service = RefPooled.Alloc<T>());
            service.Awake();
        }

        return (T)service;
    }

    /// <summary>
    /// 分发消息
    /// </summary>
    /// <param name="key"></param>
    /// <param name="args"></param>
    public static void Dispatch(object key, params object[] args)
    {
        int hashCode = key.GetHashCode();
        if (_subscribes.TryGetValue(hashCode, out SubscribeHandle subscribe) is false)
        {
            return;
        }

        subscribe.Handle(args);
    }

    /// <summary>
    /// 订阅消息
    /// </summary>
    /// <param name="key"></param>
    /// <param name="handle"></param>
    public static void Subscribe(object key, Action<object> handle)
    {
        int hashCode = key.GetHashCode();
        if (_subscribes.TryGetValue(hashCode, out SubscribeHandle subscribe) is false)
        {
            subscribe = RefPooled.Alloc<SubscribeHandle>();
            _subscribes.Add(hashCode, subscribe);
        }

        subscribe.Subscribe(handle);
    }

    /// <summary>
    /// 取消消息订阅
    /// </summary>
    /// <param name="key"></param>
    /// <param name="handle"></param>
    public static void Unsubscribe(object key, Action<object> handle)
    {
        int hashCode = key.GetHashCode();
        if (_subscribes.TryGetValue(hashCode, out SubscribeHandle subscribe) is false)
        {
            return;
        }

        subscribe.Unsubscribe(handle);
    }

    /// <summary>
    /// 订阅消息
    /// </summary>
    /// <param name="key"></param>
    /// <param name="handle"></param>
    /// <typeparam name="T"></typeparam>
    public static void Subscribe<T>(object key, Action<T> handle) where T : class
    {
        int hashCode = key.GetHashCode();
        if (_subscribes.TryGetValue(hashCode, out SubscribeHandle subscribe) is false)
        {
            subscribe = RefPooled.Alloc<SubscribeHandle<T>>();
            _subscribes.Add(hashCode, subscribe);
        }

        (subscribe as SubscribeHandle<T>).Subscribe(handle);
    }

    /// <summary>
    /// 取消消息订阅
    /// </summary>
    /// <param name="key"></param>
    /// <param name="handle"></param>
    /// <typeparam name="T"></typeparam>
    public static void Unsubscribe<T>(object key, Action<T> handle) where T : class
    {
        int hashCode = key.GetHashCode();
        if (_subscribes.TryGetValue(hashCode, out SubscribeHandle subscribe) is false)
        {
            return;
        }

        (subscribe as SubscribeHandle<T>).Unsubscribe(handle);
    }


    /// <summary>
    /// 订阅消息
    /// </summary>
    /// <param name="key"></param>
    /// <param name="handle"></param>
    /// <typeparam name="T"></typeparam>
    public static void Subscribe<T, T2>(object key, Action<T, T2> handle) where T : class where T2 : class
    {
        int hashCode = key.GetHashCode();
        if (_subscribes.TryGetValue(hashCode, out SubscribeHandle subscribe) is false)
        {
            subscribe = RefPooled.Alloc<SubscribeHandle<T, T2>>();
            _subscribes.Add(hashCode, subscribe);
        }

        (subscribe as SubscribeHandle<T, T2>).Subscribe(handle);
    }

    /// <summary>
    /// 取消消息订阅
    /// </summary>
    /// <param name="key"></param>
    /// <param name="handle"></param>
    /// <typeparam name="T"></typeparam>
    public static void Unsubscribe<T, T2>(object key, Action<T, T2> handle) where T : class where T2 : class
    {
        int hashCode = key.GetHashCode();
        if (_subscribes.TryGetValue(hashCode, out SubscribeHandle subscribe) is false)
        {
            return;
        }

        (subscribe as SubscribeHandle<T, T2>).Unsubscribe(handle);
    }

    public static void Log(object message)
    {
        Console.WriteLine($"{DateTime.Now.ToString("yyyy-M-d HH:mm:ss:fff")} | INFO | {message}");
    }

    public static void LogWarning(object message)
    {
        Console.WriteLine($"{DateTime.Now.ToString("yyyy-M-d HH:mm:ss:fff")} | WARNING | {message}");
    }

    public static void LogError(object message)
    {
        Console.WriteLine($"{DateTime.Now.ToString("yyyy-M-d HH:mm:ss:fff")} | ERRO | {message}");
    }
}