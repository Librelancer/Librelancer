// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;

using LibreLancer.Utf.Mat;

namespace LibreLancer.Utf.Cmp
{
    public class Part
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
    }
}
