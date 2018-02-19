using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml;
using Microsoft.Win32;

using Accord.Video.FFMPEG;
using Unosquare.FFME.Events;
using Xceed.Wpf.Toolkit;

using PGVideoPlayer.Controls;
using PGVideoPlayer.Util;
using PGVideoPlayer.Windows;

using Bitmap		= System.Drawing.Bitmap;
using Rectangle		= System.Drawing.Rectangle;
using MediaElement	= Unosquare.FFME.MediaElement;

namespace PGVideoPlayer.Control {

	public enum SkipSpeed {
		Small,
		Medium,
		Large
	}

	public static class PGControl {

		//-----------------------------------------------------------------------------
		// Classes
		//-----------------------------------------------------------------------------

		private struct FrameBookmark {
			public ListViewItem Owner { get; }
			public ColumnDefinition ColumnTimelapse { get; }
			public TextBlock TextTimelapse { get; }
			public TextBlock TextName { get; }
			public TextBox TextBoxName { get; }
			public int Frame { get; }

			public FrameBookmark(ListViewItem owner, ColumnDefinition columnTimelapse,
				TextBlock textTimelapse, TextBlock textName, TextBox textBoxName, int frame)
			{
				Owner			= owner;
				ColumnTimelapse	= columnTimelapse;
				TextTimelapse	= textTimelapse;
				TextName		= textName;
				TextBoxName		= textBoxName;
				Frame			= frame;
			}
		}

		//-----------------------------------------------------------------------------
		// Constants
		//-----------------------------------------------------------------------------

		public static readonly string PreferencesFile =
				Path.Combine(Path.GetDirectoryName(
					Assembly.GetExecutingAssembly().Location), "PGVP-Preferences.xml");

		public const string LongWindowTitle = "Pixel Graphics Video Player";
		public const string ShortWindowTitle = "PGVP - ";

		// Just take a shot at standard formats.
		public static readonly string[] SupportedVideoFormats = new string[] {
			".avi",
			".mp4",
			".webm",
			".flv",
			".mov",
			".mpg",
			".mpeg"
		};

		private const int ColumnTimelapseWidth = 56;
		private static readonly Thickness TextTimelapseMargin = new Thickness(2, 0, 2, 0);

		//-----------------------------------------------------------------------------
		// Members
		//-----------------------------------------------------------------------------

		private static MainWindow mainWindow;
		private static BookmarkWindow bookmarkWindow;

		private static string fileName;
		private static VideoFileReader videoReader;

		private static Dictionary<int, ListViewItem> bookmarkLookup;
		private static ObservableCollection<ListViewItem> bookmarkItems;
		private static ListViewItem editingBookmark;

		private static short[] samples;

		private static int mediumIncrement;
		private static int largeIncrement;

		private static bool showDif;
		private static bool showSoundVisualizer;

		private static double startupVideoScale;

		private static bool forceNearestNeighbor;

		private static bool autoPlay;

		private static int sampleRate;

		private static bool showBookmarkTimes;

		private static double volume;
		

		//-----------------------------------------------------------------------------
		// Constructor
		//-----------------------------------------------------------------------------

		public static void Initialize(MainWindow window) {
			mainWindow = window;
			bookmarkWindow = null;

			bookmarkLookup = new Dictionary<int, ListViewItem>();
			bookmarkItems = new ObservableCollection<ListViewItem>();

			samples = new short[0];
			mediumIncrement = 10;
			largeIncrement = 30;
			showDif = true;
			showSoundVisualizer = true;
			videoReader = null;
			fileName = "";
			startupVideoScale = 1.0;
			forceNearestNeighbor = false;
			sampleRate = 1;
			autoPlay = false;
			volume = 1;
			showBookmarkTimes = true;
			editingBookmark = null;

			Media.MediaOpened += OnMediaOpened;
			Media.MediaClosed += OnMediaClosed;
			Media.MediaEnded += OnMediaEnded;
			Media.MediaFailed += OnMediaFailed;
			Media.RenderingAudio += OnRenderingAudio;

			RegisterCommands();

			BindEvent(Window.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown));

			LoadPreferences();
		}

		private static void RegisterCommands() {
			BindCommand(ApplicationCommands.Open, OpenVideo);
			BindCommand(ApplicationCommands.Close, CloseVideo, CanExecuteIsOpen);
			BindCommand(ApplicationCommands.Copy, CopyFrame, CanExecuteIsOpen);

			BindCommand(Commands.Exit, Exit);
			BindCommand(Commands.SavePreferences, SavePreferences);
			BindCommand(Commands.BookmarkFrame, BookmarkFrame, CanExecuteIsOpen);
			BindCommand(Commands.ClearBookmarks, ClearBookmarks, CanExecuteIsOpen);
			BindCommand(Commands.FindNextDif, FindNextDif, CanExecuteIsOpen);
			BindCommand(Commands.FindPreviousDif, FindPreviousDif, CanExecuteIsOpen);
			BindCommand(Commands.BookmarkTimes, ToggleBookmarkTimes);
			BindCommand(Commands.Mute, ToggleMute);
			BindCommand(Commands.AutoPlay, ToggleAutoPlay);
			BindCommand(Commands.ShowDif, ToggleShowDif, CanExecuteIsOpen);
			BindCommand(Commands.BookmarkList, ToggleBookmarkList);
			BindCommand(Commands.SoundVisualizer, ToggleSoundVisualizer);
			BindCommand(Commands.ForceNearestNeighbor, ToggleForceNearestNeighbor);
			BindCommand(Commands.ScaleDown, ScaleDown, CanExecuteScaleDown);
			BindCommand(Commands.ScaleUp, ScaleUp, CanExecuteScaleUp);

			BindCommand(Commands.PlayPause, PlayPause, CanExecuteIsOpen);
			BindCommand(Commands.Stop, Stop, CanExecuteIsOpen);
			BindCommand(Commands.Restart, Restart, CanExecuteIsOpen);
			BindCommand(Commands.End, End, CanExecuteIsOpen);
			BindCommand(Commands.PreviousFrame, delegate { SkipBack(SkipSpeed.Small); }, CanExecuteIsOpen);
			BindCommand(Commands.NextFrame, delegate { SkipForward(SkipSpeed.Small); }, CanExecuteIsOpen);
			BindCommand(Commands.SkipBackMedium, delegate { SkipBack(SkipSpeed.Medium); }, CanExecuteIsOpen);
			BindCommand(Commands.SkipForwardMedium, delegate { SkipForward(SkipSpeed.Medium); }, CanExecuteIsOpen);
			BindCommand(Commands.SkipBackLarge, delegate { SkipBack(SkipSpeed.Large); }, CanExecuteIsOpen);
			BindCommand(Commands.SkipForwardLarge, delegate { SkipForward(SkipSpeed.Large); }, CanExecuteIsOpen);
		}

		private static void BindCommand(ICommand command, Action executed) {
			BindCommand(command, executed, CanAlwaysExecute);
		}

		private static void BindCommand(ICommand command, Action executed, CanExecuteRoutedEventHandler canExecute) {
			var binding = new CommandBinding(command, delegate { executed(); }, canExecute);
			CommandManager.RegisterClassCommandBinding(typeof(MainWindow), binding);
			CommandManager.RegisterClassCommandBinding(typeof(BookmarkWindow), binding);
		}

		private static void BindEvent(RoutedEvent routedEvent, Delegate handler) {
			EventManager.RegisterClassHandler(typeof(MainWindow), routedEvent, handler);
			EventManager.RegisterClassHandler(typeof(BookmarkWindow), routedEvent, handler);
		}


		//-----------------------------------------------------------------------------
		// UI Invalidation
		//-----------------------------------------------------------------------------

		public static void InvalidateBookmark() {
			if (ShowBookmarkList)
				ListViewBookmarks.SelectedIndex = -1;
		}

		public static void InvalidateDif() {
			ImageDifs.Source = null;
		}


		//-----------------------------------------------------------------------------
		// Preferences
		//-----------------------------------------------------------------------------

		public static void SavePreferences() {
			try {
				XmlDocument doc = new XmlDocument();
				XmlElement element;

				XmlElement root = doc.CreateElement("PGPlayer");
				doc.AppendChild(root);

				element = doc.CreateElement("ShowSoundVisualizer");
				element.AppendChild(doc.CreateTextNode(ShowSoundVisualizer.ToString()));
				root.AppendChild(element);

				element = doc.CreateElement("MediumIncrement");
				element.AppendChild(doc.CreateTextNode(MediumIncrement.ToString()));
				root.AppendChild(element);

				element = doc.CreateElement("LargeIncrement");
				element.AppendChild(doc.CreateTextNode(LargeIncrement.ToString()));
				root.AppendChild(element);

				element = doc.CreateElement("VideoScale");
				element.AppendChild(doc.CreateTextNode(VideoScale.ToString()));
				root.AppendChild(element);

				element = doc.CreateElement("ForceNearestNeighbor");
				element.AppendChild(doc.CreateTextNode(ForceNearestNeighbor.ToString()));
				root.AppendChild(element);

				element = doc.CreateElement("Volume");
				element.AppendChild(doc.CreateTextNode(Volume.ToString()));
				root.AppendChild(element);

				element = doc.CreateElement("IsMuted");
				element.AppendChild(doc.CreateTextNode(IsMuted.ToString()));
				root.AppendChild(element);

				element = doc.CreateElement("AutoPlay");
				element.AppendChild(doc.CreateTextNode(AutoPlay.ToString()));
				root.AppendChild(element);

				element = doc.CreateElement("ShowBookmarkTimes");
				element.AppendChild(doc.CreateTextNode(ShowBookmarkTimes.ToString()));
				root.AppendChild(element);

				doc.Save(PreferencesFile);
			}
			catch (Exception) {

			}
		}

		public static void LoadPreferences() {
			if (!File.Exists(PreferencesFile))
				return;
			try {
				XmlDocument doc = new XmlDocument();
				doc.Load(PreferencesFile);

				XmlNode node;
				bool boolValue;
				int intValue;
				double doubleValue;

				node = doc.SelectSingleNode("/PGPlayer/ShowSoundVisualizer");
				if (node != null && bool.TryParse(node.InnerText, out boolValue))
					ShowSoundVisualizer = boolValue;

				node = doc.SelectSingleNode("/PGPlayer/MediumIncrement");
				if (node != null && int.TryParse(node.InnerText, out intValue))
					mediumIncrement = intValue;

				node = doc.SelectSingleNode("/PGPlayer/LargeIncrement");
				if (node != null && int.TryParse(node.InnerText, out intValue))
					largeIncrement = intValue;

				node = doc.SelectSingleNode("/PGPlayer/VideoScale");
				if (node != null && double.TryParse(node.InnerText, out doubleValue))
					startupVideoScale = doubleValue;

				node = doc.SelectSingleNode("/PGPlayer/ForceNearestNeighbor");
				if (node != null && bool.TryParse(node.InnerText, out boolValue))
					forceNearestNeighbor = boolValue;

				node = doc.SelectSingleNode("/PGPlayer/Volume");
				if (node != null && double.TryParse(node.InnerText, out doubleValue))
					volume = doubleValue;

				node = doc.SelectSingleNode("/PGPlayer/IsMuted");
				if (node != null && bool.TryParse(node.InnerText, out boolValue))
					IsMuted = boolValue;

				node = doc.SelectSingleNode("/PGPlayer/AutoPlay");
				if (node != null && bool.TryParse(node.InnerText, out boolValue))
					autoPlay = boolValue;

				node = doc.SelectSingleNode("/PGPlayer/ShowBookmarkTimes");
				if (node != null && bool.TryParse(node.InnerText, out boolValue))
					showBookmarkTimes = boolValue;
			}
			catch (Exception) {

			}
		}

		//-----------------------------------------------------------------------------
		// Commands
		//-----------------------------------------------------------------------------

		public static void OpenVideo() {
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Title = "Open Video";
			dialog.Filter = "Video Files|";
			for (int i = 0; i < SupportedVideoFormats.Length; i++) {
				if (i > 0)
					dialog.Filter += ";";
				dialog.Filter += "*" + SupportedVideoFormats[i];
			}
			dialog.FilterIndex = 0;
			var result = dialog.ShowDialog();
			if (result.HasValue && result.Value) {
				OpenVideo(dialog.FileName);
			}
		}

		public static void OpenVideo(string filePath) {
			bookmarkLookup.Clear();
			bookmarkItems.Clear();
			samples = new short[0];
			sampleRate = 1;
			fileName = filePath;
			Media.Source = new Uri(fileName);
		}

		public static void CloseVideo() {
			Media.Close();
		}

		public static void Exit() {
			mainWindow.Close();
		}

		public static void CopyFrame() {
			Bitmap bitmap = GetFrameBitmap(CurrentFrame);
			if (bitmap != null) {
				System.Windows.Forms.Clipboard.SetImage(bitmap);
				bitmap.Dispose();
			}
		}

		public static void SelectBookmark(int index) {
			if (index == -1) return;
			var data = (FrameBookmark) bookmarkItems[index].Tag;
			Media.SeekFrame(data.Frame);
			InvalidateDif();
		}
		
		public static void BookmarkFrame() {
			BookmarkFrame(CurrentFrame);
		}

		public static void BookmarkFrame(int frame) {
			ShowBookmarkList = true;
			if (bookmarkLookup.ContainsKey(frame)) return;

			int index = GetBookmarkIndexOfFrame(frame);
			ListViewItem item = MakeBookmarkItem(frame);
			bookmarkLookup.Add(frame, item);
			bookmarkItems.Insert(index, item);

			UpdateBookmarkItemAt(index);
			if (index + 1 < bookmarkItems.Count)
				UpdateBookmarkItemAt(index + 1);
		}

		public static void ClearBookmarks() {
			bookmarkItems.Clear();
			bookmarkLookup.Clear();
		}

		public static void ToggleBookmarkTimes() {
			ShowBookmarkTimes = !ShowBookmarkTimes;
		}

		public static void ToggleMute() {
			IsMuted = !IsMuted;
		}

		public static void ToggleAutoPlay() {
			AutoPlay = !AutoPlay;
		}

		public static void ToggleBookmarkList() {
			ShowBookmarkList = !ShowBookmarkList;
		}

		public static void ToggleSoundVisualizer() {
			ShowSoundVisualizer = !ShowSoundVisualizer;
		}

		public static void ToggleShowDif() {
			ShowDif = !ShowDif;
		}

		public static void ToggleForceNearestNeighbor() {
			ForceNearestNeighbor = !ForceNearestNeighbor;
		}

		public static void FindNextDif() {
			InvalidateDif();

			int frame = CurrentFrame;
			if (frame > FrameDuration)
				return;

			Bitmap frameBmp = GetFrameBitmap(frame);
			if (frameBmp == null)
				return;

			var unlockedFrame = BitmapDif.Unlock(frameBmp);

			BitmapSource resultSource = null;

			frame++;
			while (frame <= FrameDuration) {
				Bitmap bmp = GetFrameBitmap(frame);
				if (bmp == null) break;
				if (BitmapDif.ContainsDifferences(unlockedFrame, bmp, true)) {
					Bitmap result = BitmapDif.ReturnDifferences(unlockedFrame, bmp, true);
					resultSource = BitmapFactory.WriteBitmapToWriteable(result, true);
					resultSource.Freeze();
					break;
				}
				frame++;
			}
			CurrentFrame = Math.Min(FrameDuration, frame);
			ImageDifs.Source = resultSource;

			BitmapDif.Lock(unlockedFrame, true);
			PauseIfPlaying();
		}

		public static void FindPreviousDif() {
			InvalidateDif();

			int frame = CurrentFrame;
			if (frame == 0)
				return;

			Bitmap frameBmp = GetFrameBitmap(frame);
			if (frameBmp == null) {
				frame--;
				frameBmp = GetFrameBitmap(frame);
				if (frameBmp == null)
					return;
			}

			var unlockedFrame = BitmapDif.Unlock(frameBmp);

			BitmapSource resultSource = null;

			frame--;
			while (frame >= 0) {
				Bitmap bmp = GetFrameBitmap(frame);
				if (bmp == null) break;
				if (BitmapDif.ContainsDifferences(unlockedFrame, bmp, true)) {
					Bitmap result = BitmapDif.ReturnDifferences(unlockedFrame, bmp, true);
					resultSource = BitmapFactory.WriteBitmapToWriteable(result, true);
					resultSource.Freeze();
					break;
				}
				frame--;
			}
			CurrentFrame = Math.Max(0, frame);
			ImageDifs.Source = resultSource;

			BitmapDif.Lock(unlockedFrame, true);
			PauseIfPlaying();
		}

		public static void PlayPause() {
			if (Media.MediaState == MediaState.Play)
				Pause();
			else
				Play();
		}

		public static void Play() {
			Media.Play();
			InvalidateBookmark();
			InvalidateDif();
		}

		public static void Pause() {
			Media.Pause();
		}

		public static void Stop() {
			Media.Stop();
			InvalidateBookmark();
			InvalidateDif();
		}

		public static void Restart() {
			Media.Position = TimeSpan.Zero;
			Media.Play();
			InvalidateBookmark();
			InvalidateDif();
		}

		public static void End() {
			CurrentFrame = FrameDuration;
			Media.Pause();
			InvalidateBookmark();
			InvalidateDif();
		}

		public static void SkipBack(SkipSpeed speed) {
			switch (speed) {
			case SkipSpeed.Small:	CurrentFrame--; break;
			case SkipSpeed.Medium:	CurrentFrame -= mediumIncrement; break;
			case SkipSpeed.Large:	CurrentFrame -= largeIncrement; break;
			}
		}

		public static void SkipForward(SkipSpeed speed) {
			switch (speed) {
			case SkipSpeed.Small:	CurrentFrame++; break;
			case SkipSpeed.Medium:	CurrentFrame += mediumIncrement; break;
			case SkipSpeed.Large:	CurrentFrame += largeIncrement; break;
			}
		}

		public static void ScaleDown() {
			VideoScale = Math.Ceiling(VideoScale) - 1;
		}

		public static void ScaleUp() {
			VideoScale = Math.Floor(VideoScale) + 1;
		}


		//-----------------------------------------------------------------------------
		// Event Handlers
		//-----------------------------------------------------------------------------


		private static void OnRenderingAudio(object sender, RenderingAudioEventArgs e) {
			if (sampleRate == 1) {
				sampleRate = e.SampleRate;
				samples = new short[(int) ((Duration.TotalSeconds + 0.5) * sampleRate)];
			}
			int samplesStart = (int) Math.Round(e.StartTime.TotalSeconds * 48000);
			for (int i = 0; i < e.SamplesPerChannel; i++) {
				samples[samplesStart + i] = Marshal.ReadInt16(e.Buffer, i * 2 * e.ChannelCount);
			}
		}

		private static void OnMediaEnded(object sender, RoutedEventArgs e) {
			
		}

		private static void OnMediaOpened(object sender, RoutedEventArgs e) {
			CommandManager.InvalidateRequerySuggested();
			videoReader = new VideoFileReader();
			videoReader.Open(fileName);
			UpdateWindowTitle();
			Media.Volume = volume;
			if (autoPlay)
				Media.Play();
		}

		private static void OnMediaFailed(object sender, ExceptionRoutedEventArgs e) {
			TriggerMessageBox.Show(mainWindow, MessageIcon.Error, "Failed to open '" + Path.GetFileName(fileName) + "'!", "Open Error");
		}

		private static void OnMediaClosed(object sender, RoutedEventArgs e) {
			CommandManager.InvalidateRequerySuggested();
			InvalidateDif();
			videoReader.Close();
			videoReader.Dispose();
			videoReader = null;
			samples = new short[0];
			sampleRate = 1;
			UpdateWindowTitle();
		}

		private static void OnBookmarkWindowClosed(object sender, EventArgs e) {
			bookmarkWindow = null;

			// Failsafe to reset list view item back to its normal state
			// Should normally get called first by LostKeyboardFocus
			FinishBookmarkEdit(editingBookmark);
		}


		//-----------------------------------------------------------------------------
		// Internal Methods
		//-----------------------------------------------------------------------------

		private static void UpdateWindowTitle() {
			string title = LongWindowTitle;
			if (Media.IsOpen) {
				title = ShortWindowTitle +
					Path.GetFileName(fileName);
			}
			mainWindow.Title = title;
		}

		private static void InitSamples() {
			if (Media.IsOpen && samples.Length == 0)
				samples = new short[(int) ((Duration.TotalSeconds + 0.5) * 48000)];
			else if (!Media.IsOpen && samples.Length != 0)
				samples = new short[0];
		}

		private static int GetBookmarkIndexOfFrame(int frame) {
			for (int i = 0; i < bookmarkItems.Count; i++) {
				FrameBookmark data = (FrameBookmark) bookmarkItems[i].Tag;
				if (data.Frame >= frame) {
					return i;
				}
			}
			return bookmarkItems.Count;
		}

		private static ListViewItem MakeBookmarkItem(int frame) {
			ListViewItem item = new ListViewItem();
			item.FocusVisualStyle = null;

			Grid grid = new Grid();
			grid.Background = null;
			ColumnDefinition column = new ColumnDefinition();
			column.Width = new GridLength(56);
			grid.ColumnDefinitions.Add(column);
			column = new ColumnDefinition();
			column.Width = new GridLength(1, GridUnitType.Star);
			grid.ColumnDefinitions.Add(column);
			ColumnDefinition columnTimelapse = new ColumnDefinition();
			columnTimelapse.Width = new GridLength(1, GridUnitType.Auto);
			grid.ColumnDefinitions.Add(columnTimelapse);
			column = new ColumnDefinition();
			column.Width = new GridLength(40);
			grid.ColumnDefinitions.Add(column);
			item.Content = grid;

			TextBlock textFrame = new TextBlock();
			textFrame.VerticalAlignment = VerticalAlignment.Center;
			textFrame.HorizontalAlignment = HorizontalAlignment.Left;
			textFrame.Text = frame.ToString();
			textFrame.Margin = new Thickness(5, 0, 0, 0);
			textFrame.IsHitTestVisible = false;
			grid.Children.Add(textFrame);
			Grid.SetColumn(textFrame, 0);

			TextBlock textName = new TextBlock();
			textName.VerticalAlignment = VerticalAlignment.Center;
			textName.HorizontalAlignment = HorizontalAlignment.Left;
			textName.IsHitTestVisible = false;
			textName.TextTrimming = TextTrimming.CharacterEllipsis;
			textName.Margin = new Thickness(2, 0, 0, 0);
			grid.Children.Add(textName);
			Grid.SetColumn(textName, 1);

			TextBlock textTimelapse = new TextBlock();
			textTimelapse.VerticalAlignment = VerticalAlignment.Center;
			textTimelapse.HorizontalAlignment = HorizontalAlignment.Left;
			textTimelapse.IsHitTestVisible = false;
			textTimelapse.Margin = TextTimelapseMargin;
			grid.Children.Add(textTimelapse);
			Grid.SetColumn(textTimelapse, 2);

			ImageButton buttonEdit = new ImageButton();
			buttonEdit.VerticalAlignment = VerticalAlignment.Center;
			buttonEdit.HorizontalAlignment = HorizontalAlignment.Right;
			buttonEdit.Source = PGImages.BookmarkEdit;
			buttonEdit.Height = 16;
			buttonEdit.Width = 16;
			buttonEdit.Margin = new Thickness(0, 1, 22, 0);
			buttonEdit.Padding = new Thickness(0);
			buttonEdit.Click += (o, e) => {
				StartBookmarkEdit(frame);
			};
			grid.Children.Add(buttonEdit);
			Grid.SetColumn(buttonEdit, 3);

			ImageButton buttonRemove = new ImageButton();
			buttonRemove.VerticalAlignment = VerticalAlignment.Center;
			buttonRemove.HorizontalAlignment = HorizontalAlignment.Right;
			buttonRemove.Source = PGImages.BookmarkRemove;
			buttonRemove.Height = 16;
			buttonRemove.Width = 16;
			buttonRemove.Margin = new Thickness(0, 1, 2, 0);
			buttonRemove.Padding = new Thickness(0);
			buttonRemove.Click += (o, e) => {
				RemoveBookmark(frame);
			};
			grid.Children.Add(buttonRemove);
			Grid.SetColumn(buttonRemove, 3);

			TextBox textBoxName = new TextBox();
			textBoxName.VerticalAlignment = VerticalAlignment.Stretch;
			textBoxName.HorizontalAlignment = HorizontalAlignment.Stretch;
			textBoxName.Padding = new Thickness(0, 0, 0, 0);
			textBoxName.Visibility = Visibility.Collapsed;
			textBoxName.LostKeyboardFocus += (o, e) => {
				FinishBookmarkEdit(item);
			};
			textBoxName.PreviewKeyDown += (o, e) => {
				if (e.Key == Key.Enter || e.Key == Key.Escape)
					FinishBookmarkEdit(item);
			};
			grid.Children.Add(textBoxName);
			Grid.SetColumn(textBoxName, 0);
			Grid.SetColumnSpan(textBoxName, 4);

			item.MouseEnter += (o, e) => {
				FrameBookmark data = (FrameBookmark) item.Tag;
				if (data.TextName.Text.Length > 0 && showBookmarkTimes) {
					data.ColumnTimelapse.Width = new GridLength(0);
					data.TextTimelapse.Margin = new Thickness(0);
				}
			};
			item.MouseLeave += (o, e) => {
				FrameBookmark data = (FrameBookmark) item.Tag;
				if (showBookmarkTimes && bookmarkItems.IndexOf(item) > 0) {
					data.ColumnTimelapse.Width = new GridLength(1, GridUnitType.Auto);
					data.TextTimelapse.Margin = TextTimelapseMargin;
				}
			};

			item.Tag = new FrameBookmark(item, columnTimelapse, textTimelapse, textName, textBoxName, frame);
			
			item.Style = Application.Current.Resources[typeof(ListViewItem)] as Style;

			return item;
		}

		private static void UpdateBookmarkItemAt(int index) {
			ListViewItem item = bookmarkItems[index];
			FrameBookmark data = (FrameBookmark) item.Tag;

			data.TextTimelapse.Margin = new Thickness(0);
			data.ColumnTimelapse.Width = new GridLength(0);

			if (index > 0) {
				ListViewItem item2 = bookmarkItems[index - 1];
				FrameBookmark data2 = (FrameBookmark) item2.Tag;
				data.TextTimelapse.Text = "(+" + (data.Frame - data2.Frame) + ")";
				if ((!item.IsMouseOver || data.TextName.Text.Length == 0) && showBookmarkTimes) {
					data.ColumnTimelapse.Width = new GridLength(1, GridUnitType.Auto);
					data.TextTimelapse.Margin = TextTimelapseMargin;
				}
			}
			else {
				data.TextTimelapse.Text = "";
			}
		}

		private static void StartBookmarkEdit(int frame) {
			if (editingBookmark != null) {
				FinishBookmarkEdit(editingBookmark);
			}

			editingBookmark = bookmarkLookup[frame];
			FrameBookmark data = (FrameBookmark) editingBookmark.Tag;

			data.TextBoxName.Visibility = Visibility.Visible;
			data.TextBoxName.Focus();
		}

		private static void FinishBookmarkEdit(ListViewItem item) {
			if (item != editingBookmark || editingBookmark == null) return;
			FrameBookmark data = (FrameBookmark) item.Tag;

			if (bookmarkWindow != null && ListViewBookmarks.SelectedItem != item) {
				if (ListViewBookmarks.SelectedItem != null)
					((ListViewItem) ListViewBookmarks.SelectedItem).Focus();
				else
					FocusManager.SetFocusedElement(bookmarkWindow, bookmarkWindow);
			}

			data.TextBoxName.Visibility = Visibility.Collapsed;
			data.TextName.Text = data.TextBoxName.Text;

			editingBookmark = null;
		}

		private static void RemoveBookmark(int frame) {
			ListViewItem item = bookmarkLookup[frame];
			FrameBookmark data = (FrameBookmark) item.Tag;
			bookmarkLookup.Remove(data.Frame);
			int index = bookmarkItems.IndexOf(item);
			bookmarkItems.RemoveAt(index);
			if (index < bookmarkItems.Count) {
				UpdateBookmarkItemAt(index);
			}
		}

		private static Bitmap GetFrameBitmap(int frame) {
			if (videoReader != null)
				return videoReader.ReadVideoFrame(frame);
			return null;
		}

		private static void PauseIfPlaying() {
			if (Media.IsOpen && Media.MediaState != MediaState.Stop)
				Media.Pause();
		}

		//-----------------------------------------------------------------------------
		// Commands Handlers
		//-----------------------------------------------------------------------------

		private static void OnPreviewKeyDown(object sender, KeyEventArgs e) {
			if (!Media.IsOpen) return;
			var focus = Keyboard.FocusedElement;
			if (!(focus is TextBox) && !(focus is IntegerUpDown) &&
				!(focus is Menu) && !(focus is MenuItem))
			{
				if (Keyboard.Modifiers == ModifierKeys.None) {
					if (e.Key == Key.Left) {
						SkipBack(SkipSpeed.Small);
					}
					else if (e.Key == Key.Right) {
						SkipForward(SkipSpeed.Small);
					}
					else if (e.Key == Key.P || e.Key == Key.Space) {
						PlayPause();
					}
					else if (e.Key >= Key.D1 && e.Key <= Key.D4) {
						VideoScale = 1 + (int) e.Key - (int) Key.D1;
					}
				}
				else if (Keyboard.Modifiers == ModifierKeys.Shift) {
					if (e.Key >= Key.D1 && e.Key <= Key.D6 && Media.IsOpen) {
						double[] speeds = { 0.25, 0.5, 0.75, 1.0, 1.5, 2.0 };
						int index = (int) e.Key - (int) Key.D1;
						mainWindow.DesiredSpeed = speeds[index];
					}
				}
			}
		}


		//-----------------------------------------------------------------------------
		// Commands CanExecute
		//-----------------------------------------------------------------------------

		private static void CanAlwaysExecute(object sender, CanExecuteRoutedEventArgs e) {
			if (!mainWindow.IsLoaded) return;
			e.CanExecute = true;
		}

		private static void CanExecuteIsOpen(object sender, CanExecuteRoutedEventArgs e) {
			if (!mainWindow.IsLoaded) return;
			e.CanExecute = Media.IsOpen;
		}

		private static void CanExecuteScaleDown(object sender, CanExecuteRoutedEventArgs e) {
			if (!mainWindow.IsLoaded) return;
			e.CanExecute = (VideoScale > 1);
		}

		private static void CanExecuteScaleUp(object sender, CanExecuteRoutedEventArgs e) {
			if (!mainWindow.IsLoaded) return;
			e.CanExecute = (VideoScale < 4);
		}


		//-----------------------------------------------------------------------------
		// Properties
		//-----------------------------------------------------------------------------

		public static MediaElement Media {
			get { return mainWindow.Media; }
		}

		public static Image ImageDifs {
			get { return mainWindow.ImageDifs; }
		}

		public static ListView ListViewBookmarks {
			get { return bookmarkWindow.ListViewBookmarks; }
		}

		public static int CurrentFrame {
			get { return Media.CurrentFrame(); }
			set {
				Media.SeekFrame(value);
				InvalidateBookmark();
				InvalidateDif();
			}
		}

		public static int FrameDuration {
			get {
				if (Media.IsOpen) {
					int duration = (int) videoReader.FrameCount;
					if (Media.NaturalDuration.HasTimeSpan)
						duration = Math.Min(Media.FrameDuration(), duration);
					return (int) duration;
				}
				return 1;
			}
		}

		public static TimeSpan CurrentTime {
			get { return Media.Position; }
		}

		public static TimeSpan Duration {
			get {
				if (Media.NaturalDuration.HasTimeSpan)
					return Media.NaturalDuration.TimeSpan;
				return TimeSpan.Zero;
			}
		}

		public static int MediumIncrement {
			get { return mediumIncrement; }
			set {
				mediumIncrement = value;
				if (mainWindow.IsLoaded)
					mainWindow.UpdateControls();
			}
		}

		public static int LargeIncrement {
			get { return largeIncrement; }
			set {
				largeIncrement = value;
				if (mainWindow.IsLoaded)
					mainWindow.UpdateControls();
			}
		}

		public static bool ShowDif {
			get { return showDif; }
			set {
				showDif = value;
				ImageDifs.Visibility = (showDif ? Visibility.Visible : Visibility.Hidden);
			}
		}

		public static bool ShowSoundVisualizer {
			get { return showSoundVisualizer; }
			set {
				if (showSoundVisualizer != value) {
					double oldScale = (mainWindow.IsLoaded ? VideoScale : 0);
					showSoundVisualizer = value;
					if (mainWindow.IsLoaded) {
						mainWindow.MakeRoomForAudioVisualizer(oldScale);
						mainWindow.UpdateControls();
					}
				}
			}
		}

		public static bool ShowBookmarkList {
			get { return bookmarkWindow != null; }
			set {
				if (ShowBookmarkList != value) {
					if (value) {
						bookmarkWindow = BookmarkWindow.Show(mainWindow, bookmarkItems);
						bookmarkWindow.Closed += OnBookmarkWindowClosed;
					}
					else {
						bookmarkWindow.Close();
					}
					if (mainWindow.IsLoaded)
						mainWindow.UpdateControls();
				}
			}
		}

		public static short[] Samples {
			get { return samples; }
		}

		public static int SampleCount {
			get { return samples.Length; }
		}

		public static int SampleRate {
			get { return sampleRate; }
		}

		public static double VideoScale {
			get { return mainWindow.VideoScale; }
			set { mainWindow.VideoScale = value; }
		}

		public static double StartupVideoScale {
			get { return startupVideoScale; }
		}

		public static bool ForceNearestNeighbor {
			get { return forceNearestNeighbor; }
			set {
				if (forceNearestNeighbor != value) {
					forceNearestNeighbor = value;
					if (mainWindow.IsLoaded) {
						mainWindow.UpdateScalingMode();
						mainWindow.UpdateControls();
					}
				}
			}
		}

		public static double Volume {
			get { return volume; }
			set {
				volume = value;
				Media.Volume = value;
				if (mainWindow.IsLoaded)
					mainWindow.UpdateControls();
			}
		}

		public static bool IsMuted {
			get { return Media.IsMuted; }
			set {
				Media.IsMuted = value;
				if (mainWindow.IsLoaded)
					mainWindow.UpdateControls();
			}
		}

		public static bool AutoPlay {
			get { return autoPlay; }
			set {
				autoPlay = value;
				if (mainWindow.IsLoaded)
					mainWindow.UpdateControls();
			}
		}

		public static bool ShowBookmarkTimes {
			get { return showBookmarkTimes; }
			set {
				showBookmarkTimes = value;
				for (int i = 0; i < bookmarkItems.Count; i++) {
					UpdateBookmarkItemAt(i);
				}
				if (bookmarkWindow != null)
					bookmarkWindow.UpdateControls();
			}
		}
	}
}
