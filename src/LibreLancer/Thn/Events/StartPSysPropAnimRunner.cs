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
using LibreLancer.Thorn;
namespace LibreLancer
{
    [ThnEventRunner(EventTypes.StartPSysPropAnim)]
    public class StartPSysPropAnimRunner : IThnEventRunner
    {
        public void Process(ThnEvent ev, Cutscene cs)
        {
            var obj = cs.Objects[(string)ev.Targets[0]];
            var ren = ((ParticleEffectRenderer)obj.Object.RenderComponent);
            var props = (LuaTable)ev.Properties["psysprops"];
            var targetSparam = (float)props["sparam"];
            if (ev.Duration == 0)
            {
                ren.SParam = targetSparam;
            }
            else
            {
                cs.Coroutines.Add(new SParamAnimation()
                {
                    Renderer = ren,
                    StartSParam = ren.SParam,
                    EndSParam = targetSparam,
                    Duration = ev.Duration,
                });
            }
        }

        class SParamAnimation : IThnRoutine
        {
            public ParticleEffectRenderer Renderer;
            public float StartSParam;
            public float EndSParam;
            public double Duration;
            double time;
            public bool Run(Cutscene cs, double delta)
            {
                time += delta;
                if (time >= Duration)
                {
                    Renderer.SParam = EndSParam;
                    return false;
                }
                var pct = (float)(time / Duration);
                Renderer.SParam = MathHelper.Lerp(StartSParam, EndSParam, pct);
                return true;
            }
        }
    }
}
