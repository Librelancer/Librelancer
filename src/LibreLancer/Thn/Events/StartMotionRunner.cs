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
namespace LibreLancer
{
    [ThnEventRunner(EventTypes.StartMotion)]
    public class StartMotionRunner : IThnEventRunner
    {
        public void Process(ThnEvent ev, Cutscene cs)
        {
            //How to tie this in with .anm files?
            var obj = cs.Objects[(string)ev.Targets[0]];

            if (obj.Object != null && obj.Object.AnimationComponent != null) //Check if object has Cmp animation
            {
                object o;
                bool loop = true;
                if (ev.Properties.TryGetValue("event_flags", out o))
                {
                    if (((int)(float)o) == 3)
                    {
                        loop = false; //Play once?
                    }
                }
                obj.Object.AnimationComponent.StartAnimation((string)ev.Properties["animation"], loop);
            }
        }
    }
}
