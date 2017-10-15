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
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
using Xwt;
using Xwt.Drawing;
namespace LancerEdit
{
	public enum ImageViewMode
	{
		Stretch,
		Zoom
	}
	public class FlexibleImageView : Canvas
	{
		public Image Image;
		ImageViewMode _mode = ImageViewMode.Stretch;
		public ImageViewMode Mode
		{
			get { return _mode; }
			set { _mode = value; QueueDraw(); }
		}
		float _scale = 1f;
		public float Scale
		{
			get { return _scale; }
			set { _scale = value; QueueDraw(); }
		}
		public FlexibleImageView()
		{
			
		}

		public int CheckerboardSize = 8;
		public bool UseCheckerboard = true;

		protected override void OnDraw(Context ctx, Rectangle dirtyRect)
		{
			ctx.Rectangle(Bounds);
			if (UseCheckerboard)
			{
				ctx.SetColor(Colors.LightGray);
				ctx.Fill();
				ctx.SetColor(Colors.DimGray);
				for (int x = 0; x < (Bounds.Width + Bounds.Width % (2 * CheckerboardSize)); x += (2 * CheckerboardSize))
				{
					for (int y = 0; y < (Bounds.Height + Bounds.Height % CheckerboardSize); y += CheckerboardSize)
					{
						int offset = (y % (2 * CheckerboardSize));
						ctx.Rectangle(new Rectangle(x + offset, y, CheckerboardSize, CheckerboardSize));
						ctx.Fill();
					}
				}
			}
			else
			{
				ctx.SetColor(BackgroundColor);
				ctx.Fill();
			}
			if (Image == null) return;
			float scale = Scale;
			if (Mode == ImageViewMode.Stretch) {
				scale = (float)Math.Min(Bounds.Width / Image.Width, Bounds.Height / Image.Height);
			}

			var scaleWidth = (int)(Image.Width * scale);
			var scaleHeight = (int)(Image.Height * scale);
			ctx.DrawImage(
				Image,
				new Rectangle(((int)Bounds.Width - scaleWidth) / 2, ((int)Bounds.Height - scaleHeight) / 2, scaleWidth, scaleHeight)
			);            
		}
	}
}
