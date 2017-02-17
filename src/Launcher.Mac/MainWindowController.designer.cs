// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Launcher.Mac
{
	[Register ("MainWindowController")]
	partial class MainWindowController
	{
		[Outlet]
		AppKit.NSTextField DirectoryField { get; set; }

		[Outlet]
		AppKit.NSButton MuteMusic { get; set; }

		[Outlet]
		AppKit.NSButton SkipIntroMovies { get; set; }

		[Action ("BrowseAction:")]
		partial void BrowseAction (Foundation.NSObject sender);

		[Action ("LaunchAction:")]
		partial void LaunchAction (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (MuteMusic != null) {
				MuteMusic.Dispose ();
				MuteMusic = null;
			}

			if (SkipIntroMovies != null) {
				SkipIntroMovies.Dispose ();
				SkipIntroMovies = null;
			}

			if (DirectoryField != null) {
				DirectoryField.Dispose ();
				DirectoryField = null;
			}
		}
	}
}
