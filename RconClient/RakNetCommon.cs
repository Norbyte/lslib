using System;

namespace LSLib.Rcon;

public enum PacketId : byte
{
    ConnectedPing = 0x00,
    UnconnectedPing = 0x01,
    ConnectedPong = 0x03,
    OpenConnectionRequest1 = 0x05,
    OpenConnectionResponse1 = 0x06,
    OpenConnectionRequest2 = 0x07,
    OpenConnectionResponse2 = 0x08,
    ConnectionRequest = 0x09,
    ConnectionRequestAccepted = 0x10,
    NewIncomingConnection = 0x13,
    DisconnectionNotification = 0x15,
    UnconnectedPong = 0x1C,
    EncapsulatedData = 0x84,
    ACK = 0xC0
};

public class RakNetConstants
{
    public const Byte ProtocolVersion = 6;
    public static readonly byte[] Magic = new byte[] { 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 };
}

public interface Packet
{
    void Read(BinaryReaderBE Reader);
    void Write(BinaryWriterBE Writer);
}

public struct SequenceNumber
{
    public UInt32 Number;

    public void Read(BinaryReaderBE Reader)
    {
        Byte b1 = Reader.ReadByte();
        Byte b2 = Reader.ReadByte();
        Byte b3 = Reader.ReadByte();
        Number = (UInt32)b1 | ((UInt32)b2 << 8) | ((UInt32)b3 << 16);
    }

    public void Write(BinaryWriterBE Writer)
    {
        Byte b1 = (Byte)(Number & 0xff);
        Byte b2 = (Byte)((Number >> 8) & 0xff);
        Byte b3 = (Byte)((Number >> 16) & 0xff);
        Writer.Write(b1);
        Writer.Write(b2);
        Writer.Write(b3);
    }
}
