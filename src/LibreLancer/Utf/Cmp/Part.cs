/* The contents of this file a
 * re subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * The Original Code is Starchart code (http://flapi.sourceforge.net/).
 * Data structure from Freelancer UTF Editor by Cannon & Adoxa, continuing the work of Colin Sanby and Mario 'HCl' Brito (http://the-starport.net)
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;

//using FLCommon;

//using FLApi.Universe;
using LibreLancer.Utf.Anm;

namespace LibreLancer.Utf.Cmp
{
    public class Part : IDrawable
    {
        private Dictionary<string, ModelFile> models;
        private ConstructCollection constructs;

        private string objectName;
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

		public void Update(ICamera camera, TimeSpan delta)
        {
			Model.Update (camera, delta);
        }

		public void Draw(RenderState rstate, Matrix4 world, Lighting light)
        {
            Matrix4 transform = world;
            if (Construct != null) transform = Construct.Transform * world;
            Model.Draw(rstate, transform, light);
        }

		public void DrawBuffer(CommandBuffer buffer, Matrix4 world, Lighting light)
		{
			Matrix4 transform = world;
			if (Construct != null) transform = Construct.Transform * world;
			Model.DrawBuffer(buffer, transform, light);
		}
    }
}
