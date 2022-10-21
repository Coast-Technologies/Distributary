#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Text;
using static System.IO.File;

namespace Distributary {
	[Cmdlet(VerbsCommunications.Write, "OutStream", SupportsShouldProcess = true)]
	[OutputType(typeof(Object))]
	public class WriteOutStreamCommand : PSCmdlet {
		[Parameter(
			Mandatory = true,
			Position = 0,
			ValueFromPipeline = true,
			ValueFromPipelineByPropertyName = true
		)]
		public string InputObject {
			get => _inputObject;
			set => _inputObject = value;
		}


		[Parameter()]
		[ValidateSet("All", "Success", "Error", "Warning", "Verbose", "Debug", "Information")]
		public OutputStream[] OutStream {
			get => _outStream;
			set => _outStream = value;
		}

		[Parameter()]
		public SwitchParameter ThrowOnError {
			get => _throwOnError;
			set => _throwOnError = value;
		}

		[Parameter()]
		public SwitchParameter Log {
			get => _log;
			set => _log = value;
		}

		[Parameter()]
		public string LogPath {
			get => _logPath;
			set => _logPath = value;
		}

		[Parameter()]
		public SwitchParameter Append {
			get => _append;
			set => _append = value;
		}

		[Parameter()]
		[Alias("NoTime", "No")]
		public SwitchParameter NoTimeStamp {
			get => _noTimeStamp;
			set => _noTimeStamp = value;
		}

		[Parameter()]
		public SwitchParameter Force {
			get => _force;
			set => _force = value;
		}


		private string? CallingCommand {
			get => _callingCommand;
			set => _callingCommand = value;
		}

		private int? HighestLogLevel {
			get => _highestLogLevel;
			set => _highestLogLevel = value;
		}

		private string? TimeStamp {
			get => _timeStamp;
			set => _timeStamp = value;
		}

		private Dictionary<OutputStream, string> Messages {
			get => _messages;
			set => _messages = value;
		}

		private OutputStream[] _outStream = {OutputStream.Success};
		private OutputStream highestLoglevel = OutputStream.Success;
		private SwitchParameter _throwOnError;

		private SwitchParameter _log;

		private string _logPath =
			$"{Environment.GetEnvironmentVariable("TEMP")}\\{{callstack_1}}_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.log";

		private SwitchParameter _append;

		private SwitchParameter _noTimeStamp;

		private SwitchParameter _force;

		private string? _callingCommand = "Unknown";

		private int? _highestLogLevel;

		private string? _timeStamp;

		private Dictionary<OutputStream, string> _messages = new();
		private string? _inputObject;

		// end vars


		protected override void BeginProcessing() {
			HighestLogLevel = OutStream.Cast<int>().OrderByDescending(x => x).FirstOrDefault();

			if ( Equals(
				    Enum.Parse(
					    typeof(OutputStream),
					    HighestLogLevel.ToString(),
					    true
				    ),
				    OutputStream.All)
			   ) {
				highestLoglevel = OutputStream.Information;
			}

			TimeStamp = NoTimeStamp ? null : $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ";
		}

		protected override void ProcessRecord() {
			GetCallingCommand();
			ConfigureLogging();
			string preformMessage = $"{TimeStamp}{highestLoglevel}:\t[{CallingCommand}]\t:\t{InputObject}";
			Messages = new Dictionary<OutputStream, string> {
				{
					OutputStream.Success,
					string.Format(preformMessage, "Success", CallingCommand)
				}, {
					OutputStream.Error,
					string.Format(preformMessage, "Error", CallingCommand)
				}, {
					OutputStream.Warning,
					string.Format(preformMessage, "Warning", CallingCommand)
				}, {
					OutputStream.Verbose,
					string.Format(preformMessage, "Verbose", CallingCommand)
				}, {
					OutputStream.Debug,
					string.Format(preformMessage, "Debug", CallingCommand)
				}, {
					OutputStream.Information,
					string.Format(preformMessage, "Information", CallingCommand)
				}
			};
			if ( OutStream.Any(x => x == OutputStream.Success) ) {
				WriteObject(Messages[OutputStream.Success], true);
				// Host.UI.WriteLine(Messages[OutputStream.Success]);
			}

			if ( OutStream.Any(x => x == OutputStream.Error) ) {
				if ( ThrowOnError ) {
					ThrowTerminatingError(new ErrorRecord(new Exception(Messages[OutputStream.Error]), "Error",
						ErrorCategory.InvalidArgument, Messages[OutputStream.Error]));
				} else {
					// Host.UI.WriteErrorLine(Messages[OutputStream.Error]);
					WriteError(new ErrorRecord(new Exception(Messages[OutputStream.Error]), "Error",
						ErrorCategory.InvalidArgument, Messages[OutputStream.Error]));
				}
			}

			if ( OutStream.Any(x => x == OutputStream.Warning) ) {
				// Host.UI.WriteWarningLine(Messages[OutputStream.Warning]);
				WriteWarning(Messages[OutputStream.Warning]);
			}

			if ( OutStream.Any(x => x == OutputStream.Verbose) ) {
				// Host.UI.WriteVerboseLine(Messages[OutputStream.Verbose]);
				WriteVerbose(Messages[OutputStream.Verbose]);
			}

			if ( OutStream.Any(x => x == OutputStream.Debug) ) {
				// WriteDebug(Messages[OutputStream.Debug]);
				WriteDebug(Messages[OutputStream.Debug]);
			}

			if ( OutStream.Any(x => x == OutputStream.Information) ) {
				// Host.UI.WriteInformation(new InformationRecord(Messages[OutputStream.Information], "Information"));
				WriteInformation(Messages[OutputStream.Information], new[] {"Info"});
			}

			if ( Log == true && HighestLogLevel != null ) {
				var logMessage = Messages[(OutputStream) HighestLogLevel.Value];
				if ( !Append ) {
					Append = false;
				}

				try {
					AppendAllText(LogPath, logMessage);
				} catch (Exception e) {
					Console.WriteLine(e);
					throw;
				}
			}
		}

		protected override void EndProcessing() {
			if ( Log && MyInvocation.BoundParameters.ContainsKey("Verbose") ) WriteVerbose($"LogPath: {LogPath}");
		}

		private void GetCallingCommand() {
			// execute powershell command 
			var results = InvokeCommand.NewScriptBlock("Get-PSCallStack").Invoke();
			// get the current call stack
			var callStack = results.Select(r => r.BaseObject).Cast<CallStackFrame>().ToArray();
			// get the calling cmdlet
			var callingCmdlet = callStack.Last().InvocationInfo.InvocationName;
			CallingCommand = callingCmdlet;
			if ( string.IsNullOrWhiteSpace(callingCmdlet) ) CallingCommand = "Unknown";
		}

		private void ConfigureLogging() {
			// Validate LogPath //
			//? We're not validating in the parameter set because we can't guarantee that the force parameter was called *before* the logpath parameter ?//
			// bool overwrite = File.Exists(LogPath) && (true == Append || true == force);

			// Validate LogPath //
			if ( MyInvocation.BoundParameters.ContainsKey("LogPath") ) {
				LogPath = MyInvocation.BoundParameters["LogPath"].ToString();
			} else {
				LogPath = LogPath.Replace("{{callstack_1}}", CallingCommand);
			}
			
			if ( Exists(LogPath) &&  true != Append &&  true ==  Force ) {
				WriteAllText(LogPath, "");
			} else if ( Exists(LogPath) ) {
				// var e = ;
				var er = new ErrorRecord(new Exception($"File exists: {LogPath}"), "exists", ErrorCategory.ResourceExists, LogPath);
				WriteError(er);
				ThrowTerminatingError(new ErrorRecord(new Exception($"File exists: {LogPath}"), "FileExists",
					ErrorCategory.InvalidArgument, LogPath));
			}
		}
	}

	public enum OutputStream {
		All = 0,
		Success = 1,
		Error = 2,
		Warning = 3,
		Verbose = 4,
		Debug = 5,
		Information = 6
	}
}