using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

if (args.Length < 2)
{
    Console.WriteLine("Usages: RoomLinker <OutputFile> <RoomFile1> [<RoomFile2> <RoomFile3>...]");
    return;
}

byte[] signature = [
    0x4C,
    0x49,
    0x42,
    0x00
];

byte[] roomSignature = [
    0x4C,
    0x59,
    0x54,
    0x00
];

byte[] buf = new byte[4];

Console.WriteLine($"╥┤ Writing to {args[0]}");

try
{
    using var output = new MemoryStream();
    output.WriteByte(0);

    int total = 0;
    if(total > 255)
    {
        throw new NotSupportedException("Operation provides more than the maximum (255) supported levels per-file.");
    }

    for (var i = 1; i < args.Length; i++)
    {
        using var input = File.OpenRead(args[i]);
        input.Read(buf);

        if (buf.SequenceEqual(roomSignature))
        {
            total++;
            input.CopyTo(output);

            Console.WriteLine($"╟───┤ Appended {args[i]}.");
        }
        else if (buf.SequenceEqual(signature))
        {
            using var lib = new InflaterInputStream(input);

            int count = lib.ReadByte();
            if(count <= 0)
            {
                Console.WriteLine($"╟─x─┤ Appended 0 from {args[i]}.");
                continue;
            }

            total += count;

            if (total > 255)
            {
                Console.WriteLine($"╟─x─┤ Appended 0 of {count} from {args[i]}.");
                throw new NotSupportedException("Operation provides more than the maximum (255) supported levels per-file.");
            }

            lib.CopyTo(output);

            Console.WriteLine($"╟───┤ Appended {count} from {args[i]}.");
        }
        else
        {
            Console.WriteLine($"╟─x─┤ {args[i]} unsupported.");
        }
    }

    if(total <= 0)
    {
        Console.WriteLine("╨┤ Aborted, nothing to link.");
    }
    else
    {
        Console.Write("╨┤ Compressing... ");

        output.Position = 0;
        output.WriteByte((byte)total);
        output.Position = 0;

        using var outFile = File.OpenWrite(args[0]);
        using var compressed = new DeflaterOutputStream(outFile);

        outFile.Write(signature);
        output.CopyTo(compressed);
        compressed.Flush();

        Console.WriteLine($"DONE! <{args[0]}>");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\r╨┤ Failed! {ex.GetType().FullName}: {ex.Message}");
}

Console.WriteLine();
