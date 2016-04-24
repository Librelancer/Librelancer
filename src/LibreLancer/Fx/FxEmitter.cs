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
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class FxEmitter : FxNode
	{
		public int InitialParticles;
		public AlchemyCurveAnimation Frequency;
		public AlchemyFloatAnimation EmitCount;
		public AlchemyCurveAnimation InitLifeSpan;
		public AlchemyCurveAnimation LODCurve;
		public AlchemyCurveAnimation Pressure;
		public AlchemyCurveAnimation VelocityApproach;

		public FxEmitter (AlchemyNode ale) : base(ale)
		{
			AleParameter temp;
			if (ale.TryGetParameter ("Emitter_InitialPartices", out temp)) {
				InitialParticles = (int)temp.Value;
			}
			if (ale.TryGetParameter ("Emitter_Frequency", out temp)) {
				Frequency = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("Emitter_EmitCount", out temp)) {
				EmitCount = (AlchemyFloatAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("Emitter_InitLifeSpan", out temp)) {
				InitLifeSpan = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("Emitter_LODCurve", out temp)) {
				LODCurve = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("Emitter_Pressure", out temp)) {
				Pressure = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("Emitter_VelocityApproach", out temp)) {
				VelocityApproach = (AlchemyCurveAnimation)temp.Value;
			}
		}
	}
}

