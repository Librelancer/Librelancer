// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and confiditons defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;

using LibreLancer.Utf.Mat;

namespace LibreLancer.Utf.Cmp
{
    public class Part : IDrawable
    {
        private Dictionary<string, ModelFile> models;
        private ConstructCollection constructs;

        private string objectName;
		public string ObjectName
		{
			get
			{
				return objectName;
			}
		}
        private AbstractConstruct construct;
        public AbstractConstruct Construct
        {
            get
            {
                if (construct == null) construct = constructs.Find(objectName);
                return construct;
            }
        }

        private string fileName;
        private ModelFile model;
        public ModelFile Model
        {
            get
            {
                if (model == null) model = models[fileName];
                return model;
            }
        }
        public string FileName
        {
            get { return fileName; }
        }
        public bool IsBroken()
        {
            return !models.ContainsKey(fileName);
        }

        public Part(string objectName, string fileName, Dictionary<string, ModelFile> models, ConstructCollection constructs)
        {
            this.models = models;
            this.constructs = constructs;
            this.objectName = objectName;
            this.fileName = fileName;
        }

		public void Initialize(ResourceManager cache)
        {
            Model.Initialize(cache);
        }

        public void Resized()
        {
            Model.Resized();
        }

		public float GetRadius()
		{
			throw new NotImplementedException();
		}

		public void Update(ICamera camera, TimeSpan delta, TimeSpan totalTime)
        {
			Model.Update (camera, delta, totalTime);
        }

		public void Draw(RenderState rstate, Matrix4 world, Lighting light)
        {
            Matrix4 transform = world;
            if (Construct != null) transform = Construct.Transform * world;
            Model.Draw(rstate, transform, light);
        }

		public void DrawBuffer(CommandBuffer buffer, Matrix4 world, ref Lighting light, Material overrideMat = null)
		{
			Matrix4 transform = world;
			if (Construct != null) transform = Construct.Transform * world;
			Model.DrawBuffer(buffer, transform, ref light, overrideMat);
		}

        public void DrawBufferLevel(int level, CommandBuffer buffer, Matrix4 world, ref Lighting light, Material overrideMat = null)
        {
            Matrix4 transform = world;
            if (Construct != null) transform = Construct.Transform * world;
            Model.DrawBufferLevel(Model.Levels[level], buffer, transform, ref light, overrideMat);
        }

        public Matrix4 GetTransform(Matrix4 world)
        {
            Matrix4 transform = world;
            if (Construct != null) transform = Construct.Transform * world;
            return transform;
        }

        public Part Clone(ConstructCollection newcol)
		{
			return new Part(ObjectName, fileName, models, newcol);
		}
    }
}
