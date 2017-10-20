using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.ComponentModel;

namespace LancerEdit.Wpf
{
    class WpfSortableList : ISortableList
    {
        ListView view;
        GridView grid;
        ScrollViewer scroll;
        int idx = 0;
        public WpfSortableList()
        {
            grid = new GridView();
            view = new ListView();
            view.View = grid;
            view.SelectionChanged += View_SelectionChanged;
            scroll = new ScrollViewer();
            scroll.Content = view;
            scroll.CanContentScroll = true;
        }

        private void View_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectionChanged != null)
                SelectionChanged();
        }

        public event Action SelectionChanged;

        GridViewColumnHeader listViewSortCol;
        SortAdorner listViewSortAdorner = null;

        public void AddColumn(string name)
        {
            var column = new GridViewColumn();
            var h = new GridViewColumnHeader() { Content = name };
            h.Tag = idx;
            column.Header = h;
            h.Click += GridViewColumnHeader_Click;
            var b = new Binding();
            b.Path = new System.Windows.PropertyPath("[" + idx++ + "]");
            column.DisplayMemberBinding = b;
            grid.Columns.Add(column);
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader column = (sender as GridViewColumnHeader);
            var sortBy = (int)column.Tag;
            if (listViewSortCol != null)
            {
                AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
                view.Items.SortDescriptions.Clear();
            }
            ListSortDirection newDir = ListSortDirection.Ascending;
            if (listViewSortCol == column && listViewSortAdorner.Direction == newDir)
                newDir = ListSortDirection.Descending;
            listViewSortCol = column;
            listViewSortAdorner = new SortAdorner(listViewSortCol, newDir);
            AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
            view.Items.SortDescriptions.Add(new SortDescription("[" + sortBy + "]", newDir));
        }

        public void AddRow(params object[] values)
        {
            view.Items.Add(values);
        }

        public object[] GetSelectedRow()
        {
            return (object[])view.SelectedItem;
        }

        public Xwt.Widget GetWidget()
        {
            return Xwt.Toolkit.CurrentEngine.WrapWidget(scroll, Xwt.NativeWidgetSizing.External, true);
        }

        public class SortAdorner : Adorner
        {
            private static Geometry ascGeometry =
                    Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");

            private static Geometry descGeometry =
                    Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");

            public ListSortDirection Direction { get; private set; }

            public SortAdorner(UIElement element, ListSortDirection dir)
                    : base(element)
            {
                this.Direction = dir;
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                base.OnRender(drawingContext);

                if (AdornedElement.RenderSize.Width < 20)
                    return;

                TranslateTransform transform = new TranslateTransform
                        (
                                AdornedElement.RenderSize.Width - 15,
                                (AdornedElement.RenderSize.Height - 5) / 2
                        );
                drawingContext.PushTransform(transform);

                Geometry geometry = ascGeometry;
                if (this.Direction == ListSortDirection.Descending)
                    geometry = descGeometry;
                drawingContext.DrawGeometry(Brushes.Black, null, geometry);

                drawingContext.Pop();
            }
        }

    }
}
