using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public unsafe struct MemoryBlock<T> where T : unmanaged
{
    [ReadOnly]
    public RawArray<T> Data;

    [ReadOnly]
    public RawArray<bool> Allocated;

    readonly int* _count;

    readonly Allocator _allocator;
    readonly int _length;

    public readonly bool IsCreated => Data.IsCreated;
    public readonly bool IsFull => *_count == _length;
    public readonly bool IsEmpty => *_count == 0;

    public MemoryBlock(int length, Allocator allocator)
    {
        Data = new RawArray<T>(length, allocator);
        Allocated = new RawArray<bool>(length, allocator, false);
        _count = (int*)UnsafeUtility.Malloc(sizeof(int), UnsafeUtility.AlignOf<int>(), allocator);

        _allocator = allocator;
        _length = length;
    }

    public void Dispose()
    {
        if (!Data.IsCreated)
            return;

        Data.Dispose();
        Allocated.Dispose();
        UnsafeUtility.Free(_count, _allocator);
    }

    public T* Allocate()
    {
        if (IsFull)
            throw new Exception("MemoryBlock is full!");

        int index = FindFree();
        (*_count)++;

        Allocated[index] = true;

        return Data.Ptr(index);
    }

    public void Release(T* ptr)
    {
        if (IsEmpty)
            throw new Exception("MemoryBlock is empty!");

        int index = FindPtr(ptr);
        (*_count)--;

        Allocated[index] = false;
    }

    readonly int FindFree()
    {
        for (int i = 0; i < _length; i++)
        {
            if (!Allocated[i])
                return i;
        }

        throw new Exception("Cannot find free!");
    }

    readonly int FindPtr(T* ptr)
    {
        for (int i = 0; i < _length; i++)
        {
            if (Data.Ptr(i) == ptr)
                return i;
        }

        throw new Exception("Cannot find ptr!");
    }
}
