using System.Collections.Concurrent;
using System.Net;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace ZServer;

public static class App
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
    /// 服务器刷新帧率
    /// </summary>
    public static int targetFrameRate { get; set; } = 30;

    /// <summary>
    /// 当前逻辑服务器
    /// </summary>
    public static IServer server { get; private set; }

    /// <summary>
    /// 帧率时间
    /// </summary>
    public static double deltaTime => targetFrameRate / 1000f;

    /// <summary>
    /// 从app启动到现在的时间
    /// </summary>
    public static double timeSinceStartUp => _timeSinceStartUp;

    private static bool isRunning = false;
    private static double _timeSinceStartUp;
    private static DateTime _startUpTimeStamp;
    private static List<Client> _clients = new();
    private static DateTime _lastUpdateTimeStamp;

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

    public static async void Startup<T>(ushort port) where T : class, IServer, new()
    {
        _startUpTimeStamp = DateTime.Now;
        server = RefPooled.Spawner<T>();
        await KCPServer.RunServerAsync(port);
        isRunning = true;
        Task.Factory.StartNew(FixedUpdate);
        Log("服务器启动");
    }

    private static async void FixedUpdate()
    {
        while (isRunning)
        {
            CheckClientHeartbeat();
            RefreshServerFrame();
            await Task.Delay(3);
        }
    }

    /// <summary>
    /// 刷新服务器帧
    /// </summary>
    private static void RefreshServerFrame()
    {
        var now = DateTime.Now;
        var _deltaTime = (now - _lastUpdateTimeStamp).TotalSeconds;
        _timeSinceStartUp = (now - _startUpTimeStamp).TotalSeconds;
        if (_deltaTime < deltaTime)
        {
            return;
        }

        _lastUpdateTimeStamp = now;
        if (server is null)
        {
            return;
        }

        server.FixedUpdate();
    }

    /// <summary>
    /// 检查客户端心跳超时
    /// </summary>
    private static void CheckClientHeartbeat()
    {
        if (_clients is null || _clients.Count == 0)
        {
            return;
        }

        for (int i = _clients.Count - 1; i >= 0; i--)
        {
            if (_clients[i].Timeout() is false)
            {
                continue;
            }

            RefPooled.Release(_clients[i]);
            _clients.Remove(_clients[i]);
        }
    }

    /// <summary>
    /// 关闭服务器
    /// </summary>
    public static void Shutdown()
    {
        isRunning = false;
        RefPooled.Release(server);
        KCPServer.Shutdown();
        Log("服务器关闭");
    }

    /// <summary>
    /// 将消息广播给所有已连接的客户端
    /// </summary>
    /// <param name="opcode"></param>
    /// <param name="bytes"></param>
    public static void Broadcast(int opcode, byte[] bytes)
    {
        byte[] s = Packet.Create(opcode, bytes);
        foreach (var client in _clients)
        {
            if (client is null)
            {
                return;
            }

            client.Write(s);
        }
    }

    /// <summary>
    /// 添加客户端链接
    /// </summary>
    /// <param name="client"></param>
    internal static void NewClient(Client client)
    {
        if (_clients.Exists(x => x.cid == client.cid))
        {
            LogError("重复添加客户端对象");
            return;
        }

        _clients.Add(client);
    }

    /// <summary>
    /// 获取客户端链接对象
    /// </summary>
    /// <param name="cid"></param>
    /// <returns></returns>
    public static Client GetClient(int cid)
    {
        return _clients.Find(x => x.cid == cid);
    }

    /// <summary>
    /// 尝试获取客户端链接
    /// </summary>
    /// <param name="cid"></param>
    /// <param name="client"></param>
    /// <returns></returns>
    public static bool TryGetClient(int cid, out Client client)
    {
        client = GetClient(cid);
        return client is not null;
    }

    internal static void RemoveClient(int cid)
    {
        _clients.RemoveAll(x => x.cid == cid);
    }

    /// <summary>
    /// 收到客户端发送的消息
    /// </summary>
    /// <param name="cid"></param>
    /// <param name="packet"></param>
    /// <param name="Sender"></param>
    /// <param name="Recipient"></param>
    /// <param name="writeable"></param>
    internal static async void HandleClientMessaged(int cid, Packet packet, EndPoint Sender, EndPoint Recipient, IWriteable writeable)
    {
        if (App.TryGetClient(cid, out var client))
        {
            App.NewClient(client = Client.Create(cid, writeable, Sender, Recipient));
        }

        if (packet.opcode == 1)
        {
            client.RefreshHeartbeat();
            return;
        }

        if (server is null)
        {
            return;
        }

        await server.OnMessage(client, packet.opcode, packet.Data);
        RefPooled.Release(packet);
    }
}