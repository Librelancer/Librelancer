using System;
using System.Collections.Generic;
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

    private record struct SortedUpdate(FetchedDelta Old, GameObject Object, ObjectUpdate Update)
        : IComparable<SortedUpdate>
    {
        public int CompareTo(SortedUpdate other)
        {
            return other.Old.Priority.CompareTo(Old.Priority);
        }
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
            var packet = new PackedUpdatePacket
            {
                Tick = tick
            };
            self.GetAcknowledgedState(out var oldTick, out var oldState);
            self.GetUpdates(allObjects, deltas);
            packet.Tick = tick;
            packet.OldTick = oldTick;
            packet.InputSequence = self.LatestReceived;
            packet.SetAuthState(authState, oldState, tick);

            maxPacketSize -= packet.DataSize;

            Dictionary<int, ObjectUpdate> written = new();

            // Locate self in array and skip
            int totalSorted = 0;
            for (int i = 0; i < allObjects.Length; i++)
            {
                if (allObjects[i] != selfObj)
                {
                    sorted[totalSorted++] = new SortedUpdate(deltas[i], allObjects[i], allUpdates[i]);
                }
            }

            if (totalSorted > 0)
            {
                Array.Sort(sorted, 0, totalSorted);

                var idWriter = new BitWriter(pk.PacketUpdatesBuffer, false);
                idWriter.PutByte(0);

                int idByteSum = 0;
                NetRleWriter rle = new(pk.WorldUpdateBuffer);
                int idx;
                int lastId = 0;
                int updateCount = 0;

                idWriter.PutVarInt32(sorted[0].Update.ID.Value);
                for (idx = 0; idx < totalSorted && updateCount < 255; idx++)
                {
                    int idBytes = NetPacking.ByteCountInt64(sorted[idx].Update.ID.Value - lastId);
                    if (rle.Length + idByteSum + idBytes + 8 >= maxPacketSize)
                    {
                        break; // we can't possibly fit anything
                    }

                    rle.Checkpoint();
                    sorted[idx].Update.WriteDelta(sorted[idx].Old.Update, sorted[idx].Old.Tick, tick, rle);
                    if (rle.Length + idByteSum + idBytes >= maxPacketSize)
                    {
                        // didn't fit (maybe a smaller one fits)
                        // Bump priorities for non-written objects
                        self.SetPriority(sorted[idx].Object, sorted[idx].Old.Priority + 1);
                        rle.Rewind();
                    }
                    else
                    {
                        idByteSum += idBytes;
                        if (idx > 0)
                        {
                            idWriter.PutVarInt32(sorted[idx].Update.ID.Value - lastId);
                        }
                        written[sorted[idx].Object.Unique] = sorted[idx].Update;
                        rle.RemoveCheckpoint();
                        lastId = sorted[idx].Update.ID.Value;
                        updateCount++;
                    }
                }
                // Bump priorities for non-written objects
                for (int i = idx; i < totalSorted; i++)
                {
                    self.SetPriority(sorted[i].Object, sorted[i].Old.Priority + 1);
                }

                pk.WorldUpdateBuffer = rle.Buffer;
                // Copy to packet
                int totalSize = idWriter.ByteLength + rle.Length;
                packet.Updates = new byte[totalSize];
                pk.PacketUpdatesBuffer.AsSpan(0, idWriter.ByteLength).CopyTo(packet.Updates);
                packet.Updates[0] = (byte)updateCount;
                rle.CopyTo(packet.Updates.AsSpan(idWriter.ByteLength));
            }
            else
            {
                packet.Updates = [0];
            }

            self.EnqueueState((uint) tick, authState, written);

            return packet;
        }
    }
}
