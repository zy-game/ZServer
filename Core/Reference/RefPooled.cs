using System.Collections.Concurrent;
using TouchSocket.Core;

namespace ZGame;

/// <summary>
/// 引用池
/// <para>所有引用对象都应该从这里创建，以减少GC</para>
/// </summary>
public static class RefPooled
{
    class Archetype : IDisposable
    {
        private ConcurrentQueue<IReference> pool;

        public Type owner { get; }

        public Archetype(Type owner)
        {
            this.owner = owner;
            pool = new ConcurrentQueue<IReference>();
        }

        public IReference Spawner()
        {
            if (pool.TryDequeue(out IReference result))
            {
                return result;
            }
            return (IReference)Activator.CreateInstance(owner);
        }

        public void Release(IReference reference)
        {
            pool.Enqueue(reference);
            reference.Release();
            GC.SuppressFinalize(reference);
        }

        public void Dispose()
        {
            pool.Clear();
            pool = null;
        }
    }

    private static ConcurrentStack<Archetype> _archetypes = new();

    private static Archetype GetArchetype(Type type)
    {
        //todo check this type is IReference
        if (typeof(IReference).IsAssignableFrom(type) is false)
        {
            throw new NotImplementedException(nameof(IReference));
        }

        Archetype archetype = _archetypes.FirstOrDefault(x => x.owner == type);
        if (archetype == null)
        {
            archetype = new Archetype(type);
            _archetypes.Push(archetype);
        }

        return archetype;
    }

    /// <summary>
    /// 产生一个引用对象
    /// </summary>
    /// <typeparam name="T">引用对象类型</typeparam>
    /// <returns></returns>
    public static T Alloc<T>() where T : IReference
    {
        return (T)Alloc(typeof(T));
    }

    /// <summary>
    /// 产生一个引用对象
    /// </summary>
    /// <param name="type">引用对象类型</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static IReference Alloc(Type type)
    {
        //todo check this type is IReference
        if (typeof(IReference).IsAssignableFrom(type) is false)
        {
            throw new NotImplementedException(nameof(IReference));
        }

        return GetArchetype(type).Spawner();
    }

    /// <summary>
    /// 回收引用对象
    /// </summary>
    /// <param name="reference"></param>
    public static void Free(IReference reference)
    {
        if (reference is null)
        {
            return;
        }
        
        GetArchetype(reference.GetType()).Release(reference);
    }
}