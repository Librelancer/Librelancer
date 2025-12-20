// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.Schema.Effects;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Render.Materials;
using LibreLancer.Resources;

namespace LibreLancer.Render
{
    public unsafe class BeamsBuffer : IDisposable
    {
        public int MAX_BEAMS = 500;

        VertexBuffer bufferSpear;
        private VertexBuffer bufferBolt;
        private CommandBuffer commands;
        private ResourceManager res;
        private ProjectileMaterial material;

        /*
         * BeamSpear Indices
         * 0 - Quad Center
         * 1 - Quad TL
         * 2 - Quad TR
         * 3 - Quad BL
         * 4 - Quad BR
         * 5 - Mid-top
         * 6 - Mid-bottom
         * 7 - Mid-left
         * 8 - Mid-right
         * 9 - Mid-mid
         * 10 - Tip
         * 11 - Trail
         */
        static readonly ushort[] spearIndices =
        {
            0,1,2,
            0,1,3,
            0,2,4,
            0,3,4,
            5,9,11,
            5,9,10,
            6,9,11,
            6,9,10,
            7,9,11,
            7,9,10,
            8,9,11,
            8,9,10
        };
        /*
         * BeamBolt Indices
         * 0 - Quad Center
         * 1 - Quad Up
         * 2 - Quad Down
         * 3 - Quad Left
         * 4 - Quad Right
         * 5 - Core Center
         * 6 - Core Up
         * 7 - Core Down
         * 8 - Core Left
         * 9 - Core Right
         * 10 - Secondary Center
         * 11 - Secondary Up
         * 12 - Secondary Down
         * 13 - Secondary Left
         * 14 - Secondary Right
         * 15 - Tip
         * 16 - Tail
         */
        static readonly ushort[] boltIndices =
        {
            0,1,2,
            0,1,3,
            0,2,4,
            0,3,4,
            5,9,15,
            6,9,15,
            7,9,15,
            8,9,15,
            //mid to sec-up
            5,6,10,
            6,10,11,
            //mid to sec-down
            5,7,10,
            10,12,7,
            //mid to sec-left
            5,8,10,
            10,13,8,
            //mid to sec-right
            5,9,10,
            10,14,9,
            //sec to tail
            10,11,16,
            10,12,16,
            10,13,16,
            10,14,16
        };
        //TODO: Finish BeamBolt
        static ushort[] ConstructIndices(ushort[] source, int vcount, int count)
        {
            var indices = new ushort[source.Length * count];
            int j = 0;
            for (int i = 0; i < count; i++)
            {
                var vOff = vcount * i;
                for (int k = 0; k < source.Length; k++) indices[j++] = (ushort) (vOff + source[k]);
            }
            return indices;
        }
        public BeamsBuffer(ResourceManager res, RenderContext context)
        {
            var idx1 = ConstructIndices(spearIndices, 12, MAX_BEAMS);
            bufferSpear = new VertexBuffer(context ,typeof(VertexPositionColorTexture), MAX_BEAMS * 12, true);
            var el1 = new ElementBuffer(context, idx1.Length);
            el1.SetData(idx1);
            bufferSpear.SetElementBuffer(el1);
            var idx2 = ConstructIndices(boltIndices, 17, MAX_BEAMS);
            bufferBolt = new VertexBuffer(context, typeof(VertexPositionColorTexture), MAX_BEAMS * 17, true);
            var el2 = new ElementBuffer(context, idx2.Length);
            el2.SetData(idx2);
            bufferBolt.SetElementBuffer(el2);
            material = new ProjectileMaterial(res);
        }

        public void Dispose()
        {
            bufferSpear.Elements.Dispose();
            bufferSpear.Dispose();
            bufferBolt.Elements.Dispose();
            bufferBolt.Dispose();
        }
        private VertexPositionColorTexture* verticesSpear;
        private VertexPositionColorTexture* verticesBolt;
        private int vertexCountSpear = 0;
        private int vertexCountBolt = 0;
        private int boltCount = 0;
        private int spearCount = 0;
        private bool begun = false;
        public void Begin(CommandBuffer commands, ResourceManager res, ICamera cam)
        {
            this.commands = commands;
            this.res = res;
            verticesSpear = (VertexPositionColorTexture*)bufferSpear.BeginStreaming();
            verticesBolt = (VertexPositionColorTexture*) bufferBolt.BeginStreaming();
            if(begun) throw new InvalidOperationException();
            begun = true;
        }

        private static Texture2D code_beam;
        public void End()
        {
            if(!begun) throw new InvalidOperationException();
            begun = false;
            if(code_beam == null || code_beam.IsDisposed)
                code_beam = (Texture2D) res.FindTexture("code_beam");
            bufferSpear.EndStreaming(vertexCountSpear);
            bufferBolt.EndStreaming(vertexCountBolt);
            if (vertexCountSpear > 0)
            {
                commands.AddCommand(material, null, commands.WorldBuffer.Identity, Lighting.Empty,
                    bufferSpear,
                    PrimitiveTypes.TriangleList, 0, 0, spearCount * (spearIndices.Length / 3), SortLayers.OBJECT);
                spearCount = 0;
                vertexCountSpear = 0;
            }
            if (boltCount > 0)
            {
                commands.AddCommand(material, null, commands.WorldBuffer.Identity, Lighting.Empty,
                    bufferBolt,
                    PrimitiveTypes.TriangleList, 0, 0, boltCount * (boltIndices.Length / 3),SortLayers.OBJECT);
                vertexCountBolt = 0;
                boltCount = 0;
            }
        }

        public void AddBeamBolt(Vector3 p, Vector3 normal, BeamBolt bolt, float maxTrailLen)
        {
            //Head
            CoordsFromTexture(bolt.HeadTexture, out var tl, out var tr, out var bl, out var br, out var mid);
            var right = Vector3.Cross(normal, Vector3.UnitY);
            right.Normalize();
            var up = Vector3.Cross(right, normal);
            up.Normalize();
            var hRad = bolt.HeadWidth / 2;
            var cRad = bolt.CoreWidth / 2;
            var secRad = bolt.SecCoreWidth / 2;
            //Quad Center
            verticesBolt[vertexCountBolt++] = new VertexPositionColorTexture(p, bolt.CoreColor, mid);
            //Quad TL
            verticesBolt[vertexCountBolt++] = new VertexPositionColorTexture(p + (up * hRad) - (right * hRad), bolt.OuterColor, tl);
            //Quad TR
            verticesBolt[vertexCountBolt++] = new VertexPositionColorTexture(p + (up * hRad) + (right * hRad), bolt.OuterColor, tr);
            //Quad BL
            verticesBolt[vertexCountBolt++] = new VertexPositionColorTexture(p - (up * hRad) - (right * hRad), bolt.OuterColor, bl);
            //Quad BR
            verticesBolt[vertexCountBolt++] = new VertexPositionColorTexture(p - (up * hRad) + (right * hRad), bolt.OuterColor, br);
            //Tip and trail
            CoordsFromTexture(bolt.TrailTexture, out tl, out tr, out bl, out br, out mid);
            //Mid-Mid
            verticesBolt[vertexCountBolt++] = new VertexPositionColorTexture(p, bolt.CoreColor, bl);
            //Mid-Top
            verticesBolt[vertexCountBolt++] = new VertexPositionColorTexture(p + (up * cRad), bolt.CoreColor, br);
            //Mid-Bottom
            verticesBolt[vertexCountBolt++] = new VertexPositionColorTexture(p - (up * cRad), bolt.CoreColor, br);
            //Mid-Left
            verticesBolt[vertexCountBolt++] = new VertexPositionColorTexture(p -(right * cRad), bolt.CoreColor, br);
            //Mid-Right
            verticesBolt[vertexCountBolt++] = new VertexPositionColorTexture(p + (right * cRad), bolt.CoreColor, br);

            //sec
            var coreLen = Math.Min(maxTrailLen, bolt.CoreLength);
            var p2 = p - (normal * coreLen);
            //Sec-Mid
            verticesBolt[vertexCountBolt++] = new VertexPositionColorTexture(p2, bolt.SecCoreColor, bl);
            //Sec-Top
            verticesBolt[vertexCountBolt++] = new VertexPositionColorTexture(p2 + (up * secRad), bolt.SecOuterColor, br);
            //Sec-Bottom
            verticesBolt[vertexCountBolt++] = new VertexPositionColorTexture(p2 - (up * secRad), bolt.SecOuterColor, br);
            //Sec-Left
            verticesBolt[vertexCountBolt++] = new VertexPositionColorTexture(p2 -(right * secRad), bolt.SecOuterColor, br);
            //Sec-Right
            verticesBolt[vertexCountBolt++] = new VertexPositionColorTexture(p2 + (right * secRad), bolt.SecOuterColor, br);

            //Tip
            verticesBolt[vertexCountBolt++] =
                new VertexPositionColorTexture(p + (normal * bolt.TipLength), bolt.TipColor, tr);
            //Trail
            var tailLength = Math.Min(maxTrailLen, bolt.TailLength + bolt.CoreLength);
            verticesBolt[vertexCountBolt++] =
                new VertexPositionColorTexture(p - (normal * tailLength), bolt.TailColor, tr);
            boltCount++;
        }
        public void AddBeamSpear(Vector3 p, Vector3 normal, BeamSpear spear, float maxTrailLen)
        {
            if(!begun) throw new InvalidOperationException();
            //Head
            CoordsFromTexture(spear.HeadTexture, out var tl, out var tr, out var bl, out var br, out var mid);
            var right = Vector3.Cross(normal, Vector3.UnitY);
            right.Normalize();
            var up = Vector3.Cross(right, normal);
            up.Normalize();
            var hRad = spear.HeadWidth / 2;
            var cRad = spear.CoreWidth / 2;
            //Quad Center
            verticesSpear[vertexCountSpear++] = new VertexPositionColorTexture(p, spear.CoreColor, mid);
            //Quad TL
            verticesSpear[vertexCountSpear++] = new VertexPositionColorTexture(p + (up * hRad) - (right * hRad), spear.OuterColor, tl);
            //Quad TR
            verticesSpear[vertexCountSpear++] = new VertexPositionColorTexture(p + (up * hRad) + (right * hRad), spear.OuterColor, tr);
            //Quad BL
            verticesSpear[vertexCountSpear++] = new VertexPositionColorTexture(p - (up * hRad) - (right * hRad), spear.OuterColor, bl);
            //Quad BR
            verticesSpear[vertexCountSpear++] = new VertexPositionColorTexture(p - (up * hRad) + (right * hRad), spear.OuterColor, br);
            //Tip and trail
            CoordsFromTexture(spear.TrailTexture, out tl, out tr, out bl, out br, out mid);
            //Mid-Top
            verticesSpear[vertexCountSpear++] = new VertexPositionColorTexture(p + (up * cRad), spear.CoreColor, br);
            //Mid-Bottom
            verticesSpear[vertexCountSpear++] = new VertexPositionColorTexture(p - (up * cRad), spear.CoreColor, br);
            //Mid-Left
            verticesSpear[vertexCountSpear++] = new VertexPositionColorTexture(p -(right * cRad), spear.CoreColor, br);
            //Mid-Right
            verticesSpear[vertexCountSpear++] = new VertexPositionColorTexture(p + (right * cRad), spear.CoreColor, br);
            //Mid-Mid
            verticesSpear[vertexCountSpear++] = new VertexPositionColorTexture(p, spear.CoreColor, bl);
            //Tip
            verticesSpear[vertexCountSpear++] =
                new VertexPositionColorTexture(p + (normal * spear.TipLength), spear.TipColor, tr);
            //Trail
            var tailLength = Math.Min(maxTrailLen, spear.TailLength);
            verticesSpear[vertexCountSpear++] =
                new VertexPositionColorTexture(p - (normal * tailLength), spear.TailColor, tr);
            spearCount++;
        }

        static void CoordsFromTexture(string tex, out Vector2 tl, out Vector2 tr, out Vector2 bl, out Vector2 br, out Vector2 mid)
        {
            switch (tex)
            {
                case "ball":
                    tl = new Vector2(0.5f, 0.5f);
                    tr = new Vector2(1, 0.5f);
                    bl = new Vector2(0.5f, 0f);
                    br = new Vector2(1f, 0f);
                    mid = new Vector2(0.75f, 0.25f);
                    break;
                case "star":
                    tl = new Vector2(0.5f, 1f);
                    tr = new Vector2(1f, 1f);
                    bl = new Vector2(0.5f, 0.5f);
                    br = new Vector2(1f, 0.5f);
                    mid = new Vector2(0.75f, 0.75f);
                    break;
                case "wide":
                    tl = new Vector2(0f, 1f);
                    tr = new Vector2(0.25f, 1f);
                    bl = new Vector2(0f, 0.5f);
                    br = new Vector2(0.25f, 0.45f);
                    mid = new Vector2(0.125f, 0.25f);
                    break;
                case "thin":
                    tl =new Vector2(0f, 0.55f);
                    tr = new Vector2(0.25f, 0.55f);
                    bl = new Vector2(0, 0f);
                    br = new Vector2(0.25f, 0f);
                    mid = new Vector2(0.125f, 0.75f);
                    break;
                default:
                    throw new Exception("bad texture");
            }
        }
    }
}
