using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace ZServer;

public interface IWriteable
{
    void WriteMessage(byte[] bytes);
}

public class Client : IReference
{
    public IChannelId id;
    private IWriteable _channel;


    public static Client Create(IChannelId id, IWriteable adapter)
    {
        Client client = RefPooled.Spawner<Client>();
        client.id = id;
        client._channel = adapter;
        return client;
    }

    public async void Send(byte[] messaged)
    {
        _channel.WriteMessage(messaged);
    }

    public void Release()
    {
        _channel = null;
    }
}