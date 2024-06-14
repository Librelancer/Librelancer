// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Graphics
{
    public static class BlendMode
    {
        public const ushort Opaque = 0;
        public const ushort Normal = (D3DBLEND_SRCALPHA << 8) | D3DBLEND_INVSRCALPHA;
        public const ushort Additive = (D3DBLEND_SRCALPHA << 8) | D3DBLEND_ONE;
        public const ushort OneInvSrcColor = (D3DBLEND_ONE << 8) | D3DBLEND_INVSRCCOLOR;
        //D3DBLEND, we base our consts on this
        private const int D3DBLEND_ZERO = 1;
        private const int D3DBLEND_ONE              = 2;
        private const int D3DBLEND_SRCCOLOR         = 3;
        private const int D3DBLEND_INVSRCCOLOR      = 4;
        private const int D3DBLEND_SRCALPHA         = 5;
        private const int D3DBLEND_INVSRCALPHA      = 6;
        private const int D3DBLEND_DESTALPHA        = 7;
        private const int D3DBLEND_INVDESTALPHA     = 8;
        private const int D3DBLEND_DESTCOLOR        = 9;
        private const int D3DBLEND_INVDESTCOLOR     = 10;
        private const int D3DBLEND_SRCALPHASAT      = 11;
        /* Either obsolete or not available in D3D8
        private const int D3DBLEND_BOTHSRCALPHA     = 12;
        private const int D3DBLEND_BOTHINVSRCALPHA  = 13;
        private const int D3DBLEND_BLENDFACTOR      = 14;
        private const int D3DBLEND_INVBLENDFACTOR   = 15;
        private const int D3DBLEND_SRCCOLOR2        = 16;
        private const int D3DBLEND_INVSRCCOLOR2     = 17;*/

        internal static void Validate(ushort blend)
        {
            Validate((ushort)((blend >> 8) & 0xFF), (ushort)(blend & 0xFF));
        }
        static void Validate(ushort src, ushort dst)
        {
            if (src > 11) throw new ArgumentOutOfRangeException(nameof(src));
            if (dst > 11) throw new ArgumentOutOfRangeException(nameof(dst));
            if(src == 0 && dst != 0) throw new ArgumentOutOfRangeException(nameof(dst));
            if(dst == 0 && src != 0) throw new ArgumentOutOfRangeException(nameof(src));
        }

        public static ushort Create(ushort src, ushort dst)
        {
            Validate(src, dst);
            return (ushort)((src << 8) | dst);
        }
    }
}

