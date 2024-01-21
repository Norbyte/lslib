using LSLib.LS.Enums;
using LSLib.LS.Story;

namespace LSLib.LS.Save;

public class SavegameHelpers : IDisposable
{
    private readonly Package Package;

    public SavegameHelpers(string path)
    {
        var reader = new PackageReader();
        Package = reader.Read(path);
    }

    public void Dispose()
    {
        Package.Dispose();
    }

    public Resource LoadGlobals()
    {
        var globalsInfo = Package.Files.FirstOrDefault(p => p.Name.ToLowerInvariant() == "globals.lsf");
        if (globalsInfo == null)
        {
            throw new InvalidDataException("The specified package is not a valid savegame (globals.lsf not found)");
        }

        using var rsrcStream = globalsInfo.CreateContentReader();
        using var rsrcReader = new LSFReader(rsrcStream);
        return rsrcReader.Read();
    }

    public Story.Story LoadStory(Stream s)
    {
        var reader = new StoryReader();
        return reader.Read(s);
    }

    public Story.Story LoadStory()
    {
        var storyInfo = Package.Files.FirstOrDefault(p => p.Name == "StorySave.bin");
        if (storyInfo != null)
        {
            using var rsrcStream = storyInfo.CreateContentReader();
            return LoadStory(rsrcStream);
        }
        else
        {
            var globals = LoadGlobals();

            Node storyNode = globals.Regions["Story"].Children["Story"][0];
            var storyStream = new MemoryStream(storyNode.Attributes["Story"].Value as byte[] ?? throw new InvalidOperationException("Cannot proceed with null Story node"));
            return LoadStory(storyStream);
        }
    }

    public byte[] ResaveStoryToGlobals(Story.Story story, ResourceConversionParameters conversionParams)
    {
        var globals = LoadGlobals();

        // Save story resource and pack into the Story.Story attribute in globals.lsf
        using (var storyStream = new MemoryStream())
        {
            var storyWriter = new StoryWriter();
            storyWriter.Write(storyStream, story, true);

            var storyNode = globals.Regions["Story"].Children["Story"][0];
            storyNode.Attributes["Story"].Value = storyStream.ToArray();
        }

        // Save globals.lsf
        var rewrittenStream = new MemoryStream();
        var rsrcWriter = new LSFWriter(rewrittenStream)
        {
            Version = conversionParams.LSF,
            EncodeSiblingData = false
        };
        rsrcWriter.Write(globals);
        rewrittenStream.Seek(0, SeekOrigin.Begin);
        return rewrittenStream.ToArray();
    }

    public void ResaveStory(Story.Story story, Game game, string path)
    {
        // Re-package global.lsf/StorySave.bin
        var conversionParams = ResourceConversionParameters.FromGameVersion(game);

        var build = new PackageBuildData
        {
            Version = conversionParams.PAKVersion,
            Compression = CompressionMethod.Zlib,
            CompressionLevel = LSCompressionLevel.Default
        };

        var storyBin = Package.Files.FirstOrDefault(p => p.Name == "StorySave.bin");
        if (storyBin == null)
        {
            var globals = ResaveStoryToGlobals(story, conversionParams);

            var globalsLsf = Package.Files.FirstOrDefault(p => p.Name.ToLowerInvariant() == "globals.lsf");
            var globalsRepacked = PackageBuildInputFile.CreateFromBlob(globals, globalsLsf.Name);
            build.Files.Add(globalsRepacked);

            foreach (var file in Package.Files.Where(x => x.Name.ToLowerInvariant() != "globals.lsf"))
            {
                using var stream = file.CreateContentReader();
                var contents = new byte[stream.Length];
                stream.ReadExactly(contents, 0, contents.Length);

                build.Files.Add(PackageBuildInputFile.CreateFromBlob(contents, file.Name));
            }
        }
        else
        {
            // Save story resource and pack into the Story.Story attribute in globals.lsf
            var storyStream = new MemoryStream();
            var storyWriter = new StoryWriter();
            storyWriter.Write(storyStream, story, true);

            var storyRepacked = PackageBuildInputFile.CreateFromBlob(storyStream.ToArray(), "StorySave.bin");
            build.Files.Add(storyRepacked);

            foreach (var file in Package.Files.Where(x => x.Name.ToLowerInvariant() != "StorySave.bin"))
            {
                using var stream = file.CreateContentReader();
                var contents = new byte[stream.Length];
                stream.ReadExactly(contents, 0, contents.Length);

                build.Files.Add(PackageBuildInputFile.CreateFromBlob(contents, file.Name));
            }
        }

        using (var packageWriter = new PackageWriter(build, path))
        {
            packageWriter.Write();
        }
    }
}
