using LSLib.Rcon.DosPackets;
using System;
using System.IO;
using System.Net;
using System.Timers;

namespace LSLib.Rcon;

public class RconApp
{
    static private bool Executed = false;
    static private bool ReceivedEvents = false;
    static private RakNetSession Session;
    static private string Command;
    static private string[] Arguments;

    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Usage: Rcon <ip:port> <command> [<args> ...]");
            Console.ResetColor();
            Environment.Exit(1);
        }

        var ipPort = args[0].Split(':');
        var port = Int32.Parse(ipPort[1]);
        Command = args[1];
        Arguments = new string[args.Length - 2];
        Array.Copy(args, 2, Arguments, 0, args.Length - 2);
        
        var socket = new RakNetSocket();
        socket.SessionEstablished += OnSessionEstablished;

        // Create a disconnect timer to make sure that we disconnect after the last console message
        var timer = new System.Timers.Timer(3000);
        timer.Elapsed += OnTimedEvent;
        timer.Enabled = true;
        
        IPEndPoint target = new IPEndPoint(IPAddress.Parse(ipPort[0]), port);
        socket.BeginConnection(target);
    }

    static void OnTimedEvent(object source, ElapsedEventArgs e)
    {
        if (!ReceivedEvents)
        {
            ReceivedEvents = true;

            if (Session != null)
            {
                Console.WriteLine("Disconnecting.");
                var disconnectCmd = new DosDisconnectConsole();
                Session.SendEncapsulated(disconnectCmd, EncapsulatedReliability.ReliableOrdered);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Timed out waiting for session establishment");
                Console.ResetColor();
            }
        }
        else
        {
            ReceivedEvents = false;
        }
    }

    static Packet OnPacketParse(Byte id)
    {
        switch ((DosPacketId)id)
        {
            case DosPacketId.DosUnknown87: return new DosUnknown87();
            case DosPacketId.DosEnumerationList: return new DosEnumerationList();
            case DosPacketId.DosConsoleResponse: return new DosConsoleResponse();
            default: return null;
        }
    }

    static void OnSessionEstablished(RakNetSession session)
    {
        Console.WriteLine("RakNet session established to Rcon server.");
        session.PacketConstructor += OnPacketParse;
        session.PacketReceived += OnPacketReceived;
        session.SessionDisconnected += OnSessionDisconnected;
        Session = session;
    }

    static void OnSessionDisconnected(RakNetSession session)
    {
        if (!Executed)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Received DisconnectionNotification before console command could be sent.");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("Closed connection to Rcon server.");
        }

        Environment.Exit(0);
    }

    static void OnPacketReceived(RakNetSession session, Packet packet)
    {
        if (packet is DosUnknown87)
        {
            // Unknown.
        }
        else if (packet is DosEnumerationList)
        {
            if (!Executed)
            {
                Console.WriteLine("Sending console command:");
                Console.WriteLine("> " + Command + " " + String.Join(" ", Arguments));
                var consoleCmd = new DosSendConsoleCommand
                {
                    Command = Command,
                    Arguments = Arguments
                };
                session.SendEncapsulated(consoleCmd, EncapsulatedReliability.ReliableOrdered);
            }

            ReceivedEvents = true;
        }
        else if (packet is DosConsoleResponse)
        {
            bool hasResult = false;
            var lines = (packet as DosConsoleResponse).Lines;
            foreach (var line in lines)
            {
                switch (line.Level)
                {
                    case 4: Console.ForegroundColor = ConsoleColor.Green; hasResult = true; break;
                    case 5: Console.ForegroundColor = ConsoleColor.Red; hasResult = true; break;
                    default: Console.ResetColor(); break;
                }
                Console.WriteLine(line.Line);
            }
            Console.ResetColor();
            Executed = true;
            ReceivedEvents = true;

            if (hasResult)
            {
                var disconnectCmd = new DosDisconnectConsole();
                Session.SendEncapsulated(disconnectCmd, EncapsulatedReliability.ReliableOrdered);
            }
        }
        else
        {
            throw new Exception("Unhandled DOS encapsulated packet");
        }
    }
}
