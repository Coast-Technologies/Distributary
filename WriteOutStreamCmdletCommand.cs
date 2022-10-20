#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Distributary
{
    public enum OutputStream
    {
        All = 0,
        Success = 1,
        Error = 2,
        Warning = 3,
        Verbose = 4,
        Debug = 5,
        Information = 6
    }

    [Cmdlet(VerbsCommunications.Write, "OutStream")]
    [OutputType(typeof(PSObject))]
    public class WriteOutStreamCmdletCommand : PSCmdlet
    {
        private OutputStream[] _outStream = { OutputStream.Success };
        private OutputStream highestLoglevel = OutputStream.Success;
        private SwitchParameter _throwOnError;

        private SwitchParameter _log;

        private string _logPath =
            $"{Environment.GetEnvironmentVariable("TEMP")}\\{{callstack_1}}_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.log";

        private SwitchParameter _append;

        private SwitchParameter _noTimeStamp;

        private SwitchParameter _force;

        private string? _callingCommand = "Unknown";

        private int? _highestLogLevel;

        private string? _timeStamp;

        private Dictionary<OutputStream, string> _messages = new();
        // value passed in from pipeline
        // The WriteOutStreamCmdletCommand class is a wrapper for the Write-Output cmdlet.
        // It is used to write objects to one or more outut streams.
        // The output streams are defined by the OutputStream enumeration.
        // The default output stream is All, which writes to all output streams.

        [Parameter(Position = 1, ValueFromPipeline = false)]
        [ValidateSet("All", "Success", "Error", "Warning", "Verbose", "Debug", "Information")]
        public OutputStream[] OutStream
        {
            get => _outStream;
            set => _outStream = value;
        }

        [Parameter(
            Position = 0,
            Mandatory = true,
            ValueFromPipeline = true
        )]
        // [ValidateNotNullOrEmpty]
        public string? InputObject { get; set; }

        [Parameter(DontShow = true)]
        private SwitchParameter ThrowOnError
        {
            get => _throwOnError;
            set => _throwOnError = value;
        }

        [Parameter(ParameterSetName = "Log")]
        private SwitchParameter Log
        {
            get => _log;
            set => _log = value;
        }

        [Parameter(ParameterSetName = "Log")]
        private string LogPath
        {
            get => _logPath;
            set => _logPath = value;
        }

        [Parameter(ParameterSetName = "Log")]
        private SwitchParameter Append
        {
            get => _append;
            set => _append = value;
        }

        [Parameter]
        [Alias("NoTime", "No")]
        private SwitchParameter NoTimeStamp
        {
            get => _noTimeStamp;
            set => _noTimeStamp = value;
        }

        [Parameter]
        public SwitchParameter Force
        {
            get => _force;
            set => _force = value;
        }


        private string? CallingCommand
        {
            get => _callingCommand;
            set => _callingCommand = value;
        }

        private int? HighestLogLevel
        {
            get => _highestLogLevel;
            set => _highestLogLevel = value;
        }

        private string? TimeStamp
        {
            get => _timeStamp;
            set => _timeStamp = value;
        }

        private Dictionary<OutputStream, string> Messages
        {
            get => _messages;
            set => _messages = value;
        }

        protected override void BeginProcessing()
        {
            ;
            // Validate LogPath //
            //? We're not validating in the parameter set because we can't guarantee that the force parameter was called *before* the logpath parameter ?//
            var boundParams = MyInvocation.BoundParameters;
            var exists = File.Exists(LogPath);
            var force = boundParams.ContainsKey("Force");
            if (exists && force)
            {
                File.Delete(LogPath);
            }
            else if (exists)
            {
                ThrowTerminatingError(new ErrorRecord(new Exception($"File exists: {LogPath}"), "FileExists",
                    ErrorCategory.InvalidArgument, LogPath));
            }

            // execute powershell command 
            var results = InvokeCommand.NewScriptBlock("Get-PSCallStack").Invoke();
            // get the current call stack
            var callStack = results.Select(r => r.BaseObject).Cast<CallStackFrame>().ToArray();
            // get the calling cmdlet
            var callingCmdlet = callStack.Last().InvocationInfo.InvocationName;
            CallingCommand = callingCmdlet;
            if (string.IsNullOrWhiteSpace(callingCmdlet)) CallingCommand = "Unknown";

            // Validate LogPath //
            if (MyInvocation.BoundParameters.ContainsKey("LogPath"))
            {
                LogPath = MyInvocation.BoundParameters["LogPath"].ToString();
            }
            else
            {
                LogPath = LogPath.Replace("{{callstack_1}}", CallingCommand);
            }

            HighestLogLevel = OutStream.Cast<int>().OrderByDescending(x => x).FirstOrDefault();

            if (Equals(OutputStream.Parse(typeof(OutputStream), HighestLogLevel.ToString(), true), OutputStream.All))
            {
                highestLoglevel = OutputStream.Information;
            }

            TimeStamp = NoTimeStamp ? null : $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] ";
            var preformMessage =
                $"{TimeStamp}{highestLoglevel.ToString()}:\t[{CallingCommand}]\t:\t{InputObject}";
            Host.UI.WriteDebugLine($"inputObject:{InputObject}");
            Messages = new Dictionary<OutputStream, string>
            {
                {
                    OutputStream.Success,
                    string.Format(preformMessage, "Success", CallingCommand)
                },
                {
                    OutputStream.Error,
                    string.Format(preformMessage, "Error", CallingCommand)
                },
                {
                    OutputStream.Warning,
                    string.Format(preformMessage, "Warning", CallingCommand)
                },
                {
                    OutputStream.Verbose,
                    string.Format(preformMessage, "Verbose", CallingCommand)
                },
                {
                    OutputStream.Debug,
                    string.Format(preformMessage, "Debug", CallingCommand)
                },
                {
                    OutputStream.Information,
                    string.Format(preformMessage, "Information", CallingCommand)
                }
            };
        }

        protected override void ProcessRecord()
        {
            if (OutStream.Any(x => x == OutputStream.Success))
            {
                WriteObject(Messages[OutputStream.Success]);
                Host.UI.WriteLine(Messages[OutputStream.Success]);
            }

            if (OutStream.Any(x => x == OutputStream.Error))
            {
                if (ThrowOnError)
                {
                    ThrowTerminatingError(new ErrorRecord(new Exception(Messages[OutputStream.Error]), "Error",
                        ErrorCategory.InvalidArgument, Messages[OutputStream.Error]));
                }
                else
                {
                    Host.UI.WriteErrorLine(Messages[OutputStream.Error]);
                    WriteError(new ErrorRecord(new Exception(Messages[OutputStream.Error]), "Error",
                        ErrorCategory.InvalidArgument, Messages[OutputStream.Error]));
                }
            }

            if (OutStream.Any(x => x == OutputStream.Warning))
            {
                Host.UI.WriteWarningLine(Messages[OutputStream.Warning]);
                WriteWarning(Messages[OutputStream.Warning]);
            }

            if (OutStream.Any(x => x == OutputStream.Verbose))
            {
                Host.UI.WriteVerboseLine(Messages[OutputStream.Verbose]);
                WriteVerbose(Messages[OutputStream.Verbose]);
            }

            if (OutStream.Any(x => x == OutputStream.Debug))
            {
                Host.UI.WriteDebugLine(Messages[OutputStream.Debug]);
                WriteDebug(Messages[OutputStream.Debug]);
            }

            if (OutStream.Any(x => x == OutputStream.Information))
            {
                Host.UI.WriteInformation(new InformationRecord(Messages[OutputStream.Information], "Information"));
                WriteInformation(Messages[OutputStream.Information], new[] { "Info" });
            }

            if (Log && HighestLogLevel != null)
            {
                var logMessage = Messages[(OutputStream)HighestLogLevel.Value];
                if (!Append)
                {
                    Append = false;
                }

                File.AppendAllText(LogPath, logMessage, Encoding.ASCII);
            }
        }

        protected override void EndProcessing()
        {
            if (Log && MyInvocation.BoundParameters.ContainsKey("Verbose")) WriteVerbose($"LogPath: {LogPath}");
        }
    }
}