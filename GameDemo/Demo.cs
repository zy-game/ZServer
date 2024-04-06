using DotNetty.Buffers;
using ZServer;

namespace GameDemo;

public class Demo : IServer
{
    public uint id { get; }

    public string name { get; } = "Demo";

    public Task<Status> Start()
    {
        return Task.FromResult(Status.Success);
    }

    public Task<Status> Shutdown()
    {
        return Task.FromResult(Status.Success);
    }

    public Task<IServerResult> OnMessage(Client client, IByteBuffer messaged)
    {
        App.Broadcast(messaged);
        return default;
    }

    public void Release()
    {
    }
}