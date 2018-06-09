/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
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
                    if (string.IsNullOrEmpty(YText)) return 0;
                    if (y == null) y = Parser.Percentage(YText);
                    return y.Value;
                }
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
