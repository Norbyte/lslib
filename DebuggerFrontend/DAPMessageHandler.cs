using LSLib.LS.Story.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LSTools.DebuggerFrontend
{
    public class DAPMessageHandler
    {
        private DAPStream Stream;
        private Stream LogStream;

        private StoryDebugInfo DebugInfo;
        private String DebugInfoPath;
        private DebugInfoSync DebugInfoSync;
        private Thread DbgThread;
        private AsyncProtobufClient DbgClient;
        private DebuggerClient DbgCli;
        private StackTracePrinter TracePrinter;
        private BreakpointManager Breakpoints;
        private List<CoalescedFrame> Stack;
        private bool Stopped;
        // Should we send a continue message after story synchronization is done?
        // This is needed if the sync was triggered by a global breakpoint.
        private bool ContinueAfterSync;
        // Should we pause on the next instruction?
        private bool PauseRequested;


        public DAPMessageHandler(DAPStream stream)
        {
            Stream = stream;
            Stream.MessageReceived += this.MessageReceived;
        }

        public void EnableLogging(Stream logStream)
        {
            LogStream = logStream;
        }

        private void SendEvent(string command, IDAPMessagePayload body)
        {
            var reply = new DAPEvent
            {
                type = "event",
                @event = command,
                body = body
            };

            Stream.Send(reply);
        }

        private void SendReply(DAPRequest request, IDAPMessagePayload response)
        {
            var reply = new DAPResponse
            {
                type = "response",
                request_seq = request.seq,
                success = true,
                command = request.command,
                body = response
            };

            Stream.Send(reply);
        }

        private void SendReply(DAPRequest request, string errorText)
        {
            var reply = new DAPResponse
            {
                type = "response",
                request_seq = request.seq,
                success = false,
                command = request.command,
                message = errorText
            };

            Stream.Send(reply);
        }

        private void SendBreakpoint(string eventType, Breakpoint bp)
        {
            var bpMsg = new DAPBreakpointEvent
            {
                reason = eventType,
                breakpoint = bp.ToDAP()
            };
            SendEvent("breakpoint", bpMsg);
        }

        private void LogError(String message)
        {
            var outputMsg = new DAPOutputMessage
            {
                category = "stderr",
                output = message
            };
            SendEvent("output", outputMsg);

            if (LogStream != null)
            {
                using (var writer = new StreamWriter(LogStream, Encoding.UTF8, 0x1000, true))
                {
                    writer.WriteLine(message);
                    Console.WriteLine(message);
                }
            }
        }

        private void MessageReceived(DAPMessage message)
        {
            if (message is DAPRequest)
            {
                try
                {
                    HandleRequest(message as DAPRequest);
                }
                catch (Exception e)
                {
                    LogError(e.ToString());

                    if (message.type == "request")
                    {
                        SendReply(message as DAPRequest, e.ToString());
                    }
                }
            }
            else if (message is DAPEvent)
            {
                HandleEvent(message as DAPEvent);
            }
            else
            {
                throw new InvalidDataException("DAP replies not handled");
            }
        }

        private void InitDebugger()
        {
            var debugPayload = File.ReadAllBytes(DebugInfoPath);
            var loader = new DebugInfoLoader();
            DebugInfo = loader.Load(debugPayload);
            TracePrinter = new StackTracePrinter(DebugInfo);
            Stack = null;
            Stopped = false;

            var changedBps = Breakpoints.DebugInfoLoaded(DebugInfo);
            // Notify the debugger that the status of breakpoints changed
            changedBps.ForEach(bp => SendBreakpoint("changed", bp));
        }

        private void SynchronizeStoryWithBackend(bool continueAfterSync)
        {
            DebugInfoSync = new DebugInfoSync(DebugInfo);
            ContinueAfterSync = continueAfterSync;
            DbgCli.SendSyncStory();
        }

        private void OnBackendInfo(BkVersionInfoResponse response)
        {
            if (response.StoryLoaded)
            {
                InitDebugger();
            }

            if (response.StoryInitialized)
            {
                SynchronizeStoryWithBackend(false);
            }
        }

        private void OnStoryLoaded()
        {
            InitDebugger();
        }

        private void OnDebugSessionEnded()
        {
            Stopped = false;
            DebugInfo = null;
            TracePrinter = null;

            var changedBps = Breakpoints.DebugInfoUnloaded();
            // Notify the debugger that the status of breakpoints changed
            changedBps.ForEach(bp => SendBreakpoint("changed", bp));

            var outputMsg = new DAPOutputMessage
            {
                category = "console",
                output = "Story unloaded - debug session terminated\r\n"
            };
            SendEvent("output", outputMsg);
        }

        private void OnBreakpointTriggered(BkBreakpointTriggered bp)
        {
            Stack = TracePrinter.BreakpointToStack(bp);
            Stopped = true;
            PauseRequested = false;

            var stopped = new DAPStoppedEvent
            {
                reason = "breakpoint",
                threadId = 1
            };
            SendEvent("stopped", stopped);
        }

        private void OnGlobalBreakpointTriggered(BkGlobalBreakpointTriggered message)
        {
            if (message.Reason == BkGlobalBreakpointTriggered.Types.Reason.BreakpointStoryLoaded)
            {
                DbgCli.SendSetGlobalBreakpoints(0x80); // TODO const
                // Break on next node
                DbgCli.SendContinue(DbgContinue.Types.Action.StepInto);
            }
            else if (message.Reason == BkGlobalBreakpointTriggered.Types.Reason.BreakpointGameInit)
            {
                SynchronizeStoryWithBackend(true);
            }
            else
            {
                throw new InvalidOperationException($"Global breakpoint type not supported: {message.Reason}");
            }
        }

        private void OnStorySyncData(BkSyncStoryData data)
        {
            DebugInfoSync.AddData(data);
        }

        private void OnStorySyncFinished()
        {
            DebugInfoSync.Finish();

            if (!DebugInfoSync.Matches)
            {
                var reasons = DebugInfoSync.Reasons.Aggregate((a, b) => a + "\r\n" + b);
                var msg = $"Could not attach to backend: Debug info does not match loaded story.\r\nMismatches:\r\n{reasons}";
                LogError(msg);
                throw new SystemException(msg);
            }

            DebugInfoSync = null;

            if (ContinueAfterSync)
            {
                if (PauseRequested)
                {
                    DbgCli.SendContinue(DbgContinue.Types.Action.StepInto);
                }
                else
                {
                    DbgCli.SendContinue(DbgContinue.Types.Action.Continue);
                }
            }
        }

        private void HandleInitializeRequest(DAPRequest request, DAPInitializeRequest init)
        {
            var reply = new DAPCapabilities();
            reply.supportsConfigurationDoneRequest = true;
            SendReply(request, reply);
        }

        private void DebugThreadMain()
        {
            try
            {
                DbgClient.RunLoop();
            }
            catch (Exception e)
            {
                LogError(e.ToString());
                Environment.Exit(2);
            }
        }

        private void HandleLaunchRequest(DAPRequest request, DAPLaunchRequest launch)
        {
            if (!File.Exists(launch.debugInfoPath))
            {
                DebugInfoPath = launch.debugInfoPath;
                SendReply(request, "Story debug file does not exist: " + launch.debugInfoPath);
                return;
            }

            DebugInfoPath = launch.debugInfoPath;

            try
            {
                DbgClient = new AsyncProtobufClient(launch.backendHost, launch.backendPort);
            }
            catch (SocketException e)
            {
                SendReply(request, "Could not connect to Osiris backend server: " + e.Message);
                return;
            }

            DbgCli = new DebuggerClient(DbgClient, DebugInfo)
            {
                OnStoryLoaded = this.OnStoryLoaded,
                OnDebugSessionEnded = this.OnDebugSessionEnded,
                OnBackendInfo = this.OnBackendInfo,
                OnBreakpointTriggered = this.OnBreakpointTriggered,
                OnGlobalBreakpointTriggered = this.OnGlobalBreakpointTriggered,
                OnStorySyncData = this.OnStorySyncData,
                OnStorySyncFinished = this.OnStorySyncFinished
            };
            if (LogStream != null)
            {
                DbgCli.EnableLogging(LogStream);
            }
            
            DbgCli.SendIdentify();

            DbgThread = new Thread(new ThreadStart(DebugThreadMain));
            DbgThread.Start();

            Breakpoints = new BreakpointManager(DbgCli);

            var reply = new DAPLaunchResponse();
            SendReply(request, reply);

            var initializedEvt = new DAPInitializedEvent();
            SendEvent("initialized", initializedEvt);
        }

        private void HandleSetBreakpointsRequest(DAPRequest request, DAPSetBreakpointsRequest breakpoints)
        {
            if (Breakpoints != null)
            {
                var goalName = Path.GetFileNameWithoutExtension(breakpoints.source.name);
                Breakpoints.ClearGoalBreakpoints(goalName);

                var reply = new DAPSetBreakpointsResponse
                {
                    breakpoints = new List<DAPBreakpoint>()
                };

                foreach (var breakpoint in breakpoints.breakpoints)
                {
                    var bp = Breakpoints.AddBreakpoint(breakpoints.source, breakpoint);
                    reply.breakpoints.Add(bp.ToDAP());
                }

                Breakpoints.UpdateBreakpointsOnBackend();

                SendReply(request, reply);
            }
            else
            {
                SendReply(request, "Cannot add breakpoint - breakpoint manager not yet initialized");
            }
        }

        private void HandleConfigurationDoneRequest(DAPRequest request, DAPEmptyPayload msg)
        {
            SendReply(request, new DAPEmptyPayload());
        }

        private void HandleThreadsRequest(DAPRequest request, DAPEmptyPayload msg)
        {
            var reply = new DAPThreadsResponse
            {
                threads = new List<DAPThread> {
                    new DAPThread
                    {
                        id = 1,
                        name = "OsirisThread"
                    }
                }
            };
            SendReply(request, reply);
        }

        private void HandleStackTraceRequest(DAPRequest request, DAPStackFramesRequest msg)
        {
            if (!Stopped)
            {
                SendReply(request, "Cannot get stack when story is running");
                return;
            }

            if (msg.threadId != 1)
            {
                SendReply(request, "Requested stack trace for unknown thread");
                return;
            }

            int startFrame = msg.startFrame == null ? 0 : (int)msg.startFrame;
            int levels = (msg.levels == null || msg.levels == 0) ? Stack.Count : (int)msg.levels;
            int lastFrame = Math.Min(startFrame + levels, Stack.Count);

            var frames = new List<DAPStackFrame>();
            for (var i = startFrame; i < lastFrame; i++)
            {
                var frame = Stack[i];
                var dapFrame = new DAPStackFrame();
                dapFrame.id = i;
                // TODO DAPStackFrameFormat for name formatting
                dapFrame.name = frame.Name;
                if (frame.File != null)
                {
                    dapFrame.source = new DAPSource
                    {
                        name = Path.GetFileNameWithoutExtension(frame.File),
                        path = frame.File
                    };
                    dapFrame.line = frame.Line;
                    dapFrame.column = 1;
                }

                // TODO presentationHint
                frames.Add(dapFrame);
            }

            var reply = new DAPStackFramesResponse
            {
                stackFrames = frames,
                totalFrames = Stack.Count
            };
            SendReply(request, reply);
        }

        private void HandleScopesRequest(DAPRequest request, DAPScopesRequest msg)
        {
            if (!Stopped)
            {
                SendReply(request, "Cannot get scopes when story is running");
                return;
            }

            if (msg.frameId < 0 || msg.frameId >= Stack.Count)
            {
                SendReply(request, "Requested scopes for unknown frame");
                return;
            }

            var frame = Stack[msg.frameId];
            var reply = new DAPScopesResponse
            {
                scopes = new List<DAPScope>
                {
                    new DAPScope
                    {
                        // TODO DB insert args?
                        name = "Locals",
                        variablesReference = msg.frameId + 1,
                        namedVariables = frame.Variables.Count,
                        indexedVariables = 0,
                        expensive = false
                    }
                }
            };
            SendReply(request, reply);
        }

        private void HandleVariablesRequest(DAPRequest request, DAPVariablesRequest msg)
        {
            if (!Stopped)
            {
                SendReply(request, "Cannot get variables when story is running");
                return;
            }

            if (msg.variablesReference < 1 || msg.variablesReference > Stack.Count)
            {
                SendReply(request, "Requested variables for unknown frame");
                return;
            }

            var frame = Stack[msg.variablesReference - 1];
            int startIndex = msg.start == null ? 0 : (int)msg.start;
            int numVars = (msg.count == null || msg.count == 0) ? frame.Variables.Count : (int)msg.count;
            int lastIndex = Math.Min(startIndex + numVars, frame.Variables.Count);
            // TODO req.filter, format

            var variables = new List<DAPVariable>();
            for (var i = startIndex; i < startIndex + numVars; i++)
            {
                var variable = frame.Variables[i];
                var dapVar = new DAPVariable();
                dapVar.name = variable.Name;
                dapVar.value = variable.Value;
                dapVar.type = variable.Type;
                variables.Add(dapVar);
            }

            var reply = new DAPVariablesResponse
            {
                variables = variables
            };
            SendReply(request, reply);
        }

        private void HandleContinueRequest(DAPRequest request, DAPContinueRequest msg, DbgContinue.Types.Action action)
        {
            if (msg.threadId != 1)
            {
                SendReply(request, "Requested continue for unknown thread");
                return;
            }

            if (action == DbgContinue.Types.Action.Pause)
            {
                if (Stopped)
                {
                    SendReply(request, "Already stopped");
                    return;
                }

                PauseRequested = true;
            }
            else
            {
                if (!Stopped)
                {
                    SendReply(request, "Already running");
                    return;
                }

                Stopped = false;
            }
            
            DbgCli.SendContinue(action);

            var reply = new DAPContinueResponse
            {
                allThreadsContinued = false
            };
            SendReply(request, reply);
        }

        private void HandleDisconnectRequest(DAPRequest request, DAPDisconnectRequest msg)
        {
            var reply = new DAPEmptyPayload();
            SendReply(request, reply);
            // TODO - close session
        }

        private void HandleRequest(DAPRequest request)
        {
            switch (request.command)
            {
                case "initialize":
                    HandleInitializeRequest(request, request.arguments as DAPInitializeRequest);
                    break;

                case "launch":
                    HandleLaunchRequest(request, request.arguments as DAPLaunchRequest);
                    break;

                case "setBreakpoints":
                    HandleSetBreakpointsRequest(request, request.arguments as DAPSetBreakpointsRequest);
                    break;

                case "configurationDone":
                    HandleConfigurationDoneRequest(request, request.arguments as DAPEmptyPayload);
                    break;

                case "threads":
                    HandleThreadsRequest(request, request.arguments as DAPEmptyPayload);
                    break;

                case "stackTrace":
                    HandleStackTraceRequest(request, request.arguments as DAPStackFramesRequest);
                    break;

                case "scopes":
                    HandleScopesRequest(request, request.arguments as DAPScopesRequest);
                    break;

                case "variables":
                    HandleVariablesRequest(request, request.arguments as DAPVariablesRequest);
                    break;

                case "continue":
                    HandleContinueRequest(request, request.arguments as DAPContinueRequest,
                        DbgContinue.Types.Action.Continue);
                    break;

                case "next":
                    HandleContinueRequest(request, request.arguments as DAPContinueRequest,
                        DbgContinue.Types.Action.StepOver);
                    break;

                case "stepIn":
                    HandleContinueRequest(request, request.arguments as DAPContinueRequest,
                        DbgContinue.Types.Action.StepInto);
                    break;

                case "stepOut":
                    HandleContinueRequest(request, request.arguments as DAPContinueRequest,
                        DbgContinue.Types.Action.StepOut);
                    break;

                case "pause":
                    HandleContinueRequest(request, request.arguments as DAPContinueRequest,
                        DbgContinue.Types.Action.Pause);
                    break;

                case "disconnect":
                    HandleDisconnectRequest(request, request.arguments as DAPDisconnectRequest);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported DAP request: {request.command}");
            }
        }

        private void HandleEvent(DAPEvent evt)
        {
            throw new InvalidOperationException($"Unsupported DAP event: {evt.@event}");
        }
    }
}
