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
using System.Collections.Generic;
using Gtk;

namespace LancerEdit.Gtk
{
	public class GtkSortableList : ISortableList
	{
		TreeView gtkTreeView;
		ListStore listStore;
		int columnCount = 0;

		public event System.Action SelectionChanged;

		public GtkSortableList()
		{
			gtkTreeView = new TreeView();
			gtkTreeView.HeadersClickable = true;
			gtkTreeView.Selection.Mode = SelectionMode.Single; 
			gtkTreeView.Selection.Changed += OnSelectionChanged;
		}

		public void AddColumn(string name)
		{
			int id = columnCount++;
			var column = new TreeViewColumn(name, new CellRendererText(), "text", id);
			column.SortColumnId = id;
			gtkTreeView.AppendColumn(column);
		}

		public void AddRow(params object[] values)
		{
			if (listStore == null)
			{
				var types = new Type[values.Length];
				for (int i = 0; i < types.Length; i++)
					types[i] = values[i].GetType();
				listStore = new ListStore(types);
			}
			listStore.AppendValues(values);
		}

		public object[] GetSelectedRow()
		{
			TreeIter iter;
			if (gtkTreeView.Selection.GetSelected(out iter))
			{
				var values = new object[columnCount];
				for (int i = 0; i < columnCount; i++) values[i] = listStore.GetValue(iter, i);
				return values;
			}
			else
			{
				return null;
			}
		}

		void OnSelectionChanged(object sender, EventArgs e)
		{
			if (SelectionChanged != null)
				SelectionChanged();
		}

		public Xwt.Widget GetWidget()
		{
			ScrolledWindow sw = new ScrolledWindow();
			sw.ShadowType = ShadowType.EtchedIn;
			sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
			gtkTreeView.Model = listStore;
			sw.Add(gtkTreeView);
			sw.ShowAll();
			return Xwt.Toolkit.CurrentEngine.WrapWidget(sw, Xwt.NativeWidgetSizing.DefaultPreferredSize, true);
		}
	}
}
