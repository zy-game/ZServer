using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace ZServer;

public class TCPServer
{
    private static IChannel boundChannel;
    private static IEventLoopGroup bossGroup;
    private static IEventLoopGroup workerGroup;

    public static async Task RunServerAsync(ushort port)
    {
        bossGroup = new MultithreadEventLoopGroup(1);
        workerGroup = new MultithreadEventLoopGroup();
        try
        {
            var bootstrap = new ServerBootstrap();
            bootstrap.Group(bossGroup, workerGroup);
            bootstrap.Channel<TcpServerSocketChannel>().Option(ChannelOption.SoBacklog, 100).Handler(new LoggingHandler("SRV-LSTN")).ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
            {
                IChannelPipeline pipeline = channel.Pipeline;
                pipeline.AddLast(new LoggingHandler("SRV-CONN"));
                pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));
                pipeline.AddLast("echo", new EchoServerHandler());
            }));
            boundChannel = await bootstrap.BindAsync(port);
        }
        catch (Exception e)
        {
            Shutdown();
            App.LogError(e);
        }
    }

    public static async Task Shutdown()
    {
        await boundChannel.CloseAsync();
        await Task.WhenAll(
            bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
            workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
    }

    public class EchoServerHandler : ChannelHandlerAdapter
    {
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var buffer = message as IByteBuffer;
            if (buffer != null)
            {
                Console.WriteLine("Received from client: " + buffer.ToString(Encoding.UTF8));
            }

            context.WriteAsync(message);
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Console.WriteLine("Exception: " + exception);
            context.CloseAsync();
        }
    }
}