using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
namespace LibreLancer
{
    /// <summary>
    /// Represents a color with 4 byte components (R, G, B, A).
    /// </summary>
    public struct Colorb : IEquatable<Colorb>
    {

        #region Fields

        /// <summary>
        /// The red component of this Color structure.
        /// </summary>
		public byte R;
        /// <summary>
        /// The green component of this Color structure.
        /// </summary>
        public byte G;
        /// <summary>
        /// The blue component of this Color structure.
        /// </summary>
		public byte B;
        /// <summary>
        /// The alpha component of this Color structure.
        /// </summary>
        public byte A;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new Color structure from the specified components.
        /// </summary>
        /// <param name="r">The red component of the new Color structure.</param>
        /// <param name="g">The green component of the new Color structure.</param>
        /// <param name="b">The blue component of the new Color structure.</param>
        /// <param name="a">The alpha component of the new Color structure.</param>
        public Colorb(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        /// <summary>
        /// Constructs a new Color structure from the specified components.
        /// </summary>
        /// <param name="r">The red component of the new Color structure.</param>
        /// <param name="g">The green component of the new Color structure.</param>
        /// <param name="b">The blue component of the new Color structure.</param>
        public Colorb(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
            A = 255;
        }

        /// <summary>
        /// Constructs a new Color structure from the specified components.
        /// </summary>
        /// <param name="r">The red component of the new Color structure.</param>
        /// <param name="g">The green component of the new Color structure.</param>
        /// <param name="b">The blue component of the new Color structure.</param>
        /// <param name="a">The alpha component of the new Color structure.</param>
        public Colorb(float r, float g, float b, float a)
        {
            R = (byte)(r * (byte)Byte.MaxValue);
            G = (byte)(g * (byte)Byte.MaxValue);
            B = (byte)(b * (byte)Byte.MaxValue);
            A = (byte)(a * (byte)Byte.MaxValue);
        }

        /// <summary>
        /// Constructs a new Color structure from the specified components.
        /// </summary>
        /// <param name="r">The red component of the new Color structure.</param>
        /// <param name="g">The green component of the new Color structure.</param>
        /// <param name="b">The blue component of the new Color structure.</param>
        public Colorb(float r, float g, float b)
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
        /// Compares the specified Color structures for equality.
        /// </summary>
        /// <param name="left">The left-hand side of the comparison.</param>
        /// <param name="right">The right-hand side of the comparison.</param>
        /// <returns>True if left is equal to right; false otherwise.</returns>
        public static bool operator ==(Colorb left, Colorb right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares the specified Color structures for inequality.
        /// </summary>
        /// <param name="left">The left-hand side of the comparison.</param>
        /// <param name="right">The right-hand side of the comparison.</param>
        /// <returns>True if left is not equal to right; false otherwise.</returns>
        public static bool operator !=(Colorb left, Colorb right)
        {
            return !left.Equals(right);
        }


        /// <summary>
        /// Compares whether this Color structure is equal to the specified object.
        /// </summary>
        /// <param name="obj">An object to compare to.</param>
        /// <returns>True obj is a Color structure with the same components as this Color; false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Colorb))
                return false;

            return Equals((Colorb)obj);
        }

        /// <summary>
        /// Calculates the hash code for this Color structure.
        /// </summary>
        /// <returns>A System.Int32 containing the hashcode of this Color structure.</returns>
        public override int GetHashCode()
        {
            return ToArgb();
        }

        /// <summary>
        /// Creates a System.String that describes this Color structure.
        /// </summary>
        /// <returns>A System.String that describes this Color structure.</returns>
        public override string ToString()
        {
            return String.Format("{{(R, G, B, A) = ({0}, {1}, {2}, {3})}}", R.ToString(), G.ToString(), B.ToString(), A.ToString());
        }

        #region System colors

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 255, 0).
        /// </summary>
        public static Colorb Transparent { get { return new Colorb(255, 255, 255, 0); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 248, 255, 255).
        /// </summary>
        public static Colorb AliceBlue { get { return new Colorb(240, 248, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 235, 215, 255).
        /// </summary>
        public static Colorb AntiqueWhite { get { return new Colorb(250, 235, 215, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 255, 255).
        /// </summary>
        public static Colorb Aqua { get { return new Colorb(0, 255, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (127, 255, 212, 255).
        /// </summary>
        public static Colorb Aquamarine { get { return new Colorb(127, 255, 212, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 255, 255, 255).
        /// </summary>
        public static Colorb Azure { get { return new Colorb(240, 255, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 245, 220, 255).
        /// </summary>
        public static Colorb Beige { get { return new Colorb(245, 245, 220, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 228, 196, 255).
        /// </summary>
        public static Colorb Bisque { get { return new Colorb(255, 228, 196, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 0, 255).
        /// </summary>
        public static Colorb Black { get { return new Colorb(0, 0, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 235, 205, 255).
        /// </summary>
        public static Colorb BlanchedAlmond { get { return new Colorb(255, 235, 205, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 255, 255).
        /// </summary>
        public static Colorb Blue { get { return new Colorb(0, 0, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (138, 43, 226, 255).
        /// </summary>
        public static Colorb BlueViolet { get { return new Colorb(138, 43, 226, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (165, 42, 42, 255).
        /// </summary>
        public static Colorb Brown { get { return new Colorb(165, 42, 42, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (222, 184, 135, 255).
        /// </summary>
        public static Colorb BurlyWood { get { return new Colorb(222, 184, 135, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (95, 158, 160, 255).
        /// </summary>
        public static Colorb CadetBlue { get { return new Colorb(95, 158, 160, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (127, 255, 0, 255).
        /// </summary>
        public static Colorb Chartreuse { get { return new Colorb(127, 255, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (210, 105, 30, 255).
        /// </summary>
        public static Colorb Chocolate { get { return new Colorb(210, 105, 30, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 127, 80, 255).
        /// </summary>
        public static Colorb Coral { get { return new Colorb(255, 127, 80, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (100, 149, 237, 255).
        /// </summary>
        public static Colorb CornflowerBlue { get { return new Colorb(100, 149, 237, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 248, 220, 255).
        /// </summary>
        public static Colorb Cornsilk { get { return new Colorb(255, 248, 220, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (220, 20, 60, 255).
        /// </summary>
        public static Colorb Crimson { get { return new Colorb(220, 20, 60, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 255, 255).
        /// </summary>
        public static Colorb Cyan { get { return new Colorb(0, 255, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 139, 255).
        /// </summary>
        public static Colorb DarkBlue { get { return new Colorb(0, 0, 139, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 139, 139, 255).
        /// </summary>
        public static Colorb DarkCyan { get { return new Colorb(0, 139, 139, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (184, 134, 11, 255).
        /// </summary>
        public static Colorb DarkGoldenrod { get { return new Colorb(184, 134, 11, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (169, 169, 169, 255).
        /// </summary>
        public static Colorb DarkGray { get { return new Colorb(169, 169, 169, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 100, 0, 255).
        /// </summary>
        public static Colorb DarkGreen { get { return new Colorb(0, 100, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (189, 183, 107, 255).
        /// </summary>
        public static Colorb DarkKhaki { get { return new Colorb(189, 183, 107, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (139, 0, 139, 255).
        /// </summary>
        public static Colorb DarkMagenta { get { return new Colorb(139, 0, 139, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (85, 107, 47, 255).
        /// </summary>
        public static Colorb DarkOliveGreen { get { return new Colorb(85, 107, 47, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 140, 0, 255).
        /// </summary>
        public static Colorb DarkOrange { get { return new Colorb(255, 140, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (153, 50, 204, 255).
        /// </summary>
        public static Colorb DarkOrchid { get { return new Colorb(153, 50, 204, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (139, 0, 0, 255).
        /// </summary>
        public static Colorb DarkRed { get { return new Colorb(139, 0, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (233, 150, 122, 255).
        /// </summary>
        public static Colorb DarkSalmon { get { return new Colorb(233, 150, 122, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (143, 188, 139, 255).
        /// </summary>
        public static Colorb DarkSeaGreen { get { return new Colorb(143, 188, 139, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (72, 61, 139, 255).
        /// </summary>
        public static Colorb DarkSlateBlue { get { return new Colorb(72, 61, 139, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (47, 79, 79, 255).
        /// </summary>
        public static Colorb DarkSlateGray { get { return new Colorb(47, 79, 79, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 206, 209, 255).
        /// </summary>
        public static Colorb DarkTurquoise { get { return new Colorb(0, 206, 209, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (148, 0, 211, 255).
        /// </summary>
        public static Colorb DarkViolet { get { return new Colorb(148, 0, 211, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 20, 147, 255).
        /// </summary>
        public static Colorb DeepPink { get { return new Colorb(255, 20, 147, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 191, 255, 255).
        /// </summary>
        public static Colorb DeepSkyBlue { get { return new Colorb(0, 191, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (105, 105, 105, 255).
        /// </summary>
        public static Colorb DimGray { get { return new Colorb(105, 105, 105, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (30, 144, 255, 255).
        /// </summary>
        public static Colorb DodgerBlue { get { return new Colorb(30, 144, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (178, 34, 34, 255).
        /// </summary>
        public static Colorb Firebrick { get { return new Colorb(178, 34, 34, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 250, 240, 255).
        /// </summary>
        public static Colorb FloralWhite { get { return new Colorb(255, 250, 240, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (34, 139, 34, 255).
        /// </summary>
        public static Colorb ForestGreen { get { return new Colorb(34, 139, 34, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 0, 255, 255).
        /// </summary>
        public static Colorb Fuchsia { get { return new Colorb(255, 0, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (220, 220, 220, 255).
        /// </summary>
        public static Colorb Gainsboro { get { return new Colorb(220, 220, 220, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (248, 248, 255, 255).
        /// </summary>
        public static Colorb GhostWhite { get { return new Colorb(248, 248, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 215, 0, 255).
        /// </summary>
        public static Colorb Gold { get { return new Colorb(255, 215, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (218, 165, 32, 255).
        /// </summary>
        public static Colorb Goldenrod { get { return new Colorb(218, 165, 32, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 128, 128, 255).
        /// </summary>
        public static Colorb Gray { get { return new Colorb(128, 128, 128, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 128, 0, 255).
        /// </summary>
        public static Colorb Green { get { return new Colorb(0, 128, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (173, 255, 47, 255).
        /// </summary>
        public static Colorb GreenYellow { get { return new Colorb(173, 255, 47, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 255, 240, 255).
        /// </summary>
        public static Colorb Honeydew { get { return new Colorb(240, 255, 240, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 105, 180, 255).
        /// </summary>
        public static Colorb HotPink { get { return new Colorb(255, 105, 180, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (205, 92, 92, 255).
        /// </summary>
        public static Colorb IndianRed { get { return new Colorb(205, 92, 92, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (75, 0, 130, 255).
        /// </summary>
        public static Colorb Indigo { get { return new Colorb(75, 0, 130, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 240, 255).
        /// </summary>
        public static Colorb Ivory { get { return new Colorb(255, 255, 240, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 230, 140, 255).
        /// </summary>
        public static Colorb Khaki { get { return new Colorb(240, 230, 140, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (230, 230, 250, 255).
        /// </summary>
        public static Colorb Lavender { get { return new Colorb(230, 230, 250, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 240, 245, 255).
        /// </summary>
        public static Colorb LavenderBlush { get { return new Colorb(255, 240, 245, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (124, 252, 0, 255).
        /// </summary>
        public static Colorb LawnGreen { get { return new Colorb(124, 252, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 250, 205, 255).
        /// </summary>
        public static Colorb LemonChiffon { get { return new Colorb(255, 250, 205, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (173, 216, 230, 255).
        /// </summary>
        public static Colorb LightBlue { get { return new Colorb(173, 216, 230, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 128, 128, 255).
        /// </summary>
        public static Colorb LightCoral { get { return new Colorb(240, 128, 128, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (224, 255, 255, 255).
        /// </summary>
        public static Colorb LightCyan { get { return new Colorb(224, 255, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 250, 210, 255).
        /// </summary>
        public static Colorb LightGoldenrodYellow { get { return new Colorb(250, 250, 210, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (144, 238, 144, 255).
        /// </summary>
        public static Colorb LightGreen { get { return new Colorb(144, 238, 144, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (211, 211, 211, 255).
        /// </summary>
        public static Colorb LightGray { get { return new Colorb(211, 211, 211, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 182, 193, 255).
        /// </summary>
        public static Colorb LightPink { get { return new Colorb(255, 182, 193, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 160, 122, 255).
        /// </summary>
        public static Colorb LightSalmon { get { return new Colorb(255, 160, 122, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (32, 178, 170, 255).
        /// </summary>
        public static Colorb LightSeaGreen { get { return new Colorb(32, 178, 170, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (135, 206, 250, 255).
        /// </summary>
        public static Colorb LightSkyBlue { get { return new Colorb(135, 206, 250, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (119, 136, 153, 255).
        /// </summary>
        public static Colorb LightSlateGray { get { return new Colorb(119, 136, 153, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (176, 196, 222, 255).
        /// </summary>
        public static Colorb LightSteelBlue { get { return new Colorb(176, 196, 222, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 224, 255).
        /// </summary>
        public static Colorb LightYellow { get { return new Colorb(255, 255, 224, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 0, 255).
        /// </summary>
        public static Colorb Lime { get { return new Colorb(0, 255, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (50, 205, 50, 255).
        /// </summary>
        public static Colorb LimeGreen { get { return new Colorb(50, 205, 50, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 240, 230, 255).
        /// </summary>
        public static Colorb Linen { get { return new Colorb(250, 240, 230, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 0, 255, 255).
        /// </summary>
        public static Colorb Magenta { get { return new Colorb(255, 0, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 0, 0, 255).
        /// </summary>
        public static Colorb Maroon { get { return new Colorb(128, 0, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (102, 205, 170, 255).
        /// </summary>
        public static Colorb MediumAquamarine { get { return new Colorb(102, 205, 170, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 205, 255).
        /// </summary>
        public static Colorb MediumBlue { get { return new Colorb(0, 0, 205, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (186, 85, 211, 255).
        /// </summary>
        public static Colorb MediumOrchid { get { return new Colorb(186, 85, 211, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (147, 112, 219, 255).
        /// </summary>
        public static Colorb MediumPurple { get { return new Colorb(147, 112, 219, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (60, 179, 113, 255).
        /// </summary>
        public static Colorb MediumSeaGreen { get { return new Colorb(60, 179, 113, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (123, 104, 238, 255).
        /// </summary>
        public static Colorb MediumSlateBlue { get { return new Colorb(123, 104, 238, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 250, 154, 255).
        /// </summary>
        public static Colorb MediumSpringGreen { get { return new Colorb(0, 250, 154, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (72, 209, 204, 255).
        /// </summary>
        public static Colorb MediumTurquoise { get { return new Colorb(72, 209, 204, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (199, 21, 133, 255).
        /// </summary>
        public static Colorb MediumVioletRed { get { return new Colorb(199, 21, 133, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (25, 25, 112, 255).
        /// </summary>
        public static Colorb MidnightBlue { get { return new Colorb(25, 25, 112, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 255, 250, 255).
        /// </summary>
        public static Colorb MintCream { get { return new Colorb(245, 255, 250, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 228, 225, 255).
        /// </summary>
        public static Colorb MistyRose { get { return new Colorb(255, 228, 225, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 228, 181, 255).
        /// </summary>
        public static Colorb Moccasin { get { return new Colorb(255, 228, 181, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 222, 173, 255).
        /// </summary>
        public static Colorb NavajoWhite { get { return new Colorb(255, 222, 173, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 128, 255).
        /// </summary>
        public static Colorb Navy { get { return new Colorb(0, 0, 128, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (253, 245, 230, 255).
        /// </summary>
        public static Colorb OldLace { get { return new Colorb(253, 245, 230, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 128, 0, 255).
        /// </summary>
        public static Colorb Olive { get { return new Colorb(128, 128, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (107, 142, 35, 255).
        /// </summary>
        public static Colorb OliveDrab { get { return new Colorb(107, 142, 35, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 165, 0, 255).
        /// </summary>
        public static Colorb Orange { get { return new Colorb(255, 165, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 69, 0, 255).
        /// </summary>
        public static Colorb OrangeRed { get { return new Colorb(255, 69, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (218, 112, 214, 255).
        /// </summary>
        public static Colorb Orchid { get { return new Colorb(218, 112, 214, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (238, 232, 170, 255).
        /// </summary>
        public static Colorb PaleGoldenrod { get { return new Colorb(238, 232, 170, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (152, 251, 152, 255).
        /// </summary>
        public static Colorb PaleGreen { get { return new Colorb(152, 251, 152, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (175, 238, 238, 255).
        /// </summary>
        public static Colorb PaleTurquoise { get { return new Colorb(175, 238, 238, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (219, 112, 147, 255).
        /// </summary>
        public static Colorb PaleVioletRed { get { return new Colorb(219, 112, 147, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 239, 213, 255).
        /// </summary>
        public static Colorb PapayaWhip { get { return new Colorb(255, 239, 213, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 218, 185, 255).
        /// </summary>
        public static Colorb PeachPuff { get { return new Colorb(255, 218, 185, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (205, 133, 63, 255).
        /// </summary>
        public static Colorb Peru { get { return new Colorb(205, 133, 63, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 192, 203, 255).
        /// </summary>
        public static Colorb Pink { get { return new Colorb(255, 192, 203, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (221, 160, 221, 255).
        /// </summary>
        public static Colorb Plum { get { return new Colorb(221, 160, 221, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (176, 224, 230, 255).
        /// </summary>
        public static Colorb PowderBlue { get { return new Colorb(176, 224, 230, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 0, 128, 255).
        /// </summary>
        public static Colorb Purple { get { return new Colorb(128, 0, 128, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 0, 0, 255).
        /// </summary>
        public static Colorb Red { get { return new Colorb(255, 0, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (188, 143, 143, 255).
        /// </summary>
        public static Colorb RosyBrown { get { return new Colorb(188, 143, 143, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (65, 105, 225, 255).
        /// </summary>
        public static Colorb RoyalBlue { get { return new Colorb(65, 105, 225, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (139, 69, 19, 255).
        /// </summary>
        public static Colorb SaddleBrown { get { return new Colorb(139, 69, 19, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 128, 114, 255).
        /// </summary>
        public static Colorb Salmon { get { return new Colorb(250, 128, 114, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (244, 164, 96, 255).
        /// </summary>
        public static Colorb SandyBrown { get { return new Colorb(244, 164, 96, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (46, 139, 87, 255).
        /// </summary>
        public static Colorb SeaGreen { get { return new Colorb(46, 139, 87, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 245, 238, 255).
        /// </summary>
        public static Colorb SeaShell { get { return new Colorb(255, 245, 238, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (160, 82, 45, 255).
        /// </summary>
        public static Colorb Sienna { get { return new Colorb(160, 82, 45, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (192, 192, 192, 255).
        /// </summary>
        public static Colorb Silver { get { return new Colorb(192, 192, 192, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (135, 206, 235, 255).
        /// </summary>
        public static Colorb SkyBlue { get { return new Colorb(135, 206, 235, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (106, 90, 205, 255).
        /// </summary>
        public static Colorb SlateBlue { get { return new Colorb(106, 90, 205, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (112, 128, 144, 255).
        /// </summary>
        public static Colorb SlateGray { get { return new Colorb(112, 128, 144, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 250, 250, 255).
        /// </summary>
        public static Colorb Snow { get { return new Colorb(255, 250, 250, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 127, 255).
        /// </summary>
        public static Colorb SpringGreen { get { return new Colorb(0, 255, 127, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (70, 130, 180, 255).
        /// </summary>
        public static Colorb SteelBlue { get { return new Colorb(70, 130, 180, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (210, 180, 140, 255).
        /// </summary>
        public static Colorb Tan { get { return new Colorb(210, 180, 140, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 128, 128, 255).
        /// </summary>
        public static Colorb Teal { get { return new Colorb(0, 128, 128, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (216, 191, 216, 255).
        /// </summary>
        public static Colorb Thistle { get { return new Colorb(216, 191, 216, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 99, 71, 255).
        /// </summary>
        public static Colorb Tomato { get { return new Colorb(255, 99, 71, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (64, 224, 208, 255).
        /// </summary>
        public static Colorb Turquoise { get { return new Colorb(64, 224, 208, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (238, 130, 238, 255).
        /// </summary>
        public static Colorb Violet { get { return new Colorb(238, 130, 238, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 222, 179, 255).
        /// </summary>
        public static Colorb Wheat { get { return new Colorb(245, 222, 179, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 255, 255).
        /// </summary>
        public static Colorb White { get { return new Colorb(255, 255, 255, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 245, 245, 255).
        /// </summary>
        public static Colorb WhiteSmoke { get { return new Colorb(245, 245, 245, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 0, 255).
        /// </summary>
        public static Colorb Yellow { get { return new Colorb(255, 255, 0, 255); } }

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (154, 205, 50, 255).
        /// </summary>
        public static Colorb YellowGreen { get { return new Colorb(154, 205, 50, 255); } }

        #endregion

        #endregion

        #region IEquatable<Color> Members

        /// <summary>
        /// Compares whether this Color structure is equal to the specified Color.
        /// </summary>
        /// <param name="other">The Color structure to compare to.</param>
        /// <returns>True if both Color structures contain the same components; false otherwise.</returns>
        public bool Equals(Colorb other)
        {
            return
                this.R == other.R &&
            this.G == other.G &&
            this.B == other.B &&
            this.A == other.A;
        }

        #endregion

    }
}
