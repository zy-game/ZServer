using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace ZServer;

public enum ServerState : byte
{
    Free,
    Running,
}

/// <summary>
/// 服务
/// </summary>
public interface IServer : IReference
{
    uint id { get; }

    ServerState state { get; }


    /// <summary>
    /// 启动服务
    /// </summary>
    /// <returns></returns>
    Task<Status> Start();

    /// <summary>
    /// 关闭服务
    /// </summary>
    /// <returns></returns>
    Task<Status> Shutdown();

    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="messaged"></param>
    /// <returns></returns>
    Task OnMessage(IChannelId cid, int opcode, byte[] messaged);

    /// <summary>
    /// 固定帧轮询
    /// </summary>
    void FixedUpdate();
}