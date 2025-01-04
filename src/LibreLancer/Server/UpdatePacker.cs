using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LibreLancer.Net.Protocol;
using LibreLancer.Server.Components;
using LibreLancer.World;

namespace LibreLancer.Server;

public class UpdatePacker
{
    public byte[] WorldUpdateBuffer = new byte[2048];
    public byte[] PacketUpdatesBuffer = new byte[2048];

    public UpdatePackerInstance Begin(ObjectUpdate[] allUpdates, GameObject[] allObjects)
    {
        return new UpdatePackerInstance(this, allUpdates, allObjects);
    }

    record struct SortedUpdate(FetchedDelta Old, int Size, int Offset, GameObject Object, ObjectUpdate Update) : IComparable<SortedUpdate>
    {
        public int CompareTo(SortedUpdate other)
        {
            var x = ((ulong)other.Old.Priority) << 32 | (uint)other.Size;
            var y = ((ulong)Old.Priority) << 32 | (uint)Size;
            return x.CompareTo(y);
        }
    }

    class IdComparer : IComparer<SortedUpdate>
    {
        public static readonly IdComparer Instance = new IdComparer();
        private IdComparer() { }
        public int Compare(SortedUpdate x, SortedUpdate y) =>
            x.Update.ID.Value.CompareTo(y.Update.ID.Value);
    }


    public class UpdatePackerInstance
    {
        private UpdatePacker pk;
        private ObjectUpdate[] allUpdates;
        private GameObject[] allObjects;
        private FetchedDelta[] deltas;
        private SortedUpdate[] sorted;
        internal UpdatePackerInstance(UpdatePacker pk, ObjectUpdate[] allUpdates, GameObject[] allObjects)
        {
            this.pk = pk;
            this.allUpdates = allUpdates;
            this.allObjects = allObjects;
            deltas = new FetchedDelta[allObjects.Length];
            sorted = new SortedUpdate[allObjects.Length];
        }

        public PackedUpdatePacket Pack(uint tick, PlayerAuthState authState, SPlayerComponent self, GameObject selfObj,
            int maxPacketSize)
        {
            // Set up packet
            var packet = new PackedUpdatePacket();
            packet.Tick = tick;
            self.GetAcknowledgedState(out var oldTick, out var oldState);
            self.GetUpdates(allObjects, deltas);
            packet.Tick = tick;
            packet.OldTick = oldTick;
            packet.InputSequence = self.LatestReceived;
            packet.SetAuthState(authState, oldState, tick);

            // Locate self in array and skip
            int skipIndex = -1;
            for (int i = 0; i < allObjects.Length; i++)
            {
                if (allObjects[i] == selfObj)
                {
                    skipIndex = i;
                    break;
                }
            }

            //Calculate size + encode deltas
            var deltaWriter = new BitWriter(pk.WorldUpdateBuffer, true);
            int totalSum = 0;
            int totalSorted = 0;
            for (int i = 0; i < allUpdates.Length; i++)
            {
                if (skipIndex == i)
                    continue;
                int start = deltaWriter.ByteLength;
                allUpdates[i].WriteDelta(deltas[i].Update, deltas[i].Tick, packet.Tick, ref deltaWriter);
                deltaWriter.Align();
                var len = (deltaWriter.ByteLength - start);
                totalSum += len + 4; //over-estimate overhead of sending ID
                sorted[totalSorted++] = new SortedUpdate(deltas[i], len, start, allObjects[i], allUpdates[i]);
            }

            // In case it was resized
            pk.WorldUpdateBuffer = deltaWriter.Backing;
            // Check we are below size
            if (maxPacketSize >= (packet.DataSize + totalSum))
            {
                //Skip sorting, just shove the packet in
                var d = new Dictionary<int, ObjectUpdate>();
                for (int i = 0; i < allUpdates.Length; i++)
                {
                    if (skipIndex == i)
                        continue;
                    d[allObjects[i].Unique] = allUpdates[i];
                }

                self.EnqueueState((uint)tick, authState, d);
                var allWriter = new BitWriter(pk.PacketUpdatesBuffer, false);
                // Write IDs
                allWriter.PutVarUInt32((uint)totalSorted);
                if (totalSorted > 0)
                {
                    allWriter.PutVarInt32(sorted[0].Update.ID.Value);
                }

                for (int i = 1; i < totalSorted; i++)
                {
                    int curr = sorted[i].Update.ID.Value;
                    int prev = sorted[i - 1].Update.ID.Value;
                    allWriter.PutVarInt32(curr - prev);
                }

                // Write Updates
                int offset = allWriter.ByteLength;
                for (int i = 0; i < totalSorted; i++)
                {
                    var dest = pk.PacketUpdatesBuffer.AsSpan(offset, sorted[i].Size);
                    var src = pk.WorldUpdateBuffer.AsSpan(sorted[i].Offset, sorted[i].Size);
                    src.CopyTo(dest);
                    offset += sorted[i].Size;
                }

                packet.Updates = new byte[offset];
                pk.PacketUpdatesBuffer.AsSpan(0, offset).CopyTo(packet.Updates);
            }
            // We are above size
            else
            {
                int dataSum = 0;
                Array.Sort(sorted, 0, totalSorted);
                var d = new Dictionary<int, ObjectUpdate>();
                // First pass counting packet size
                int newSum = packet.DataSize;
                int uIdx = 1;
                newSum += NetPacking.ByteCountInt64((int)sorted[0].Update.ID.Value);
                newSum += sorted[0].Size;
                for (; uIdx < totalSorted && uIdx <= 127; uIdx++)
                {
                    int curr = sorted[uIdx].Update.ID.Value;
                    int prev = sorted[uIdx - 1].Update.ID.Value;
                    if (newSum +
                        NetPacking.ByteCountInt64(curr - prev)
                        + sorted[uIdx].Size > maxPacketSize)
                    {
                        break;
                    }

                    newSum += NetPacking.ByteCountInt64(curr - prev) + sorted[uIdx].Size;
                }

                // Sort available updates by ID to get a few bytes back
                Array.Sort(sorted, 0, uIdx, IdComparer.Instance);
                // First update will always be fine
                // Start writing ID list
                var allWriter = new BitWriter(pk.PacketUpdatesBuffer, false);
                allWriter.PutByte(0);
                allWriter.PutVarInt32((int)sorted[0].Update.ID.Value);
                uIdx = 1;
                dataSum += sorted[0].Size;
                d[sorted[0].Object.Unique] = sorted[0].Update;
                // Max 127 updates as we reserve one byte for count
                for (int j = 1; uIdx < totalSorted && uIdx <= 127; uIdx++)
                {
                    // If adding another update would make the packet oversized
                    // we stop
                    int curr = sorted[uIdx].Update.ID.Value;
                    int prev = sorted[uIdx - 1].Update.ID.Value;
                    if ((packet.DataSize + //authstate + headers
                         allWriter.ByteLength + //current ID list
                         dataSum + //size of existing updates
                         sorted[uIdx].Size +
                         NetPacking.ByteCountInt64(curr - prev)) //size of ID list add
                        >= maxPacketSize)
                    {
                        break;
                    }

                    d[sorted[uIdx].Object.Unique] = sorted[uIdx].Update;
                    allWriter.PutVarInt32(curr - prev);
                    dataSum += sorted[uIdx].Size;
                }

                pk.PacketUpdatesBuffer[0] = (byte)uIdx; //Set count
                //copy updates
                int offset = allWriter.ByteLength;
                for (int i = 0; i < uIdx; i++)
                {
                    var dest = pk.PacketUpdatesBuffer.AsSpan(offset, sorted[i].Size);
                    var src = pk.WorldUpdateBuffer.AsSpan(sorted[i].Offset, sorted[i].Size);
                    src.CopyTo(dest);
                    offset += sorted[i].Size;
                }

                packet.Updates = new byte[offset];
                pk.PacketUpdatesBuffer.AsSpan(0, offset).CopyTo(packet.Updates);
                // Increase priority for any non-updated objects
                for (int i = uIdx; i < totalSorted; i++)
                {
                    self.SetPriority(sorted[i].Object, sorted[i].Old.Priority + 1);
                }
                self.EnqueueState((uint)tick, authState, d);
            }
            return packet;
        }
    }

}
