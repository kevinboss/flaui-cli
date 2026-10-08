using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using BitmapDecoder = Windows.Graphics.Imaging.BitmapDecoder;

namespace FlauiCli.Core;

/// <summary>
/// Reads text off a captured screen area with the Windows OCR engine. This is the fallback for UI
/// that exposes nothing through UI Automation: every position it reports is a screen coordinate
/// (the center of the line or word), ready to pass to <c>click-at</c>.
/// </summary>
public static class TextRecognizer
{
    private const double MaxUpscale = 2;

    public static string Read(Bitmap capture, Point origin, bool words, string? language)
    {
        var engine = CreateEngine(language);
        var scale = Math.Min(MaxUpscale, (double)OcrEngine.MaxImageDimension / Math.Max(capture.Width, capture.Height));

        using var scaled = Scale(capture, scale);
        var result = engine.RecognizeAsync(ToSoftwareBitmap(scaled)).AsTask().GetAwaiter().GetResult();

        var sb = new StringBuilder();
        foreach (var line in result.Lines)
        {
            var rects = line.Words.Select(w => w.BoundingRect).ToList();
            sb.Append("- line \"").Append(Escape(line.Text)).Append("\" [at=").Append(Center(rects, origin, scale)).Append("]\n");
            if (!words) continue;
            foreach (var word in line.Words)
                sb.Append("  - word \"").Append(Escape(word.Text)).Append("\" [at=").Append(Center([word.BoundingRect], origin, scale)).Append("]\n");
        }
        return sb.Length > 0 ? sb.ToString() : "(no text recognized)\n";
    }

    private static OcrEngine CreateEngine(string? language)
    {
        var engine = string.IsNullOrWhiteSpace(language)
            ? OcrEngine.TryCreateFromUserProfileLanguages()
            : OcrEngine.TryCreateFromLanguage(new Language(language));
        if (engine != null) return engine;

        var installed = string.Join(", ", OcrEngine.AvailableRecognizerLanguages.Select(l => l.LanguageTag));
        throw new InvalidOperationException(
            $"No OCR engine for {(string.IsNullOrWhiteSpace(language) ? "the user profile languages" : $"'{language}'")}. Installed OCR languages: {installed}");
    }

    private static Bitmap Scale(Bitmap source, double scale)
    {
        var target = new Bitmap(Math.Max(1, (int)Math.Round(source.Width * scale)), Math.Max(1, (int)Math.Round(source.Height * scale)));
        using var g = Graphics.FromImage(target);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(source, 0, 0, target.Width, target.Height);
        return target;
    }

    private static SoftwareBitmap ToSoftwareBitmap(Bitmap bitmap)
    {
        using var png = new MemoryStream();
        bitmap.Save(png, ImageFormat.Png);
        png.Position = 0;
        var decoder = BitmapDecoder.CreateAsync(png.AsRandomAccessStream()).AsTask().GetAwaiter().GetResult();
        return decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied).AsTask().GetAwaiter().GetResult();
    }

    private static string Center(IReadOnlyList<Windows.Foundation.Rect> rects, Point origin, double scale)
    {
        var left = rects.Min(r => r.X);
        var top = rects.Min(r => r.Y);
        var right = rects.Max(r => r.X + r.Width);
        var bottom = rects.Max(r => r.Y + r.Height);
        var x = origin.X + (int)Math.Round((left + right) / 2 / scale);
        var y = origin.Y + (int)Math.Round((top + bottom) / 2 / scale);
        return $"{x},{y}";
    }

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
