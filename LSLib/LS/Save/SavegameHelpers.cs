using LSLib.Granny;
using LSLib.LS.Enums;
using LSLib.LS.Story;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LSLib.LS.Save
{
    public class SavegameHelpers : IDisposable
    {
        private PackageReader Reader;
        private Package Package;

        public SavegameHelpers(string path)
        {
            Reader = new PackageReader(path);
            Package = Reader.Read();
        }

        public void Dispose()
        {
            Reader.Dispose();
        }

        public Resource LoadGlobals()
        {
            AbstractFileInfo globalsInfo = Package.Files.FirstOrDefault(p => p.Name.ToLowerInvariant() == "globals.lsf");
            if (globalsInfo == null)
            {
                throw new InvalidDataException("The specified package is not a valid savegame (globals.lsf not found)");
            }

            Resource resource;
            Stream rsrcStream = globalsInfo.MakeStream();
            try
            {
                using (var rsrcReader = new LSFReader(rsrcStream))
                {
                    resource = rsrcReader.Read();
                }
            }
            finally
            {
                globalsInfo.ReleaseStream();
            }

            return resource;
        }

        public Story.Story LoadStory(Stream s)
        {
            var reader = new StoryReader();
            return reader.Read(s);
        }

        public Story.Story LoadStory()
        {
            AbstractFileInfo storyInfo = Package.Files.FirstOrDefault(p => p.Name == "StorySave.bin");
            if (storyInfo != null)
            {
                Stream rsrcStream = storyInfo.MakeStream();
                try
                {
                    return LoadStory(rsrcStream);
                }
                finally
                {
                    storyInfo.ReleaseStream();
                }
            }
            else
            {
                var globals = LoadGlobals();

                Node storyNode = globals.Regions["Story"].Children["Story"][0];
                var storyStream = new MemoryStream(storyNode.Attributes["Story"].Value as byte[] ?? throw new InvalidOperationException("Cannot proceed with null Story node"));
                return LoadStory(storyStream);
            }
        }

        public MemoryStream ResaveStoryToGlobals(Story.Story story, ResourceConversionParameters conversionParams)
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
            var rsrcWriter = new LSFWriter(rewrittenStream);
            rsrcWriter.Version = conversionParams.LSF;
            rsrcWriter.EncodeSiblingData = false;
            rsrcWriter.Write(globals);
            rewrittenStream.Seek(0, SeekOrigin.Begin);
            return rewrittenStream;
        }

        public void ResaveStory(Story.Story story, Game game, string path)
        {
            // Re-package global.lsf/StorySave.bin
            var rewrittenPackage = new Package();
            var conversionParams = ResourceConversionParameters.FromGameVersion(game);

            AbstractFileInfo storyBin = Package.Files.FirstOrDefault(p => p.Name == "StorySave.bin");
            if (storyBin == null)
            {
                var globalsStream = ResaveStoryToGlobals(story, conversionParams);

                AbstractFileInfo globalsLsf = Package.Files.FirstOrDefault(p => p.Name.ToLowerInvariant() == "globals.lsf");
                StreamFileInfo globalsRepacked = StreamFileInfo.CreateFromStream(globalsStream, globalsLsf.Name);
                rewrittenPackage.Files.Add(globalsRepacked);

                List<AbstractFileInfo> files = Package.Files.Where(x => x.Name.ToLowerInvariant() != "globals.lsf").ToList();
                rewrittenPackage.Files.AddRange(files);
            }
            else
            {
                // Save story resource and pack into the Story.Story attribute in globals.lsf
                var storyStream = new MemoryStream();
                var storyWriter = new StoryWriter();
                storyWriter.Write(storyStream, story, true);
                storyStream.Seek(0, SeekOrigin.Begin);

                StreamFileInfo storyRepacked = StreamFileInfo.CreateFromStream(storyStream, "StorySave.bin");
                rewrittenPackage.Files.Add(storyRepacked);

                List<AbstractFileInfo> files = Package.Files.Where(x => x.Name != "StorySave.bin").ToList();
                rewrittenPackage.Files.AddRange(files);
            }

            using (var packageWriter = new PackageWriter(rewrittenPackage, path))
            {
                packageWriter.Version = conversionParams.PAKVersion;
                packageWriter.Compression = CompressionMethod.Zlib;
                packageWriter.CompressionLevel = CompressionLevel.DefaultCompression;
                packageWriter.Write();
            }
        }
    }
}
