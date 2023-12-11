using System;
using System.Collections.Generic;
using System.Text;

namespace LSLib.Rcon.DosPackets;

public enum DosPacketId : byte
{
    DosUnknown87 = 0x87,
    DosEnumerationList = 0x8B,
    DosDisconnectConsole = 0x89,
    DosConsoleResponse = 0x8A,
    DosSendConsoleCommand = 0x8B
};

public class DosUnknown87 : Packet
{
    public void Read(BinaryReaderBE Reader)
    {
    }

    public void Write(BinaryWriterBE Writer)
    {
        throw new NotImplementedException();
    }
}

public class DosEnumeration
{
    public String Name;
    public Byte Type;
    public List<String> Values;
}

public class DosEnumerationList : Packet
{
    public List<DosEnumeration> Enumerations;

    private static String ReadString(BinaryReaderBE Reader)
    {
        var length = Reader.ReadInt32();
        var strBytes = Reader.ReadBytes(length);
        return Encoding.UTF8.GetString(strBytes);
    }

    public void Read(BinaryReaderBE Reader)
    {
        Enumerations = new List<DosEnumeration>();
        var numEnums = Reader.ReadUInt32();
        for (var i = 0; i < numEnums; i++)
        {
            var enumeration = new DosEnumeration();
            enumeration.Name = ReadString(Reader);
            enumeration.Type = Reader.ReadByte();
            enumeration.Values = new List<String>();

            var numElems = Reader.ReadUInt32();
            for (var j = 0; j < numElems; j++)
            {
                enumeration.Values.Add(ReadString(Reader));
            }

            Enumerations.Add(enumeration);
        }
    }

    public void Write(BinaryWriterBE Writer)
    {
        throw new NotImplementedException();
    }
}

public class DosDisconnectConsole : Packet
{
    public void Read(BinaryReaderBE Reader)
    {
        throw new NotImplementedException();
    }

    public void Write(BinaryWriterBE Writer)
    {
        Writer.Write((Byte)DosPacketId.DosDisconnectConsole);
        byte[] pkt = new byte[]
        {
            0x00, 0x00, 0x00, 0x60,
            0x00, 0x08, 0x0A, 0x00,
            0x00, 0x09, 0x00, 0x00,
            0x00, 0x15
        };
        Writer.Write(pkt);
    }
}

public class DosSendConsoleCommand : Packet
{
    public String Command;
    public String[] Arguments;

    public void Read(BinaryReaderBE Reader)
    {
        throw new NotImplementedException();
    }

    public void Write(BinaryWriterBE Writer)
    {
        Writer.Write((Byte)DosPacketId.DosSendConsoleCommand);
        Writer.Write((UInt32)1);
        byte[] cmd = Encoding.UTF8.GetBytes(Command);
        Writer.Write((UInt32)cmd.Length);
        Writer.Write(cmd);
        Writer.Write((Byte)0);

        if (Arguments == null)
        {
            Writer.Write((UInt32)0);
        }
        else
        {
            Writer.Write((UInt32)Arguments.Length);
            for (var i = 0; i < Arguments.Length; i++)
            {
                byte[] arg = Encoding.UTF8.GetBytes(Arguments[i]);
                Writer.Write((UInt32)arg.Length);
                Writer.Write(arg);
                Writer.Write((Byte)0);
            }
        }

        Writer.Write((UInt16)0);
    }
}

public class DosConsoleResponse : Packet
{
    public class ConsoleLine
    {
        public String Line;
        public UInt32 Level;
    };

    public ConsoleLine[] Lines;

    public void Read(BinaryReaderBE Reader)
    {
        var lines = Reader.ReadUInt32();
        Lines = new ConsoleLine[lines];
        for (var i = 0; i < lines; i++)
        {
            var consoleLine = new ConsoleLine();
            var length = Reader.ReadUInt32();
            var unknown = Reader.ReadByte();
            var length2 = Reader.ReadUInt32();
            var line = Reader.ReadBytes((int)length);
            consoleLine.Level = Reader.ReadUInt32();
            consoleLine.Line = Encoding.UTF8.GetString(line);
            Lines[i] = consoleLine;
        }
    }

    public void Write(BinaryWriterBE Writer)
    {
        throw new NotImplementedException();
    }
}
