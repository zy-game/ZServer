using DotNetty.Buffers;

namespace ZServer;

/// <summary>
/// 服务
/// </summary>
public interface IServer : IReference
{
    uint id { get; }

    string name { get; }

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
    Task<IServerResult> OnMessage(Client client, IByteBuffer messaged);
}