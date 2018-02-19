using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

using Xceed.Wpf.Toolkit;

using PGVideoPlayer.Control;
using PGVideoPlayer.Util;
using PGVideoPlayer.Windows;

using MediaElement = Unosquare.FFME.MediaElement;

namespace PGVideoPlayer {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		//-----------------------------------------------------------------------------
		// Constants
		//-----------------------------------------------------------------------------

		const double ExtraWidth = 44;
		const double ExtraHeight = 12;

		const double MaxVideoDimensions = 800;


		//-----------------------------------------------------------------------------
		// Members
		//-----------------------------------------------------------------------------
		
		private bool supressEvents;

		private DispatcherTimer timer;
		
		private Size playerOffset;
		private double baseWidth = 160;
		private double baseHeight = 144;
		private double finalMinWidth;
		private bool forceCloseVideo;
		private double desiredSpeed;


		//-----------------------------------------------------------------------------
		// Constructor
		//-----------------------------------------------------------------------------

		public MainWindow() {
			supressEvents = true;
			InitializeComponent();

			forceCloseVideo = false;
			desiredSpeed = 1.0;

			PGControl.Initialize(this);

			// Subscribe to these events after PGControl does
			media.MediaOpened	+= OnMediaOpened;
			media.MediaClosed	+= OnMediaClosed;
			media.MediaEnded	+= OnMediaEnded;

			timer = new DispatcherTimer(
				TimeSpan.FromSeconds(0.02),
				DispatcherPriority.ApplicationIdle,
				delegate {
					if (forceCloseVideo) {
						forceCloseVideo = false;
						PGControl.CloseVideo();
					}
					UpdateMediaButtons();
					UpdateInformation();
					RenderAudio();
				}, Dispatcher);

			UpdateControls();

			// Allow stealing focus from toolbar textboxes
			FocusManager.SetIsFocusScope(sectionToolBar, false);
		}

		//-----------------------------------------------------------------------------
		// Event Handlers
		//-----------------------------------------------------------------------------
		
		private void OnWindowLoaded(object sender, RoutedEventArgs e) {
			supressEvents = false;
			CalculateSizes();
			string[] args = Environment.GetCommandLineArgs();
			if (args.Length > 1)
				PGControl.OpenVideo(args[1]);
		}

		private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e) {
			if (supressEvents) return;
			UpdatePlayerDimensions(CalculateScale(e.NewSize));
			RenderAudio();
		}

		// Input Events ---------------------------------------------------------------

		private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e) {
			var directlyOver = Mouse.DirectlyOver as DependencyObject;
			if (WpfHelper.ContainsParent<TextBox>(directlyOver) ||
				WpfHelper.ContainsParent<IntegerUpDown>(directlyOver))
				return;
			// Make text boxes lose focus on click away
			FocusManager.SetFocusedElement(this, this);
		}

		private void OnTextBoxPreviewKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Enter) {
				Keyboard.ClearFocus();
			}
		}

		// Media Events ---------------------------------------------------------------

		private void OnMediaOpened(object sender, RoutedEventArgs e) {
			supressEvents = true;
			sliderPosition.Value = 0;
			sliderPosition.Maximum = PGControl.FrameDuration;

			// Choose a tick frequency that's not *too* frequent
			double frameRate = Math.Round(Media.VideoFrameRate);
			double totalSeconds = PGControl.Duration.TotalSeconds;
			if (totalSeconds <= 60)
				sliderPosition.TickFrequency = frameRate;
			else if (totalSeconds <= 60 * 2)
				sliderPosition.TickFrequency = frameRate * 2;
			else if (totalSeconds <= 60 * 5)
				sliderPosition.TickFrequency = frameRate * 5;
			else if (totalSeconds <= 60 * 10)
				sliderPosition.TickFrequency = frameRate * 30;
			else
				sliderPosition.TickFrequency = frameRate * 60;

			spinnerFrame.Maximum = PGControl.FrameDuration;
			supressEvents = false;
			labelDuration.Content = PGControl.Duration.ToString(@"mm\:ss\.ff");
			if (Media.NaturalVideoWidth > MaxVideoDimensions || Media.NaturalVideoHeight > MaxVideoDimensions) {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "Video dimensions are too large to be supported by the player!", "Video too Large");
				forceCloseVideo = true;
				return;
			}

			double oldScale = VideoScale;
			Size oldSize = new Size(Width, Height);
			baseWidth = Media.NaturalVideoWidth;
			baseHeight = Media.NaturalVideoHeight;
			MinWidth  = Math.Max(finalMinWidth, playerOffset.Width + (baseWidth * 2));
			MinHeight = playerOffset.Height + baseHeight;
			MakeRoomForNewVideoSize(oldScale, oldSize);
		}

		private void OnMediaClosed(object sender, RoutedEventArgs e) {
			supressEvents = true;
			sliderPosition.Value = 0;
			sliderPosition.Maximum = 1;
			spinnerFrame.Maximum = 0;
			supressEvents = false;
			labelDuration.Content = PGControl.Duration.ToString(@"mm\:ss\.ff");

			desiredSpeed = 1.0;
			for (int i = 0; i < menuItemSpeed.Items.Count; i++) {
				MenuItem item = (MenuItem) menuItemSpeed.Items[i];
				double speed = double.Parse((string) item.Tag);

				item.IsChecked = (speed == 1.0);
			}
		}

		private void OnMediaEnded(object sender, RoutedEventArgs e) {
			UpdateControls();
		}
		
		// Changed Events -------------------------------------------------------------

		private void OnPositionChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			if (supressEvents) return;
			PGControl.CurrentFrame = (int) Math.Round(sliderPosition.Value);
			UpdateInformation();
		}

		private void OnLargeIncrementChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			if (supressEvents) return;
			if (spinnerLargeIncrement.Value.HasValue)
				PGControl.LargeIncrement = spinnerLargeIncrement.Value.Value;
		}

		private void OnMediumIncrementChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			if (supressEvents) return;
			if (spinnerMediumIncrement.Value.HasValue)
				PGControl.MediumIncrement = spinnerMediumIncrement.Value.Value;
		}

		private void OnFrameChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			if (supressEvents) return;
			if (spinnerFrame.Value.HasValue)
				PGControl.CurrentFrame = spinnerFrame.Value.Value;
		}

		private void OnVolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			if (supressEvents) return;
			PGControl.Volume = sliderVolume.Value;
		}

		private void OnSpeedChanged(object sender, RoutedEventArgs e) {
			string speedStr = (string) ((FrameworkElement) sender).Tag;
			desiredSpeed = double.Parse(speedStr);

			Media.SpeedRatio = desiredSpeed;
			for (int i = 0; i < menuItemSpeed.Items.Count; i++) {
				MenuItem item = (MenuItem) menuItemSpeed.Items[i];
				item.IsChecked = (item == sender);
			}
		}

		private void OnSetVideoScale(object sender, RoutedEventArgs e) {
			string scaleStr = (string) ((FrameworkElement) sender).Tag;
			double scale = double.Parse(scaleStr);

			VideoScale = scale;
		}

		// File Drop Events -----------------------------------------------------------

		private void OnFileDragEnter(object sender, DragEventArgs e) {
			if (IsValidDropFile(e)) {
				e.Effects = DragDropEffects.Link;
				labelFileDrop.Visibility = Visibility.Visible;
			}
			else {
				e.Effects = DragDropEffects.None;
				labelFileDrop.Visibility = Visibility.Collapsed;
			}
		}

		private void OnFileDragLeave(object sender, DragEventArgs e) {
			labelFileDrop.Visibility = Visibility.Collapsed;
		}

		private void OnFileDragOver(object sender, DragEventArgs e) {
			if (IsValidDropFile(e)) {
				e.Effects = DragDropEffects.Link;
				labelFileDrop.Visibility = Visibility.Visible;
			}
			else {
				e.Effects = DragDropEffects.None;
				labelFileDrop.Visibility = Visibility.Collapsed;
			}
		}

		private void OnFileDrop(object sender, DragEventArgs e) {
			if (IsValidDropFile(e)) {
				string[] files = (string[]) e.Data.GetData(DataFormats.FileDrop);
				PGControl.OpenVideo(files[0]);
			}
			labelFileDrop.Visibility = Visibility.Collapsed;
		}

		// Help Events ----------------------------------------------------------------

		private void OnAbout(object sender, RoutedEventArgs e) {
			AboutWindow.Show(this);
		}

		private void OnCredits(object sender, RoutedEventArgs e) {
			CreditsWindow.Show(this);
		}

		private void OnViewOnGitHub(object sender, RoutedEventArgs e) {
			Process.Start(@"https://github.com/trigger-death/PGVideoPlayer");
		}


		//-----------------------------------------------------------------------------
		// Public Methods
		//-----------------------------------------------------------------------------

		public void UpdateControls() {
			UpdateMediaButtons();
			menuItemShowDif.IsChecked = PGControl.ShowDif;
			menuItemShowBookmarkList.IsChecked = PGControl.ShowBookmarkList;
			menuItemShowSoundVisualizer.IsChecked = PGControl.ShowSoundVisualizer;
			borderVisualizer.Visibility =
				(PGControl.ShowSoundVisualizer ? Visibility.Visible : Visibility.Collapsed);

			bool supressEventsOld = supressEvents;
			supressEvents = true;
			spinnerMediumIncrement.Value = PGControl.MediumIncrement;
			menuItemSkipBackMedium.Header = "Back " + PGControl.MediumIncrement + " Frames";
			menuItemSkipBackLarge.Header = "Back " + PGControl.LargeIncrement + " Frames";
			buttonSkipBack.ToolTip = "Back " + PGControl.LargeIncrement + " Frames (Shift+Left)";
			spinnerLargeIncrement.Value = PGControl.LargeIncrement;
			menuItemSkipForwardMedium.Header = "Forward " + PGControl.MediumIncrement + " Frames";
			menuItemSkipForwardLarge.Header = "Forward " + PGControl.LargeIncrement + " Frames";
			buttonSkipForward.ToolTip = "Forward " + PGControl.LargeIncrement + " Frames (Shift+Right)";
			menuItemForceNearestNeighbor.IsChecked = PGControl.ForceNearestNeighbor;
			if (PGControl.IsMuted) {
				menuItemMute.Header = "Unmute";
				menuItemMute.Source = PGImages.VolumeMuted;
			}
			else {
				menuItemMute.Header = "Mute";
				menuItemMute.Source = PGImages.VolumeUnmuted;
			}
			menuItemAutoPlay.IsChecked = PGControl.AutoPlay;
			sliderVolume.Value = PGControl.Volume;
			supressEvents = supressEventsOld;
		}

		public void UpdateScalingMode() {
			UpdateScalingMode(VideoScale);
		}


		//-----------------------------------------------------------------------------
		// Internal Methods
		//-----------------------------------------------------------------------------

		// Internal Updating Methods --------------------------------------------------

		private void UpdateMediaButtons() {
			buttonStop.IsChecked = Media.MediaState == MediaState.Stop;
			buttonPlay.IsChecked = Media.MediaState == MediaState.Play;
			buttonPause.IsChecked = Media.MediaState == MediaState.Pause ||
									Media.MediaState == MediaState.Manual;
			if (Media.MediaState == MediaState.Play) {
				menuItemPlayPause.Header = "Pause";
				menuItemPlayPause.Source = PGImages.MediaPause;
			}
			else {
				menuItemPlayPause.Header = "Play";
				menuItemPlayPause.Source = PGImages.MediaPlay;
			}
		}
		
		private void UpdateInformation() {
			supressEvents = true;
			sliderPosition.Value = PGControl.CurrentFrame;
			spinnerFrame.Value = PGControl.CurrentFrame;
			labelTime.Content = Media.Position.ToString(@"mm\:ss\.ff") + "/";
			if (Media.SpeedRatio != desiredSpeed) {
				Media.SpeedRatio = desiredSpeed;
				for (int i = 0; i < menuItemSpeed.Items.Count; i++) {
					MenuItem item = (MenuItem) menuItemSpeed.Items[i];
					double speed = double.Parse((string) item.Tag);

					item.IsChecked = (speed == Media.SpeedRatio);
				}
			}
			supressEvents = false;
		}

		private void RenderAudio() {
			canvasVisualizer.Children.Clear();
			if (double.IsInfinity(Media.VideoFrameLength))
				return;
			if (double.IsNaN(canvasVisualizer.Width) || double.IsNaN(canvasVisualizer.Height))
				return;
			int samplesPerFrame = (int) (Media.VideoFrameLength * PGControl.SampleRate * Media.SpeedRatio);
			int samplesStart = ((int) (Media.Position.TotalSeconds * PGControl.SampleRate));
			int width = (int) canvasVisualizer.Width;
			int halfHeight = (int) canvasVisualizer.Height / 2;
			int maxShapes = 160;
			for (int i = 0; i < maxShapes; i++) {
				int sampleIndex = samplesStart + i * samplesPerFrame / maxShapes;
				if (sampleIndex >= PGControl.SampleCount)
					break;
				int sample = PGControl.Samples[sampleIndex];
				Rectangle rect = new Rectangle();
				int x = i * width / maxShapes;
				rect.Width = ((i + 1) * width / maxShapes) - x;
				rect.Height = Math.Min(halfHeight, 1 + (Math.Abs(sample) * halfHeight * 5 / 4 / short.MaxValue));
				Canvas.SetLeft(rect, x);
				if (sample < 0)
					Canvas.SetTop(rect, halfHeight - rect.Height);
				else
					Canvas.SetTop(rect, halfHeight - 1);
				rect.Fill = Brushes.WhiteSmoke;
				canvasVisualizer.Children.Add(rect);
			}
		}

		// Internal Scaling Methods ---------------------------------------------------
		
		private void CalculateSizes() {
			double playerWidth = baseWidth * 2 + ExtraWidth;
			double playerHeight = baseHeight + ExtraHeight;

			double totalWidth = clientArea.ActualWidth;
			double minWidth =   playerWidth;
			double totalHeight = clientArea.ActualHeight;
			double minHeight =  sectionMenu.ActualHeight +
								//sectionSplitter.ActualHeight +
								playerHeight +
								sectionControls.ActualHeight +
								sectionToolBar.ActualHeight;

			playerOffset = new Size(
				ActualWidth  - (totalWidth  - minWidth)  - baseWidth * 2,
				ActualHeight - (totalHeight - minHeight) - baseHeight);

			supressEvents = true;
			MinWidth  = ActualWidth  - (totalWidth  - minWidth);
			MinHeight = ActualHeight - (totalHeight - minHeight);
			finalMinWidth = MinWidth;
			UpdatePlayerScale(PGControl.StartupVideoScale);
			supressEvents = false;
		}

		private void UpdateScalingMode(double scale) {
			if (Math.Floor(scale) == scale || PGControl.ForceNearestNeighbor) {
				RenderOptions.SetBitmapScalingMode(media, BitmapScalingMode.NearestNeighbor);
				RenderOptions.SetBitmapScalingMode(imageDifs, BitmapScalingMode.NearestNeighbor);
			}
			else {
				RenderOptions.SetBitmapScalingMode(media, BitmapScalingMode.Unspecified);
				RenderOptions.SetBitmapScalingMode(imageDifs, BitmapScalingMode.Unspecified);
			}
			for (int i = 0; i < menuItemVideoScaling.Items.Count; i++) {
				object itemObj = menuItemVideoScaling.Items[i];
				if (itemObj is MenuItem) {
					var item = (MenuItem) menuItemVideoScaling.Items[i];
					string scaleStr = (string) item.Tag;
					double itemScale = double.Parse(scaleStr);
					item.IsChecked = (itemScale == scale);
				}
				else {
					break;
				}
			}
		}

		private double CalculateScale(Size size) {
			Size available = new Size(
				(int) (size.Width - playerOffset.Width),
				(int) (size.Height - playerOffset.Height));

			double widthScale = available.Width / ScalableWidth;
			double heightScale = available.Height / baseHeight;

			return Math.Min(widthScale, heightScale);
		}

		private void UpdatePlayerDimensions(double scale) {
			media.Width = Math.Floor(baseWidth * scale);
			media.Height = Math.Floor(baseHeight * scale);
			imageDifs.Width = Math.Floor(baseWidth * scale);
			imageDifs.Height = Math.Floor(baseHeight * scale);
			canvasVisualizer.Width = Math.Floor(baseWidth * scale);
			canvasVisualizer.Height = Math.Floor(baseHeight * scale);

			UpdateScalingMode(scale);
		}

		private void UpdatePlayerScale(double scale) {
			scale = Math.Min(4, Math.Max(1, scale));
			supressEvents = true;
			Width = Math.Ceiling(ScalableWidth * scale + playerOffset.Width);
			Height = Math.Ceiling(baseHeight * scale + playerOffset.Height);
			supressEvents = false;
			UpdatePlayerDimensions(scale);
		}

		public void MakeRoomForAudioVisualizer(double oldScale, double oldWidth = 0) {
			if (oldWidth == 0)
				oldWidth = Width;
			double newScale = Math.Max(1, VideoScale);

			double requiredWidth = Math.Ceiling(ScalableWidth * newScale + playerOffset.Width);
			if (requiredWidth > oldWidth) {
				supressEvents = true;
				Width = requiredWidth;
				supressEvents = false;
			}
			if (newScale != oldScale) {
				UpdatePlayerDimensions(newScale);
			}
		}

		public void MakeRoomForNewVideoSize(double oldScale, Size oldSize) {
			double newScale = Math.Max(1, VideoScale);

			Size required = new Size(
				Math.Ceiling(ScalableWidth * newScale + playerOffset.Width),
				Math.Ceiling(baseHeight * newScale + playerOffset.Height));

			if (required.Width > oldSize.Width) {
				supressEvents = true;
				Width = required.Width;
				supressEvents = false;
			}
			if (required.Height > oldSize.Height) {
				supressEvents = true;
				Height = required.Height;
				supressEvents = false;
			}
			UpdatePlayerDimensions(newScale);
		}
		
		// Internal File Drop Methods -------------------------------------------------

		private bool IsValidDropFile(DragEventArgs e) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
				string[] files = (string[]) e.Data.GetData(DataFormats.FileDrop);
				if (files.Length != 0) {
					string ext = System.IO.Path.GetExtension(files[0]).ToLower();
					foreach (string supportedExt in PGControl.SupportedVideoFormats) {
						if (ext == supportedExt)
							return true;
					}
				}
			}
			return false;
		}


		//-----------------------------------------------------------------------------
		// Internal Properties
		//-----------------------------------------------------------------------------
		
		private double ScalableWidth {
			get { return baseWidth * (PGControl.ShowSoundVisualizer ? 2 : 1); }
		}


		//-----------------------------------------------------------------------------
		// Properties
		//-----------------------------------------------------------------------------

		public MediaElement Media {
			get { return media; }
		}

		public Image ImageDifs {
			get { return imageDifs; }
		}

		public double VideoScale {
			get { return CalculateScale(new Size(ActualWidth, ActualHeight)); }
			set { UpdatePlayerScale(value); }
		}

		public double DesiredSpeed {
			get { return desiredSpeed; }
			set {
				desiredSpeed = value;
				Media.SpeedRatio = desiredSpeed;
				for (int i = 0; i < menuItemSpeed.Items.Count; i++) {
					MenuItem item = (MenuItem) menuItemSpeed.Items[i];
					double speed = double.Parse((string) item.Tag);

					item.IsChecked = (speed == Media.SpeedRatio);
				}
			}
		}
	}
}
