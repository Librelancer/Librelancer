// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
namespace LibreLancer
{
    //Sherlog's FLHash algorithm implemented in C#
    public static class FLHash
    {
        const int CRC16IBM_POLYNOMIAL = 0xA001;
        const int NUM_BITS = 8;
        const int HASH_TABLE_SIZE = (1 << NUM_BITS);
        static uint[] modded_crc16_ibm_table;
        static FLHash()
        {
            modded_crc16_ibm_table = new uint[HASH_TABLE_SIZE];
            for (int i = 0; i < HASH_TABLE_SIZE; i++)
            {
                uint x = (uint)i;
                for (int j = 0; j < 16 - NUM_BITS; j++)
                {
                    x = ((x & 1) == 1) ? (x >> 1) ^ (CRC16IBM_POLYNOMIAL << 14) : x >> 1;
                }
                modded_crc16_ibm_table[i] = x;
            }
        }
        public static uint CreateID(string id)
        {
            uint hash = 0;
            for(int i = 0; i < id.Length; i++)
            {
                hash = (hash >> NUM_BITS) ^ modded_crc16_ibm_table[(hash & 0x000000FF) ^ (byte)char.ToLowerInvariant(id[i])];
            }
            hash = (hash >> 24) | ((hash >> 8) & 0x0000FF00) | ((hash << 8) & 0x00FF0000) | (hash << 24);
            hash = (hash >> 2) | 0x80000000;
            return hash;
        }
    }
}
