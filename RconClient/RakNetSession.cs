using LSLib.Rcon.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace LSLib.Rcon;

public class SplitPacket
{
    public UInt16 Index;
    public UInt32 Available;
    public byte[][] Buffers;
}

public class RakNetSession
{
    private RakNetSocket Socket;
    private IPEndPoint Address;
    private byte[] ClientId;

    private UInt32 NextPacketId = 0;
    private UInt32 NextReliableId = 0;
    private UInt32 NextSequenceId = 0;
    private UInt32 NextOrderId = 0;
    private Dictionary<UInt16, SplitPacket> Splits;

    public delegate Packet PacketConstructorDelegate(Byte id);
    public PacketConstructorDelegate PacketConstructor = delegate { return null; };

    public delegate void PacketReceivedDelegate(RakNetSession session, Packet packet);
    public PacketReceivedDelegate PacketReceived = delegate { };

    public delegate void SessionDisconnectedDelegate(RakNetSession session);
    public SessionDisconnectedDelegate SessionDisconnected = delegate { };

    public RakNetSession(RakNetSocket Socket, IPEndPoint Address, byte[] ClientId)
    {
        this.Socket = Socket;
        this.Address = Address;
        this.ClientId = ClientId;
        Splits = new Dictionary<UInt16, SplitPacket>();
    }

    private void HandleConnectedPing(ConnectedPing packet)
    {
        var pong = new ConnectedPong();
        pong.ReceiveTime = packet.SendTime;
        pong.SendTime = packet.SendTime;
        SendEncapsulated(pong, EncapsulatedReliability.Unreliable);
    }

    private void HandleConnectionRequestAccepted(ConnectionRequestAccepted packet)
    {
        var ackReq = new NewIncomingConnection();
        SendEncapsulated(ackReq, EncapsulatedReliability.ReliableOrdered);
    }

    private void HandleDisconnectionNotification(DisconnectionNotification packet)
    {
        SessionDisconnected(this);
    }

    private void HandleEncapsulatedPayload(byte[] payload)
    {
        using (var encapMemory = new MemoryStream(payload))
        using (var encapStream = new BinaryReaderBE(encapMemory))
        {
            var encapId = encapStream.ReadByte();
            HandlePacketDecapsulated(encapId, encapStream);
        }
    }

    private void HandleSplitPacket(EncapsulatedPacket packet)
    {
        SplitPacket split = null;
        if (!Splits.TryGetValue(packet.SplitId, out split))
        {
            split = new SplitPacket();
            split.Index = packet.SplitId;
            split.Available = 0;
            split.Buffers = new byte[packet.SplitCount][];
            Splits.Add(packet.SplitId, split);
        }

        if (split.Buffers.Length != packet.SplitCount)
        {
            throw new InvalidDataException("Packet split count mismatch");
        }

        if (split.Buffers[packet.SplitIndex] != null)
        {
            return;
        }

        split.Buffers[packet.SplitIndex] = packet.Payload;
        split.Available++;

        if (split.Available == split.Buffers.Length)
        {
            Splits.Remove(split.Index);
            using (var memory = new MemoryStream())
            using (var stream = new BinaryWriter(memory))
            {
                foreach (var buffer in split.Buffers)
                {
                    stream.Write(buffer);
                }

                memory.SetLength(memory.Position);
                HandleEncapsulatedPayload(memory.ToArray());
            }
        }
    }

    private void SendAcknowledgement(SequenceNumber sequence)
    {
        var ack = new Acknowledgement
        {
            SequenceNumbers = new List<SequenceNumber> { sequence }
        };
        Socket.Send(Address, ack);
    }

    private void HandleEncapsulatedPacket(DataPacket data, EncapsulatedPacket packet)
    {
        if (packet.Flags.Split)
        {
            HandleSplitPacket(packet);
        }
        else
        {
            HandleEncapsulatedPayload(packet.Payload);
        }

        if (packet.Flags.IsReliable())
        {
            SendAcknowledgement(data.Sequence);
        }
    }

    private void HandlePacketDecapsulated(Byte id, BinaryReaderBE reader)
    {
        var packet = DecodePacketDecapsulated(id, reader);

        if (packet is ConnectedPing)
        {
            HandleConnectedPing(packet as ConnectedPing);
        }
        else if (packet is ConnectionRequestAccepted)
        {
            HandleConnectionRequestAccepted(packet as ConnectionRequestAccepted);
        }
        else if (packet is DisconnectionNotification)
        {
            HandleDisconnectionNotification(packet as DisconnectionNotification);
        }
        else if (id >= 0x80)
        {
            PacketReceived(this, packet);
        }
        else
        {
            throw new Exception("Unhandled encapsulated packet");
        }
    }

    public void HandlePacket(Byte id, BinaryReaderBE reader)
    {
        var packet = DecodePacket(id, reader);

        if (packet is Acknowledgement)
        {
            // TODO - ACK mechanism not handled
        }
        else if (packet is DataPacket)
        {
            var encap = (packet as DataPacket).WrappedPacket as EncapsulatedPacket;
            HandleEncapsulatedPacket(packet as DataPacket, encap);
        }
        else
        {
            throw new Exception("Unhandled packet");
        }
    }

    private Packet DecodePacket(Byte id, BinaryReaderBE reader)
    {
        Packet packet = null;
        if (id >= 0x80 && id < 0xA0)
        {
            var dataPkt = new DataPacket();
            dataPkt.WrappedPacket = new EncapsulatedPacket();
            packet = dataPkt;
        }
        else
        {
            switch ((PacketId)id)
            {
                case PacketId.ACK: packet = new Acknowledgement(); break;
                default: throw new InvalidDataException("Unrecognized packet ID");
            }
        }

        packet.Read(reader);
        return packet;
    }

    private Packet DecodePacketDecapsulated(Byte id, BinaryReaderBE reader)
    {
        Packet packet = null;
        switch ((PacketId)id)
        {
            case PacketId.ConnectedPing: packet = new ConnectedPing(); break;
            case PacketId.ConnectionRequest: packet = new ConnectionRequest(); break;
            case PacketId.ConnectionRequestAccepted: packet = new ConnectionRequestAccepted(); break;
            case PacketId.DisconnectionNotification: packet = new DisconnectionNotification(); break;
            default:
                packet = PacketConstructor(id);
                if (packet == null) throw new InvalidDataException("Unrecognized encapsulated packet ID");
                break;
        }

        packet.Read(reader);
        return packet;
    }

    public void SendEncapsulated(Packet packet, EncapsulatedReliability reliability)
    {
        var dataPkt = new DataPacket();
        dataPkt.Id = (byte)PacketId.EncapsulatedData;
        dataPkt.Sequence.Number = NextPacketId++;

        var encapPkt = new EncapsulatedPacket();
        encapPkt.Flags.Reliability = reliability;
        if (encapPkt.Flags.IsReliable())
        {
            encapPkt.MessageIndex.Number = NextReliableId++;
        }

        if (encapPkt.Flags.IsSequenced())
        {
            encapPkt.SequenceIndex.Number = NextSequenceId++;
        }

        if (encapPkt.Flags.IsSequenced() || encapPkt.Flags.IsOrdered())
        {
            encapPkt.OrderChannel = 0;
            encapPkt.OrderIndex.Number = NextOrderId++;
        }

        using (var memory = new MemoryStream())
        using (var stream = new BinaryWriterBE(memory))
        {
            packet.Write(stream);
            memory.SetLength(memory.Position);
            encapPkt.Payload = memory.ToArray();
            encapPkt.Length = (UInt16)(encapPkt.Payload.Length * 8);
        }

        dataPkt.WrappedPacket = encapPkt;
        Socket.Send(Address, dataPkt);
    }

    public void OnConnected()
    {
        var currentTimestamp = (UInt32)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;
        var connReq = new ConnectionRequest
        {
            ClientId = ClientId,
            Time = currentTimestamp,
            Security = 0
        };
        SendEncapsulated(connReq, EncapsulatedReliability.Reliable);
    }
}
