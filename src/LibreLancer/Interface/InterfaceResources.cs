// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using LibreLancer.Graphics.Text;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    public class InterfaceResources
    {
        [XmlElement("Color")] public List<InterfaceColor> Colors = new List<InterfaceColor>();
        [XmlElement("Model")] public List<InterfaceModel> Models = new List<InterfaceModel>();
        [XmlElement("Image")] public List<InterfaceImage> Images = new List<InterfaceImage>();
        [XmlElement("LibraryFile")] public List<string> LibraryFiles = new List<string>();

        private static XmlSerializer _serializer = new XmlSerializer(typeof(InterfaceResources));

        public string ToXml()
        {
            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;
            using (StringWriter sw = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(sw, settings))
            {
                var xmlns = new XmlSerializerNamespaces();
                xmlns.Add(string.Empty, string.Empty);
                _serializer.Serialize(writer, this, xmlns);
                return sw.ToString();
            }
        }

        public static InterfaceResources FromXml(string xml)
        {
            using (var reader = new StringReader(xml))
            {
                return (InterfaceResources) _serializer.Deserialize(reader);
            }
        }

        public static InterfaceResources FromFile(string file)
        {
            using (var stream = File.OpenRead(file))
            {
                return (InterfaceResources) _serializer.Deserialize(stream);
            }
        }
    }

    [WattleScript.Interpreter.WattleScriptUserData]
    public class InterfaceColor
    {
        public static readonly InterfaceColor White = new InterfaceColor() {Color = Color4.White};
        public static readonly InterfaceColor Black = new InterfaceColor() {Color = Color4.Black};
        public string Name;
        public Color4 Color;
        public InterfaceColorAnimation Animation;

        [XmlIgnore]
        float alphaFactor = 1;

        public InterfaceColor SetAlpha(float factor)
        {
            return new InterfaceColor() {
                Color = Color,
                Animation = Animation,
                alphaFactor = factor
            };
        }

        public Color4 GetColor(double time)
        {
            if (Animation != null)
            {
                var x = time;
                var factor = (float) Math.Abs(Math.Sin(Math.PI * x * Animation.Speed));
                return new Color4(
                    MathHelper.Lerp(Animation.Color1.R, Animation.Color2.R, factor),
                    MathHelper.Lerp(Animation.Color1.G, Animation.Color2.G, factor),
                    MathHelper.Lerp(Animation.Color1.B, Animation.Color2.B, factor),
                    MathHelper.Lerp(Animation.Color1.A * alphaFactor, Animation.Color2.A * alphaFactor, factor)
                );
            }
            else
                return new Color4(Color.R, Color.G, Color.B, Color.A * alphaFactor);
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name)) return Name;
            var r = (int) (Color.R * 255);
            var g = (int) (Color.G * 255);
            var b = (int) (Color.B * 255);
            var a = (int) ((Color.A * alphaFactor) * 255);
            return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }
    }

    public enum InterfaceImageKind
    {
        Normal,
        Triangle,
        Quad
    }
    [WattleScript.Interpreter.WattleScriptUserData]
    public class InterfaceImage
    {
        [XmlAttribute("name")] public string Name;
        [XmlAttribute("texname")] public string TexName;
        [XmlAttribute("texpath")] public string TexPath;
        [XmlAttribute("type")] public InterfaceImageKind Type;
        [XmlAttribute("rot")] public QuadRotation Rotation;
        [XmlAttribute("originx")] public float OriginX;
        [XmlAttribute("originy")] public float OriginY;
        [XmlAttribute("angle")] public float Angle;
        [XmlAttribute("flip")] public bool Flip;
        [XmlElement("TexCoords")] public InterfacePoints TexCoords = new InterfacePoints();
        [XmlElement("DisplayCoords")] public InterfacePoints DisplayCoords = new InterfacePoints();
        [XmlAttribute("animu")] public float AnimU;
        [XmlAttribute("animv")] public float AnimV;
    }
    [WattleScript.Interpreter.WattleScriptUserData]
    public class InterfacePoints
    {
        //Top left
        public float X0;
        public float Y0;
        //Top right
        public float X1 = 1;
        public float Y1;
        //Bottom left
        public float X2;
        public float Y2 = 1;
        //Bottom right
        public float X3 = 1;
        public float Y3 = 1;
    }

    [WattleScript.Interpreter.WattleScriptUserData]
    public class InterfaceColorAnimation
    {
        public Color4 Color1 = Color4.White;
        public Color4 Color2 = Color4.White;
        public float Speed = 1f;
    }

    [UiLoadable]
    [WattleScriptUserData]
    public class InterfaceModel
    {
        [XmlAttribute("name")] public string Name;
        [XmlAttribute("path")] public string Path;
        [XmlAttribute("x")] public float X;
        [XmlAttribute("y")] public float Y;
        [XmlAttribute("xscale")] public float XScale = 1;
        [XmlAttribute("yscale")] public float YScale = 1;
        [XmlAttribute("xz-plane")] public bool XZPlane;
    }
}
