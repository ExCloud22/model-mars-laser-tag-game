using System.Collections.Generic;

namespace LaserTagBox.Model.Mind;

public class FixedSizedQueue<T>
{
    public readonly Queue<T> Queue = new Queue<T>();

    private int MaxSize { get; set; }

    public FixedSizedQueue(int maxSize)
    {
        MaxSize = maxSize;
    }

    public void Enqueue(T obj)
    {
        Queue.Enqueue(obj);

        if (Queue.Count > MaxSize)
        {
            Queue.Dequeue();
        }
    }

    public T Dequeue()
    {
        return Queue.Dequeue();
    }

    public int Count()
    {
        return Queue.Count;
    }
}