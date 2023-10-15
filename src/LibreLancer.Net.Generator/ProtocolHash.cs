using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace LibreLancer.Net.Generator;

public class ProtocolHash
{
    public static string Hash(IEnumerable<string> source)
    {
        IncrementalHash sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach(var m in source)
            sha256.AppendData(Encoding.UTF8.GetBytes(m));
        byte[] hash = sha256.GetHashAndReset();
        return Z85Encode(hash);
    }

    private static string Z85Encode(byte[] data)
    {
        const string ENCODER = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ.-:+=^!/*?&<>()[]{}@%$#";

        var size = data.Length;

        //  accepts only byte arrays bounded to 4 bytes
        if (size % 4 != 0) throw new ArgumentException();

        var encodedSize = size * 5 / 4;
        var encoded = new char[encodedSize];
        uint chars = 0;
        uint bytes = 0;
        uint value = 0;
        while (bytes < size)
        {
            //  Accumulate value in base 256 (binary)
            value = value * 256 + data[bytes++];
            if (bytes % 4 == 0)
            {
                //  Output value in base 85
                uint divisor = 85 * 85 * 85 * 85;
                while (divisor > 0)
                {
                    encoded[chars++] = ENCODER[(int)(value / divisor % 85)];
                    divisor /= 85;
                }
                value = 0;
            }
        }

        return new string(encoded);
    }
}
