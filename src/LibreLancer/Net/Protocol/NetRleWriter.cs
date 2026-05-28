using System;

namespace LibreLancer.Net.Protocol;

/// <summary>
/// Quick packing for deltas, encodes long runs of 0 in few bits
/// 1 Zero = 4-bits (2x saving), up to 23 Zeros in 8-bits (23x saving)
/// 0% overhead for bytes 1-64
/// Bytes >64 carry 12.5% to 50% overhead
/// </summary>
public unsafe class NetRleWriter
{
    struct StreamState
    {
        public int Nibble;
        public byte BufferByte;

        public int ZeroCount;
        public int LiteralCount;

        public fixed byte Literals[4];
    }

    private StreamState? _checkPoint;
    private StreamState state;

    public int Length => ((PendingCount() + state.Nibble + 1) >> 1);

    private byte[] buffer;

    public NetRleWriter(byte[] buffer)
    {
        this.buffer = buffer;
    }

    public NetRleWriter()
    {
        buffer = new byte[512];
    }

    public byte[] Buffer => buffer;

    void Write4Bit(byte value)
    {
        var byteIndex = state.Nibble >> 1;
        if (byteIndex >= buffer.Length)
        {
            Array.Resize(ref buffer, (int)(buffer.Length * 1.5));
        }

        if ((state.Nibble & 1) == 0)
        {
            // high 4-bit
            buffer[byteIndex] = (byte)((buffer[byteIndex] & 0x0F) | (value << 4));
        }
        else
        {
            // low 4-bit
            buffer[byteIndex] = (byte)((buffer[byteIndex] & 0xF0) | value);
        }

        state.Nibble++;
        state.BufferByte = buffer[state.Nibble >> 1];
    }

    int PendingCount()
    {
        if (state.ZeroCount > 0)
        {
            int nCount = 0;
            int bc = state.ZeroCount;
            while (bc > 0)
            {
                if (bc > 8 + 15)
                {
                    nCount += 2; //0111 1111
                    bc -= 8 + 15;
                }
                else if (bc > 7)
                {
                    nCount += 2; //0111 XXXX
                    break;
                }
                else
                {
                    nCount += 1; //0111
                    break;
                }
            }

            return nCount;
        }

        if (state.LiteralCount > 0)
        {
            return 1 + (state.LiteralCount * 2);
        }

        return 0;
    }

    void FlushZeros()
    {
        if (state.ZeroCount <= 0)
            return;
        while (state.ZeroCount > 0)
        {
            if (state.ZeroCount > 7)
            {
                int disp = state.ZeroCount > 23 ? 23 : state.ZeroCount;
                Stat($"W: Zero2 {disp}");
                //val 8 (8 - 1)
                Write4Bit(7); //0111
                state.ZeroCount -= 8;
                //4 bit extra count
                var x = state.ZeroCount > 15 ? 15 : state.ZeroCount;
                Write4Bit((byte)x);
                state.ZeroCount -= x;
            }
            else
            {
                Stat($"W: Zero1 ({state.ZeroCount})");
                Write4Bit((byte)(state.ZeroCount - 1));
                break;
            }
        }
        state.ZeroCount = 0;
    }

    void FlushLiterals()
    {
        if (state.LiteralCount <= 0)
            return;
        Stat($"W: LC ({state.LiteralCount})");
        Write4Bit((byte)(0xC | (state.LiteralCount - 1)));
        for (int i = 0; i < state.LiteralCount; i++)
        {
            Stat($"W: L ({state.Literals[i]})");
            Write4Bit((byte)(state.Literals[i] >> 4 & 0xF));
            Write4Bit((byte)(state.Literals[i] & 0xF));
        }
        state.LiteralCount = 0;
    }

    public void Write(byte b)
    {
        if (b == 0)
        {
            FlushLiterals();
            state.ZeroCount++;
        }
        else if (state.LiteralCount > 0 || b > 64)
        {
            FlushZeros();
            state.Literals[state.LiteralCount++] = b;
            if (state.LiteralCount == 4)
            {
                FlushLiterals();
            }
        }
        else //small bit
        {
            FlushZeros();
            Stat($"W: B ({b})");
            var x = b - 1;
            //11XX
            Write4Bit((byte)(0x8 | ((x >> 4) & 0x3)));
            //XXXX
            Write4Bit((byte)(x & 0xF));
        }

    }

    public void Write0(uint u)
    {
        Write((byte)((u >> 24) & 0xFF));
    }
    public void Write1(uint u)
    {
        Write((byte)((u >> 16) & 0xFF));
    }
    public void Write2(uint u)
    {
        Write((byte)((u >> 8) & 0xFF));
    }
    public void Write3(uint u)
    {
        Write((byte)(u & 0xFF));
    }

    void Stat(string str)
    {
       //Console.WriteLine(str);
    }

    public void CopyTo(Span<byte> destination)
    {
        FlushZeros();
        FlushLiterals();
        this.buffer.AsSpan(0, Length).CopyTo(destination);
    }

    public byte[] GetCopy()
    {
        var b = new byte[Length];
        CopyTo(b);
        return b;
    }

    public void Checkpoint()
    {
        if (_checkPoint != null)
        {
            throw new InvalidOperationException("Only one checkpoint allowed");
        }

        _checkPoint = state;
    }

    public void RemoveCheckpoint()
    {
        if (_checkPoint == null)
        {
            throw new InvalidOperationException("No checkpoint set");
        }

        _checkPoint = null;
    }

    public void Rewind()
    {
        if (_checkPoint == null)
        {
            throw new InvalidOperationException("No checkpoint set");
        }

        state = _checkPoint.Value;
        buffer[state.Nibble >> 1] = state.BufferByte;
        _checkPoint = null;
    }
}
