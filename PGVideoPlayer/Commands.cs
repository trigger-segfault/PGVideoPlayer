using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PGVideoPlayer {
	public static class Commands {

		//-----------------------------------------------------------------------------
		// General
		//-----------------------------------------------------------------------------
		
		/// <summary>The command to exit the designer.</summary>
		public static readonly RoutedUICommand Exit = new RoutedUICommand(
			"Exit", "Exit", typeof(Commands),
			new InputGestureCollection() {
				new KeyGesture(Key.W, ModifierKeys.Control) });

		/// <summary>The command to save the current preferences as the defaults.</summary>
		public static readonly RoutedUICommand SavePreferences = new RoutedUICommand(
			"SavePreferences", "Save Preferences", typeof(Commands));


		//-----------------------------------------------------------------------------
		// Frames
		//-----------------------------------------------------------------------------

		/// <summary>The command to bookmark the current frame.</summary>
		public static readonly RoutedUICommand BookmarkFrame = new RoutedUICommand(
			"BookmarkFrame", "Bookmark Frame", typeof(Commands),
			new InputGestureCollection() {
				new KeyGesture(Key.B, ModifierKeys.Control) });

		/// <summary>The command to clear all bookmarked frames.</summary>
		public static readonly RoutedUICommand ClearBookmarks = new RoutedUICommand(
			"ClearBookmarks", "Clear Bookmarks", typeof(Commands));

		/// <summary>The command to toggle visibility of bookmark time advances.</summary>
		public static readonly RoutedUICommand BookmarkTimes = new RoutedUICommand(
			"BookmarkTimes", "Bookmark Times", typeof(Commands));

		/// <summary>The command to find the next frame with differences.</summary>
		public static readonly RoutedUICommand FindNextDif = new RoutedUICommand(
			"FindNextDif", "Find Next Dif", typeof(Commands),
			new InputGestureCollection() {
				new KeyGesture(Key.F, ModifierKeys.Control) });

		/// <summary>The command to find the previous frame with differences.</summary>
		public static readonly RoutedUICommand FindPreviousDif = new RoutedUICommand(
			"FindPreviousDif", "Find Previous Dif", typeof(Commands),
			new InputGestureCollection() {
				new KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Shift) });

		/// <summary>The command to show the current frame differences.</summary>
		public static readonly RoutedUICommand ShowDif = new RoutedUICommand(
			"ShowDif", "Show Dif", typeof(Commands),
			new InputGestureCollection() {
				new KeyGesture(Key.D, ModifierKeys.Control) });


		//-----------------------------------------------------------------------------
		// View
		//-----------------------------------------------------------------------------

		/// <summary>The command to toggle the bookmark list window.</summary>
		public static readonly RoutedUICommand BookmarkList = new RoutedUICommand(
			"BookmarkList", "Bookmark List", typeof(Commands),
			new InputGestureCollection() {
				new KeyGesture(Key.B, ModifierKeys.Control | ModifierKeys.Shift) });

		/// <summary>The command to toggle the sound visualizer.</summary>
		public static readonly RoutedUICommand SoundVisualizer = new RoutedUICommand(
			"SoundVisualizer", "Sound Visualizer", typeof(Commands),
			new InputGestureCollection() {
				new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift) });

		/// <summary>The command to scale down the video.</summary>
		public static readonly RoutedUICommand ScaleDown = new RoutedUICommand(
			"ScaleDown", "Scale Down", typeof(Commands),
			new InputGestureCollection() {
				new KeyGesture(Key.PageDown) });

		/// <summary>The command to scale up the video.</summary>
		public static readonly RoutedUICommand ScaleUp = new RoutedUICommand(
			"ScaleUp", "Scale Up", typeof(Commands),
			new InputGestureCollection() {
				new KeyGesture(Key.PageUp) });

		/// <summary>The command to toggle forcing of nearest neighbor.</summary>
		public static readonly RoutedUICommand ForceNearestNeighbor = new RoutedUICommand(
			"ForceNearestNeighbor", "Force Nearest Neighbor", typeof(Commands),
			new InputGestureCollection() {
				new KeyGesture(Key.N, ModifierKeys.Control) });


		//-----------------------------------------------------------------------------
		// Playback
		//-----------------------------------------------------------------------------

		/// <summary>The command to toggle mute.</summary>
		public static readonly RoutedUICommand Mute = new RoutedUICommand(
			"Mute", "Mute", typeof(Commands),
			new InputGestureCollection() {
				new KeyGesture(Key.M, ModifierKeys.Control) });

		/// <summary>The command to toggle automatically playing videos when they're opened.</summary>
		public static readonly RoutedUICommand AutoPlay = new RoutedUICommand(
			"AutoPlay", "Auto Play", typeof(Commands));

		/// <summary>The command to play or pause the video.</summary>
		public static readonly RoutedUICommand PlayPause = new RoutedUICommand(
			"PlayPause", "Play/Pause", typeof(Commands));

		/// <summary>The command to stop the video.</summary>
		public static readonly RoutedUICommand Stop = new RoutedUICommand(
			"Stop", "Stop", typeof(Commands),
			new InputGestureCollection() {
				new KeyGesture(Key.S, ModifierKeys.Control) });

		/// <summary>The command to restart the video.</summary>
		public static readonly RoutedUICommand Restart = new RoutedUICommand(
			"Restart", "Restart", typeof(Commands),
			new InputGestureCollection() {
				new KeyGesture(Key.R, ModifierKeys.Control) });

		/// <summary>The command to end the video.</summary>
		public static readonly RoutedUICommand End = new RoutedUICommand(
			"End", "End", typeof(Commands),
			new InputGestureCollection() {
				new KeyGesture(Key.E, ModifierKeys.Control) });

		/// <summary>The command to skip back one frame in the video.</summary>
		public static readonly RoutedUICommand PreviousFrame = new RoutedUICommand(
			"PreviousFrame", "Previous Frame", typeof(Commands));

		/// <summary>The command to skip forward one frame in the video.</summary>
		public static readonly RoutedUICommand NextFrame = new RoutedUICommand(
			"NextFrame", "Next Frame", typeof(Commands));

		/// <summary>The command to skip back a medium amount of frames in the video.</summary>
		public static readonly RoutedUICommand SkipBackMedium = new RoutedUICommand(
			"SkipBackMedium", "Skip Back Medium", typeof(Commands),
			new InputGestureCollection() {
				new KeyGesture(Key.Left, ModifierKeys.Control) });

		/// <summary>The command to skip forward a medium amount of frames in the video.</summary>
		public static readonly RoutedUICommand SkipForwardMedium = new RoutedUICommand(
			"SkipForwardMedium", "Skip Forward Medium", typeof(Commands),
			new InputGestureCollection() {
				new KeyGesture(Key.Right, ModifierKeys.Control) });

		/// <summary>The command to skip back a large amount of frames in the video.</summary>
		public static readonly RoutedUICommand SkipBackLarge = new RoutedUICommand(
			"SkipBackLarge", "Skip Back Large", typeof(Commands),
			new InputGestureCollection() {
				new KeyGesture(Key.Left, ModifierKeys.Shift) });

		/// <summary>The command to skip forward a large amount of frames in the video.</summary>
		public static readonly RoutedUICommand SkipForwardLarge = new RoutedUICommand(
			"SkipForwardLarge", "Skip Forward Large", typeof(Commands),
			new InputGestureCollection() {
				new KeyGesture(Key.Right, ModifierKeys.Shift) });
	}
}
