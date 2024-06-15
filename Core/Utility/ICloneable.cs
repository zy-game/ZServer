namespace ZGame;

public interface ICloneable<T> : ICloneable where T : IReference
{
    T Clone();
}