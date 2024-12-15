using System;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;


namespace AutoFilterPresets.Helpers
{
    public class ImageHelper
    {
        public static void UnloadImagesWithSource(Window window, string targetFilePath)
        {
            Uri fileUri = new Uri(targetFilePath, UriKind.Absolute);
            foreach (var image in VisualTreeHelperEx.FindVisualChildren<Image>(window))
            {
                if (image.Source is BitmapFrame bitmapFrame && bitmapFrame.Decoder != null)
                {
                    foreach (var frame in bitmapFrame.Decoder.Frames)
                    {
                        if (frame is BitmapFrame bf && bf.Decoder != null && bf.Decoder.ToString() == fileUri.ToString())
                        {
                            var method = typeof(Decoder).GetMethod("Finalize", BindingFlags.NonPublic | BindingFlags.Instance);
                            method.Invoke(bitmapFrame.Decoder, null);
                            break;
                        }
                    }
                }
            }
        }


    }
}