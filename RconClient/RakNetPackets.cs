using System;
using System.Collections.Generic;
using System.IO;

namespace LSLib.Rcon.Packets;

public struct RakAddress
{
    public UInt32 Address;
    public UInt16 Port;

    public void Read(BinaryReaderBE Reader)
    {
        var type = Reader.ReadByte();
        if (type != 4) throw new InvalidDataException("Only IPv4 addresses are supported");
        Address = ~Reader.ReadUInt32();
        Port = Reader.ReadUInt16BE();
    }

    public void Write(BinaryWriterBE Writer)
    {
        Writer.Write((byte)4);
        Writer.Write(~Address);
        Writer.WriteBE(Port);
    }
}

public class OpenConnectionRequest1 : Packet
{
    public byte[] Magic;
    public Byte Protocol;

    public void Read(BinaryReaderBE Reader)
    {
        Magic = Reader.ReadBytes(16);
        Protocol = Reader.ReadByte();
    }

    public void Write(BinaryWriterBE Writer)
    {
        Writer.Write((byte)PacketId.OpenConnectionRequest1);
        Writer.Write(Magic);
        Writer.Write(Protocol);
        byte[] pad = new byte[0x482];
        Writer.Write(pad);
    }
}

public class OpenConnectionResponse1 : Packet
{
    public byte[] Magic;
    public byte[] ServerId;
    public Byte Security;
    public UInt16 MTU;

    public void Read(BinaryReaderBE Reader)
    {
        Magic = Reader.ReadBytes(16);
        ServerId = Reader.ReadBytes(8);
        Security = Reader.ReadByte();
        MTU = Reader.ReadUInt16BE();
    }

    public void Write(BinaryWriterBE Writer)
    {
        Writer.Write((byte)PacketId.OpenConnectionResponse1);
        Writer.Write(Magic);
        Writer.Write(ServerId);
        Writer.Write(Security);
        Writer.WriteBE(MTU);
        byte[] pad = new byte[0x480];
        Writer.Write(pad);
    }
}

public class OpenConnectionRequest2 : Packet
{
    public byte[] Magic;
    public RakAddress Address;
    public UInt16 MTU;
    public byte[] ClientId;

    public void Read(BinaryReaderBE Reader)
    {
        Magic = Reader.ReadBytes(16);
        Address.Read(Reader);
        MTU = Reader.ReadUInt16BE();
        ClientId = Reader.ReadBytes(8);
    }

    public void Write(BinaryWriterBE Writer)
    {
        Writer.Write((byte)PacketId.OpenConnectionRequest2);
        Writer.Write(Magic);
        Address.Write(Writer);
        Writer.WriteBE(MTU);
        Writer.Write(ClientId);
    }
}

public class OpenConnectionResponse2 : Packet
{
    public byte[] Magic;
    public byte[] ServerId;
    public RakAddress Address;
    public UInt16 MTU;
    public Byte Security;

    public void Read(BinaryReaderBE Reader)
    {
        Magic = Reader.ReadBytes(16);
        ServerId = Reader.ReadBytes(8);
        Address.Read(Reader);
        MTU = Reader.ReadUInt16BE();
        Security = Reader.ReadByte();
    }

    public void Write(BinaryWriterBE Writer)
    {
        Writer.Write((byte)PacketId.OpenConnectionRequest2);
        Writer.Write(Magic);
        Writer.Write(ServerId);
        Address.Write(Writer);
        Writer.WriteBE(MTU);
        Writer.Write(Security);
    }
}

public class ConnectionRequest : Packet
{
    public byte[] ClientId;
    public UInt32 Time;
    public Byte Security;

    public void Read(BinaryReaderBE Reader)
    {
        ClientId = Reader.ReadBytes(8);
        Reader.ReadUInt32(); // Unknown
        Time = Reader.ReadUInt32BE();
        Security = Reader.ReadByte();
    }

    public void Write(BinaryWriterBE Writer)
    {
        Writer.Write((Byte)PacketId.ConnectionRequest);
        Writer.Write(ClientId);
        Writer.Write((UInt32)0);
        Writer.Write(Time);
        Writer.Write(Security);
    }
}

public class ConnectionRequestAccepted : Packet
{
    public void Read(BinaryReaderBE Reader)
    {
        // TODO - Unknown.
    }

    public void Write(BinaryWriterBE Writer)
    {
        throw new NotImplementedException();
    }
}

public class NewIncomingConnection : Packet
{
    public void Read(BinaryReaderBE Reader)
    {
    }

    public void Write(BinaryWriterBE Writer)
    {
        byte[] pkt = new byte[]
        {
            // Message ID
            0x13,
            // List of addresses?
            0x04, 0x80, 0xff, 0xff, 0xfe, 0x15, 0x0c,
            0x04, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00,
            0x04, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00,
            0x04, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00,
            0x04, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00,
            0x04, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00,
            0x04, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00,
            0x04, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00,
            0x04, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00,
            0x04, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00,
            0x04, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            // Ping time
            0x18, 0x8e, 0x2f, 0x3f,
            0x00, 0x00, 0x00, 0x00,
            // Pong time
            0x18, 0x8e, 0x2f, 0x3f
        };
        Writer.Write(pkt);
    }
}

public class DisconnectionNotification : Packet
{
    public void Read(BinaryReaderBE Reader)
    {
    }

    public void Write(BinaryWriterBE Writer)
    {
        Writer.Write((Byte)PacketId.DisconnectionNotification);
    }
}

public class ConnectedPing : Packet
{
    public UInt32 SendTime;

    public void Read(BinaryReaderBE Reader)
    {
        SendTime = Reader.ReadUInt32BE();
    }

    public void Write(BinaryWriterBE Writer)
    {
        Writer.Write((Byte)PacketId.ConnectedPing);
        Writer.WriteBE(SendTime);
    }
}

public class ConnectedPong : Packet
{
    public UInt32 ReceiveTime;
    public UInt32 SendTime;

    public void Read(BinaryReaderBE Reader)
    {
        ReceiveTime = Reader.ReadUInt32BE();
        SendTime = Reader.ReadUInt32BE();
    }

    public void Write(BinaryWriterBE Writer)
    {
        Writer.Write((Byte)PacketId.ConnectedPong);
        Writer.WriteBE(ReceiveTime);
        Writer.WriteBE(SendTime);
    }
}

public class DataPacket : Packet
{
    public Byte Id;
    public SequenceNumber Sequence;
    public Packet WrappedPacket;

    public void Read(BinaryReaderBE Reader)
    {
        Sequence.Read(Reader);
        WrappedPacket.Read(Reader);
    }

    public void Write(BinaryWriterBE Writer)
    {
        Writer.Write(Id);
        Sequence.Write(Writer);
        WrappedPacket.Write(Writer);
    }
}

public class Acknowledgement : Packet
{
    public List<SequenceNumber> SequenceNumbers;

    public void Read(BinaryReaderBE Reader)
    {
        SequenceNumbers = new List<SequenceNumber>();
        UInt16 numAcks = Reader.ReadUInt16BE();
        for (var i = 0; i < numAcks; i++)
        {
            Byte type = Reader.ReadByte();
            if (type == 0)
            {
                SequenceNumber first = new SequenceNumber(),
                    last = new SequenceNumber();
                first.Read(Reader);
                last.Read(Reader);
                for (UInt32 seq = first.Number; seq < last.Number; seq++)
                {
                    SequenceNumber num = new SequenceNumber();
                    num.Number = seq;
                    SequenceNumbers.Add(num);
                }
            }
            else
            {
                SequenceNumber num = new SequenceNumber();
                num.Read(Reader);
                SequenceNumbers.Add(num);
            }
        }
    }

    public void Write(BinaryWriterBE Writer)
    {
        Writer.Write((Byte)PacketId.ACK);
        Writer.WriteBE((UInt16)SequenceNumbers.Count);
        foreach (var seq in SequenceNumbers)
        {
            Writer.Write((Byte)1);
            seq.Write(Writer);
        }
    }
}
