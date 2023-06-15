using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace OpencvDesktop; 

/// <summary>
/// <para>
/// Converts a string path to a bitmap asset.
/// </para>
/// <para>
/// The asset must be in the same assembly as the program. If it isn't,
/// specify "avares://<assemblynamehere>/" in front of the path to the asset.
/// </para>
/// </summary>
public class BitmapAssetValueConverter : IValueConverter
{
    public static BitmapAssetValueConverter Instance = new BitmapAssetValueConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return null;

        if (value is string rawUri && targetType.IsAssignableFrom(typeof(Bitmap)))
        {
            Uri uri;

            // Allow for assembly overrides
            if (rawUri.StartsWith("avares://"))
            {
                uri = new Uri(rawUri);
            }
            else
            {
                //string assemblyName = Assembly.GetEntryAssembly().GetName().Name;
                //uri = new Uri($"avares://{assemblyName}/{rawUri}");
                uri = new Uri(rawUri);
            }

            // var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            // var asset = assets.Open(uri);
            using var stream = File.Open(uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return new Bitmap(memoryStream);
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
       return Convert(value, targetType, parameter, culture);
    }
}