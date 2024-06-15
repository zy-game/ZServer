namespace ZGame.Networking;

public interface ISocket : IReference
{
    void OnStart(ushort port);
    void OnUpdate();
}