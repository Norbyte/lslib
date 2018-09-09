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
        private const UInt32 ProtocolVersion = 5;

        private DAPStream Stream;
        private Stream LogStream;

        private StoryDebugInfo DebugInfo;
        private String DebugInfoPath;
        private DebugInfoSync DebugInfoSync;
        private Thread DbgThread;
        private AsyncProtobufClient DbgClient;
        private DebuggerClient DbgCli;
        private ValueFormatter Formatter;
        private StackTracePrinter TracePrinter;
        private BreakpointManager Breakpoints;
        private DatabaseEnumerator DatabaseDumper;
        private List<CoalescedFrame> Stack;
        private DAPCustomConfiguration Config;
        private bool Stopped;
        // Should we send a continue message after story synchronization is done?
        // This is needed if the sync was triggered by a global breakpoint.
        private bool ContinueAfterSync;
        // Should we pause on the next instruction?
        private bool PauseRequested;
        // Mod/project UUID we'll send to the debugger instead of the packaged path
        public string ModUuid;


        public DAPMessageHandler(DAPStream stream)
        {
            Stream = stream;
            Stream.MessageReceived += this.MessageReceived;
        }

        public void EnableLogging(Stream logStream)
        {
            LogStream = logStream;
        }

        private void SendBreakpoint(string eventType, Breakpoint bp)
        {
            var bpMsg = new DAPBreakpointEvent
            {
                reason = eventType,
                breakpoint = bp.ToDAP()
            };
            Stream.SendEvent("breakpoint", bpMsg);
        }

        private void LogError(String message)
        {
            var outputMsg = new DAPOutputMessage
            {
                category = "stderr",
                output = message + "\r\n"
            };
            Stream.SendEvent("output", outputMsg);

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
                catch (RequestFailedException e)
                {
                    Stream.SendReply(message as DAPRequest, e.Message);
                }
                catch (Exception e)
                {
                    LogError(e.ToString());
                    Stream.SendReply(message as DAPRequest, e.ToString());
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

            Formatter = new ValueFormatter(DebugInfo);
            TracePrinter = new StackTracePrinter(DebugInfo, Formatter);
            TracePrinter.ModUuid = ModUuid;
            if (Config != null)
            {
                TracePrinter.MergeFrames = !Config.rawFrames;
            }

            Stack = null;
            Stopped = false;

            DatabaseDumper = new DatabaseEnumerator(DbgCli, Stream, DebugInfo, Formatter);

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
            if (response.ProtocolVersion != ProtocolVersion)
            {
                throw new InvalidDataException($"Backend sent unsupported protocol version; got {response.ProtocolVersion}, we only support {ProtocolVersion}");
            }

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
            DatabaseDumper = null;
            TracePrinter = null;
            Formatter = null;

            var changedBps = Breakpoints.DebugInfoUnloaded();
            // Notify the debugger that the status of breakpoints changed
            changedBps.ForEach(bp => SendBreakpoint("changed", bp));

            var outputMsg = new DAPOutputMessage
            {
                category = "console",
                output = "Story unloaded - debug session terminated\r\n"
            };
            Stream.SendEvent("output", outputMsg);
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
            Stream.SendEvent("stopped", stopped);
        }

        private void OnGlobalBreakpointTriggered(BkGlobalBreakpointTriggered message)
        {
            if (message.Reason == BkGlobalBreakpointTriggered.Types.Reason.StoryLoaded)
            {
                DbgCli.SendSetGlobalBreakpoints(0x80); // TODO const
                // Break on next node
                SendContinue(DbgContinue.Types.Action.StepInto);
            }
            else if (message.Reason == BkGlobalBreakpointTriggered.Types.Reason.GameInit)
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
                    SendContinue(DbgContinue.Types.Action.StepInto);
                }
                else
                {
                    SendContinue(DbgContinue.Types.Action.Continue);
                }
            }
        }

        private void OnDebugOutput(BkDebugOutput msg)
        {
            var outputMsg = new DAPOutputMessage
            {
                category = "stdout",
                output = "DebugBreak: " + msg.Message + "\r\n"
            };
            Stream.SendEvent("output", outputMsg);
        }

        private void HandleInitializeRequest(DAPRequest request, DAPInitializeRequest init)
        {
            var reply = new DAPCapabilities
            {
                supportsConfigurationDoneRequest = true,
                supportsEvaluateForHovers = true
            };
            Stream.SendReply(request, reply);
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
            Config = launch.dbgOptions;
            ModUuid = launch.modUuid;

            if (!File.Exists(launch.debugInfoPath))
            {
                throw new RequestFailedException("Story debug file does not exist: " + launch.debugInfoPath);
            }

            DebugInfoPath = launch.debugInfoPath;

            try
            {
                DbgClient = new AsyncProtobufClient(launch.backendHost, launch.backendPort);
            }
            catch (SocketException e)
            {
                throw new RequestFailedException("Could not connect to Osiris backend server: " + e.Message);
            }

            DbgCli = new DebuggerClient(DbgClient, DebugInfo)
            {
                OnStoryLoaded = this.OnStoryLoaded,
                OnDebugSessionEnded = this.OnDebugSessionEnded,
                OnBackendInfo = this.OnBackendInfo,
                OnBreakpointTriggered = this.OnBreakpointTriggered,
                OnGlobalBreakpointTriggered = this.OnGlobalBreakpointTriggered,
                OnStorySyncData = this.OnStorySyncData,
                OnStorySyncFinished = this.OnStorySyncFinished,
                OnDebugOutput = this.OnDebugOutput
            };
            if (LogStream != null)
            {
                DbgCli.EnableLogging(LogStream);
            }
            
            DbgCli.SendIdentify(ProtocolVersion);

            DbgThread = new Thread(new ThreadStart(DebugThreadMain));
            DbgThread.Start();

            Breakpoints = new BreakpointManager(DbgCli);

            var reply = new DAPLaunchResponse();
            Stream.SendReply(request, reply);

            var initializedEvt = new DAPInitializedEvent();
            Stream.SendEvent("initialized", initializedEvt);
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

                Stream.SendReply(request, reply);
            }
            else
            {
                throw new RequestFailedException("Cannot add breakpoint - breakpoint manager not yet initialized");
            }
        }

        private void HandleConfigurationDoneRequest(DAPRequest request, DAPEmptyPayload msg)
        {
            Stream.SendReply(request, new DAPEmptyPayload());
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
            Stream.SendReply(request, reply);
        }

        private void HandleStackTraceRequest(DAPRequest request, DAPStackFramesRequest msg)
        {
            if (!Stopped)
            {
                throw new RequestFailedException("Cannot get stack when story is running");
            }

            if (msg.threadId != 1)
            {
                throw new RequestFailedException("Requested stack trace for unknown thread");
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
            Stream.SendReply(request, reply);
        }

        private void HandleScopesRequest(DAPRequest request, DAPScopesRequest msg)
        {
            if (!Stopped)
            {
                throw new RequestFailedException("Cannot get scopes when story is running");
            }

            if (msg.frameId < 0 || msg.frameId >= Stack.Count)
            {
                throw new RequestFailedException("Requested scopes for unknown frame");
            }

            var frame = Stack[msg.frameId];
            var scope = new DAPScope
            {
                // TODO DB insert args?
                name = "Locals",
                variablesReference = msg.frameId + 1,
                namedVariables = frame.Variables.Count,
                indexedVariables = 0,
                expensive = false
            };

            // Send location information for rule-local scopes.
            // If the scope location is missing, the value of local variables will be displayed in 
            // every rule that has variables with the same name.
            // This restricts them so they're only displayed in the rule that the stack frame belongs to.
            if (frame.Rule != null)
            {
                scope.source = new DAPSource
                {
                    name = Path.GetFileNameWithoutExtension(frame.File),
                    path = frame.File
                };
                scope.line = (int)frame.Rule.ConditionsStartLine;
                scope.column = 1;
                scope.endLine = (int)frame.Rule.ActionsEndLine + 1;
                scope.endColumn = 1;
            }

            var reply = new DAPScopesResponse
            {
                scopes = new List<DAPScope>{ scope }
            };
            Stream.SendReply(request, reply);
        }

        private List<DAPVariable> GetStackVariables(DAPVariablesRequest msg, int frameIndex)
        {
            if (frameIndex < 0 || frameIndex >= Stack.Count)
            {
                throw new RequestFailedException($"Requested variables for unknown frame {frameIndex}");
            }

            var frame = Stack[frameIndex];
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

            return variables;
        }

        private void HandleVariablesRequest(DAPRequest request, DAPVariablesRequest msg)
        {
            if (!Stopped)
            {
                throw new RequestFailedException("Cannot get variables when story is running");
            }

            long variableType = (msg.variablesReference >> 48);
            List<DAPVariable> variables;
            if (variableType == 0)
            {
                int frameIndex = (int)msg.variablesReference - 1;
                variables = GetStackVariables(msg, frameIndex);
            }
            else if (variableType == 1 || variableType == 2)
            {
                variables = DatabaseDumper.GetVariables(msg, msg.variablesReference);
            }
            else
            {
                throw new InvalidOperationException($"Unknown variables reference type: {msg.variablesReference}");
            }

            var reply = new DAPVariablesResponse
            {
                variables = variables
            };
            Stream.SendReply(request, reply);
        }

        private UInt32 GetContinueBreakpointMask()
        {
            if (Config != null && Config.stopOnAllFrames)
            {
                return
                    // Break on all possible node events
                    (UInt32)MsgBreakpoint.Types.BreakpointType.Valid
                    | (UInt32)MsgBreakpoint.Types.BreakpointType.Pushdown
                    | (UInt32)MsgBreakpoint.Types.BreakpointType.Insert
                    | (UInt32)MsgBreakpoint.Types.BreakpointType.RuleAction
                    | (UInt32)MsgBreakpoint.Types.BreakpointType.InitCall
                    | (UInt32)MsgBreakpoint.Types.BreakpointType.ExitCall
                    | (UInt32)MsgBreakpoint.Types.BreakpointType.Delete;
            }
            else
            {
                return
                    // Break on Pushdown for rule "AND/NOT AND" nodes
                    (UInt32)MsgBreakpoint.Types.BreakpointType.Pushdown
                    // Break on rule THEN part actions
                    | (UInt32)MsgBreakpoint.Types.BreakpointType.RuleAction
                    // Break on goal Init/Exit calls
                    | (UInt32)MsgBreakpoint.Types.BreakpointType.InitCall
                    | (UInt32)MsgBreakpoint.Types.BreakpointType.ExitCall;
            }
        }

        private UInt32 GetContinueFlags()
        {
            UInt32 flags = 0;
            if (Config == null || !Config.stopOnAllFrames)
            {
                flags |= (UInt32)DbgContinue.Types.Flags.SkipRulePushdown;
            }

            if (Config == null || !Config.stopOnDbPropagation)
            {
                flags |= (UInt32)DbgContinue.Types.Flags.SkipDbPropagation;
            }

            return flags;
        }

        private void SendContinue(DbgContinue.Types.Action action)
        {
            DbgCli.SendContinue(action, GetContinueBreakpointMask(), GetContinueFlags());
        }

        private void HandleContinueRequest(DAPRequest request, DAPContinueRequest msg, DbgContinue.Types.Action action)
        {
            if (msg.threadId != 1)
            {
                throw new RequestFailedException("Requested continue for unknown thread");
            }

            if (action == DbgContinue.Types.Action.Pause)
            {
                if (Stopped)
                {
                    throw new RequestFailedException("Already stopped");
                }

                PauseRequested = true;
            }
            else
            {
                if (!Stopped)
                {
                    throw new RequestFailedException("Already running");
                }

                Stopped = false;
            }
            
            SendContinue(action);

            var reply = new DAPContinueResponse
            {
                allThreadsContinued = false
            };
            Stream.SendReply(request, reply);
        }

        private void HandleEvaluateRequest(DAPRequest request, DAPEvaulateRequest req)
        {
            if (!Stopped)
            {
                throw new RequestFailedException("Can only evaluate expressions when stopped");
            }

            // TODO - this is bad for performance!
            var db = DebugInfo.Databases.Values.FirstOrDefault(r => r.Name == req.expression);
            if (db == null)
            {
                throw new RequestFailedException($"Database does not exist: \"{req.expression}\"");
            }

            DatabaseDumper.RequestDatabaseEvaluation(request, db.Id);
        }

        private void HandleDisconnectRequest(DAPRequest request, DAPDisconnectRequest msg)
        {
            var reply = new DAPEmptyPayload();
            Stream.SendReply(request, reply);
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

                case "evaluate":
                    HandleEvaluateRequest(request, request.arguments as DAPEvaulateRequest);
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
