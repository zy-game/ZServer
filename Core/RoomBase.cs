using FixMath.NET;
using ZGame.Networking;

namespace ZGame.Room;

public enum RoomState
{
    /// <summary>
    /// 空闲阶段
    /// </summary>
    None,

    /// <summary>
    /// 等待就绪阶段，一般是等所有玩家场景加载完毕
    /// </summary>
    Prepare,

    /// <summary>
    /// 比赛开始
    /// </summary>
    Running,

    /// <summary>
    /// 玩家结算,结算阶段
    /// </summary>
    Balance,

    /// <summary>
    /// 比赛结束
    /// </summary>
    End
}

public class RoomBase : IReference
{
    private int _rid;
    private int _userCount;
    private string _rName;
    private RoomState _state;
    private List<Client> _clients;

    /// <summary>
    /// 房间ID
    /// </summary>
    public int rid => _rid;

    /// <summary>
    /// 房间名
    /// </summary>
    public string name => _rName;

    /// <summary>
    /// 房间状态
    /// </summary>
    public RoomState state => _state;

    /// <summary>
    /// 最大玩家数
    /// </summary>
    public int maxUserCount => _userCount;

    /// <summary>
    /// 玩家列表
    /// </summary>
    public Client[] clients => _clients.ToArray();

    /// <summary>
    /// 向房间内的所有玩家发送消息
    /// </summary>
    /// <param name="packet"></param>
    public void Broadcast(byte[] packet)
    {
        for (int i = 0; i < _clients.Count; i++)
        {
            _clients[i].Send(packet);
        }
    }

    /// <summary>
    /// 激活房间
    /// </summary>
    public void Awake()
    {
        _rid = GetHashCode();
        _rName = "Room_" + _rid;
        _clients = new List<Client>();
        DoAwake();
    }

    /// <summary>
    /// 轮询房间
    /// </summary>
    public void Update()
    {
        DoUpdate();
    }

    /// <summary>
    /// 固定帧率轮询房间
    /// </summary>
    public void FixedUpdate()
    {
        DoFixedUpdate();
    }

    /// <summary>
    /// 设置房间状态
    /// </summary>
    /// <param name="state"></param>
    public void SetState(RoomState state)
    {
        this._state = state;
    }

    /// <summary>
    /// 设置房间最大玩家数量
    /// </summary>
    /// <param name="count"></param>
    public void SetUserLimit(int count)
    {
        _userCount = count;
    }

    /// <summary>
    /// 是否存在玩家
    /// </summary>
    /// <param name="uid"></param>
    /// <returns></returns>
    public bool HasUser(uint uid)
    {
        return _clients.Exists(x => x.uid == uid);
    }

    /// <summary>
    /// 玩家进入房间
    /// </summary>
    /// <param name="client"></param>
    public void OnJoin(Client client)
    {
        //todo 强制踢出上次的链接
        if (_clients.Exists(x => x.uid == client.uid))
        {
            DoKickOffUser(client.uid);
            _clients.RemoveAll(x => x.uid == client.uid);
            AppCore.Log(($"[{_rName}] 玩家{client.uid}重连"));
        }

        _clients.Add(client);
        client.state = UserState.None;
        DoUserJoin(client);
        AppCore.Log(($"[{_rName}] 玩家{client.uid}加入"));
    }

    /// <summary>
    /// 玩家离开房间
    /// </summary>
    /// <param name="client"></param>
    public void OnLeave(Client client)
    {
        if (_clients.Exists(x => x.uid == client.uid) is false)
        {
            return;
        }

        AppCore.Log(($"[{_rName}] 玩家{client.uid}离开"));
        _clients.RemoveAll(x => x.uid == client.uid);
        DoUserLeave(client.uid);
        if (_state is RoomState.Running && clients.Length == 0)
        {
            GameOver();
        }
    }

    /// <summary>
    /// 玩家准备
    /// </summary>
    /// <param name="uid"></param>
    public void UserReady(uint uid)
    {
        AppCore.Log($"[{name}] 玩家{uid}准备");
        var client = _clients.Find(x => x.uid == uid);
        if (client is null)
        {
            return;
        }

        client.state = UserState.Ready;
        DoUserReady(uid);
        if (clients.All(x => x.state == UserState.Ready) is false)
        {
            return;
        }

        if (_clients.Count < _userCount)
        {
            return;
        }

        SetState(RoomState.Prepare);
        DoLoadGame();
    }

    /// <summary>
    /// 玩家加载游戏场景完成
    /// </summary>
    /// <param name="uid"></param>
    public void UserLoadComplete(uint uid)
    {
        var client = _clients.Find(x => x.uid == uid);
        if (client is null)
        {
            return;
        }

        client.state = UserState.Gaming;
        if (_clients.All(x => x.state == UserState.Gaming) is false)
        {
            return;
        }

        DoGameStart();
        AppCore.Log($"[{name}] 所有玩家准备完毕,游戏开始");
        SetState(RoomState.Running);
    }

    /// <summary>
    /// 游戏结束
    /// </summary>
    public void GameOver()
    {
        DoGameOver();
        SetState(RoomState.Balance);
        AppCore.Log($"[{name}] 游戏结束，开始结算阶段");
        DoBalance();
        SetState(RoomState.End);
        AppCore.Log($"[{name}] 结算结束，游戏结束");
        Release();
    }

    public virtual void Release()
    {
        _clients.Clear();
        _userCount = 0;
        _state = RoomState.None;
        AppCore.Log("重置房间数据");
    }

    /// <summary>
    /// 激活房间
    /// </summary>
    protected virtual void DoAwake()
    {
    }

    /// <summary>
    /// 轮询
    /// </summary>
    protected virtual void DoUpdate()
    {
    }

    /// <summary>
    /// 固定帧轮询
    /// </summary>
    protected virtual void DoFixedUpdate()
    {
    }

    /// <summary>
    /// 玩家被踢出房间
    /// </summary>
    /// <param name="uid"></param>
    protected virtual void DoKickOffUser(uint uid)
    {
    }

    /// <summary>
    /// 玩家进入房间
    /// </summary>
    /// <param name="client"></param>
    protected virtual void DoUserJoin(Client client)
    {
    }

    /// <summary>
    /// 玩家离开房间
    /// </summary>
    /// <param name="uid"></param>
    protected virtual void DoUserLeave(uint uid)
    {
    }

    /// <summary>
    /// 玩家准备
    /// </summary>
    /// <param name="uid"></param>
    protected virtual void DoUserReady(uint uid)
    {
    }

    /// <summary>
    /// 进入加载游戏场景数据阶段
    /// </summary>
    protected virtual void DoLoadGame()
    {
    }

    /// <summary>
    /// 游戏开始
    /// </summary>
    protected virtual void DoGameStart()
    {
    }

    /// <summary>
    /// 游戏结束
    /// </summary>
    protected virtual void DoGameOver()
    {
    }

    /// <summary>
    /// 游戏结算
    /// </summary>
    protected virtual void DoBalance()
    {
    }
}