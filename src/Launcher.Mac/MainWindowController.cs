using System;
using System.IO;
using Foundation;
using AppKit;

namespace Launcher.Mac
{
	public partial class MainWindowController : NSWindowController
	{
		public MainWindowController(IntPtr handle) : base(handle)
		{
		}

		[Export("initWithCoder:")]
		public MainWindowController(NSCoder coder) : base(coder)
		{
		}

		public MainWindowController() : base("MainWindow")
		{
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();
		}

		public new MainWindow Window
		{
			get { return (MainWindow)base.Window; }
		}
		partial void BrowseAction(NSObject sender)
		{
			var dlg = NSOpenPanel.OpenPanel;
			dlg.CanChooseFiles = false;
			dlg.CanChooseDirectories = true;
			dlg.AllowsMultipleSelection = false;
			if (dlg.RunModal() == 1)
			{
				// Nab the first file
				var url = dlg.Urls[0];
				DirectoryField.StringValue = url.AbsoluteUrl.Path;
			}
		}
		partial void LaunchAction(NSObject sender)
		{
			if (Directory.Exists(DirectoryField.StringValue))
			{
				if (!Directory.Exists(Path.Combine(DirectoryField.StringValue, "DATA"))
					|| !Directory.Exists(Path.Combine(DirectoryField.StringValue, "EXE")))
				{
					var alert = new NSAlert()
					{
						AlertStyle = NSAlertStyle.Critical,
						InformativeText = "Not a valid freelancer directory.",
						MessageText = "Error",
					};
					alert.RunModal();
					return;
				}
				MainClass.LaunchPath = DirectoryField.StringValue;
				NSApplication.SharedApplication.Stop(this);
			}
			else
			{
				var alert = new NSAlert()
				{
					AlertStyle = NSAlertStyle.Critical,
					InformativeText = "Path does not exist.",
					MessageText = "Error",
				};
				alert.RunModal();
			}
		}
	}
}
