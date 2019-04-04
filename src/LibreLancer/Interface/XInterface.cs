// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
namespace LibreLancer
{
    public class XInterface
    {
        [XmlElement("Scene")]
        public XInt.Scene[] Scenes { get; set; }

        public string DefaultScene { get; set; }

        [XmlElement("Include")]
        public XInt.Include[] Includes { get; set; }

        [XmlElement("ResourceFile")]
        public string[] ResourceFiles { get; set; }
        [XmlElement("Style")]
        public XInt.Style[] Styles { get; set; }
        public static XInterface Load(string source)
        {
            var xml = new XmlSerializer(typeof(XInterface));
            using(var reader =new StringReader(source)) {
                return (XInterface)xml.Deserialize(reader);
            }
        }
    }

    namespace XInt
    {
        public class Include
        {
            [XmlAttribute("file")]
            public string File { get; set; }
        }

        public class Style
        {
            [XmlAttribute("id")]
            public string ID { get; set; }
            [XmlAttribute("scissor")]
            public bool Scissor { get; set; }
            [XmlElement("Model")]
            public Model[] Models { get; set; }
            [XmlElement("Size")]
            public StyleSize Size { get; set; }
            [XmlElement("Text")]
            public StyleText[] Texts { get; set; }
            [XmlElement("Background")]
            public StyleBackground Background { get; set; }
            [XmlElement("Border")]
            public StyleBackground Border { get; set; }
            [XmlElement("HoverStyle")]
            public string HoverStyle { get; set; }
        }
        public class HitRectangle
        {
            [XmlAttribute("x")]
            public string XText { get; set; }
            float? x;
            [XmlIgnore]
            public float X
            {
                get
                {
                    if (x == null) x = Parser.Percentage(XText);
                    return x.Value;
                }
            }
            [XmlAttribute("y")]
            public string YText { get; set; }
            float? y;
            [XmlIgnore]
            public float Y
            {
                get
                {
                    if (y == null) y = Parser.Percentage(YText);
                    return y.Value;
                }
            }
            [XmlAttribute("width")]
            public string WidthText { get; set; }
            float? width;
            [XmlIgnore]
            public float Width
            {
                get
                {
                    if (width == null) width = Parser.Percentage(WidthText);
                    return width.Value;
                }
            }
            [XmlAttribute("height")]
            public string HeightText { get; set; }
            float? height;
            [XmlIgnore]
            public float Height
            {
                get
                {
                    if (height == null) height = Parser.Percentage(HeightText);
                    return height.Value;
                }
            }
            [XmlAttribute("draw")]
            public bool Draw { get; set; }
        }
        public class StyleBackground
        {
            [XmlAttribute("color")]
            public string ColorText { get; set; }
            Color4? color;
            [XmlIgnore]
            public Color4 Color {
                get {
                    if (string.IsNullOrEmpty(ColorText)) return Color4.White;
                    if (color == null) color = Parser.Color(ColorText);
                    return color.Value;
                }
            }
        }
        public enum Align
        {
            [XmlEnum("default")]
            Default,
            [XmlEnum("baseline")]
            Baseline
        }
        public class StyleText
        {
            [XmlAttribute("x")]
            public string XText { get; set;  }
            float? x;
            [XmlIgnore]
            public float X {
                get {
                    if (x == null) x = Parser.Percentage(XText);
                    return x.Value;
                }
            }
            [XmlAttribute("y")]
            public string YText { get; set; }
            float? y;
            [XmlIgnore]
            public float Y
            {
                get
                {
                    if (y == null) y = Parser.Percentage(YText);
                    return y.Value;
                }
            }
            [XmlAttribute("width")]
            public string WidthText { get; set; }
            float? width;
            [XmlIgnore]
            public float Width
            {
                get
                {
                    if (width == null) width = Parser.Percentage(WidthText);
                    return width.Value;
                }
            }
            [XmlAttribute("height")]
            public string HeightText { get; set; }
            float? height;
            [XmlIgnore]
            public float Height
            {
                get
                {
                    if (height == null) height = Parser.Percentage(HeightText);
                    return height.Value;
                }
            }

            [XmlAttribute("color")]
            public string ColorText { get; set; }
            Color4? color;
            [XmlIgnore]
            public Color4 Color {
                get {
                    if (string.IsNullOrEmpty(ColorText)) return Color4.White;
                    if (color == null) color = Parser.Color(ColorText);
                    return color.Value;
                }
            }

            [XmlAttribute("background")]
            public string BackgroundText { get; set; }
            Color4? background;
            [XmlIgnore]
            public Color4? Background {
                get {
                    if (string.IsNullOrEmpty(BackgroundText)) return null;
                    if (background == null) background = Parser.Color(BackgroundText);
                    return background.Value;
                }
            }

            [XmlAttribute("shadow")]
            public string ShadowText { get; set; }
            Color4? shadow;
            [XmlIgnore]
            public Color4? Shadow
            {
                get
                {
                    if (string.IsNullOrEmpty(ShadowText)) return null;
                    if (shadow == null) shadow = Parser.Color(ShadowText);
                    return shadow.Value;
                }
            }

            [XmlAttribute("id")]
            public string ID { get; set; }

            [XmlAttribute("lines")]
            public int Lines { get; set; }

            [XmlAttribute("align")]
            public Align Align { get; set; }
        }
      
        public class StyleSize
        {
            [XmlAttribute("height")]
            public string HeightText { get; set; }
            float? height;
            public float Height {
                get {
                    if (height == null) height = Parser.Percentage(HeightText);
                    return height.Value;
                }
            }
            [XmlAttribute("ratio")]
            public float Ratio { get; set; }
        }
        public class StyleBorder
        {
            [XmlAttribute("color")]
            public string ColorText { get; set; }
            Color4? color;
            [XmlIgnore]
            public Color4 Color
            {
                get
                {
                    if (string.IsNullOrEmpty(ColorText)) return Color4.White;
                    if (color == null) color = Parser.Color(ColorText);
                    return color.Value;
                }
            }
        }
        public class Model
        {
            [XmlAttribute("path")]
            public string Path { get; set; }
            [XmlAttribute("transform")]
            public string TransformString { get; set; }
            float[] _transformFloats;
            [XmlIgnore]
            public float[] Transform
            {
                get
                {
                    if (_transformFloats == null) _transformFloats = Parser.FloatArray(TransformString);
                    return _transformFloats;
                }
            }
            [XmlAttribute("color")]
            public string ColorText { get; set; }
            Color4? color;
            [XmlIgnore]
            public Color4? Color {
                get {
                    if (string.IsNullOrEmpty(ColorText)) return null;
                    if (color == null) color = Parser.Color(ColorText);
                    return color.Value;
                }
            }
        }

        public class Scene
        {
            [XmlAttribute("id")]
            public string ID { get; set; }

            [XmlElement("Script")]
            public string[] Scripts { get; set; }

            [XmlElement("Button", Type = typeof(XInt.Button))]
            [XmlElement("Panel", Type = typeof(XInt.Panel))]
            [XmlElement("ServerList", Type = typeof(XInt.ServerList))]
            [XmlElement("Image", Type = typeof(XInt.Image))]
            [XmlElement("ChatBox", Type = typeof(XInt.ChatBox))]
            public object[] Items;
        }

        public class Positionable
        {
            [XmlAttribute("id")]
            public string ID { get; set; }
            [XmlAttribute("x")]
            public string XText { get; set; }
            float? x;
            [XmlIgnore]
            public float X
            {
                get
                {
                    if (string.IsNullOrEmpty(XText)) return 0;
                    if (x == null) x = Parser.Percentage(XText);
                    return x.Value;
                } set { x = value; }
            }
            [XmlAttribute("y")]
            public string YText { get; set; }
            float? y;
            [XmlIgnore]
            public float Y
            {
                get
                {
                    if (string.IsNullOrEmpty(YText)) return 0;
                    if (y == null) y = Parser.Percentage(YText);
                    return y.Value;
                } set { y = value; }
            }
            [XmlAttribute("aspect")]
            public string Aspect { get; set; }
            [XmlAttribute("anchor")]
            public Anchor Anchor { get; set; }
        }

        public enum Anchor
        {
            topleft,
            top,
            topright,
            bottomleft,
            bottom,
            bottomright
        }

        public class Panel : Positionable
        {
            [XmlAttribute("style")]
            public string Style { get; set; }
        }
        public class ServerList : Panel
        {
        }
        public class ChatBox : Positionable
        {
            [XmlAttribute("style")]
            public string Style { get; set; }
            [XmlAttribute("displayarea")]
            public string DisplayArea { get; set; }
        }

        public class Button : Positionable
        {
            [XmlAttribute("onclick")]
            public string OnClick { get; set; }
            [XmlAttribute("style")]
            public string Style { get; set; }
            [XmlAttribute("text")]
            public string Text { get; set; }
        }

        public class Image
        {
            [XmlAttribute("path")]
            public string Path { get; set; }
        }
    }
}
