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
using Pinta.Docking.Gui;
using Gtk;
namespace LancerEdit.Gtk
{
	public class LancerContent : IViewContent
	{
		public LTabPage Page;
		Widget widget;
		public LancerContent(LTabPage page)
		{
			Page = page;
			_contentName = Page.TabName;
			Page.TabNameChanged += Page_NameChanged;
			widget = MainClass.GetNative(Page);
		}

		public event EventHandler ContentNameChanged;

		public event EventHandler ContentChanged;

		public event EventHandler DirtyChanged;

		public event EventHandler BeforeSave;

		public void Load(string fileName)
		{

		}

		public void LoadNew(System.IO.Stream content, string mimeType)
		{

		}

		public void Save(string fileName)
		{

		}

		public void Save()
		{
			throw new NotImplementedException();
		}

		public void DiscardChanges()
		{
			throw new NotImplementedException();
		}
		string _contentName = "New Document";
		public string ContentName
		{
			get
			{
				return _contentName;
			}
			set
			{
				_contentName = value;
			}
		}
		string _untitledName = "Untitled";
		public string UntitledName
		{
			get
			{
				return _untitledName;
			}
			set
			{
				_untitledName = value;
			}
		}

		public string StockIconId
		{
			get
			{
				return string.Empty;
			}
		}

		public bool IsUntitled
		{
			get
			{
				return false;
			}
		}

		public bool IsViewOnly
		{
			get
			{
				return false;
			}
		}

		public bool IsFile
		{
			get
			{
				return true;
			}
		}

		public bool IsDirty
		{
			get
			{
				return false;
			}
			set
			{

			}
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}
		public object GetContent(Type type)
		{
			return null;
		}

		public bool CanReuseView(string fileName)
		{
			return false;
		}

		public void RedrawContent()
		{
			//?
		}
		IWorkbenchWindow win;
		public IWorkbenchWindow WorkbenchWindow
		{
			get
			{
				return win;
			}
			set
			{
				win = value;
			}
		}

		public Widget Control
		{
			get
			{
				return widget;
			}
		}

		public string TabPageLabel
		{
			get
			{
				return string.Empty;
			}
		}

		void Page_NameChanged(LTabPage obj)
		{
			_contentName = Page.TabName;
		}

		public void Dispose()
		{
			Page.TabNameChanged -= Page_NameChanged;
		}

	}
}
