using System;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
#if WINDOWS_UWP
using Windows.ApplicationModel.Core;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.Core;
#else
using System.IO;
#endif

namespace Emmellsoft.IoT.Rpi.SenseHat;

#if WINDOWS_UWP
	public static class NativePixelSupport
{
		/// <summary>
		/// Gets a 2-dimensional pixel array from an image.
		/// </summary>
		/// <param name="imageUri">The URI to the image.</param>
		public static async Task<Color[,]> GetPixels(Uri imageUri)
		{
			Color[,] pixels = null;

			var pixelsLoadedEvent = new ManualResetEvent(false);

			await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
			{
				StorageFile imageFile = await StorageFile.GetFileFromApplicationUriAsync(imageUri);

				using (var imageContent = await imageFile.OpenReadAsync())
				{
					BitmapDecoder bitmapDecoder = await BitmapDecoder.CreateAsync(imageContent);

					pixels = new Color[bitmapDecoder.PixelWidth, bitmapDecoder.PixelHeight];

					PixelDataProvider pixelDataProvider = await bitmapDecoder.GetPixelDataAsync(
						BitmapPixelFormat.Bgra8,
						BitmapAlphaMode.Straight,
						new BitmapTransform(),
						ExifOrientationMode.IgnoreExifOrientation,
						ColorManagementMode.DoNotColorManage);

					byte[] pixelData = pixelDataProvider.DetachPixelData();

					int pixelDataIndex = 0;
					for (int y = 0; y < bitmapDecoder.PixelHeight; y++)
					{
						for (int x = 0; x < bitmapDecoder.PixelWidth; x++)
						{
							byte b = pixelData[pixelDataIndex];
							byte g = pixelData[pixelDataIndex + 1];
							byte r = pixelData[pixelDataIndex + 2];
							byte a = pixelData[pixelDataIndex + 3];

							pixels[x, y] = Color.FromArgb(a, r, g, b);

							pixelDataIndex += 4;
						}
					}
				}

				pixelsLoadedEvent.Set();
			});

			pixelsLoadedEvent.WaitOne();

			return pixels;
		}
	}
#elif NETFRAMEWORK
public static class NativePixelSupport
{
    public async static Task<Color[,]> GetPixels(Uri imageUri)
    {
        if (imageUri.IsFile)
        {
            if (!File.Exists(imageUri.LocalPath))
            {
                throw new FileNotFoundException($"File Missing: {imageUri.LocalPath}");
            }

            var bitmap = await Task.Run(() => new Bitmap(imageUri.LocalPath));

            Color[,] pixels = new Color[bitmap.Width, bitmap.Height];
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    pixels[x, y] = bitmap.GetPixel(x, y);
                }
            }

            return pixels;
        }

        throw new NotImplementedException();
    }
}
#endif
