using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unosquare.FFME;

namespace PGVideoPlayer.Util {
	public static class FrameExtensions {

		public static int TimeSpanToFrame(this MediaElement media, TimeSpan time) {
			double frameRate = 1;
			if (media != null)
				frameRate = Math.Max(1.0, media.VideoFrameRate);

			return (int) Math.Floor(time.TotalSeconds * frameRate);
		}

		public static TimeSpan FrameToTimeSpan(this MediaElement media, int frame) {
			double frameRate = 1;
			if (media != null)
				frameRate = Math.Max(1, media.VideoFrameRate);

			// 0.001 offset fixes an issue where sometimes the frame ends up as the frame before.
			return TimeSpan.FromSeconds(frame / frameRate + 0.001);
		}

		public static int FrameDuration(this MediaElement media) {
			if (media.NaturalDuration.HasTimeSpan)
				return TimeSpanToFrame(media, media.NaturalDuration.TimeSpan);
			return 1;
		}

		public static int CurrentFrame(this MediaElement media) {
			return TimeSpanToFrame(media, media.Position);
		}

		public static void SeekFrame(this MediaElement media, int frame) {
			media.Position = FrameToTimeSpan(media, frame);
		}
	}
}
