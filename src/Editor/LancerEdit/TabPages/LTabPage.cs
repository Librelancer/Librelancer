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
namespace LancerEdit
{
	public class LTabPage : Xwt.VBox
	{
		public object Platform;
		string tabName;

		public string TabName
		{
			get
			{
				return tabName;
			}
			set
			{
				tabName = value;
				if (TabNameChanged != null) 
					TabNameChanged(this);
			}
		}

		public event Action<LTabPage> TabNameChanged;

		public LTabPage(string name)
		{
			tabName = name;
		}

		public virtual bool CloseRequest()
		{
			return true;
		}

		public virtual void DoSave()
		{
		}

		public virtual void DoModelView()
		{
		}
	}
}
