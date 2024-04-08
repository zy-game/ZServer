using System.Collections.Concurrent;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace ZServer;

public sealed class ID
{
    private static uint _current = 0;
    private static readonly object _lock = new object();

    public static uint Generic()
    {
        lock (_lock)
        {
            return ++_current;
        }
    }
}

public static class App
{
    private static bool isRunning = false;
    public static string name { get; set; }
    public static string version { get; set; }
    public static int fixedUpdateRate { get; set; } = 30;

    private static ConcurrentDictionary<IServer, List<IChannelId>> servers = new();
    private static Type type;

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
        type = typeof(T);
        await KCPServer.RunServerAsync(port);
        isRunning = true;
        Task.Run(FixedUpdate);
        Log("服务器启动");
    }

    private static void FixedUpdate()
    {
        while (isRunning)
        {
            if (servers is null || servers.Count == 0)
            {
                continue;
            }

            foreach (var server in servers.Keys)
            {
                server.FixedUpdate();
            }

            Task.Delay(fixedUpdateRate);
        }
    }

    public static void Shutdown()
    {
        isRunning = false;
        foreach (var server in servers)
        {
            RefPooled.Release(server.Key);
        }

        servers.Clear();
        KCPServer.Shutdown();
        Log("服务器关闭");
    }


    public static IServer GetServer(IChannelId id)
    {
        foreach (var server in servers)
        {
            if (server.Value.Contains(id))
            {
                return server.Key;
            }
        }

        return default;
    }

    public static async Task<IServer> GetFreeServer(IChannelId id)
    {
        if (servers.Count > 0)
        {
            foreach (var VARIABLE in servers.Keys)
            {
                if (VARIABLE.state == ServerState.Free)
                {
                    return VARIABLE;
                }
            }
        }


        IServer server = (IServer)RefPooled.Spawner(type);
        servers.AddOrUpdate(server, new List<IChannelId>() { id }, (k, v) =>
        {
            if (v.Contains(id))
            {
                v.Remove(id);
            }

            v.Add(id);
            return v;
        });
        await server.Start();
        return server;
    }
}