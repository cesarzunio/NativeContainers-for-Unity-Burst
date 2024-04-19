using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

[BurstCompile]
public unsafe struct RawList<T> where T : unmanaged
{
    const int BYTES_TO_ALLOCATE_MIN = 64;

    [NativeDisableUnsafePtrRestriction]
    Data* _data;

    readonly Allocator _allocator;

    public readonly int Count => _data->Count;

    public readonly int Capacity => _data->Capacity;

    public readonly bool IsCreated => _data != null;

    public RawList(Allocator allocator, int capacity = 8)
    {
        if (capacity < 1)
        {
            _data = null;
            _allocator = allocator;
            return;
        }

        _data = (Data*)UnsafeUtility.Malloc(sizeof(Data), UnsafeUtility.AlignOf<Data>(), allocator);
        *_data = new Data();

        _allocator = allocator;

        RequestCapacity(capacity);
    }

    public T this[int index]
    {
        get => _data->Ptr[index];
        set => _data->Ptr[index] = value;
    }

    public ref T Ref(int index) => ref _data->Ptr[index];

    public readonly T* Ptr(int index) => _data->Ptr + index;

    public void Add(T item)
    {
        if (Count == Capacity)
            RequestCapacity(Capacity * 2);

        _data->Ptr[_data->Count++] = item;
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= Count)
            return;

        int itemsToMoveCount = Count - index - 1;
        _data->Count--;

        if (itemsToMoveCount <= 0)
            return;

        long bytesToMove = itemsToMoveCount * sizeof(T);
        var dest = _data->Ptr + index;
        var source = dest + 1;

        UnsafeUtility.MemMove(dest, source, bytesToMove);
    }

    public void Dispose()
    {
        if (_data == null)
            return;

        _data->Dispose(_allocator);

        UnsafeUtility.Free(_data, _allocator);
        _data = null;
    }

    void RequestCapacity(int capacity)
    {
        var sizeOf = sizeof(T);
        var capacityNew = math.max(capacity, BYTES_TO_ALLOCATE_MIN / sizeOf);
        capacityNew = math.ceilpow2(capacityNew);

        SetCapacity(capacityNew);
    }

    void SetCapacity(int capacity)
    {
        if (capacity <= _data->Capacity)
            return;

        _data->Capacity = capacity;

        if (capacity == 0)
        {
            Dispose();
            return;
        }

        long sizeOf = sizeof(T);
        var ptr = (T*)UnsafeUtility.Malloc(capacity * sizeOf, UnsafeUtility.AlignOf<T>(), _allocator);

        if (_data->Ptr != null)
        {
            if (_data->Count > 0)
            {
                long bytesToCopy = math.min(_data->Count, capacity) * sizeOf;
                UnsafeUtility.MemCpy(ptr, _data->Ptr, bytesToCopy);
            }

            UnsafeUtility.Free(_data->Ptr, _allocator);
        }

        _data->Ptr = ptr;
    }

    // -----

    [BurstCompile] // not sure if this is necessary here
    struct Data
    {
        [NativeDisableUnsafePtrRestriction]
        public T* Ptr;

        public int Count;
        public int Capacity;

        public void Dispose(Allocator allocator)
        {
            if (Ptr == null)
                return;

            UnsafeUtility.Free(Ptr, allocator);
            Ptr = null;
        }
    }
}
