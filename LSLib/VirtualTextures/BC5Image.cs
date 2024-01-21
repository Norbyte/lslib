using LSLib.LS;

namespace LSLib.VirtualTextures;

public class BC5Image
{
    public byte[] Data;
    public int Width;
    public int Height;

    public BC5Image(byte[] data, int width, int height)
    {
        Data = data;
        Width = width;
        Height = height;
    }

    public BC5Image(int width, int height)
    {
        Data = new byte[width * height];
        Array.Clear(Data);
        Width = width;
        Height = height;
    }

    public int CalculateOffset(int x, int y)
    {
        if ((x % 4) != 0 || (y % 4) != 0)
        {
            throw new ArgumentException("BC coordinates must be multiples if 4");
        }

        return ((x >> 2) + (y >> 2) * (Width >> 2)) << 4;
    }

    public void CopyTo(BC5Image destination, int srcX, int srcY, int dstX, int dstY, int width, int height)
    {
        if ((srcX % 4) != 0 || (srcY % 4) != 0 || (dstX % 4) != 0 || (dstY % 4) != 0 || (width % 4) != 0 || (height % 4) != 0)
        {
            throw new ArgumentException("BC coordinates must be multiples of 4");
        }

        if (srcX < 0 || dstX < 0 || srcY < 0 || dstY < 0
            || srcX + width > Width
            || srcY + height > Height
            || dstX + width > destination.Width
            || dstY + height > destination.Height)
        {
            throw new ArgumentException("Texture coordinates out of bounds");
        }

        var wrX = dstX;
        var wrY = dstY;
        for (var y = srcY; y < srcY + height; y += 4)
        {
            for (var x = srcX; x < srcX + width; x += 4)
            {
                var srcoff = CalculateOffset(x, y);
                var dstoff = destination.CalculateOffset(wrX, wrY);
                Array.Copy(Data, srcoff, destination.Data, dstoff, 16);
                wrX += 4;
            }

            wrY += 4;
            wrX = dstX;
        }
    }

    public void SaveDDS(string path)
    {
        var header = new DDSHeader
        {
            dwMagic = DDSHeader.DDSMagic,
            dwSize = DDSHeader.HeaderSize,
            dwFlags = 0x1007,
            dwWidth = (uint)Width,
            dwHeight = (uint)Height,
            dwPitchOrLinearSize = (uint)(Width * Height),
            dwDepth = 1,
            dwMipMapCount = 1,

            dwPFSize = 32,
            dwPFFlags = 0x04,
            dwFourCC = DDSHeader.FourCC_DXT5,

            dwCaps = 0x1000
        };

        using var pagef = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(pagef);
        BinUtils.WriteStruct<DDSHeader>(bw, ref header);
        bw.Write(Data, 0, Data.Length);
    }
}

public class BC5Mips
{
    public List<BC5Image> Mips;

    public void LoadDDS(string path)
    {
        using var f = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(f);
        var header = BinUtils.ReadStruct<DDSHeader>(reader);
        Mips = [];

        if (header.dwMagic != DDSHeader.DDSMagic)
        {
            throw new InvalidDataException($"{path}: Incorrect DDS signature, or file is not a DDS file");
        }

        if (header.dwSize != DDSHeader.HeaderSize)
        {
            throw new InvalidDataException($"{path}: Incorrect DDS header size");
        }

        if ((header.dwFlags & 0xffff) != 0x1007)
        {
            throw new InvalidDataException($"{path}: Incorrect DDS texture flags");
        }

        if (header.dwDepth != 0 && header.dwDepth != 1)
        {
            throw new InvalidDataException($"{path}: Only single-layer textures are supported");
        }

        if ((header.dwPFFlags & 4) != 4)
        {
            throw new InvalidDataException($"{path}: DDS does not have a valid FourCC code");
        }

        if (header.FourCCName != "DXT5")
        {
            throw new InvalidDataException($"{path}: Expected a DXT5 encoded texture, got: " + header.FourCCName);
        }

        Int32 mips = 1;
        if ((header.dwFlags & 0x20000) == 0x20000)
        {
            mips = (Int32)header.dwMipMapCount;
        }

        Mips = new List<BC5Image>(mips);
        for (var i = 0; i < mips; i++)
        {
            var width = Math.Max((int)header.dwWidth >> i, 1);
            var height = Math.Max((int)header.dwHeight >> i, 1);
            var bytes = Math.Max(width / 4, 1) * Math.Max(height / 4, 1) * 16;
            var blob = reader.ReadBytes(bytes);
            Mips.Add(new BC5Image(blob, width, height));
        }
    }
}
