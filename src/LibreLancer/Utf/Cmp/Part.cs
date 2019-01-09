// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;

using LibreLancer.Utf.Mat;

namespace LibreLancer.Utf.Cmp
{
    public class Part : IDrawable
    {
        private Dictionary<string, ModelFile> models;
        private Dictionary<string, CmpCameraInfo> cameras;
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
        /// <summary>
        /// EDITOR USE ONLY: Changes the construct object for the part
        /// </summary>
        public void UpdateConstruct(AbstractConstruct con)
        {
            construct = con;
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

        private CmpCameraInfo camera;
        bool cameraTried = false;
        public CmpCameraInfo Camera
        {
            get
            {
                if (camera == null && !cameraTried) {
                    cameraTried = true;
                    cameras.TryGetValue(fileName, out camera);
                }
                return camera;
            }
        }

        public string FileName
        {
            get { return fileName; }
        }
        public bool IsBroken()
        {
            return !cameras.ContainsKey(fileName) && !models.ContainsKey(fileName);
        }

        public Part(string objectName, string fileName, Dictionary<string, ModelFile> models, Dictionary<string,CmpCameraInfo> cams, ConstructCollection constructs)
        {
            this.models = models;
            this.cameras = cams;
            this.constructs = constructs;
            this.objectName = objectName;
            this.fileName = fileName;
        }

		public void Initialize(ResourceManager cache)
        {
            if (Camera != null) return;
            Model.Initialize(cache);
        }

        public void Resized()
        {
            if (Camera != null) return;
            Model.Resized();
        }

		public float GetRadius()
		{
			throw new NotImplementedException();
		}

		public void Update(ICamera camera, TimeSpan delta, TimeSpan totalTime)
        {
            if (Camera != null) return;
            Model.Update (camera, delta, totalTime);
        }

		public void Draw(RenderState rstate, Matrix4 world, Lighting light)
        {
            if (Camera != null) return;
            Matrix4 transform = world;
            if (Construct != null) transform = Construct.Transform * world;
            Model.Draw(rstate, transform, light);
        }

		public void DrawBuffer(CommandBuffer buffer, Matrix4 world, ref Lighting light, Material overrideMat = null)
		{
            if (Camera != null) return;
            Matrix4 transform = world;
			if (Construct != null) transform = Construct.Transform * world;
			Model.DrawBuffer(buffer, transform, ref light, overrideMat);
		}

        public void DrawBufferLevel(int level, CommandBuffer buffer, Matrix4 world, ref Lighting light, Material overrideMat = null)
        {
            if (Camera != null) return;
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
			return new Part(ObjectName, fileName, models,cameras, newcol);
		}
    }
}
