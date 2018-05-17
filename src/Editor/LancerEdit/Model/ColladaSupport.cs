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
using System.Linq;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;
using CL = Collada141;
using LibreLancer;
using LibreLancer.Vertices;
using LibreLancer.Utf.Vms;
namespace LancerEdit
{
    public class ColladaObject
    {
        public string Name;
        public string ID;
        public Matrix4 Transform;
        public ColladaGeometry Geometry;
        public ColladaSpline Spline;
        public List<ColladaObject> Children;
    }
    public struct ColladaDrawcall
    {
        public int Start;
        public int TriCount;
        public string Material;
    }
    public class ColladaSpline
    {
        
    }
    public class ColladaGeometry 
    {
        public float Radius = float.MinValue;
        public ushort[] Indices;
        public VertexPositionNormalDiffuseTextureTwo[] Vertices;
        public ColladaDrawcall[] Drawcalls;
        public D3DFVF FVF;
    }
    public class ColladaSupport
    {
        static XmlSerializer xml = new XmlSerializer(typeof(CL.COLLADA));
        public static List<ColladaObject> Parse(string filename)
        {
            CL.COLLADA dae;
            using(var reader =new StreamReader(filename)) {
                dae = (CL.COLLADA)xml.Deserialize(reader);
            }
            //Get libraries
            var geometrylib = dae.Items.OfType<CL.library_geometries>().First();
            var scenelib = dae.Items.OfType<CL.library_visual_scenes>().First();
            //Get main scene
            var urlscn = CheckURI(dae.scene.instance_visual_scene.url);
            var scene = scenelib.visual_scene.Where((x) => x.id == urlscn).First();
            //Walk through objects
            var up = dae.asset.up_axis;
            var objs = new List<ColladaObject>();
            foreach(var node in scene.node) {
                objs.Add(ProcessNode(up, geometrylib, node));
            }
            return objs;
        }

        static ColladaObject ProcessNode(CL.UpAxisType up, CL.library_geometries geom, CL.node n)
        {
            var obj = new ColladaObject();
            obj.Name = n.name;
            obj.ID = n.id;
            if(n.instance_geometry != null && n.instance_geometry.Length > 0) {
                //Geometry object
                if (n.instance_geometry.Length != 1) throw new Exception("How to handle multiple geometries/node?");
                var uri = CheckURI(n.instance_geometry[0].url);
                var g = geom.geometry.Where((x) => x.id == uri).First();
                if(g.Item is CL.mesh) {
                    obj.Geometry = GetGeometry(up, g);
                } else if (g.Item is CL.spline) {
                    obj.Spline = GetSpline(up, g);
                }
            }
            if(n.Items.OfType<CL.matrix>().Any()) {
                var tr = n.Items.OfType<CL.matrix>().First();
                obj.Transform = GetMatrix(tr.Text);
            } else {
                //TODO: Non-matrix transforms
            }
            if(n.node1 != null && n.node1.Length > 0) {
                obj.Children = new List<ColladaObject>();
                foreach(var node in n.node1) {
                    obj.Children.Add(ProcessNode(up, geom, node));
                }
            }
            return obj;
        }
        static ColladaSpline GetSpline(CL.UpAxisType up, CL.geometry geo)
        {
            var spline = geo.Item as CL.spline;
            if (spline == null) return null;
            var conv = new ColladaSpline();
            Dictionary<string, float[]> arrays = new Dictionary<string, float[]>();
            Dictionary<string, GeometrySource> sources = new Dictionary<string, GeometrySource>();
            //Get arrays
            foreach (var acc in spline.source){
                var arr = acc.Item as CL.float_array;
                arrays.Add(arr.id, FloatArray(arr.Text));
            }
            //Accessors
            foreach (var acc in spline.source) {
                sources.Add(acc.id, new GeometrySource(acc, arrays));
            }
            //Process spline

            /*foreach(var input in spline.control_vertices.input) {
                switch(input.semantic) {
                    
                }
            }
            spline.*/
            return conv;
        }
        const string SEM_VERTEX = "VERTEX";
        const string SEM_POSITION = "POSITION";
        const string SEM_COLOR = "COLOR";
        const string SEM_NORMAL = "NORMAL";
        const string SEM_TEXCOORD = "TEXCOORD";
        static ColladaGeometry GetGeometry(CL.UpAxisType up, CL.geometry geo)
        {
            var conv = new ColladaGeometry() { FVF = D3DFVF.XYZ };
            var msh = geo.Item as CL.mesh;
            if (msh == null) return null;
            List<VertexPositionNormalDiffuseTextureTwo> vertices = new List<VertexPositionNormalDiffuseTextureTwo>();
            List<ushort> indices = new List<ushort>();
            List<ColladaDrawcall> drawcalls = new List<ColladaDrawcall>();
            Dictionary<string, GeometrySource> sources = new Dictionary<string, GeometrySource>();
            Dictionary<string, float[]> arrays = new Dictionary<string, float[]>();
            Dictionary<string, GeometrySource> verticesRefs = new Dictionary<string, GeometrySource>();
            //Get arrays
            foreach(var acc in msh.source) {
                var arr = acc.Item as CL.float_array;
                arrays.Add(arr.id, FloatArray(arr.Text));
            }
            //Accessors
            foreach(var acc in msh.source) {
                sources.Add(acc.id, new GeometrySource(acc, arrays));
            }
            //Process geometry
            if (msh.Items.Where(x => x is CL.triangles || x is CL.polylist).Count() != msh.Items.Length)
                throw new Exception("Non-triangle geometry");
            foreach(var item in msh.Items.Where(x => x is CL.triangles || x is CL.polylist)) {
                CL.InputLocalOffset[] inputs;
                int[] pRefs;
                int triangleCount;
                string material;
                if(item is CL.triangles) {
                    var triangles = (CL.triangles)item;
                    pRefs = IntArray(triangles.p);
                    inputs = triangles.input;
                    triangleCount = (int)(triangles.count * 3);
                    material = triangles.material;
                } else {
                    var plist = (CL.polylist)item;
                    pRefs = IntArray(plist.p);
                    foreach(var c in IntArray(plist.vcount)) {
                        if(c != 3) {
                            throw new Exception("Polylist: non-triangle geometry");
                        }
                    }
                    material = plist.material;
                    inputs = plist.input;
                    triangleCount = (int)(plist.count * 3);
                }
                int pStride = 0;
                foreach (var input in inputs)
                    pStride = Math.Max((int)input.offset, pStride);
                pStride++;
                GeometrySource sourceXYZ = null; int offXYZ = int.MinValue;
                GeometrySource sourceNORMAL = null; int offNORMAL = int.MinValue;
                GeometrySource sourceCOLOR = null; int offCOLOR = int.MinValue;
                GeometrySource sourceUV1 = null; int offUV1 = int.MinValue;
                GeometrySource sourceUV2 = null; int offUV2 = int.MinValue;
                int texCount = 0;
                int startIdx = indices.Count;
                foreach(var input in inputs) {
                    switch(input.semantic) {
                        case SEM_VERTEX:
                            if (CheckURI(input.source) != msh.vertices.id)
                                throw new Exception("VERTEX doesn't match mesh vertices");
                            foreach(var ip2 in msh.vertices.input) {
                                switch(ip2.semantic) {
                                    case SEM_POSITION:
                                        offXYZ = (int)input.offset;
                                        sourceXYZ = sources[CheckURI(ip2.source)];
                                        break;
                                    case SEM_NORMAL:
                                        offNORMAL = (int)input.offset;
                                        sourceNORMAL = sources[CheckURI(ip2.source)];
                                        conv.FVF |= D3DFVF.NORMAL;
                                        break;
                                    case SEM_COLOR:
                                        offCOLOR = (int)input.offset;
                                        sourceCOLOR = sources[CheckURI(ip2.source)];
                                        conv.FVF |= D3DFVF.DIFFUSE;
                                        break;
                                    case SEM_TEXCOORD:
                                        if (texCount == 2) throw new Exception("Too many texcoords!");
                                        if (texCount == 1)
                                        {
                                            offUV2 = (int)input.offset;
                                            sourceUV2 = sources[CheckURI(ip2.source)];
                                            conv.FVF &= ~D3DFVF.TEX1;
                                            conv.FVF |= D3DFVF.TEX2;
                                        }
                                        else
                                        {
                                            offUV1 = (int)input.offset;
                                            sourceUV1 = sources[CheckURI(ip2.source)];
                                            if ((conv.FVF & D3DFVF.TEX2) != D3DFVF.TEX2) conv.FVF |= D3DFVF.TEX1;
                                        }
                                        texCount++;
                                        break;
                                }
                            }
                            break;
                        case SEM_POSITION:
                            offXYZ = (int)input.offset;
                            sourceXYZ = sources[CheckURI(input.source)];
                            break;
                        case SEM_NORMAL:
                            offNORMAL = (int)input.offset;
                            sourceNORMAL = sources[CheckURI(input.source)];
                            conv.FVF |= D3DFVF.NORMAL;
                            break;
                        case SEM_COLOR:
                            offCOLOR = (int)input.offset;
                            sourceCOLOR = sources[CheckURI(input.source)];
                            conv.FVF |= D3DFVF.DIFFUSE;
                            break;
                        case SEM_TEXCOORD:
                            if (texCount == 2) throw new Exception("Too many texcoords!");
                            if(texCount == 1) {
                                offUV2 = (int)input.offset;
                                sourceUV2 = sources[CheckURI(input.source)];
                                conv.FVF &= ~D3DFVF.TEX1;
                                conv.FVF |= D3DFVF.TEX2;
                            } else {
                                offUV1 = (int)input.offset;
                                sourceUV1 = sources[CheckURI(input.source)];
                                if ((conv.FVF & D3DFVF.TEX2) != D3DFVF.TEX2) conv.FVF |= D3DFVF.TEX1;
                            }
                            texCount++;
                            break;
                    }
                }
                for (int i = 0; i <  triangleCount; i++) {
                    int idx = i * pStride;
                    var vert = new VertexPositionNormalDiffuseTextureTwo(
                        VecAxis(up, sourceXYZ.GetXYZ(pRefs[idx + offXYZ])),
                        offNORMAL == int.MinValue ? Vector3.Zero : VecAxis(up, sourceNORMAL.GetXYZ(pRefs[idx + offNORMAL])),
                        offCOLOR == int.MinValue ? Color4.White : sourceCOLOR.GetColor(pRefs[idx + offCOLOR]),
                        offUV1 == int.MinValue ? Vector2.Zero : sourceUV1.GetUV(pRefs[idx + offUV1]),
                        offUV2 == int.MinValue ? Vector2.Zero : sourceUV2.GetUV(pRefs[idx + offUV2])
                    );
                    conv.Radius = Math.Max(conv.Radius, vert.Position.LengthSquared);
                    var vertIdx = vertices.IndexOf(vert);
                    if (indices.Count >= ushort.MaxValue)
                        throw new Exception("Too many indices");
                    if(vertIdx == -1) {
                        if (vertices.Count + 1 >= ushort.MaxValue)
                            throw new Exception("Overflow");
                        indices.Add((ushort)vertices.Count);
                        vertices.Add(vert);
                    } else {
                        indices.Add((ushort)vertIdx);
                    }
                }
                drawcalls.Add(new ColladaDrawcall() { 
                    Start = startIdx, 
                    TriCount = (indices.Count - startIdx) / 3,
                    Material = string.IsNullOrEmpty(material) ? "NullMaterial" : material
                });
            }
            conv.Indices = indices.ToArray();
            conv.Vertices = vertices.ToArray();
            conv.Drawcalls = drawcalls.ToArray();
            conv.Radius = (float)Math.Sqrt(conv.Radius);
            return conv;
        }

        class GeometrySource
        {
            float[] array;
            int stride;
            int offset;
            public int Count { get; private set; }
            public GeometrySource(CL.source src, Dictionary<string, float[]> arrays)
            {
                var acc = src.technique_common.accessor;
                array = arrays[CheckURI(acc.source)];
                stride = (int)acc.stride;
                offset = (int)acc.offset;
                Count = (int)acc.count;
            }
            public Color4 GetColor(int index)
            {
                var i = offset + (index * stride);
                if (stride == 4)
                    return new Color4(
                        array[i],
                        array[i + 1],
                        array[i + 2],
                        array[i + 3]
                    );
                else if (stride == 3)
                    return new Color4(
                        array[i],
                        array[i + 1],
                        array[i + 2],
                        1
                    );
                else
                    throw new Exception("Color Unhandled stride " + stride);
            }
            public Vector3 GetXYZ(int index)
            {
                if (stride != 3) throw new Exception("Vec3 Unhandled stride " + stride);
                var i = offset + (index * stride);
                return new Vector3(
                    array[i],
                    array[i + 1],
                    array[i + 2]
                );
            }
            public Vector2 GetUV(int index)
            {
                if (stride != 2) throw new Exception("Vec2 Unhandled stride " + stride);
                var i = offset + (index * stride);
                return new Vector2(
                    array[i],
                    array[i + 1]
                );
            }
        }

        static Matrix4 GetMatrix(string text)
        {
            var floats = FloatArray(text);
            if (floats.Length == 16)
                return new Matrix4(
                    floats[0], floats[1], floats[2], floats[3],
                    floats[4], floats[5], floats[6], floats[7],
                    floats[8], floats[9], floats[10], floats[11],
                    floats[12], floats[13], floats[14], floats[15]
                );
            else if (floats.Length == 9)
                return new Matrix4(
                    floats[0], floats[1], floats[2], 0,
                    floats[3], floats[4], floats[5], 0,
                    floats[6], floats[7], floats[8], 0,
                    0, 0, 0, 1
                );
            else
                throw new Exception("Invalid Matrix: " + floats.Length + " elements");
        }
        static Vector3 VecAxis(CL.UpAxisType ax, Vector3 vec)
        {
            if (ax == CL.UpAxisType.Z_UP)
                return vec.Xzy * new Vector3(1, 1, -1);
            else if (ax == CL.UpAxisType.Y_UP)
                return vec;
            else
                throw new Exception("X_UP Unsupported");
        }
        static string CheckURI(string s)
        {
            if (s[0] != '#') throw new Exception("Don't support external dae refs");
            return s.Substring(1);
        }
        static string[] Tokens(string s) => s.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        static float[] FloatArray(string s) => Tokens(s).Select((x) => float.Parse(x, CultureInfo.InvariantCulture)).ToArray();
        static int[] IntArray(string s) => Tokens(s).Select((x) => int.Parse(x, CultureInfo.InvariantCulture)).ToArray();
        static float[] FloatArray(dynamic arr) => FloatArray(arr.Text);
    }
}