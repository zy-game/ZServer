using ZGame.Networking;
using ZGame.Room;

namespace ZGame;

public enum UserState : byte
{
    None,
    Ready,
    Prepare,
    Gaming,
}

public sealed class Client : IReference
{
    private INetClient _net;

    public int cid
    {
        get { return _net == null ? 0 : _net.cid; }
    }

    public bool isOnline => _net.isConnected;
    public uint uid { get; set; }
    public UserState state { get; set; }
    public RoomBase room { get; private set; }

    public void Send(byte[] bytes)
    {
        if (_net.isConnected is false)
        {
            return;
        }

        _net.Write(bytes);
    }


    public void JoinRoom(RoomBase room)
    {
        if (this.room is not null)
        {
            AppCore.Log(("玩家已经在房间中"));
            return;
        }

        this.room = room;
        room.OnJoin(this);
    }

    public void LeaveRoom()
    {
        if (room is null)
        {
            return;
        }


        room.OnLeave(this);
        room = null;
    }


    public void Release()
    {
        _net = null;
        room = null;
        uid = 0;
        state = UserState.None;
    }

    public static Client Create(INetClient net)
    {
        Client client = RefPooled.Alloc<Client>();
        client._net = net;
        return client;
    }
}