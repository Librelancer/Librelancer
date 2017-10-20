using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
namespace LancerEdit.Wpf
{
    class WpfTab : TabItem 
    {
        public LTabPage LTab;

        public WpfTab(LTabPage l)
        {
            LTab = l;
            LTab.Platform = this;
            Header = l.TabName;
            l.TabNameChanged += L_TabNameChanged;
            base.Content = (Xwt.WPFBackend.CustomPanel)Xwt.Toolkit.CurrentEngine.GetNativeWidget(l);
        }

        private void L_TabNameChanged(LTabPage obj)
        {
            Header = obj.TabName;
        }
    }
}
