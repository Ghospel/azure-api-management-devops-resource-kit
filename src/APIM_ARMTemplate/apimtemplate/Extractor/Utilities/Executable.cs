using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extract;

internal class Executable
{
    private readonly string _arguments;
    private readonly string _exeName;
    private readonly bool _shareConsole;
    private readonly bool _streamOutput;
    private readonly bool _visibleProcess;
    private readonly string _workingDirectory;

    public Executable(string exeName, string arguments = null, bool streamOutput = true, bool shareConsole = false, bool visibleProcess = false, string workingDirectory = null)
    {
        _exeName = exeName;
        _arguments = arguments;
        _streamOutput = streamOutput;
        _shareConsole = shareConsole;
        _visibleProcess = visibleProcess;
        _workingDirectory = workingDirectory;
    }

    private Process Process { get; set; }

    public async Task<int> RunAsync(Action<string> outputCallback = null, Action<string> errorCallback = null, TimeSpan? timeout = null)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = _exeName,
            Arguments = _arguments,
            CreateNoWindow = !_visibleProcess,
            UseShellExecute = _shareConsole,
            RedirectStandardError = _streamOutput,
            RedirectStandardInput = _streamOutput,
            RedirectStandardOutput = _streamOutput,
            WorkingDirectory = _workingDirectory ?? Environment.CurrentDirectory
        };

        try
        {
            Process = Process.Start(processInfo);
        }
        catch (Win32Exception ex)
        {
            if (ex.Message == "The system cannot find the file specified")
            {
                throw new FileNotFoundException(ex.Message, ex);
            }
            throw ex;
        }

        if (_streamOutput)
        {
            Process.OutputDataReceived += (s, e) => outputCallback?.Invoke(e.Data);
            Process.BeginOutputReadLine();
            Process.ErrorDataReceived += (s, e) => errorCallback?.Invoke(e.Data);
            Process.BeginErrorReadLine();
            Process.EnableRaisingEvents = true;
        }
            
        var exitTask = Process.WaitForExitAsync();

        if (timeout == null)
        {
            await exitTask;
            return Process.ExitCode;
        }

        await Task.WhenAny(exitTask, Task.Delay(timeout.Value));

        if (exitTask.IsCompleted)
        {
            return Process.ExitCode;
        }

        Process.Kill();
        throw new Exception("Process didn't exit within specified timeout");
    }
}