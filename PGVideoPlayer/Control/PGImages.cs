using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using PGVideoPlayer.Util;

namespace PGVideoPlayer.Control {
	/// <summary>A static class for storing loaded image resources.</summary>
	public static class PGImages {

		//-----------------------------------------------------------------------------
		// Members
		//-----------------------------------------------------------------------------

		public static readonly BitmapSource BookmarkEdit			= LoadIcon("BookMarkEdit");
		public static readonly BitmapSource BookmarkRemove			= LoadIcon("BookMarkRemove");
		public static readonly BitmapSource MediaPlay				= LoadIcon("MediaPlay");
		public static readonly BitmapSource MediaPause				= LoadIcon("MediaPause");
		public static readonly BitmapSource VolumeUnmuted			= LoadIcon("VolumeUnmuted");
		public static readonly BitmapSource VolumeMuted				= LoadIcon("VolumeMuted");

		
		//-----------------------------------------------------------------------------
		// Internal Methods
		//-----------------------------------------------------------------------------

		/// <summary>Load a png from the Resources\Icons folder.</summary>
		private static BitmapSource LoadIcon(string name) {
			return BitmapFactory.LoadSourceFromResource("Resources/Icons/" + name + ".png");
		}

		/// <summary>Load a png from the Resources folder.</summary>
		private static BitmapSource LoadResource(string name) {
			return BitmapFactory.LoadSourceFromResource("Resources/" + name + ".png");
		}
	}
}
