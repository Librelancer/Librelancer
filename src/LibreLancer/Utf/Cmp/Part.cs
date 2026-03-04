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

        public string ObjectName { get; }

        public AbstractConstruct? Construct
        {
            get
            {
                if (field == null) field = constructs.Find(ObjectName);
                return field;
            }
        }

        private string fileName;

        public ModelFile? Model
        {
            get
            {
                field ??= models[fileName];
                return field;
            }
        }

        private bool cameraTried = false;
        public CmpCameraInfo? Camera
        {
            get
            {
                if (field != null || cameraTried)
                {
                    return field;
                }

                cameraTried = true;
                cameras.TryGetValue(fileName, out field);

                return field;
            }
        }

        public string FileName => fileName;

        public bool IsBroken()
        {
            return !cameras.ContainsKey(fileName) && !models.ContainsKey(fileName);
        }

        public Part(string objectName, string fileName, Dictionary<string, ModelFile> models, Dictionary<string,CmpCameraInfo> cams, ConstructCollection constructs)
        {
            this.models = models;
            this.cameras = cams;
            this.constructs = constructs;
            this.ObjectName = objectName;
            this.fileName = fileName;
        }
    }
}
