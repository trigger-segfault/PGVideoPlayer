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
using System.Windows.Shapes;
using PGVideoPlayer.Control;
using PGVideoPlayer.Util;

namespace PGVideoPlayer {
	/// <summary>
	/// Interaction logic for BookmarkWindow.xaml
	/// </summary>
	public partial class BookmarkWindow : Window {

		//-----------------------------------------------------------------------------
		// Members
		//-----------------------------------------------------------------------------

		private static Size lastSize;

		private bool supressEvents;


		//-----------------------------------------------------------------------------
		// Constructor
		//-----------------------------------------------------------------------------

		private BookmarkWindow(IEnumerable<ListViewItem> bookmarkItems) {
			supressEvents = true;
			InitializeComponent();
			listViewBookmarks.ItemsSource = bookmarkItems;

			UpdateControls();
			supressEvents = false;
			if (lastSize != new Size(0, 0)) {
				Width  = lastSize.Width;
				Height = lastSize.Height;
			}
		}


		//-----------------------------------------------------------------------------
		// Event Handlers
		//-----------------------------------------------------------------------------
		
		private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
			lastSize = new Size(Width, Height);
		}

		private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e) {
			var directlyOver = Mouse.DirectlyOver as DependencyObject;
			if (WpfHelper.ContainsParent<TextBox>(directlyOver))
				return;
			// Make text boxes lose focus on click away
			FocusManager.SetFocusedElement(this, this);
		}
		
		private void OnBookmarkSelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (supressEvents) return;
			if (listViewBookmarks.SelectedIndex == -1) return;
			PGControl.SelectBookmark(listViewBookmarks.SelectedIndex);
		}

		private void OnBookmarkItemPreviewMouseDown(object sender, MouseButtonEventArgs e) {
			if (supressEvents || e.ChangedButton != MouseButton.Left) return;

			// Allow reselecting the same index
			int index = listViewBookmarks.Items.IndexOf(sender);
			if (index == listViewBookmarks.SelectedIndex)
				PGControl.SelectBookmark(index);
		}


		//-----------------------------------------------------------------------------
		// Public Methods
		//-----------------------------------------------------------------------------

		public void UpdateControls() {
			buttonBookmarkTimes.IsChecked = PGControl.ShowBookmarkTimes;
		}


		//-----------------------------------------------------------------------------
		// Showing
		//-----------------------------------------------------------------------------

		public static BookmarkWindow Show(Window owner, IEnumerable<ListViewItem> bookmarkItems) {
			BookmarkWindow window = new BookmarkWindow(bookmarkItems);
			window.Owner = owner;
			window.Show();
			return window;
		}


		//-----------------------------------------------------------------------------
		// Properties
		//-----------------------------------------------------------------------------

		public ListView ListViewBookmarks {
			get { return listViewBookmarks; }
		}
	}
}
