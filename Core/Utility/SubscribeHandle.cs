using ZGame.Networking;

namespace ZGame;

class SubscribeHandle : IReference
{
    private event Action<object> _handle;

    public virtual void Handle(params object[] data)
    {
        _handle?.Invoke(data);
    }

    public void Subscribe(Action<object> handle)
    {
        _handle += handle;
    }

    public void Unsubscribe(Action<object> handle)
    {
        _handle -= handle;
    }

    public virtual void Release()
    {
        _handle = null;
    }
}

class SubscribeHandle<T> : SubscribeHandle where T : class
{
    private event Action<T> _handle;

    public override void Handle(params object[] data)
    {
        _handle?.Invoke((T)data[0]);
    }

    public void Subscribe(Action<T> handle)
    {
        _handle += handle;
    }

    public void Unsubscribe(Action<T> handle)
    {
        _handle -= handle;
    }

    public override void Release()
    {
        base.Release();
        _handle = null;
    }
}

class SubscribeHandle<T, T2> : SubscribeHandle where T : class where T2 : class
{
    private event Action<T, T2> _handle;

    public override void Handle(params object[] data)
    {
        _handle?.Invoke((T)data[0], (T2)data[1]);
    }

    public void Subscribe(Action<T, T2> handle)
    {
        _handle += handle;
    }

    public void Unsubscribe(Action<T, T2> handle)
    {
        _handle -= handle;
    }

    public override void Release()
    {
        base.Release();
        _handle = null;
    }
}