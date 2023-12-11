using Google.Protobuf;
using LSLib.LS.Story;
using LSLib.LS.Story.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LSTools.DebuggerFrontend;

public class AsyncProtobufClient
{
    private TcpClient Socket;
    private byte[] MessageBuffer;
    private int BufferPos;

    public delegate void MessageReceivedDelegate(BackendToDebugger message);
    public MessageReceivedDelegate MessageReceived = delegate { };

    public AsyncProtobufClient(string host, int port)
    {
        MessageBuffer = new byte[0x100000];
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

                if (length >= 0x100000)
                {
                    throw new InvalidDataException($"Message too long ({length} bytes)");
                }

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

    public delegate void DebugSessionEndedDelegate();
    public DebugSessionEndedDelegate OnDebugSessionEnded = delegate { };

    public delegate void BreakpointTriggeredDelegate(BkBreakpointTriggered bp);
    public BreakpointTriggeredDelegate OnBreakpointTriggered = delegate { };

    public delegate void GlobalBreakpointTriggeredDelegate(BkGlobalBreakpointTriggered bp);
    public GlobalBreakpointTriggeredDelegate OnGlobalBreakpointTriggered = delegate { };

    public delegate void StorySyncDataDelegate(BkSyncStoryData data);
    public StorySyncDataDelegate OnStorySyncData = delegate { };

    public delegate void StorySyncFinishedDelegate();
    public StorySyncFinishedDelegate OnStorySyncFinished = delegate { };

    public delegate void DebugOutputDelegate(BkDebugOutput msg);
    public DebugOutputDelegate OnDebugOutput = delegate { };

    public delegate void BeginDatabaseContentsDelegate(BkBeginDatabaseContents msg);
    public BeginDatabaseContentsDelegate OnBeginDatabaseContents = delegate { };

    public delegate void DatabaseRowDelegate(BkDatabaseRow msg);
    public DatabaseRowDelegate OnDatabaseRow = delegate { };

    public delegate void EndDatabaseContentsDelegate(BkEndDatabaseContents msg);
    public EndDatabaseContentsDelegate OnEndDatabaseContents = delegate { };

    public delegate void EvaluateRowDelegate(UInt32 seq, BkEvaluateRow msg);
    public EvaluateRowDelegate OnEvaluateRow = delegate { };

    public delegate void EvaluateFinishedDelegate(UInt32 seq, BkEvaluateFinished msg);
    public EvaluateFinishedDelegate OnEvaluateFinished = delegate { };

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

    public UInt32 Send(DebuggerToBackend message)
    {
        message.SeqNo = OutgoingSeq++;
        LogMessage(message);
        Client.Send(message);
        return message.SeqNo;
    }

    public void SendIdentify(UInt32 protocolVersion)
    {
        var msg = new DebuggerToBackend
        {
            Identify = new DbgIdentifyRequest
            {
                ProtocolVersion = protocolVersion
            }
        };
        Send(msg);
    }

    private MsgBreakpoint BreakpointToMsg(Breakpoint breakpoint)
    {
        var msgBp = new MsgBreakpoint();
        if (breakpoint.LineInfo.Node != null)
        {
            msgBp.NodeId = breakpoint.LineInfo.Node.Id;
        }
        else
        {
            msgBp.GoalId = breakpoint.LineInfo.Goal.Id;
        }

        msgBp.IsInitAction = breakpoint.LineInfo.Type == LineType.GoalInitActionLine;
        if (breakpoint.LineInfo.Type == LineType.GoalInitActionLine
            || breakpoint.LineInfo.Type == LineType.GoalExitActionLine
            || breakpoint.LineInfo.Type == LineType.RuleActionLine)
        {
            msgBp.ActionIndex = (Int32)breakpoint.LineInfo.ActionIndex;
        }
        else
        {
            msgBp.ActionIndex = -1;
        }
        
        msgBp.BreakpointMask = 0x3f; // TODO const
        return msgBp;
    }

    public void SendSetBreakpoints(List<Breakpoint> breakpoints)
    {
        var setBps = new DbgSetBreakpoints();
        foreach (var breakpoint in breakpoints)
        {
            if (breakpoint.Verified)
            {
                var msgBp = BreakpointToMsg(breakpoint);
                setBps.Breakpoint.Add(msgBp);
            }
        }

        var msg = new DebuggerToBackend
        {
            SetBreakpoints = setBps
        };
        Send(msg);
    }

    public void SendGetDatabaseContents(UInt32 databaseId)
    {
        var msg = new DebuggerToBackend
        {
            GetDatabaseContents = new DbgGetDatabaseContents
            {
                DatabaseId = databaseId
            }
        };
        Send(msg);
    }

    public void SendSetGlobalBreakpoints(UInt32 breakpointMask)
    {
        var msg = new DebuggerToBackend
        {
            SetGlobalBreakpoints = new DbgSetGlobalBreakpoints
            {
                BreakpointMask = breakpointMask
            }
        };
        Send(msg);
    }

    public void SendContinue(DbgContinue.Types.Action action, UInt32 breakpointMask, UInt32 flags)
    {
        var msg = new DebuggerToBackend
        {
            Continue = new DbgContinue
            {
                Action = action,
                BreakpointMask = breakpointMask,
                Flags = flags
            }
        };
        Send(msg);
    }

    public void SendSyncStory()
    {
        var msg = new DebuggerToBackend
        {
            SyncStory = new DbgSyncStory()
        };
        Send(msg);
    }

    public UInt32 SendEvaluate(DbgEvaluate.Types.EvalType type, UInt32 nodeId, MsgTuple args)
    {
        var msg = new DebuggerToBackend
        {
            Evaluate = new DbgEvaluate
            {
                Type = type,
                NodeId = nodeId,
                Params = args
            }
        };
        return Send(msg);
    }

    private void BreakpointTriggered(BkBreakpointTriggered message)
    {
        OnBreakpointTriggered(message);
    }

    private void GlobalBreakpointTriggered(BkGlobalBreakpointTriggered message)
    {
        OnGlobalBreakpointTriggered(message);
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
                OnBackendInfo(message.VersionInfo);
                break;

            case BackendToDebugger.MsgOneofCase.Results:
                break;

            case BackendToDebugger.MsgOneofCase.StoryLoaded:
                OnStoryLoaded();
                break;

            case BackendToDebugger.MsgOneofCase.DebugSessionEnded:
                OnDebugSessionEnded();
                break;

            case BackendToDebugger.MsgOneofCase.BreakpointTriggered:
                BreakpointTriggered(message.BreakpointTriggered);
                break;

            case BackendToDebugger.MsgOneofCase.GlobalBreakpointTriggered:
                GlobalBreakpointTriggered(message.GlobalBreakpointTriggered);
                break;

            case BackendToDebugger.MsgOneofCase.SyncStoryData:
                OnStorySyncData(message.SyncStoryData);
                break;

            case BackendToDebugger.MsgOneofCase.SyncStoryFinished:
                OnStorySyncFinished();
                break;

            case BackendToDebugger.MsgOneofCase.DebugOutput:
                OnDebugOutput(message.DebugOutput);
                break;

            case BackendToDebugger.MsgOneofCase.BeginDatabaseContents:
                OnBeginDatabaseContents(message.BeginDatabaseContents);
                break;

            case BackendToDebugger.MsgOneofCase.DatabaseRow:
                OnDatabaseRow(message.DatabaseRow);
                break;

            case BackendToDebugger.MsgOneofCase.EndDatabaseContents:
                OnEndDatabaseContents(message.EndDatabaseContents);
                break;

            case BackendToDebugger.MsgOneofCase.EvaluateRow:
                OnEvaluateRow(message.ReplySeqNo, message.EvaluateRow);
                break;

            case BackendToDebugger.MsgOneofCase.EvaluateFinished:
                OnEvaluateFinished(message.ReplySeqNo, message.EvaluateFinished);
                break;

            default:
                throw new InvalidOperationException($"Unknown message from DBG: {message.MsgCase}");
        }
    }
}