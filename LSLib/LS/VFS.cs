namespace LSLib.LS;

public class VFSDirectory
{
    public string Path;
    public Dictionary<string, VFSDirectory> Dirs;
    public Dictionary<string, PackagedFileInfo> Files;

    public VFSDirectory GetOrAddDirectory(string absolutePath, string name)
    {
        Dirs ??= [];

        if (!Dirs.TryGetValue(name, out var dir))
        {
            dir = new VFSDirectory();
            dir.Path = absolutePath;
            Dirs[name] = dir;
        }

        return dir;
    }

    public bool TryGetDirectory(string name, out VFSDirectory dir)
    {
        if (Dirs?.TryGetValue(name, out dir) == true)
        {
            return true;
        }

        dir = null;
        return false;
    }

    public void AddFile(string name, PackagedFileInfo file)
    {
        Files ??= [];

        if (!Files.TryGetValue(name, out var curFile) || curFile.Package.Metadata.Priority < file.Package.Metadata.Priority)
        {
            Files[name] = file;
        }
    }

    public bool TryGetFile(string name, out PackagedFileInfo file)
    {
        if (Files?.TryGetValue(name, out file) == true)
        {
            return true;
        }

        file = null;
        return false;
    }
}

public class VFS : IDisposable
{
    private List<Package> Packages = [];
    private string RootDir;
    private VFSDirectory Root = new();

    public void Dispose()
    {
        Packages.ForEach(p => p.Dispose());
    }

    public void AttachRoot(string path)
    {
        RootDir = path;
    }

    public void AttachGameDirectory(string gameDataPath, bool excludeAssets = true)
    {
        AttachRoot(gameDataPath);

        // List of packages we won't ever load
        // These packages don't contain any mod resources, but have a large
        // file table that makes loading unneccessarily slow.
        HashSet<string> packageBlacklist = [];

        if (excludeAssets)
        {
            packageBlacklist = [
                "Assets.pak",
                "Effects.pak",
                "Engine.pak",
                "EngineShaders.pak",
                "Game.pak",
                "GamePlatform.pak",
                "Gustav_NavCloud.pak",
                "Gustav_Textures.pak",
                "Gustav_Video.pak",
                "Icons.pak",
                "LowTex.pak",
                "Materials.pak",
                "Minimaps.pak",
                "Models.pak",
                "PsoCache.pak",
                "SharedSoundBanks.pak",
                "SharedSounds.pak",
                "Textures.pak",
                "VirtualTextures.pak",
                // Localization
                "English_Animations.pak",
                "VoiceMeta.pak",
                "Voice.pak"
            ];
        }

        // Collect priority value from headers
        var packagePriorities = new List<Tuple<string, int>>();

        foreach (var path in Directory.GetFiles(gameDataPath, "*.pak"))
        {
            var baseName = Path.GetFileName(path);
            if (!packageBlacklist.Contains(baseName)
                // Don't load 2nd, 3rd, ... parts of a multi-part archive
                && !ModPathVisitor.archivePartRe.IsMatch(baseName))
            {
                AttachPackage(path);
            }
        }

        foreach (var path in Directory.GetFiles(Path.Join(gameDataPath, "Localization"), "*.pak"))
        {
            var baseName = Path.GetFileName(path);
            if (!packageBlacklist.Contains(baseName)
                // Don't load 2nd, 3rd, ... parts of a multi-part archive
                && !ModPathVisitor.archivePartRe.IsMatch(baseName))
            {
                AttachPackage(path);
            }
        }
    }

    public void AttachPackage(string path)
    {
        var reader = new PackageReader();
        var package = reader.Read(path);
        Packages.Add(package);
    }

    public void FinishBuild()
    {
        foreach (var package in Packages)
        {
            foreach (var file in package.Files)
            {
                TryAddFile(file);
            }
        }
    }

    private void TryAddFile(PackagedFileInfo file)
    {
        var path = file.Name;
        var namePos = 0;
        var node = Root;
        do
        {
            var endPos = path.IndexOf('/', namePos);
            if (endPos >= 0)
            {
                node = node.GetOrAddDirectory(path.Substring(0, endPos), path.Substring(namePos, endPos - namePos));
                namePos = endPos + 1;
            }
            else
            {
                node.AddFile(path.Substring(namePos), file);
                break;
            }
        } while (true);
    }

    public VFSDirectory FindVFSDirectory(string path)
    {
        var namePos = 0;
        var node = Root;
        do
        {
            var endPos = path.IndexOf('/', namePos);
            if (endPos >= 0)
            {
                if (!node.TryGetDirectory(path.Substring(namePos, endPos - namePos), out node))
                {
                    return null;
                }

                namePos = endPos + 1;
            }
            else
            {
                if (node.TryGetDirectory(path.Substring(namePos), out node))
                {
                    return node;
                }
                else
                {
                    return null;
                }
            }
        } while (true);
    }

    public bool DirectoryExists(string path)
    {
        if (FindVFSDirectory(Canonicalize(path)) != null) return true;
        return RootDir != null && Directory.Exists(Path.Combine(RootDir, path));
    }

    public PackagedFileInfo FindVFSFile(string path)
    {
        var namePos = 0;
        var node = Root;
        do
        {
            var endPos = path.IndexOf('/', namePos);
            if (endPos >= 0)
            {
                if (!node.TryGetDirectory(path.Substring(namePos, endPos - namePos), out node))
                {
                    return null;
                }

                namePos = endPos + 1;
            }
            else
            {
                if (node.TryGetFile(path.Substring(namePos), out var file))
                {
                    return file;
                }
                else
                {
                    return null;
                }
            }
        } while (true);
    }

    public string Canonicalize(string path)
    {
        return path.Replace('\\', '/');
    }

    public bool FileExists(string path)
    {
        if (FindVFSFile(Canonicalize(path)) != null) return true;
        return RootDir != null && File.Exists(Path.Combine(RootDir, path));
    }

    public List<string> EnumerateFiles(string path, bool recursive = false)
    {
        return EnumerateFiles(path, recursive, (path) => true);
    }

    public List<string> EnumerateFiles(string path, bool recursive, Func<string, bool> filter)
    {
        List<string> results = [];
        EnumerateFiles(results, path, recursive, filter);
        return results;
    }

    public void EnumerateFiles(List<string> results, string path, bool recursive, Func<string, bool> filter)
    {
        var dir = FindVFSDirectory(Canonicalize(path));
        if (dir != null)
        {
            EnumerateFiles(results, dir, recursive, filter);
        }

        if (RootDir != null)
        {
            var fsDir = Path.Join(RootDir, path);
            if (Directory.Exists(fsDir))
            {
                var files = Directory.EnumerateFiles(fsDir, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    if (filter(file))
                    {
                        results.Add(Path.GetRelativePath(RootDir, file));
                    }
                }
            }
        }
    }

    public List<string> EnumerateDirectories(string path)
    {
        List<string> results = [];
        EnumerateDirectories(results, path);
        return results;
    }

    public void EnumerateDirectories(List<string> results, string path)
    {
        var dir = FindVFSDirectory(Canonicalize(path));
        if (dir?.Dirs != null)
        {
            foreach (var subdir in dir.Dirs)
            {
                results.Add(subdir.Value.Path);
            }
        }

        if (RootDir != null)
        {
            var fsDir = Path.Join(RootDir, path);
            if (Directory.Exists(fsDir))
            {
                foreach (var subdir in Directory.EnumerateDirectories(fsDir))
                {
                    results.Add(Path.GetRelativePath(RootDir, subdir));
                }
            }
        }
    }

    private void EnumerateFiles(List<string> results, VFSDirectory dir, bool recursive, Func<string, bool> filter)
    {
        if (dir.Files != null)
        {
            foreach (var file in dir.Files)
            {
                if (!file.Value.IsDeletion() && filter(file.Key))
                {
                    results.Add(file.Value.Name);
                }
            }
        }

        if (recursive && dir.Dirs != null)
        {
            foreach (var subdir in dir.Dirs)
            {
                EnumerateFiles(results, subdir.Value, recursive, filter);
            }
        }
    }

    public bool TryOpenFromVFS(string path, out Stream stream)
    {
        var file = FindVFSFile(Canonicalize(path));
        if (file != null && !file.IsDeletion())
        {
            stream = file.CreateContentReader();
            return true;
        }
        else
        {
            stream = null;
            return false;
        }
    }

    public bool TryOpen(string path, out Stream stream)
    {
        if (TryOpenFromVFS(path, out stream)) return true;

        if (RootDir != null)
        {
            var realPath = Path.Join(RootDir, path);
            if (File.Exists(realPath))
            {
                stream = File.OpenRead(realPath);
                return true;
            }
        }

        stream = null;
        return false;
    }

    public Stream Open(string path)
    {
        if (!TryOpen(path, out var stream))
        {
            throw new FileNotFoundException($"File not found in VFS: {path}", path);
        }

        return stream;
    }
}
