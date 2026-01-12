// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
namespace LibreLancer.Data;

//Quick place to check content DLL hacks
public class ContentDll
{
    public bool AlwaysMission13;

    private static bool check(byte[] dll, int offset, params byte[] bytes)
    {
        for(int i = 0; i < bytes.Length; i++) {
            if (dll[offset + i] != bytes[i]) return false;
        }
        return true;
    }
    public void Load(byte[] dll)
    {
        //Check DLL hacks
        AlwaysMission13 = check(dll, 0x04EE3A, 0xA2, 0x6A);
    }
}