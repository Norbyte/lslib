using LSLib.Rcon.DosPackets;
using System;
using System.Net;

namespace LSLib.Rcon
{
    public class RconApp
    {
        static private bool Executed = false;
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

            // sending data (for the sake of simplicity, back to ourselves):
            IPEndPoint target = new IPEndPoint(IPAddress.Parse(ipPort[0]), port);
            socket.BeginConnection(target);
        }

        static Packet OnPacketParse(Byte id)
        {
            switch ((DosPacketId)id)
            {
                case DosPacketId.DosUnknown87: return new DosUnknown87();
                case DosPacketId.DosUnknown8B: return new DosUnknown8B();
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
            else if (packet is DosUnknown8B)
            {
                Console.WriteLine("Rcon payload received.");
                Console.WriteLine("> " + Command + " " + String.Join(" ", Arguments));
                var consoleCmd = new DosSendConsoleCommand
                {
                    Command = Command,
                    Arguments = Arguments
                };
                session.SendEncapsulated(consoleCmd, EncapsulatedReliability.ReliableOrdered);
            }
            else if (packet is DosConsoleResponse)
            {
                Console.WriteLine("Console response:");
                var lines = (packet as DosConsoleResponse).Lines;
                foreach (var line in lines)
                {
                    switch (line.Level)
                    {
                        case 4: Console.ForegroundColor = ConsoleColor.Green; break;
                        case 5: Console.ForegroundColor = ConsoleColor.Red; break;
                        default: Console.ResetColor(); break;
                    }
                    Console.WriteLine(line.Line);
                }
                Console.ResetColor();
                Executed = true;

                Console.WriteLine("Disconnecting Rcon.");
                var disconnectCmd = new DosDisconnectConsole();
                session.SendEncapsulated(disconnectCmd, EncapsulatedReliability.ReliableOrdered);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(" --> (UNHANDLED ENCAP PACKET) ");
                Console.WriteLine(packet);
                Console.ResetColor();
            }
        }
    }
}
