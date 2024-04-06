using System.Net.Sockets;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using fec;

namespace ZServer;

public class KCPServer
{
    private static bool isSsl = false;
    private static IChannel boundChannel;
    private static IEventLoopGroup bossGroup;
    public static bool Started { get; private set; }

    public static async Task RunServerAsync(ushort port)
    {
        bossGroup = new MultithreadEventLoopGroup(1);
        try
        {
            var bootstrap = new Bootstrap();
            bootstrap.Group(bossGroup); //.Channel<SocketDatagramChannel>();
            bootstrap.ChannelFactory(() => new SocketDatagramChannel(AddressFamily.InterNetwork));
            bootstrap
                .Option(ChannelOption.SoBacklog, 100)
                .Handler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;
                    pipeline.AddLast(new LoggingHandler("SRV-CONN"));
                    pipeline.AddLast(new IdleStateHandler(0, 0, 20));
                    pipeline.AddLast("echo", new UDPServerHandler());
                }));

            boundChannel = await bootstrap.BindAsync(port);
            Started = true;
            App.Log("KCP Server Start");
        }
        catch (Exception)
        {
            await Shutdown();
            throw;
        }
    }

    public static async Task Shutdown()
    {
        if (boundChannel != null)
        {
            await boundChannel.CloseAsync();
        }

        if (bossGroup != null)
        {
            await bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
        }

        Started = false;
    }

    class UDPServerHandler : ChannelHandlerAdapter
    {
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var packet = message as DatagramPacket;
            App.Log(string.Format("Received from client: {0}", packet.Content.ToString(Encoding.UTF8)));

            context.WriteAndFlushAsync(new DatagramPacket((IByteBuffer)packet.Content.Retain(), packet.Recipient, packet.Sender));
            if (packet != null)
                packet.Release();
        }

        //public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            App.Log(string.Format("Exception:{}", exception));
            context.CloseAsync();
        }
    }
}