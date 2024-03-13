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

Console.WriteLine($"╔╡ Writing to {args[0]}");

using var output = new MemoryStream();
output.WriteByte((byte)(args.Length - 1));

int total = args.Length - 1;
for(var i = 1; i < args.Length; i++)
{
    using var input = File.OpenRead(args[i]);
    input.Read(buf);

    if(buf.SequenceEqual(roomSignature))
    {
        input.CopyTo(output);

        Console.WriteLine($"╟───┤ Appended {args[i]}.");
    }
    else if(buf.SequenceEqual(signature))
    {
        using var decompress = new InflaterInputStream(input);

        using var lib = new MemoryStream();
        decompress.CopyTo(lib);

        lib.Read(buf);

        int count = BitConverter.ToInt32(buf, 0);
        total = total - 1 + count;

        long pos = output.Position;
        output.Position = 0;
        output.WriteByte((byte)total);
        output.Position = pos;

        lib.CopyTo(output);

        Console.WriteLine($"╟───┤ Appended {count} from {args[i]}.");
    }
    else
    {
        Console.WriteLine($"╟─x─┤ {args[i]} unsupported.");
    }
}

Console.Write("╚═╡ Compressing... ");

output.Position = 0;

using var outFile = File.OpenWrite(args[0]);
using var compressed = new DeflaterOutputStream(outFile);

outFile.Write(signature);
output.CopyTo(compressed);
compressed.Flush();

Console.WriteLine($"DONE! <{args[0]}>");
