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
using System.IO;
using Xwt;
using L = LibreLancer;
namespace LancerEdit
{
	public class TextureViewer : Window
	{
		Xwt.Drawing.Image image;
		FlexibleImageView view;
		CheckBox stretch;
		Label zoomLabel;
		HSlider zoomSlider;

		public bool Success = true;
		public TextureViewer(AppInstance app, byte[] data)
		{
			L.Texture2D tex = null;
			try
			{
				using (var stream = new MemoryStream(data))
				{
					tex = L.ImageLib.Generic.FromStream(stream);
				}
			}
			catch (Exception)
			{
				MessageDialog.ShowError(Txt._("Could not open image"));
				Success = false;
				return;
			}

			//Decompress
			var target = new L.RenderTarget2D(tex.Width, tex.Height);
			target.BindFramebuffer();
			app.RenderState.SetViewport(0, 0, tex.Width, tex.Height);
			app.RenderState.ClearColor = L.Color4.Transparent;
			app.RenderState.ClearAll();
			app.Render2D.Start(tex.Width, tex.Height);
			app.Render2D.DrawImageStretched(tex, new L.Rectangle(0, 0, tex.Width, tex.Height), L.Color4.White);
			app.Render2D.Finish();
			L.RenderTarget2D.ClearBinding();
			//Get data
			var pixels = new byte[tex.Width * tex.Height * 4];
			target.GetData(pixels);
			for (int i = 0; i < pixels.Length; i += 4)
			{
				var r = pixels[i];
				pixels[i] = pixels[i + 2];
				pixels[i + 2] = r;
			}
			target.Dispose();
			tex.Dispose();

			image = app.MainWindow.GetImage(pixels, tex.Width, tex.Height);
			Width = tex.Width;
			Height = tex.Height;

			var vbox = new VBox();
			view = new FlexibleImageView() { Image = image };
			view.BackgroundColor = Xwt.Drawing.Colors.CornflowerBlue;
			vbox.PackStart(view,true,true);

			var hbox = new HBox();
			stretch = new CheckBox() { State = CheckBoxState.On, Label = "Stretch" };
			stretch.Clicked += Stretch_Clicked;
			hbox.PackStart(stretch);


			zoomLabel = new Label("100%");
			zoomLabel.MinWidth = 70;
			zoomSlider = new HSlider();
			zoomSlider.MinimumValue = 0.1;
			zoomSlider.MaximumValue = 16;
			zoomSlider.Value = 1;
			zoomSlider.StepIncrement = 0.1;
			zoomSlider.SnapToTicks = true;
			zoomSlider.MinWidth = 200;
			zoomSlider.ValueChanged += ZoomSlider_ValueChanged;
			hbox.PackEnd(zoomLabel);
			hbox.PackEnd(zoomSlider);
			hbox.PackEnd(new Label("Zoom: "));


			vbox.PackStart(hbox);
			Content = vbox;
			Title = "Texture Viewer";
		}

		void ZoomSlider_ValueChanged(object sender, EventArgs e)
		{
			zoomLabel.Text = string.Format("{0}%", (int)(zoomSlider.Value * 100));
			view.Scale = (float)zoomSlider.Value;
		}

		void Stretch_Clicked(object sender, EventArgs e)
		{
			view.Mode = stretch.State == CheckBoxState.On ? ImageViewMode.Stretch : ImageViewMode.Zoom;
		}

		protected override void Dispose(bool disposing)
		{
			image.Dispose();
			base.Dispose(disposing);
		}
	}
}
