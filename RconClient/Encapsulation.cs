using System;

namespace LSLib.Rcon;

public enum EncapsulatedReliability
{
    Unreliable = 0,
    UnreliableSequenced = 1,
    Reliable = 2,
    ReliableOrdered = 3,
    ReliableSequenced = 4,
    UnreliableAcked = 5,
    RelaibleAcked = 6,
    ReliableOrderedAcked = 7
}

public struct EncapsulatedFlags
{
    public EncapsulatedReliability Reliability;
    public bool Split;

    public void Read(BinaryReaderBE reader)
    {
        Byte flags = reader.ReadByte();
        Split = (flags & 0x10) == 0x10;
        Reliability = (EncapsulatedReliability)(flags >> 5);
    }

    public void Write(BinaryWriterBE writer)
    {
        Byte flags = (Byte)(((Byte)Reliability << 5)
            | (Split ? 0x10 : 0x00));
        writer.Write(flags);
    }

    public bool IsReliable()
    {
        return Reliability == EncapsulatedReliability.Reliable
            || Reliability == EncapsulatedReliability.ReliableOrdered
            || Reliability == EncapsulatedReliability.ReliableSequenced
            || Reliability == EncapsulatedReliability.RelaibleAcked
            || Reliability == EncapsulatedReliability.ReliableOrderedAcked;
    }

    public bool IsOrdered()
    {
        return Reliability == EncapsulatedReliability.ReliableOrdered
            || Reliability == EncapsulatedReliability.ReliableOrderedAcked;
    }

    public bool IsSequenced()
    {
        return Reliability == EncapsulatedReliability.UnreliableSequenced
            || Reliability == EncapsulatedReliability.ReliableSequenced;
    }
}

public class EncapsulatedPacket : Packet
{
    public EncapsulatedFlags Flags;
    public UInt16 Length;
    public SequenceNumber MessageIndex;
    public SequenceNumber SequenceIndex;
    public SequenceNumber OrderIndex;
    public Byte OrderChannel;
    public UInt32 SplitCount;
    public UInt16 SplitId;
    public UInt32 SplitIndex;
    public byte[] Payload;

    public void Read(BinaryReaderBE Reader)
    {
        Flags.Read(Reader);
        Length = Reader.ReadUInt16BE();

        if (Flags.IsReliable())
        {
            MessageIndex.Read(Reader);
        }

        if (Flags.IsSequenced())
        {
            SequenceIndex.Read(Reader);
        }

        if (Flags.IsSequenced() || Flags.IsOrdered())
        {
            OrderIndex.Read(Reader);
            OrderChannel = Reader.ReadByte();
        }

        if (Flags.Split)
        {
            SplitCount = Reader.ReadUInt32BE();
            SplitId = Reader.ReadUInt16BE();
            SplitIndex = Reader.ReadUInt32BE();
        }

        Payload = Reader.ReadBytes(Length);
    }

    public void Write(BinaryWriterBE Writer)
    {
        Flags.Write(Writer);
        Writer.WriteBE(Length);

        if (Flags.IsReliable())
        {
            MessageIndex.Write(Writer);
        }

        if (Flags.IsSequenced())
        {
            SequenceIndex.Write(Writer);
        }

        if (Flags.IsSequenced() || Flags.IsOrdered())
        {
            OrderIndex.Write(Writer);
            Writer.Write(OrderChannel);
        }

        if (Flags.Split)
        {
            Writer.WriteBE(SplitCount);
            Writer.WriteBE(SplitId);
            Writer.WriteBE(SplitIndex);
        }

        Writer.Write(Payload);
    }
}
