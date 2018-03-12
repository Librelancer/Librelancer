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
using LibreLancer.GameData.Items;
using LibreLancer.Fx;
namespace LibreLancer
{
    public class UpdateSParamComponent : GameComponent
    {
        public UpdateSParamComponent(GameObject parent) : base(parent)
        {
        }
        public override void Update(TimeSpan time)
        {
            float sparam = 0;
            EngineComponent eng;
            if (Parent.Parent != null)
            {
                if ((eng = Parent.Parent.GetComponent<EngineComponent>()) != null)
                {
                    sparam = eng.Speed;
                }
            }
            ((ParticleEffectRenderer)Parent.RenderComponent).SParam = sparam;
        }
    }
}
