using Google.Protobuf;
using LSLib.LS.Story;
using LSLib.LS.Story.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LSTools.DebuggerFrontend
{
    public class AsyncProtobufClient
    {
        private TcpClient Socket;
        private byte[] MessageBuffer;
        private int BufferPos;

        public delegate void MessageReceivedDelegate(BackendToDebugger message);
        public MessageReceivedDelegate MessageReceived = delegate { };

        public AsyncProtobufClient(string host, int port)
        {
            MessageBuffer = new byte[0x10000];
            BufferPos = 0;

            Socket = new TcpClient();
            Socket.Connect(host, port);
        }

        public void RunLoop()
        {
            while (true)
            {
                try
                {
                    int received = Socket.Client.Receive(MessageBuffer, BufferPos, MessageBuffer.Length - BufferPos, SocketFlags.Partial);
                    BufferPos += received;
                }
                catch (SocketException e)
                {
                    throw e;
                }

                while (BufferPos >= 4)
                {
                    Int32 length = MessageBuffer[0]
                        | (MessageBuffer[1] << 8)
                        | (MessageBuffer[2] << 16)
                        | (MessageBuffer[3] << 24);
                    if (BufferPos >= length)
                    {
                        using (var stream = new CodedInputStream(MessageBuffer, 4, length - 4))
                        {
                            var message = BackendToDebugger.Parser.ParseFrom(stream);
                            MessageReceived(message);
                        }

                        Array.Copy(MessageBuffer, length, MessageBuffer, 0, BufferPos - length);
                        BufferPos -= length;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public void Send(DebuggerToBackend message)
        {
            using (var ms = new MemoryStream())
            {
                message.WriteTo(ms);

                var length = ms.Position + 4;
                var lengthBuf = new byte[4];
                lengthBuf[0] = (byte)(length & 0xff);
                lengthBuf[1] = (byte)((length >> 8) & 0xff);
                lengthBuf[2] = (byte)((length >> 16) & 0xff);
                lengthBuf[3] = (byte)((length >> 24) & 0xff);
                Socket.Client.Send(lengthBuf);
                var payload = ms.ToArray();
                Socket.Client.Send(payload);
            }
        }
    }

    public class DebuggerClient
    {
        private AsyncProtobufClient Client;
        private StoryDebugInfo DebugInfo;
        private Stream LogStream;
        private UInt32 OutgoingSeq = 1;
        private UInt32 IncomingSeq = 1;

        public delegate void BackendInfoDelegate(BkVersionInfoResponse response);
        public BackendInfoDelegate OnBackendInfo = delegate { };
        public delegate void StoryLoadedDelegate();
        public StoryLoadedDelegate OnStoryLoaded = delegate { };
        public delegate void BreakpointTriggeredDelegate(BkBreakpointTriggered bp);
        public BreakpointTriggeredDelegate OnBreakpointTriggered = delegate { };

        public DebuggerClient(AsyncProtobufClient client, StoryDebugInfo debugInfo)
        {
            Client = client;
            Client.MessageReceived = this.MessageReceived;
            DebugInfo = debugInfo;
        }

        public void EnableLogging(Stream logStream)
        {
            LogStream = logStream;
        }

        private void LogMessage(IMessage message)
        {
            if (LogStream != null)
            {
                using (var writer = new StreamWriter(LogStream, Encoding.UTF8, 0x1000, true))
                {
                    writer.Write(" DBG >>> ");
                    var settings = new JsonFormatter.Settings(true);
                    var formatter = new JsonFormatter(settings);
                    formatter.Format(message, writer);
                    writer.Write("\r\n");
                }
            }
        }

        public void Send(DebuggerToBackend message)
        {
            message.SeqNo = OutgoingSeq++;
            LogMessage(message);
            Client.Send(message);
        }

        private string TupleToString(MsgFrame frame)
        {
            string tuple = "";
            var node = DebugInfo.Nodes[frame.NodeId];
            RuleDebugInfo rule = null;
            if (node.RuleId != 0)
            {
                rule = DebugInfo.Rules[node.RuleId];
            }

            for (var i = 0; i < frame.Tuple.Column.Count; i++)
            {
                var value = frame.Tuple.Column[i];
                string columnName;
                if (rule == null)
                {
                    columnName = "";
                }
                else if (i < node.ColumnToVariableMaps.Count)
                {
                    var mappedColumnIdx = node.ColumnToVariableMaps[i];
                    if (mappedColumnIdx < rule.Variables.Count)
                    {
                        var variable = rule.Variables[mappedColumnIdx];
                        columnName = variable.Name;
                    }
                    else
                    {
                        columnName = "(Bad Variable Idx)";
                    }
                }
                else
                {
                    columnName = "(Unknown)";
                }

                string valueStr;
                switch ((Value.Type)value.TypeId)
                {
                    case Value.Type.Unknown:
                        valueStr = "(None)";
                        break;

                    case Value.Type.Integer:
                    case Value.Type.Integer64:
                        valueStr = value.Intval.ToString();
                        break;

                    case Value.Type.Float:
                        valueStr = value.Floatval.ToString();
                        break;

                    case Value.Type.String:
                    case Value.Type.GuidString:
                    default:
                        valueStr = value.Stringval;
                        break;

                }

                if (columnName.Length > 0)
                {
                    tuple += String.Format("{0}={1}, ", columnName, valueStr);
                }
                else
                {
                    tuple += String.Format("{0}, ", valueStr);
                }
            }

            return tuple;
        }

        private void DumpFrame(MsgFrame frame)
        {
            var node = DebugInfo.Nodes[frame.NodeId];

            string codeLocation = "";
            if (node.RuleId != 0)
            {
                var rule = DebugInfo.Rules[node.RuleId];
                var goal = DebugInfo.Goals[rule.GoalId];
                codeLocation = "@ " + goal.Name + ":" + node.Line.ToString() + " ";
            }

            string dbName = "";
            if (node.DatabaseId != 0)
            {
                var db = DebugInfo.Databases[node.DatabaseId];
                dbName = db.Name;
            }
            else
            {
                dbName = node.Name;
            }

            string tupleStr = TupleToString(frame);

            switch (frame.Type)
            {
                case MsgFrame.Types.FrameType.FrameIsValid:
                    if (node.Type == Node.Type.DivQuery || node.Type == Node.Type.InternalQuery || node.Type == Node.Type.UserQuery)
                    {
                        Console.WriteLine("    Query {0} ({1})", dbName, tupleStr);
                    }
                    else if (node.Type == Node.Type.Database)
                    {
                        Console.WriteLine("    Database {0} ({1})", dbName, tupleStr);
                    }
                    else
                    {
                        Console.WriteLine("    IsValid {0} {1} ({2})", node.Type, dbName, tupleStr);
                    }
                    break;

                case MsgFrame.Types.FrameType.FramePushdown:
                    if (node.Type == Node.Type.And || node.Type == Node.Type.NotAnd || node.Type == Node.Type.RelOp)
                    {
                        Console.WriteLine("    PushDown {0}({1})", codeLocation, tupleStr);
                    }
                    else if (node.Type == Node.Type.Rule)
                    {
                        Console.WriteLine("    Rule THEN part {0}({1})", codeLocation, tupleStr);
                    }
                    else
                    {
                        throw new InvalidOperationException("Pushdown operation not supported on this node");
                    }
                    break;

                case MsgFrame.Types.FrameType.FramePushdownDelete:
                    Console.WriteLine("    PushDownDelete {0} {1}({2})", node.Type, codeLocation, tupleStr);
                    break;

                case MsgFrame.Types.FrameType.FrameInsert:
                    if (node.Type == Node.Type.UserQuery)
                    {
                        Console.WriteLine("    User Query {0} ({1})", dbName, tupleStr);
                    }
                    else if (node.Type == Node.Type.Proc)
                    {
                        Console.WriteLine("    Call Proc {0} ({1})", dbName, tupleStr);
                    }
                    else if (node.Type == Node.Type.Database)
                    {
                        Console.WriteLine("    Insert Into {0} ({1})", dbName, tupleStr);
                    }
                    else
                    {
                        throw new InvalidOperationException("Insert operation not supported on this node");
                    }
                    break;

                case MsgFrame.Types.FrameType.FrameDelete:
                    if (node.Type == Node.Type.Database)
                    {
                        Console.WriteLine("    Delete from {0} ({1})", dbName, tupleStr);
                    }
                    else
                    {
                        throw new InvalidOperationException("Delete operation not supported on this node");
                    }
                    break;

                default:
                    throw new InvalidOperationException("Unsupported frame type");
            }
        }

        private List<MsgFrame> CoalesceCallStack(BkBreakpointTriggered message)
        {
            var frames = new List<MsgFrame>();
            var index = 0;
            foreach (var frame in message.CallStack)
            {
                if (frame.Type == MsgFrame.Types.FrameType.FrameInsert
                    || frame.Type == MsgFrame.Types.FrameType.FrameDelete
                    || (frame.Type == MsgFrame.Types.FrameType.FramePushdown
                        && DebugInfo.Nodes[frame.NodeId].Type == Node.Type.Rule)
                    || index == message.CallStack.Count - 1)
                {
                    frames.Add(frame);
                }

                index++;
            }

            return frames;
        }

        private void BreakpointTriggered(BkBreakpointTriggered message)
        {
            /*var msg = new DebuggerToBackend();
            msg.Continue = new DbgContinue();
            msg.Continue.Action = DbgContinue.Types.Action.Continue;
            Send(msg);
            
            Console.WriteLine("Breakpoint triggered!");
            foreach (var frame in message.CallStack)
            {
                DumpFrame(frame);
            }

            Console.WriteLine("Coalesced stack:");
            var cs = CoalesceCallStack(message);
            foreach (var frame in cs)
            {
                DumpFrame(frame);
            }*/

            OnBreakpointTriggered(message);
        }

        private void GlobalBreakpointTriggered(BkGlobalBreakpointTriggered message)
        {
            var msg = new DebuggerToBackend();
            msg.Continue = new DbgContinue();
            msg.Continue.Action = DbgContinue.Types.Action.Continue;
            Send(msg);
            
            Console.WriteLine("Global {0}", message.Reason);
        }

        private void MessageReceived(BackendToDebugger message)
        {
            LogMessage(message);

            if (message.SeqNo != IncomingSeq)
            {
                throw new InvalidDataException($"DBG sequence number mismatch; got {message.SeqNo} expected {IncomingSeq}");
            }

            IncomingSeq++;


            switch (message.MsgCase)
            {
                case BackendToDebugger.MsgOneofCase.VersionInfo:
                    {
                        Console.WriteLine("Got version info from backend");
                        if (message.VersionInfo.StoryLoaded)
                        {
                            var msg = new DebuggerToBackend();
                            msg.SetGlobalBreakpoints = new DbgSetGlobalBreakpoints();
                            msg.SetGlobalBreakpoints.BreakpointMask = 0x3f;
                            Send(msg);
                        }

                        OnBackendInfo(message.VersionInfo);
                        break;
                    }

                case BackendToDebugger.MsgOneofCase.Results:
                    Console.WriteLine("RC {0}", message.Results.StatusCode);
                    break;

                case BackendToDebugger.MsgOneofCase.StoryLoaded:
                    {
                        Console.WriteLine("StoryLoaded");
                        var msg = new DebuggerToBackend();
                        msg.SetGlobalBreakpoints = new DbgSetGlobalBreakpoints();
                        msg.SetGlobalBreakpoints.BreakpointMask = 0x3f;
                        Send(msg);

                        OnStoryLoaded();
                        break;
                    }

                case BackendToDebugger.MsgOneofCase.BreakpointTriggered:
                    BreakpointTriggered(message.BreakpointTriggered);
                    break;

                case BackendToDebugger.MsgOneofCase.GlobalBreakpointTriggered:
                    GlobalBreakpointTriggered(message.GlobalBreakpointTriggered);
                    break;

                default:
                    Console.WriteLine("Got unknown msg!");
                    break;
            }
        }
    }
}