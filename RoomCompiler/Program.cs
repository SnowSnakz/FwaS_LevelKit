
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Text.Json;

byte[] signature = [
    0x4C,
    0x59,
    0x54,
    0x00
];

if (args.Length <= 0)
{
    Console.WriteLine("Usage: RoomCompiler <file1> [<file2> <file3>...]");
}

Rgba32[] palette =
[
    new Rgba32(255,   0,   0, 255),
    new Rgba32(193, 132,  84, 255),
    new Rgba32( 44, 106,  31, 255),
    new Rgba32(255, 255,   0, 255),
    new Rgba32( 67,   0, 255, 255),
    new Rgba32(115, 255, 249, 255),
    new Rgba32( 90, 189,  99, 255),
    new Rgba32(224, 189,  62, 255),
    new Rgba32(140, 180,  78, 255)
];

foreach(var file in args)
{
    try
    {
        string path = Path.GetFullPath(file);
        int lastSlash = path.LastIndexOf(Path.PathSeparator);

        string outputFile;
        if (lastSlash == -1) outputFile = "";
        else outputFile = path[..lastSlash];

        outputFile += Path.GetFileNameWithoutExtension(path) + ".dat";

        using Image<Rgba32> image = Image.Load<Rgba32>(path);

        Rgba32[] pixels = new Rgba32[484];
        image.CopyPixelDataTo(pixels);
        
        if((image.Width != 22) || (image.Height != 22))
            throw new NotSupportedException("Image Width/Height must be 22px/22px.");

        using var output = File.OpenWrite(outputFile);
        output.Write(signature);

        bool unknownColor = false;

        Rgba32 color = new Rgba32(0, 0, 0, 128);
        int count = 0;

        for(int i = 0; i < 484; i++)
        {
            Rgba32 pixel = pixels[i];

            if (color != pixel)
            {
                if (count <= 0)
                {
                    color = pixel;
                    count = 1;
                    continue;
                }

                int val = Array.IndexOf(palette, color);
                if (val == -1)
                {
                    val = 0;

                    if (color.A != 0)
                    {
                        if (!unknownColor)
                        {
                            unknownColor = true;
                            Console.WriteLine($"[W] {file} -> This image has an invalid pixel.");
                        }
                    }
                }
                else
                {
                    val++;
                }

                while (count > 0)
                {
                    byte dat = (byte)(val | ((Math.Min(count, 16) - 1) << 4));
                    output.WriteByte(dat);
                    count -= 16;
                }

                color = pixel;
                count = 0;
            }

            count++;
        }

        if(count > 0)
        {
            int val = Array.IndexOf(palette, color);
            if (val == -1)
            {
                val = 0;

                if (color.A != 0)
                {
                    if (!unknownColor)
                    {
                        unknownColor = true;
                        Console.WriteLine($"[W] {file} -> This image has an invalid pixel.");
                    }
                }
            }
            else
            {
                val++;
            }

            while (count > 0)
            {
                byte dat = (byte)(val | ((Math.Min(count, 16) - 1) << 4));
                output.WriteByte(dat);
                count -= 16;
            }
        }

        Console.WriteLine($"[O] {file} -> {outputFile}");
    }
    catch(Exception exception)
    {
        Console.WriteLine($"[E] {file} -> {exception.GetType().FullName}: {exception.Message}");
    }
}
