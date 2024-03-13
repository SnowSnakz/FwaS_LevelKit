using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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

var palette = new Rgba32[16]
{
    new(  0,   0,   0, 255), // 0) Nothing
    new(255,   0,   0, 255), // 1) Enemy Spawn  (The type of enemy that spawns depends on adjacent tiles. Depending on the current difficulty, some of these may be ignored.)
    new(193, 132,  84, 255), // 2) Wall
    new( 44, 106,  31, 255), // 3) Decoration   (Attempts to fill area with appropriate decoration tiles, some tiles may be replaced with Nothing.)
    new(255, 255,   0, 255), // 4) Collectable  (Spawns as a Yellow Coin, unless level is a Shop.)
    new( 67,   0, 255, 255), // 5) Tide Emitter (Only one is chosen at random.)
    new(115, 255, 249, 255), // 6) Air Bubble   (Turns into an Air Bubble Emitter if adjacent to wall, becomes an Air Pocket if a 2x2 or larger area is defined.)
    new( 90, 189,  99, 255), // 7) Platform     (Turns into a floating platform object if by itself.)
    new(224, 189,  62, 255), // 8) Wall         (Turns into Nothing if the cloest edge is open.)
    new(140, 180,  78, 255), // 9) Platform     (Turns into Nothing if the closest edge is closed.)
    new( 84, 143,  39, 255), // A) Decoration   (Turns into Wall if the closest edge is closed.)
    new( 79, 157,  93, 255), // B) Decoration   (Turns into Nothing if the closest edge is open.)
    new(157,  59,  59, 255), // C) Spike Hazard
    new(172,  99, 201, 255), // D) Purple Coin  (Spawns as a Yellow coin, turns purple after collecting.)
    new(112,  75,  39, 255), // E) Secret Area  (A collisionless wall that fades when the player overlaps to reveal a hidden area.)
    new(210, 167,   0, 255)  // F) Secret Coin  (Same as Secret Area except a Yellow Coin also spawns here.)
};

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
