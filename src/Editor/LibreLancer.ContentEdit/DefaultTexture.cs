// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.ContentEdit
{
    public static class DefaultTexture
    {
        public static readonly byte[] Data;
        static DefaultTexture()
        {
            using(var stream = typeof(DefaultTexture).Assembly.GetManifestResourceStream("LibreLancer.ContentEdit.defaulttexture.dds")) {
                Data = new byte[(int)stream.Length];
                stream.Read(Data, 0, (int)stream.Length);
            }
        }
    }
}