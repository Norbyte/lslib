using LSLib.LS;
using LSLib.VirtualTextures;
using System;
using System.IO;
using System.Linq;

namespace LSTools.VTexTool;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: VTexTool.exe <build root> <configuration xml>");
            Environment.Exit(1);
        }

        Console.WriteLine($"LSLib Virtual Tile Set Generator (v{Common.MajorVersion}.{Common.MinorVersion}.{Common.PatchVersion})");

        try
        {
            var configPath = Path.Combine(args[0], args[1]);
            var descriptor = new TileSetDescriptor
            {
                RootPath = args[0]
            };
            descriptor.Load(configPath);

            var builder = new TileSetBuilder(descriptor.Config);
            foreach (var texture in descriptor.Textures)
            {
                var layerPaths = texture.Layers.Select(name => name != null ? Path.Combine(descriptor.SourceTexturePath, name) : null).ToList();
                builder.AddTexture(texture.Name, layerPaths);
            }

            builder.Build(descriptor.VirtualTexturePath);
        }
        catch (InvalidDataException e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e.Message);
            Console.ForegroundColor = ConsoleColor.Gray;
            Environment.Exit(1);
        }
        catch (FileNotFoundException e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e.Message);
            Console.ForegroundColor = ConsoleColor.Gray;
            Environment.Exit(1);
        }
    }
}
