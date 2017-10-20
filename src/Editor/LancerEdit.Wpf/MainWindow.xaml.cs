using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xwt.WPFBackend;
using MahApps.Metro.Controls;
namespace LancerEdit.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, IMainWindow
    {
        static bool first = true;
        AppInstance app;
        public MainWindow()
        {
            InitializeComponent();
            if (!first) return;
            first = false;
            Xwt.Application.InitializeAsGuest(Xwt.ToolkitType.Wpf);
            Xwt.Toolkit.CurrentEngine.RegisterBackend<Xwt.Backends.IButtonBackend, StyledButtonBackend>();
            app = new AppInstance(Xwt.Toolkit.CurrentEngine.WrapWindow(this), this);
            var xmenu = new Xwt.Menu();
            app.ConstructMenu(xmenu);
            var backend = (Xwt.WPFBackend.MenuBackend)Xwt.Toolkit.GetBackend(xmenu);
            foreach (var item in backend.Items)
                wpfMenu.Items.Add(item.Item);

            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            app.OnQuit();
            Program.Application.Shutdown();
        }

        public void AddTab(LTabPage page)
        {
            tabControl.Items.Add(new WpfTab(page));
        }

        public ISortableList ConstructList()
        {
            return new WpfSortableList();
        }

        public void EnsureUIThread(Action work)
        {
            Program.Application.Dispatcher.Invoke(work);
        }

        public LTabPage GetCurrentTab()
        {
           foreach(var item in tabControl.Items)
            {
                var t = (WpfTab)item;
                if (t.IsSelected) return t.LTab;
            }
            return null;
        }

        public Xwt.Drawing.Image GetImage(byte[] data, int width, int height)
        {
            var src = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, data, width * 4);
            return Xwt.Toolkit.CurrentEngine.WrapImage(src);
        }

        public void QueueUIThread(Action work)
        {
            Program.Application.Dispatcher.InvokeAsync(work);
        }

        public void Quit()
        {
            Program.Application.Shutdown();
        }

        public void RemoveTab(LTabPage page)
        {
            var t = (WpfTab)page.Platform;
            Dragablz.TabablzControl.CloseItem(t);
        }
    }
}
