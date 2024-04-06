namespace ZServer;

public interface IReference : IDisposable
{
    void IDisposable.Dispose()
    {
        RefPooled.Release(this);
    }

    void Release();
}