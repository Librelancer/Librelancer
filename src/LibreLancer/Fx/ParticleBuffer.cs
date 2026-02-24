using System;
using System.Numerics;

namespace LibreLancer.Fx;

public class ParticleBuffer
{
    private Particle[] backing;
    private SegmentInfo[] segments;

    struct SegmentInfo
    {
        public int Start;
        public int Capacity;
        public int Count;
        public int Head;
        public int Tail;
    }


    public ParticleBuffer(int[] counts)
    {
        int total = 0;
        segments = new SegmentInfo[counts.Length];
        for (int i = 0; i < counts.Length; i++)
        {
            segments[i] = new SegmentInfo() {
                Start = total,
                Capacity = counts[i],
                Count = 0,
                Head =  counts[i] - 1,
                Tail = 0,
            };
            total += counts[i];
        }
        backing = new Particle[total];
    }

    public void Reset()
    {
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i] = new SegmentInfo()
            {
                Start = segments[i].Start,
                Capacity = segments[i].Capacity,
                Count = 0,
                Head = segments[i].Capacity - 1,
                Tail = 0,
            };
        }
        Array.Clear(backing);
    }

    public int GetCount(int index) => segments[index].Count;

    public void RemoveAt(int segment, int index)
    {
        if (index == 0) {
            Dequeue(segment);
            return;
        }
        ref var b = ref segments[segment];
        if (index < 0 || index >= b.Count)
            throw new ArgumentOutOfRangeException("index");
        for (int i = index; i < b.Count - 1; i++)
            this[segment, i] = this[segment, i + 1];
        this[segment, b.Count - 1] = default;
        b.Head = b.Head - 1 < 0 ? (b.Capacity - 1) : b.Head - 1;
        b.Count--;
    }

    public void Dequeue(int segment)
    {
        ref var b = ref segments[segment];
        if (b.Capacity == 0) throw new InvalidOperationException("Empty particle buffer segment");
        if (b.Count == 0) throw new InvalidOperationException("Collection empty");
        backing[b.Start + b.Tail] = default;
        b.Tail = (b.Tail + 1) % b.Capacity;
        b.Count--;
    }

    public ref Particle Enqueue(int segment, out int despawned)
    {
        despawned = -1;
        ref var b = ref segments[segment];
        if (b.Capacity == 0) throw new InvalidOperationException("Empty particle buffer segment");
        b.Head = (b.Head + 1) % b.Capacity;
        if (backing[b.Start + b.Head].Orientation !=
            Quaternion.Zero)
            despawned = backing[b.Start + b.Head].EmitterIndex;
        backing[b.Start + b.Head] = default(Particle);
        if (b.Count == b.Capacity)
            b.Tail = (b.Tail + 1) % b.Capacity;
        else
            b.Count++;
        return ref backing[b.Start + b.Head];
    }


    public ref Particle this[int segment, int index]
    {
        get
        {
            ref var b = ref segments[segment];
            if (index < 0 || index >= b.Count)
                throw new ArgumentOutOfRangeException("index");
            return ref backing[b.Start + ((b.Tail + index) % b.Capacity)];
        }
    }
}
