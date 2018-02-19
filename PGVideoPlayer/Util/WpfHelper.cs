using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PGVideoPlayer.Util {
	public static class WpfHelper {

		public static bool ContainsParent<T>(DependencyObject element) {
			try {
				do {
					if (element is T)
						return true;
					element = VisualTreeHelper.GetParent(element);
				} while (element != null);
			}
			catch { }
			return false;
		}

	}
}
