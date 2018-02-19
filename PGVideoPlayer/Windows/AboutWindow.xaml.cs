using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace PGVideoPlayer.Windows {
	/// <summary>The window showing information about the program.</summary>
	public partial class AboutWindow : Window {

		//-----------------------------------------------------------------------------
		// Constructors
		//-----------------------------------------------------------------------------

		/// <summary>Constructs the about window.</summary>
		public AboutWindow() {
			InitializeComponent();

			DateTime buildDate = GetLinkerTime(Assembly.GetExecutingAssembly());
			this.labelVersion.Content = Assembly.GetExecutingAssembly().GetName().Version.ToString() + " Release";
			this.labelBuildDate.Content = buildDate.ToShortDateString() + " (" + buildDate.ToShortTimeString() + ")";
		}


		//-----------------------------------------------------------------------------
		// Event Handlers
		//-----------------------------------------------------------------------------

		private void OnWindowLoaded(object sender, RoutedEventArgs e) {
			clientArea.Height = 222 + textBlockDescription.ActualHeight;
		}


		//-----------------------------------------------------------------------------
		// Internal Methods
		//-----------------------------------------------------------------------------

		/// <summary>Gets the build date of the program.</summary>
		private DateTime GetLinkerTime(Assembly assembly, TimeZoneInfo target = null) {
			var filePath = assembly.Location;
			const int c_PeHeaderOffset = 60;
			const int c_LinkerTimestampOffset = 8;

			var buffer = new byte[2048];

			using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
				stream.Read(buffer, 0, 2048);

			var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
			var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

			var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

			var tz = target ?? TimeZoneInfo.Local;
			var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);

			return localTime;
		}


		//-----------------------------------------------------------------------------
		// Showing
		//-----------------------------------------------------------------------------

		/// <summary>Shows the window.</summary>
		public static void Show(Window owner) {
			AboutWindow window = new AboutWindow();
			window.Owner = owner;
			window.ShowDialog();
		}
	}
}
