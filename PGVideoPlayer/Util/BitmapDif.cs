using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

namespace PGVideoPlayer.Util {

	public struct UnlockedBitmap {
		public Bitmap Bitmap { get; }
		public BitmapData Data { get; }
		public Rectangle Region { get; }

		public UnlockedBitmap(Bitmap bitmap, BitmapData data, Rectangle region) {
			Bitmap = bitmap;
			Data = data;
			Region = region;
		}
	}

	public static class BitmapDif {


		public static UnlockedBitmap Unlock(Bitmap bitmap) {
			return Unlock(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
		}

		public static UnlockedBitmap Unlock(Bitmap bitmap, int x, int y, int width, int height) {
			return Unlock(bitmap, new Rectangle(x, y, width, height));
		}

		public static UnlockedBitmap Unlock(Bitmap bitmap, Rectangle region) {
			return new UnlockedBitmap(bitmap,
				bitmap.LockBits(region, ImageLockMode.ReadWrite, bitmap.PixelFormat),
				region);
		}
		
		public static void Lock(UnlockedBitmap unlockedBmp, bool dispose = false) {
			unlockedBmp.Bitmap.UnlockBits(unlockedBmp.Data);
			if (dispose)
				unlockedBmp.Bitmap.Dispose();
		}

		public static void Dispose(UnlockedBitmap unlockedBmp) {
			unlockedBmp.Bitmap.Dispose();
		}

		public static bool ContainsDifferences(UnlockedBitmap unlockedA, UnlockedBitmap unlockedB) {
			int npixels = unlockedA.Bitmap.Height * unlockedA.Data.Stride / 3;
			unsafe {
				byte* pA = (byte*) unlockedA.Data.Scan0.ToPointer();
				byte* pB = (byte*) unlockedB.Data.Scan0.ToPointer();

				for (int i = 0; i < npixels; i++) {
					if (pA[0] != pB[0] || pA[1] != pB[1] || pA[2] != pB[2]) {
						return true;
					}

					pA += 3;
					pB += 3;
				}
			}
			return false;
		}

		public static bool ContainsDifferences(UnlockedBitmap unlockedA, Bitmap b, bool disposeIfNoDifferences = false) {
			bool hasDif;
			var unlockedB = Unlock(b);

			hasDif = ContainsDifferences(unlockedA, unlockedB);

			Lock(unlockedB, !hasDif && disposeIfNoDifferences);
			return hasDif;
		}

		public static bool ContainsDifferences(Bitmap a, Bitmap b, bool disposeIfNoDifferences = false) {
			bool hasDif;
			var unlockedA = Unlock(a);
			var unlockedB = Unlock(b);

			hasDif = ContainsDifferences(unlockedA, unlockedB);

			Lock(unlockedA, !hasDif && disposeIfNoDifferences);
			Lock(unlockedB, !hasDif && disposeIfNoDifferences);
			return hasDif;
		}


		public static Bitmap ReturnDifferences(UnlockedBitmap unlockedA, UnlockedBitmap unlockedB) {
			Bitmap result = new Bitmap(unlockedA.Bitmap.Width, unlockedA.Bitmap.Height, PixelFormat.Format32bppArgb);
			Graphics g = BitmapFactory.CreateGraphics(result);
			g.Clear(Color.Transparent);
			g.Dispose();
			var unlockedRes = Unlock(result);
			int npixels = unlockedA.Bitmap.Height * unlockedA.Data.Stride / 3;
			unsafe
			{
				byte* pA = (byte*) unlockedA.Data.Scan0.ToPointer();
				byte* pB = (byte*) unlockedB.Data.Scan0.ToPointer();
				int* pRes = (int*) unlockedRes.Data.Scan0.ToPointer();

				for (int i = 0; i < npixels; i++) {
					if (pA[0] != pB[0] || pA[1] != pB[1] || pA[2] != pB[2]) {
						pRes[i] = Color.Red.ToArgb();
					}

					pA += 3;
					pB += 3;
				}
			}

			Lock(unlockedRes);
			return result;
		}

		public static Bitmap ReturnDifferences(UnlockedBitmap unlockedA, Bitmap b, bool dispose = false) {
			Bitmap result;
			var unlockedB = Unlock(b);

			result = ReturnDifferences(unlockedA, unlockedB);

			Lock(unlockedB, dispose);
			return result;
		}

		public static Bitmap ReturnDifferences(Bitmap a, Bitmap b, bool dispose = false) {
			Bitmap result;
			var unlockedA = Unlock(a);
			var unlockedB = Unlock(b);

			result = ReturnDifferences(unlockedA, unlockedB);

			Lock(unlockedA, dispose);
			Lock(unlockedB, dispose);
			return result;
		}

	}
}
