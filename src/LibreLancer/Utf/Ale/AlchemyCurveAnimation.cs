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
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
using System.IO;
namespace LibreLancer.Utf.Ale
{
	public class AlchemyCurveAnimation
	{
		public EasingTypes Type;
		public List<CurveParameterKeyframe> Keyframes;
		public AlchemyCurveAnimation (BinaryReader reader)
		{
			Type = (EasingTypes)reader.ReadByte ();
			int scount = reader.ReadByte ();
			Keyframes = new List<CurveParameterKeyframe> (scount);
			for (int i = 0; i < scount; i++) {
				var cpkf = new CurveParameterKeyframe ();
				cpkf.SParam = reader.ReadSingle ();
				cpkf.Value = reader.ReadSingle ();
				ushort loop = reader.ReadUInt16 ();
				ushort lcnt = reader.ReadUInt16 ();
				if (loop != 0 || lcnt != 0) {
					var l = new List<CurveKeyframe> (lcnt);
					for (int j = 0; j < lcnt; j++) {
						l.Add (new CurveKeyframe () {
							FrameIndex = reader.ReadSingle(),
							Value = reader.ReadSingle(),
							In = reader.ReadSingle(),
							Out = reader.ReadSingle()
						});
					}
					cpkf.Keyframes = l;
				}
				Keyframes.Add (cpkf);
			}
		}
	}
}

