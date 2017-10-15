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
	public class AboutDialog : Xwt.Dialog
	{
		public AboutDialog()
		{
			var box = new VBox();
			box.PackStart(new Label("LancerEdit") { Font = Font.SystemFont.WithStyle(FontStyle.Oblique), TextAlignment = Alignment.Center});
			var v = typeof(AboutDialog).Assembly.GetName().Version;
			box.PackStart(new Label(string.Format(Txt._("Version {0}.{1}"), v.Major, v.Minor, v.Revision)) { TextAlignment = Alignment.Center });
			Title = Txt._("About");
			Content = box;
			Width = 200;
		}
	}
}
