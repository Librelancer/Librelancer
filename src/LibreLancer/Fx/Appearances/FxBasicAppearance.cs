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
	public class FxBasicAppearance : FxAppearance
	{
		public bool QuadTexture;
		public bool MotionBlur;
		public AlchemyColorAnimation Color;
		public AlchemyFloatAnimation Alpha;
		public AlchemyFloatAnimation HToVAspect;
		public AlchemyFloatAnimation Rotate;
		public string Texture;
		public bool UseCommonAnimation = false;
		public AlchemyFloatAnimation Animation;
		public AlchemyCurveAnimation CommonAnimation;
		public bool FlipHorizontal = false;
		public bool FlipVertical = false;

		public FxBasicAppearance (AlchemyNode ale) : base(ale)
		{
			AleParameter temp;
			if (ale.TryGetParameter ("BasicApp_QuadTexture", out temp)) {
				QuadTexture = (bool)temp.Value;
			}
			if (ale.TryGetParameter ("BasicApp_MotionBlur", out temp)) {
				MotionBlur = (bool)temp.Value;
			}
			if (ale.TryGetParameter ("BasicApp_Color", out temp)) {
				Color = (AlchemyColorAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("BasicApp_Alpha", out temp)) {
				Alpha = (AlchemyFloatAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("BasicApp_HtoVAspect", out temp)) {
				HToVAspect = (AlchemyFloatAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("BasicApp_Rotate", out temp)) {
				Rotate = (AlchemyFloatAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("BasicApp_TexName", out temp)) {
				Texture = (string)temp.Value;
			}
			if (ale.TryGetParameter ("BasicApp_UseCommonTexFrame", out temp)) {
				UseCommonAnimation = (bool)temp.Value;
			}
			if (ale.TryGetParameter ("BasicApp_TexFrame", out temp)) {
				Animation = (AlchemyFloatAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("BasicApp_CommonTexFrame", out temp)) {
				CommonAnimation = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("BasicApp_FlipTexU", out temp)) {
				FlipHorizontal = (bool)temp.Value;
			}
		}

	}
}

