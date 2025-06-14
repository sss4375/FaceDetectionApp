using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

public static class NoseDetectorClient
{
    private static readonly HttpClient _client = new HttpClient();

    public static async Task<List<(int X, int Y, int Width)>> DetectNosesFromBitmapAsync(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Jpeg);
        stream.Seek(0, SeekOrigin.Begin);

        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream)
        {
            Headers =
            {
                ContentType = new MediaTypeHeaderValue("image/jpeg")
            }
        }, "file", "image.jpg");

        var response = await _client.PostAsync("http://127.0.0.1:5001/detect", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = new List<(int, int, int)>();
        foreach (var nose in root.GetProperty("noses").EnumerateArray())
        {
            int x = nose.GetProperty("x").GetInt32();
            int y = nose.GetProperty("y").GetInt32();
            int width = nose.GetProperty("width").GetInt32();
            result.Add((x, y, width));
        }

        return result;
    }
}