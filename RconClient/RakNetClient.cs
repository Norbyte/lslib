using LSLib.Rcon.Packets;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace LSLib.Rcon;

public class AsyncUdpClient
{
    private UdpClient Socket;
    public readonly UInt16 Port;

    public delegate void PacketReceivedDelegate(IPEndPoint address, byte[] packet);
    public PacketReceivedDelegate PacketReceived = delegate { };

    public AsyncUdpClient()
    {
        Random rnd = new Random();
        // Select a port number over 10000 as low port numbers
        // are frequently used by various server apps.
        Port = (UInt16)((rnd.Next() % (65536 - 10000)) + 10000);
        Socket = new UdpClient(Port);
    }
    
    public void RunLoop()
    {
        while (true)
        {
            IPEndPoint source = new IPEndPoint(0, 0);
            byte[] packet;
            try
            {
                packet = Socket.Receive(ref source);
            }
            catch (SocketException e)
            {
                // WSAECONNRESET - This may happen if the Rcon server is 
                // not running on the port we're trying to send messages to.
                if (e.ErrorCode == 10054)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Received connection reset - Rcon server probably not running.");
                    Console.ResetColor();
                    break;
                }
                else
                {
                    throw e;
                }
            }

            PacketReceived(source, packet);
        }
    }

    public void Send(IPEndPoint address, byte[] packet)
    {
        Socket.Send(packet, packet.Length, address);
    }
}

public class RakNetSocket
{
    private AsyncUdpClient Socket;
    private byte[] ClientId;
    private RakNetSession Session;

    public delegate void SessionEstablishedDelegate(RakNetSession session);
    public SessionEstablishedDelegate SessionEstablished = delegate { };

    public RakNetSocket()
    {
        Socket = new AsyncUdpClient();
        Socket.PacketReceived += this.OnPacketReceived;

        ClientId = new byte[8];
        var random = new Random();
        random.NextBytes(ClientId);
    }

    private Packet DecodePacket(Byte id, BinaryReaderBE reader)
    {
        Packet packet = null;
        switch ((PacketId)id)
        {
            case PacketId.OpenConnectionRequest1: packet = new OpenConnectionRequest1(); break;
            case PacketId.OpenConnectionResponse1: packet = new OpenConnectionResponse1(); break;
            case PacketId.OpenConnectionRequest2: packet = new OpenConnectionRequest2(); break;
            case PacketId.OpenConnectionResponse2: packet = new OpenConnectionResponse2(); break;
            default: throw new InvalidDataException("Unrecognized packet ID");
        }

        packet.Read(reader);
        return packet;
    }

    private void HandleConnectionResponse1(IPEndPoint address, OpenConnectionResponse1 response)
    {
        var connReq = new OpenConnectionRequest2
        {
            Magic = RakNetConstants.Magic,
            ClientId = ClientId,
            Address = new RakAddress
            {
                Address = (UInt32)IPAddress.Parse("127.0.0.1").Address,
                Port = Socket.Port
            },
            MTU = 1200
        };
        Send(address, connReq);
    }

    private void HandleConnectionResponse2(IPEndPoint address, OpenConnectionResponse2 response)
    {
        Session = new RakNetSession(this, address, ClientId);
        SessionEstablished(Session);
        Session.OnConnected();
    }

    private void HandlePacket(IPEndPoint address, Packet packet)
    {
        if (packet is OpenConnectionResponse1)
        {
            HandleConnectionResponse1(address, packet as OpenConnectionResponse1);
        }
        else if (packet is OpenConnectionResponse2)
        {
            HandleConnectionResponse2(address, packet as OpenConnectionResponse2);
        }
        else
        {
            throw new NotImplementedException("Packet type not handled");
        }
    }

    private void OnPacketReceived(IPEndPoint address, byte[] packet)
    {
        using (var stream = new MemoryStream(packet))
        using (var reader = new BinaryReaderBE(stream))
        {
            byte id = reader.ReadByte();
            if (id < 0x80)
            {
                var decoded = DecodePacket(id, reader);
                HandlePacket(address, decoded);
            }
            else
            {
                if (Session != null)
                {
                    Session.HandlePacket(id, reader);
                }
                else
                {
                    throw new Exception("Unhandled session packet - no session established!");
                }
            }
        }
    }

    public void Send(IPEndPoint address, Packet packet)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriterBE(stream))
        {
            packet.Write(writer);
            stream.SetLength(stream.Position);
            Socket.Send(address, stream.ToArray());
        }
    }

    public void BeginConnection(IPEndPoint address)
    {
        var connReq = new OpenConnectionRequest1
        {
            Magic = RakNetConstants.Magic,
            Protocol = RakNetConstants.ProtocolVersion
        };
        Send(address, connReq);

        Socket.RunLoop();
    }
}
