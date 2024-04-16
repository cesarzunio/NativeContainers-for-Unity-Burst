using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

[BurstCompile]
public unsafe struct RawArray<T> where T : unmanaged
{
    const int BYTES_TO_ALLOCATE_MIN = 64;

    [NativeDisableUnsafePtrRestriction]
    T* _ptr;

    readonly int _length;
    readonly Allocator _allocator;

    public readonly int Length => _length;

    public readonly bool IsCreated => _ptr != null;

    public RawArray(int length, Allocator allocator)
    {
        if (length < 1)
        {
            _ptr = null;
            _length = 0;
            _allocator = allocator;
            return;
        }

        _ptr = (T*)UnsafeUtility.Malloc(length * sizeof(T), UnsafeUtility.AlignOf<T>(), allocator);
        _length = length;
        _allocator = allocator;
    }

    public RawArray(int length, Allocator allocator, T valueDefault) : this(length, allocator)
    {
        for (int i = 0; i < length; i++)
        {
            _ptr[i] = valueDefault;
        }
    }

    public T this[int index]
    {
        get => _ptr[index];
        set => _ptr[index] = value;
    }

    public void Dispose()
    {
        if (_ptr == null)
            return;

        UnsafeUtility.Free(_ptr, _allocator);
        _ptr = null;
    }
}