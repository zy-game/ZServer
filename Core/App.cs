using System.Collections.Concurrent;
using DotNetty.Buffers;

namespace ZServer;

public static class App
{
    public static string name { get; set; }
    public static string version { get; set; }
    public static IServer server { get; private set; }
    private static List<Client> clients = new();

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
        server = RefPooled.Spawner<T>();
        server.Start();
        await KCPServer.RunServerAsync(port);
    }

    public static void Shutdown()
    {
        server.Shutdown();
        KCPServer.Shutdown();
        RefPooled.Release(server);
        server = null;
        Log("服务器关闭");
    }


    public static void NewClient(Client client)
    {
        clients.Add(client);
    }

    public static void RemoveClient(int id)
    {
        var client = GetClient(id);
        if (client == null)
        {
            return;
        }

        clients.Remove(client);
        RefPooled.Release(client);
    }

    public static Client GetClient(int id)
    {
        return clients.FirstOrDefault(x => x.id == id);
    }

    public static void Broadcast(IByteBuffer messaged)
    {
        foreach (var client in clients)
        {
            client.Send(messaged.Array);
        }
    }

    public static async void OnReceiveMessage(int id, IByteBuffer messaged)
    {
        var client = GetClient(id);
        if (client == null)
        {
            return;
        }

       IServerResult result = await server.OnMessage(client, messaged);
       
    }
}