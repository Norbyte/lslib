using LSLib.LS;
using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace TerrainFixup
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TerrainPatchHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Signature; // PVersion

        public UInt32 Version;
    }

    public class TerrainPatchLayer
    {
        public int Index;
        public int[] Data;
        public byte[] Data2;
    }

    public class TerrainPatch
    {
        public string Path;
        public TerrainPatchHeader Header;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public float[] Heightmap;
        public Vector2d[] Vertices;
        public int[] Indices;
        public int PatchIndex;
        public TerrainPatchLayer[] Layers;
    }

    public class Terrain
    {
        public string Directory;
        public string GUID;
        public int Width;
        public int Height;
        public int CellsX;
        public int CellsY;
        public int PatchesX;
        public int PatchesY;
        public int NumPatches;
        public TerrainPatch[] Patches;
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                Console.WriteLine("Usage: TerrainFixup <LSF path> <patch path> [<reference patch path>]");
                return;
            }

            Console.WriteLine($"Loading terrains from {args[1]} ...");
            var terrains = new Dictionary<string, Terrain>();
            LoadTerrainsFromPath(args[0], args[1], terrains);

            var referenceTerrains = new Dictionary<string, Terrain>();
            if (args.Length > 2)
            {
                Console.WriteLine($"Loading reference terrains from {args[2]} ...");
                LoadTerrainsFromPath(args[0], args[2], referenceTerrains);
            }

            Console.WriteLine($"Updating terrain meshes ...");
            foreach (var terrain in terrains)
            {
                Terrain refTerrain = null;
                referenceTerrains.TryGetValue(terrain.Key, out refTerrain);
                PatchTerrain(terrain.Value, refTerrain);
            }

            Console.WriteLine($"Updating patches ...");
            foreach (var terrain in terrains)
            {
                SaveTerrainPatches(terrain.Value);
            }
        }

        private static void PatchTerrain(Terrain terrain, Terrain refTerrain)
        {
            if (refTerrain != null)
            {
                if (refTerrain.CellsX == terrain.CellsX && refTerrain.CellsY == terrain.CellsY)
                {
                    Console.WriteLine($"Patching reference data to terrain {terrain.GUID}.");
                    for (var i = 0; i < terrain.Patches.Length; i++)
                    {
                        Console.WriteLine($"Patch {i}: {refTerrain.Patches[i].Indices.Length} inds, {refTerrain.Patches[i].Vertices.Length} verts");
                        terrain.Patches[i].Indices = refTerrain.Patches[i].Indices;
                        terrain.Patches[i].Vertices = refTerrain.Patches[i].Vertices;
                    }

                    return;
                }
                else
                {
                    Console.WriteLine($"Terrain {terrain.GUID} patch size differs; couldnt apply ref mesh.");
                }
            }

            Console.WriteLine($"Terrain {terrain.GUID} has no reference data, clearing vertex buffers.");
            foreach (var patch in terrain.Patches)
            {
                patch.Indices = new int[0];
                patch.Vertices = new Vector2d[0];
            }
        }

        private static void LoadTerrainsFromPath(string path, string patchDir, Dictionary<string, Terrain> terrains)
        {
            foreach (var lsfPath in Directory.GetFiles(path, "*.lsf"))
            {
                LoadTerrainsFromLSF(lsfPath, patchDir, terrains);
            }
        }

        private static void LoadTerrainsFromLSF(string path, string patchDir, Dictionary<string, Terrain> terrains)
        {
            var loadParams = ResourceLoadParameters.FromGameVersion(LSLib.LS.Enums.Game.DivinityOriginalSin2DE);
            var terrainRes = ResourceUtils.LoadResource(path, loadParams);
            var tmpls = terrainRes.Regions["Templates"];
            if (tmpls.Children.TryGetValue("GameObjects", out List<Node> terrainTemplates))
            {
                foreach (var tmpl in terrainTemplates)
                {
                    var terrain = LoadTerrainFromNode(patchDir, tmpl);
                    terrain.Directory = patchDir;
                    terrains.Add(terrain.GUID, terrain);
                }
            }
        }

        private static Terrain LoadTerrainFromNode(string dir, Node node)
        {
            var terrain = new Terrain();
            terrain.GUID = (string)node.Attributes["MapKey"].Value;
            terrain.Width = (int)node.Children["Visual"][0].Attributes["Width"].Value;
            terrain.Height = (int)node.Children["Visual"][0].Attributes["Height"].Value;
            terrain.CellsX = (int)Math.Ceiling(terrain.Width * 0.5f) + 1;
            terrain.CellsY = (int)Math.Ceiling(terrain.Height * 0.5f) + 1;
            terrain.PatchesX = (int)Math.Ceiling(terrain.Width * 0.015625f);
            terrain.PatchesY = (int)Math.Ceiling(terrain.Height * 0.015625f);
            terrain.NumPatches = terrain.PatchesX * terrain.PatchesY;
            terrain.Patches = new TerrainPatch[terrain.NumPatches];

            int offsetY = 0;
            for (int y = 0; y < terrain.PatchesY; y++)
            {
                int sizeY = Math.Min(terrain.CellsY - offsetY - 1, 32) + 1;
                int offsetX = 0;
                for (int x = 0; x < terrain.PatchesX; x++)
                {
                    int sizeX = Math.Min(terrain.CellsX - offsetX - 1, 32) + 1;

                    var patchPath = Path.Join(dir, $"{terrain.GUID}_{x}_{y}.patch");
                    var patch = new TerrainPatch();
                    patch.X = x;
                    patch.Y = y;
                    patch.Width = sizeX;
                    patch.Height = sizeY;
                    LoadPatch(patch, patchPath);
                    terrain.Patches[x + y * terrain.PatchesX] = patch;
                    offsetX += 32;
                }
                offsetY += 32;
            }

            return terrain;
        }

        private static void SaveTerrainPatches(Terrain terrain)
        {
            foreach (var patch in terrain.Patches)
            {
                SavePatch(patch, patch.Path);
            }
        }

        private static void LoadPatch(TerrainPatch patch, string sourcePath)
        {
            using (var fs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new BinaryReader(fs))
            {
                patch.Path = sourcePath;
                patch.Header = BinUtils.ReadStruct<TerrainPatchHeader>(reader);
                if (patch.Header.Version != 4)
                    throw new InvalidFormatException(String.Format("Can only read version 4 terrain patch files; this file is v{0}", patch.Header.Version));

                patch.Heightmap = new float[patch.Width * patch.Height];
                for (var i = 0; i < patch.Width * patch.Height; i++)
                {
                    patch.Heightmap[i] = reader.ReadSingle();
                }

                int numVertices = reader.ReadInt32();
                patch.Vertices = new Vector2d[numVertices];
                for (var i = 0; i < numVertices; i++)
                {
                    int x = reader.ReadInt32();
                    int y = reader.ReadInt32();
                    patch.Vertices[i] = new Vector2d(x, y);
                }

                int numIndices = reader.ReadInt32();
                patch.Indices = new int[numIndices/4];
                for (var i = 0; i < numIndices / 4; i++)
                {
                    patch.Indices[i] = reader.ReadInt32();
                }

                patch.PatchIndex = reader.ReadInt32();
                int numLayers = reader.ReadInt32();

                patch.Layers = new TerrainPatchLayer[numLayers];
                for (var i = 0; i < numLayers; i++)
                {
                    var layer = new TerrainPatchLayer();
                    layer.Index = reader.ReadInt32();
                    if (layer.Index != -1)
                    {
                        int layerBytes = reader.ReadInt32();
                        layer.Data = new int[layerBytes/4];
                        for (int j = 0; j < layerBytes/4; j++)
                        {
                            layer.Data[j] = reader.ReadInt32();
                        }
                        int layerBytes2 = reader.ReadInt32();
                        layer.Data2 = new byte[layerBytes2];
                        for (int j = 0; j < layerBytes2; j++)
                        {
                            layer.Data2[j] = reader.ReadByte();
                        }
                    }

                    patch.Layers[i] = layer;
                }

                if (fs.Position != fs.Length)
                {
                    throw new InvalidDataException("Did not reach EOF?");
                }
            }
        }

        private static void SavePatch(TerrainPatch patch, string sourcePath)
        {
            using (var fs = new FileStream(sourcePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new BinaryWriter(fs))
            {
                BinUtils.WriteStruct<TerrainPatchHeader>(writer, ref patch.Header);

                for (var i = 0; i < patch.Width * patch.Height; i++)
                {
                    writer.Write(patch.Heightmap[i]);
                }

                writer.Write((Int32)patch.Vertices.Length);
                for (var i = 0; i < patch.Vertices.Length; i++)
                {
                    writer.Write(patch.Vertices[i].X);
                    writer.Write(patch.Vertices[i].Y);
                }

                writer.Write((Int32)patch.Indices.Length * 4);
                for (var i = 0; i < patch.Indices.Length; i++)
                {
                    writer.Write(patch.Indices[i]);
                }

                writer.Write(patch.PatchIndex);
                writer.Write((Int32)patch.Layers.Length);

                for (var i = 0; i < patch.Layers.Length; i++)
                {
                    var layer = patch.Layers[i];
                    writer.Write(layer.Index);
                    if (layer.Index != -1)
                    {
                        writer.Write((Int32)layer.Data.Length*4);
                        for (int j = 0; j < layer.Data.Length; j++)
                        {
                            writer.Write(layer.Data[j]);
                        }

                        writer.Write((Int32)layer.Data2.Length);
                        for (int j = 0; j < layer.Data2.Length; j++)
                        {
                            writer.Write(layer.Data2[j]);
                        }
                    }
                }
            }
        }
    }
}
