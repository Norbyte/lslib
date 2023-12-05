using LSLib.LS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.VirtualTextures
{
    public class PageFile : IDisposable
    {
        private VirtualTileSet TileSet;
        private FileStream Stream;
        private BinaryReader Reader;
        public GTPHeader Header;
        private List<UInt32[]> ChunkOffsets;

        public PageFile(VirtualTileSet tileset, string path)
        {
            TileSet = tileset;
            Stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            Reader = new BinaryReader(Stream);

            Header = BinUtils.ReadStruct<GTPHeader>(Reader);

            var numPages = Stream.Length / tileset.Header.PageSize;
            ChunkOffsets = new List<UInt32[]>();

            for (var page = 0; page < numPages; page++)
            {
                var numOffsets = Reader.ReadUInt32();
                var offsets = new UInt32[numOffsets];
                BinUtils.ReadStructs<UInt32>(Reader, offsets);
                ChunkOffsets.Add(offsets);

                Stream.Position = (page + 1) * tileset.Header.PageSize;
            }
        }

        public void Dispose()
        {
            Reader.Dispose();
            Stream.Dispose();
        }

        private byte[] DoUnpackTileBC(GTPChunkHeader header, int outputSize)
        {
            var parameterBlock = (GTSBCParameterBlock)TileSet.ParameterBlocks[header.ParameterBlockID];
            if (parameterBlock.CompressionName1 == "lz77" && parameterBlock.CompressionName2 == "fastlz0.1.0")
            {
                var buf = Reader.ReadBytes((int)header.Size);
                return Native.FastLZCompressor.Decompress(buf, outputSize);
            }
            else if (parameterBlock.CompressionName1 == "raw")
            {
                return Reader.ReadBytes((int)header.Size);
            }
            else
            {
                throw new InvalidDataException($"Unsupported BC compression format: '{parameterBlock.CompressionName1}', '{parameterBlock.CompressionName2}'");
            }
        }

        private byte[] DoUnpackTileUniform(GTPChunkHeader header)
        {
            var parameterBlock = (GTSUniformParameterBlock)TileSet.ParameterBlocks[header.ParameterBlockID];

            byte[] img = new byte[TileSet.Header.TileWidth * TileSet.Header.TileHeight];
            Array.Clear(img, 0, img.Length);
            return img;
        }

        public byte[] UnpackTile(int pageIndex, int chunkIndex, int outputSize)
        {
            Stream.Position = ChunkOffsets[pageIndex][chunkIndex] + (pageIndex * TileSet.Header.PageSize);
            var chunkHeader = BinUtils.ReadStruct<GTPChunkHeader>(Reader);
            switch (chunkHeader.Codec)
            {
                case GTSCodec.Uniform: return DoUnpackTileUniform(chunkHeader);
                case GTSCodec.BC: return DoUnpackTileBC(chunkHeader, outputSize);
                default: throw new InvalidDataException($"Unsupported codec: {chunkHeader.Codec}");
            }
        }

        public BC5Image UnpackTileBC5(int pageIndex, int chunkIndex)
        {
            var compressedSize = 16 * ((TileSet.Header.TileWidth + 3) / 4) * ((TileSet.Header.TileHeight + 3) / 4)
                + 16 * ((TileSet.Header.TileWidth/2 + 3) / 4) * ((TileSet.Header.TileHeight/2 + 3) / 4);
            var chunk = UnpackTile(pageIndex, chunkIndex, compressedSize);
            return new BC5Image(chunk, TileSet.Header.TileWidth, TileSet.Header.TileHeight);
        }
    }
}
