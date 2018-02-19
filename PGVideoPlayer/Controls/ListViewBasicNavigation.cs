using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PGVideoPlayer.Controls {
	public class ListViewBasicNavigation : ListView {

		static ListViewBasicNavigation() {
			DefaultStyleKeyProperty.OverrideMetadata(typeof(ListViewBasicNavigation),
					   new FrameworkPropertyMetadata(typeof(ListViewBasicNavigation)));
		}

		private bool isInTextBox = false;

		protected override void OnPreviewKeyDown(KeyEventArgs e) {
			isInTextBox = (Keyboard.FocusedElement is TextBox);
			if (!isInTextBox)
				base.OnPreviewKeyDown(e);
		}

		protected override void OnKeyDown(KeyEventArgs e) {
			if (e.Key != Key.PageUp && e.Key != Key.PageDown &&
				e.Key != Key.Left && e.Key != Key.Right &&
				e.Key != Key.Space && !isInTextBox)
			{
				base.OnKeyDown(e);
			}
		}
	}
}
