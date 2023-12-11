using System;
using System.IO;

namespace LSLib.Rcon;

public class BinaryWriterBE : BinaryWriter
{
    public BinaryWriterBE(Stream s)
        : base(s)
    { }

    public void WriteBE(UInt16 value)
    {
        UInt16 be = (ushort)((ushort)((value & 0xff) << 8) | ((value >> 8) & 0xff));
        Write(be);
    }

    public void WriteBE(UInt32 value)
    {
        // swap adjacent 16-bit blocks
        UInt32 be = (value >> 16) | (value << 16);
        // swap adjacent 8-bit blocks
        be = ((be & 0xFF00FF00) >> 8) | ((be & 0x00FF00FF) << 8);
        Write(be);
    }
}

public class BinaryReaderBE : BinaryReader
{
    public BinaryReaderBE(Stream s)
        : base(s)
    { }

    public UInt16 ReadUInt16BE()
    {
        UInt16 be = ReadUInt16();
        UInt16 le = (ushort)((ushort)((be & 0xff) << 8) | ((be >> 8) & 0xff));
        return le;
    }

    public UInt32 ReadUInt32BE()
    {
        UInt32 be = ReadUInt32();
        // swap adjacent 16-bit blocks
        UInt32 le = (be >> 16) | (be << 16);
        // swap adjacent 8-bit blocks
        le = ((le & 0xFF00FF00) >> 8) | ((le & 0x00FF00FF) << 8);
        return le;
    }
}
