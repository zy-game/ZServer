using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace ZServer;

public class Client : IReference
{
    public int id;
    private IChannel _channel;


    public Client(IChannel channel)
    {
        id = channel.Id.GetHashCode();
        _channel = channel;
    }

    public async void Send(byte[] messaged)
    {
        await _channel.WriteAndFlushAsync(Unpooled.WrappedBuffer(messaged));
    }

    public void Release()
    {
        _channel?.DisconnectAsync();
        _channel?.CloseAsync();
        _channel = null;
    }
}