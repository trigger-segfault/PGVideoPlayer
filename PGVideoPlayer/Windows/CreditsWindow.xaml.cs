using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace PGVideoPlayer.Windows {
	/// <summary>A window to display credits for the program.</summary>
	public partial class CreditsWindow : Window {

		//-----------------------------------------------------------------------------
		// Constructors
		//-----------------------------------------------------------------------------

		/// <summary>Constructs the credits window.</summary>
		public CreditsWindow() {
			InitializeComponent();
		}


		//-----------------------------------------------------------------------------
		// Event Handlers
		//-----------------------------------------------------------------------------

		private void OnRequestNavigate(object sender, RequestNavigateEventArgs e) {
			Process.Start((sender as Hyperlink).NavigateUri.ToString());
		}


		//-----------------------------------------------------------------------------
		// Showing
		//-----------------------------------------------------------------------------

		/// <summary>Shows the credits window.</summary>
		public static void Show(Window owner) {
			CreditsWindow window = new CreditsWindow();
			window.Owner = owner;
			window.ShowDialog();
		}
	}
}
