// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace LibreLancer
{
    /// <summary>
    /// Represents a color with 4 byte components as little-endian 0xAARRGGBB
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct Bgra8 : IEquatable<Bgra8>
    {

        #region Fields
        [FieldOffset(0)]
        public uint Pixel;
        /// <summary>
        /// The red component of this Bgra8 structure.
        /// </summary>
        [FieldOffset(0)]
		public byte B;
        /// <summary>
        /// The green component of this Bgra8 structure.
        /// </summary>
        [FieldOffset(1)]
        public byte G;
        /// <summary>
        /// The blue component of this Bgra8 structure.
        /// </summary>
        [FieldOffset(2)]
		public byte R;
        /// <summary>
        /// The alpha component of this Bgra8 structure.
        /// </summary>
        [FieldOffset(3)]
        public byte A;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new Bgra8 structure from the packed value.
        /// </summary>
        public Bgra8(uint pixel)
        {
            Pixel = pixel;
        }
        /// <summary>
        /// Constructs a new Bgra8 structure from the specified components.
        /// </summary>
        /// <param name="r">The red component of the new Bgra8 structure.</param>
        /// <param name="g">The green component of the new Bgra8 structure.</param>
        /// <param name="b">The blue component of the new Bgra8 structure.</param>
        /// <param name="a">The alpha component of the new Bgra8 structure.</param>
        public Bgra8(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        /// <summary>
        /// Constructs a new Bgra8 structure from the specified components.
        /// </summary>
        /// <param name="r">The red component of the new Bgra8 structure.</param>
        /// <param name="g">The green component of the new Bgra8 structure.</param>
        /// <param name="b">The blue component of the new Bgra8 structure.</param>
        public Bgra8(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
            A = 255;
        }

        /// <summary>
        /// Constructs a new Bgra8 structure from the specified components.
        /// </summary>
        /// <param name="r">The red component of the new Bgra8 structure.</param>
        /// <param name="g">The green component of the new Bgra8 structure.</param>
        /// <param name="b">The blue component of the new Bgra8 structure.</param>
        /// <param name="a">The alpha component of the new Bgra8 structure.</param>
        public Bgra8(float r, float g, float b, float a)
        {
            R = (byte)(r * (byte)Byte.MaxValue);
            G = (byte)(g * (byte)Byte.MaxValue);
            B = (byte)(b * (byte)Byte.MaxValue);
            A = (byte)(a * (byte)Byte.MaxValue);
        }

        /// <summary>
        /// Constructs a new Bgra8 structure from the specified components.
        /// </summary>
        /// <param name="r">The red component of the new Bgra8 structure.</param>
        /// <param name="g">The green component of the new Bgra8 structure.</param>
        /// <param name="b">The blue component of the new Bgra8 structure.</param>
        public Bgra8(float r, float g, float b)
        {
            R = (byte)(r * (byte)Byte.MaxValue);
            G = (byte)(g * (byte)Byte.MaxValue);
            B = (byte)(b * (byte)Byte.MaxValue);
            A = 255;
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Converts this color to an integer representation with 8 bits per channel.
        /// </summary>
        /// <returns>A <see cref="System.Int32"/> that represents this instance.</returns>
        /// <remarks>This method is intended only for compatibility with System.Drawing. It compresses the color into 8 bits per channel, which means color information is lost.</remarks>
        public int ToArgb()
        {
            uint value =
                (uint)(A) << 24 |
                (uint)(R) << 16 |
                (uint)(G) << 8 |
                (uint)(B);

            return unchecked((int)value);
        }

        public Vector4 ToVector4()
        {
            return new Vector4((float)R / 255f, (float)G / 255f, (float)B / 255f, (float)A / 255f);
        }

        public Vector3 ToVector3()
        {
            return new Vector3((float)R / 255f, (float)G / 255f, (float)B / 255f);
        }

        public Color4 ToColor4()
        {
            return new Color4((float)R / 255f, (float)G / 255f, (float)B / 255f, (float)A / 255f);
        }
        /// <summary>
        /// Compares the specified Bgra8 structures for equality.
        /// </summary>
        /// <param name="left">The left-hand side of the comparison.</param>
        /// <param name="right">The right-hand side of the comparison.</param>
        /// <returns>True if left is equal to right; false otherwise.</returns>
        public static bool operator ==(Bgra8 left, Bgra8 right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares the specified Bgra8 structures for inequality.
        /// </summary>
        /// <param name="left">The left-hand side of the comparison.</param>
        /// <param name="right">The right-hand side of the comparison.</param>
        /// <returns>True if left is not equal to right; false otherwise.</returns>
        public static bool operator !=(Bgra8 left, Bgra8 right)
        {
            return !left.Equals(right);
        }


        /// <summary>
        /// Compares whether this Bgra8 structure is equal to the specified object.
        /// </summary>
        /// <param name="obj">An object to compare to.</param>
        /// <returns>True obj is a Bgra8 structure with the same components as this Color; false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Bgra8))
                return false;

            return Equals((Bgra8)obj);
        }

        /// <summary>
        /// Calculates the hash code for this Bgra8 structure.
        /// </summary>
        /// <returns>A System.Int32 containing the hashcode of this Bgra8 structure.</returns>
        public override int GetHashCode()
        {
            return ToArgb();
        }

        /// <summary>
        /// Creates a System.String that describes this Bgra8 structure.
        /// </summary>
        /// <returns>A System.String that describes this Bgra8 structure.</returns>
        public override string ToString()
        {
            return String.Format("{{(R, G, B, A) = ({0}, {1}, {2}, {3})}}", R.ToString(), G.ToString(), B.ToString(), A.ToString());
        }

        #region System colors

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 255, 0).
        /// </summary>
        public static Bgra8 Transparent { get { return new Bgra8(255, 255, 255, 0); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 248, 255, 255).
        /// </summary>
        public static Bgra8 AliceBlue { get { return new Bgra8(240, 248, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 235, 215, 255).
        /// </summary>
        public static Bgra8 AntiqueWhite { get { return new Bgra8(250, 235, 215, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 255, 255).
        /// </summary>
        public static Bgra8 Aqua { get { return new Bgra8(0, 255, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (127, 255, 212, 255).
        /// </summary>
        public static Bgra8 Aquamarine { get { return new Bgra8(127, 255, 212, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 255, 255, 255).
        /// </summary>
        public static Bgra8 Azure { get { return new Bgra8(240, 255, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 245, 220, 255).
        /// </summary>
        public static Bgra8 Beige { get { return new Bgra8(245, 245, 220, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 228, 196, 255).
        /// </summary>
        public static Bgra8 Bisque { get { return new Bgra8(255, 228, 196, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 0, 255).
        /// </summary>
        public static Bgra8 Black { get { return new Bgra8(0, 0, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 235, 205, 255).
        /// </summary>
        public static Bgra8 BlanchedAlmond { get { return new Bgra8(255, 235, 205, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 255, 255).
        /// </summary>
        public static Bgra8 Blue { get { return new Bgra8(0, 0, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (138, 43, 226, 255).
        /// </summary>
        public static Bgra8 BlueViolet { get { return new Bgra8(138, 43, 226, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (165, 42, 42, 255).
        /// </summary>
        public static Bgra8 Brown { get { return new Bgra8(165, 42, 42, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (222, 184, 135, 255).
        /// </summary>
        public static Bgra8 BurlyWood { get { return new Bgra8(222, 184, 135, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (95, 158, 160, 255).
        /// </summary>
        public static Bgra8 CadetBlue { get { return new Bgra8(95, 158, 160, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (127, 255, 0, 255).
        /// </summary>
        public static Bgra8 Chartreuse { get { return new Bgra8(127, 255, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (210, 105, 30, 255).
        /// </summary>
        public static Bgra8 Chocolate { get { return new Bgra8(210, 105, 30, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 127, 80, 255).
        /// </summary>
        public static Bgra8 Coral { get { return new Bgra8(255, 127, 80, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (100, 149, 237, 255).
        /// </summary>
        public static Bgra8 CornflowerBlue { get { return new Bgra8(100, 149, 237, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 248, 220, 255).
        /// </summary>
        public static Bgra8 Cornsilk { get { return new Bgra8(255, 248, 220, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (220, 20, 60, 255).
        /// </summary>
        public static Bgra8 Crimson { get { return new Bgra8(220, 20, 60, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 255, 255).
        /// </summary>
        public static Bgra8 Cyan { get { return new Bgra8(0, 255, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 139, 255).
        /// </summary>
        public static Bgra8 DarkBlue { get { return new Bgra8(0, 0, 139, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 139, 139, 255).
        /// </summary>
        public static Bgra8 DarkCyan { get { return new Bgra8(0, 139, 139, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (184, 134, 11, 255).
        /// </summary>
        public static Bgra8 DarkGoldenrod { get { return new Bgra8(184, 134, 11, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (169, 169, 169, 255).
        /// </summary>
        public static Bgra8 DarkGray { get { return new Bgra8(169, 169, 169, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 100, 0, 255).
        /// </summary>
        public static Bgra8 DarkGreen { get { return new Bgra8(0, 100, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (189, 183, 107, 255).
        /// </summary>
        public static Bgra8 DarkKhaki { get { return new Bgra8(189, 183, 107, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (139, 0, 139, 255).
        /// </summary>
        public static Bgra8 DarkMagenta { get { return new Bgra8(139, 0, 139, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (85, 107, 47, 255).
        /// </summary>
        public static Bgra8 DarkOliveGreen { get { return new Bgra8(85, 107, 47, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 140, 0, 255).
        /// </summary>
        public static Bgra8 DarkOrange { get { return new Bgra8(255, 140, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (153, 50, 204, 255).
        /// </summary>
        public static Bgra8 DarkOrchid { get { return new Bgra8(153, 50, 204, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (139, 0, 0, 255).
        /// </summary>
        public static Bgra8 DarkRed { get { return new Bgra8(139, 0, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (233, 150, 122, 255).
        /// </summary>
        public static Bgra8 DarkSalmon { get { return new Bgra8(233, 150, 122, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (143, 188, 139, 255).
        /// </summary>
        public static Bgra8 DarkSeaGreen { get { return new Bgra8(143, 188, 139, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (72, 61, 139, 255).
        /// </summary>
        public static Bgra8 DarkSlateBlue { get { return new Bgra8(72, 61, 139, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (47, 79, 79, 255).
        /// </summary>
        public static Bgra8 DarkSlateGray { get { return new Bgra8(47, 79, 79, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 206, 209, 255).
        /// </summary>
        public static Bgra8 DarkTurquoise { get { return new Bgra8(0, 206, 209, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (148, 0, 211, 255).
        /// </summary>
        public static Bgra8 DarkViolet { get { return new Bgra8(148, 0, 211, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 20, 147, 255).
        /// </summary>
        public static Bgra8 DeepPink { get { return new Bgra8(255, 20, 147, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 191, 255, 255).
        /// </summary>
        public static Bgra8 DeepSkyBlue { get { return new Bgra8(0, 191, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (105, 105, 105, 255).
        /// </summary>
        public static Bgra8 DimGray { get { return new Bgra8(105, 105, 105, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (30, 144, 255, 255).
        /// </summary>
        public static Bgra8 DodgerBlue { get { return new Bgra8(30, 144, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (178, 34, 34, 255).
        /// </summary>
        public static Bgra8 Firebrick { get { return new Bgra8(178, 34, 34, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 250, 240, 255).
        /// </summary>
        public static Bgra8 FloralWhite { get { return new Bgra8(255, 250, 240, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (34, 139, 34, 255).
        /// </summary>
        public static Bgra8 ForestGreen { get { return new Bgra8(34, 139, 34, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 0, 255, 255).
        /// </summary>
        public static Bgra8 Fuchsia { get { return new Bgra8(255, 0, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (220, 220, 220, 255).
        /// </summary>
        public static Bgra8 Gainsboro { get { return new Bgra8(220, 220, 220, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (248, 248, 255, 255).
        /// </summary>
        public static Bgra8 GhostWhite { get { return new Bgra8(248, 248, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 215, 0, 255).
        /// </summary>
        public static Bgra8 Gold { get { return new Bgra8(255, 215, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (218, 165, 32, 255).
        /// </summary>
        public static Bgra8 Goldenrod { get { return new Bgra8(218, 165, 32, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 128, 128, 255).
        /// </summary>
        public static Bgra8 Gray { get { return new Bgra8(128, 128, 128, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 128, 0, 255).
        /// </summary>
        public static Bgra8 Green { get { return new Bgra8(0, 128, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (173, 255, 47, 255).
        /// </summary>
        public static Bgra8 GreenYellow { get { return new Bgra8(173, 255, 47, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 255, 240, 255).
        /// </summary>
        public static Bgra8 Honeydew { get { return new Bgra8(240, 255, 240, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 105, 180, 255).
        /// </summary>
        public static Bgra8 HotPink { get { return new Bgra8(255, 105, 180, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (205, 92, 92, 255).
        /// </summary>
        public static Bgra8 IndianRed { get { return new Bgra8(205, 92, 92, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (75, 0, 130, 255).
        /// </summary>
        public static Bgra8 Indigo { get { return new Bgra8(75, 0, 130, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 240, 255).
        /// </summary>
        public static Bgra8 Ivory { get { return new Bgra8(255, 255, 240, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 230, 140, 255).
        /// </summary>
        public static Bgra8 Khaki { get { return new Bgra8(240, 230, 140, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (230, 230, 250, 255).
        /// </summary>
        public static Bgra8 Lavender { get { return new Bgra8(230, 230, 250, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 240, 245, 255).
        /// </summary>
        public static Bgra8 LavenderBlush { get { return new Bgra8(255, 240, 245, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (124, 252, 0, 255).
        /// </summary>
        public static Bgra8 LawnGreen { get { return new Bgra8(124, 252, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 250, 205, 255).
        /// </summary>
        public static Bgra8 LemonChiffon { get { return new Bgra8(255, 250, 205, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (173, 216, 230, 255).
        /// </summary>
        public static Bgra8 LightBlue { get { return new Bgra8(173, 216, 230, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 128, 128, 255).
        /// </summary>
        public static Bgra8 LightCoral { get { return new Bgra8(240, 128, 128, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (224, 255, 255, 255).
        /// </summary>
        public static Bgra8 LightCyan { get { return new Bgra8(224, 255, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 250, 210, 255).
        /// </summary>
        public static Bgra8 LightGoldenrodYellow { get { return new Bgra8(250, 250, 210, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (144, 238, 144, 255).
        /// </summary>
        public static Bgra8 LightGreen { get { return new Bgra8(144, 238, 144, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (211, 211, 211, 255).
        /// </summary>
        public static Bgra8 LightGray { get { return new Bgra8(211, 211, 211, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 182, 193, 255).
        /// </summary>
        public static Bgra8 LightPink { get { return new Bgra8(255, 182, 193, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 160, 122, 255).
        /// </summary>
        public static Bgra8 LightSalmon { get { return new Bgra8(255, 160, 122, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (32, 178, 170, 255).
        /// </summary>
        public static Bgra8 LightSeaGreen { get { return new Bgra8(32, 178, 170, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (135, 206, 250, 255).
        /// </summary>
        public static Bgra8 LightSkyBlue { get { return new Bgra8(135, 206, 250, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (119, 136, 153, 255).
        /// </summary>
        public static Bgra8 LightSlateGray { get { return new Bgra8(119, 136, 153, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (176, 196, 222, 255).
        /// </summary>
        public static Bgra8 LightSteelBlue { get { return new Bgra8(176, 196, 222, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 224, 255).
        /// </summary>
        public static Bgra8 LightYellow { get { return new Bgra8(255, 255, 224, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 0, 255).
        /// </summary>
        public static Bgra8 Lime { get { return new Bgra8(0, 255, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (50, 205, 50, 255).
        /// </summary>
        public static Bgra8 LimeGreen { get { return new Bgra8(50, 205, 50, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 240, 230, 255).
        /// </summary>
        public static Bgra8 Linen { get { return new Bgra8(250, 240, 230, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 0, 255, 255).
        /// </summary>
        public static Bgra8 Magenta { get { return new Bgra8(255, 0, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 0, 0, 255).
        /// </summary>
        public static Bgra8 Maroon { get { return new Bgra8(128, 0, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (102, 205, 170, 255).
        /// </summary>
        public static Bgra8 MediumAquamarine { get { return new Bgra8(102, 205, 170, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 205, 255).
        /// </summary>
        public static Bgra8 MediumBlue { get { return new Bgra8(0, 0, 205, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (186, 85, 211, 255).
        /// </summary>
        public static Bgra8 MediumOrchid { get { return new Bgra8(186, 85, 211, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (147, 112, 219, 255).
        /// </summary>
        public static Bgra8 MediumPurple { get { return new Bgra8(147, 112, 219, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (60, 179, 113, 255).
        /// </summary>
        public static Bgra8 MediumSeaGreen { get { return new Bgra8(60, 179, 113, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (123, 104, 238, 255).
        /// </summary>
        public static Bgra8 MediumSlateBlue { get { return new Bgra8(123, 104, 238, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 250, 154, 255).
        /// </summary>
        public static Bgra8 MediumSpringGreen { get { return new Bgra8(0, 250, 154, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (72, 209, 204, 255).
        /// </summary>
        public static Bgra8 MediumTurquoise { get { return new Bgra8(72, 209, 204, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (199, 21, 133, 255).
        /// </summary>
        public static Bgra8 MediumVioletRed { get { return new Bgra8(199, 21, 133, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (25, 25, 112, 255).
        /// </summary>
        public static Bgra8 MidnightBlue { get { return new Bgra8(25, 25, 112, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 255, 250, 255).
        /// </summary>
        public static Bgra8 MintCream { get { return new Bgra8(245, 255, 250, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 228, 225, 255).
        /// </summary>
        public static Bgra8 MistyRose { get { return new Bgra8(255, 228, 225, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 228, 181, 255).
        /// </summary>
        public static Bgra8 Moccasin { get { return new Bgra8(255, 228, 181, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 222, 173, 255).
        /// </summary>
        public static Bgra8 NavajoWhite { get { return new Bgra8(255, 222, 173, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 128, 255).
        /// </summary>
        public static Bgra8 Navy { get { return new Bgra8(0, 0, 128, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (253, 245, 230, 255).
        /// </summary>
        public static Bgra8 OldLace { get { return new Bgra8(253, 245, 230, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 128, 0, 255).
        /// </summary>
        public static Bgra8 Olive { get { return new Bgra8(128, 128, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (107, 142, 35, 255).
        /// </summary>
        public static Bgra8 OliveDrab { get { return new Bgra8(107, 142, 35, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 165, 0, 255).
        /// </summary>
        public static Bgra8 Orange { get { return new Bgra8(255, 165, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 69, 0, 255).
        /// </summary>
        public static Bgra8 OrangeRed { get { return new Bgra8(255, 69, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (218, 112, 214, 255).
        /// </summary>
        public static Bgra8 Orchid { get { return new Bgra8(218, 112, 214, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (238, 232, 170, 255).
        /// </summary>
        public static Bgra8 PaleGoldenrod { get { return new Bgra8(238, 232, 170, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (152, 251, 152, 255).
        /// </summary>
        public static Bgra8 PaleGreen { get { return new Bgra8(152, 251, 152, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (175, 238, 238, 255).
        /// </summary>
        public static Bgra8 PaleTurquoise { get { return new Bgra8(175, 238, 238, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (219, 112, 147, 255).
        /// </summary>
        public static Bgra8 PaleVioletRed { get { return new Bgra8(219, 112, 147, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 239, 213, 255).
        /// </summary>
        public static Bgra8 PapayaWhip { get { return new Bgra8(255, 239, 213, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 218, 185, 255).
        /// </summary>
        public static Bgra8 PeachPuff { get { return new Bgra8(255, 218, 185, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (205, 133, 63, 255).
        /// </summary>
        public static Bgra8 Peru { get { return new Bgra8(205, 133, 63, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 192, 203, 255).
        /// </summary>
        public static Bgra8 Pink { get { return new Bgra8(255, 192, 203, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (221, 160, 221, 255).
        /// </summary>
        public static Bgra8 Plum { get { return new Bgra8(221, 160, 221, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (176, 224, 230, 255).
        /// </summary>
        public static Bgra8 PowderBlue { get { return new Bgra8(176, 224, 230, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 0, 128, 255).
        /// </summary>
        public static Bgra8 Purple { get { return new Bgra8(128, 0, 128, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 0, 0, 255).
        /// </summary>
        public static Bgra8 Red { get { return new Bgra8(255, 0, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (188, 143, 143, 255).
        /// </summary>
        public static Bgra8 RosyBrown { get { return new Bgra8(188, 143, 143, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (65, 105, 225, 255).
        /// </summary>
        public static Bgra8 RoyalBlue { get { return new Bgra8(65, 105, 225, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (139, 69, 19, 255).
        /// </summary>
        public static Bgra8 SaddleBrown { get { return new Bgra8(139, 69, 19, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 128, 114, 255).
        /// </summary>
        public static Bgra8 Salmon { get { return new Bgra8(250, 128, 114, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (244, 164, 96, 255).
        /// </summary>
        public static Bgra8 SandyBrown { get { return new Bgra8(244, 164, 96, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (46, 139, 87, 255).
        /// </summary>
        public static Bgra8 SeaGreen { get { return new Bgra8(46, 139, 87, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 245, 238, 255).
        /// </summary>
        public static Bgra8 SeaShell { get { return new Bgra8(255, 245, 238, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (160, 82, 45, 255).
        /// </summary>
        public static Bgra8 Sienna { get { return new Bgra8(160, 82, 45, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (192, 192, 192, 255).
        /// </summary>
        public static Bgra8 Silver { get { return new Bgra8(192, 192, 192, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (135, 206, 235, 255).
        /// </summary>
        public static Bgra8 SkyBlue { get { return new Bgra8(135, 206, 235, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (106, 90, 205, 255).
        /// </summary>
        public static Bgra8 SlateBlue { get { return new Bgra8(106, 90, 205, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (112, 128, 144, 255).
        /// </summary>
        public static Bgra8 SlateGray { get { return new Bgra8(112, 128, 144, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 250, 250, 255).
        /// </summary>
        public static Bgra8 Snow { get { return new Bgra8(255, 250, 250, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 127, 255).
        /// </summary>
        public static Bgra8 SpringGreen { get { return new Bgra8(0, 255, 127, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (70, 130, 180, 255).
        /// </summary>
        public static Bgra8 SteelBlue { get { return new Bgra8(70, 130, 180, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (210, 180, 140, 255).
        /// </summary>
        public static Bgra8 Tan { get { return new Bgra8(210, 180, 140, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 128, 128, 255).
        /// </summary>
        public static Bgra8 Teal { get { return new Bgra8(0, 128, 128, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (216, 191, 216, 255).
        /// </summary>
        public static Bgra8 Thistle { get { return new Bgra8(216, 191, 216, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 99, 71, 255).
        /// </summary>
        public static Bgra8 Tomato { get { return new Bgra8(255, 99, 71, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (64, 224, 208, 255).
        /// </summary>
        public static Bgra8 Turquoise { get { return new Bgra8(64, 224, 208, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (238, 130, 238, 255).
        /// </summary>
        public static Bgra8 Violet { get { return new Bgra8(238, 130, 238, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 222, 179, 255).
        /// </summary>
        public static Bgra8 Wheat { get { return new Bgra8(245, 222, 179, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 255, 255).
        /// </summary>
        public static Bgra8 White { get { return new Bgra8(255, 255, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 245, 245, 255).
        /// </summary>
        public static Bgra8 WhiteSmoke { get { return new Bgra8(245, 245, 245, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 0, 255).
        /// </summary>
        public static Bgra8 Yellow { get { return new Bgra8(255, 255, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (154, 205, 50, 255).
        /// </summary>
        public static Bgra8 YellowGreen { get { return new Bgra8(154, 205, 50, 255); } }

        #endregion

        #endregion

        #region IEquatable<Color> Members

        /// <summary>
        /// Compares whether this Bgra8 structure is equal to the specified Color.
        /// </summary>
        /// <param name="other">The Bgra8 structure to compare to.</param>
        /// <returns>True if both Bgra8 structures contain the same components; false otherwise.</returns>
        public bool Equals(Bgra8 other)
        {
            return
                this.R == other.R &&
            this.G == other.G &&
            this.B == other.B &&
            this.A == other.A;
        }

        #endregion


        /// <summary>
        /// Converts a buffer in-place from Rgba to Bgra
        /// </summary>
        /// <param name="buffer">The buffer to modify</param>
        public static void ConvertFromRgba(Bgra8[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = new Bgra8(buffer[i].B, buffer[i].G, buffer[i].R, buffer[i].A);
        }

        public static ReadOnlySpan<Bgra8> BufferFromBytes(byte[] source)
        {
            if (source.Length % 4 != 0)
                throw new ArgumentException(nameof(source));
            return MemoryMarshal.Cast<byte, Bgra8>(source.AsSpan());
        }
    }
}
