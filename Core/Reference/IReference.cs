namespace ZGame;

public interface IReference : IDisposable
{
    void IDisposable.Dispose()
    {
        RefPooled.Free(this);
    }

    void Release();
}