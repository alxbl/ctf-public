namespace Mycoverse.Common.Data;

using System.Collections.Concurrent;
using System.Threading;

public class ResourcePool<T> where T : new()
{
    private readonly ConcurrentQueue<T> _pool = new ConcurrentQueue<T>();
    private readonly SemaphoreSlim _available;

    public ResourcePool(int capacity)
    {
        _available = new SemaphoreSlim(capacity, capacity);
        
        for (var i = 0; i < capacity; ++i)
            _pool.Enqueue(new T());
    }

    public class ResourceHandle : IDisposable
    {
        private readonly ResourcePool<T> _pool;
        
        public ResourceHandle(T resource, ResourcePool<T> pool)
        {
            Resource = resource;
            _pool = pool;
        }
        public T Resource { get; }

        public void Dispose()
        {
            _pool.Return(Resource);
        }
    }

    public async Task<ResourceHandle> GetAync(CancellationToken k)
    {
        await _available.WaitAsync(k);
        while (!k.IsCancellationRequested)
            if (_pool.TryDequeue(out var res)) return new ResourceHandle(res, this);
        k.ThrowIfCancellationRequested();
        throw new InvalidOperationException("no space left in pool."); // Should never happen because of the line aboe.
    }

    protected void Return(T resource)
    {
        _pool.Enqueue(resource);
        _available.Release();
    }
}