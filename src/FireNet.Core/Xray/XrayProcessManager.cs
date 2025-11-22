using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FireNet.Core.Xray
{
    public class XrayProcessManager
    {
        private Process _process;
        private readonly string _xrayPath;
        private readonly string _xrayLogPath;

        public event Action<string> OnLog;
        public event Action OnCrashed;

        public bool IsRunning => _process != null && !_process.HasExited;

        public XrayProcessManager()
            : this(Path.Combine(AppContext.BaseDirectory, "xray-core"))
        {
        }

        public XrayProcessManager(string xrayDirectory)
        {
            _xrayPath = Path.Combine(xrayDirectory, "xray.exe");
            _xrayLogPath = Path.Combine(AppContext.BaseDirectory, "logs", "xray.log");

            try
            {
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "logs"));
            }
            catch { }
        }


        public Task StartAsync(string configPath)
        {
            try
            {
                if (!File.Exists(_xrayPath))
                {
                    WriteXrayLog("xray.exe not found");
                    throw new FileNotFoundException("xray.exe not found", _xrayPath);
                }

                if (!File.Exists(configPath))
                {
                    WriteXrayLog("config.json not found");
                    throw new FileNotFoundException("config.json not found", configPath);
                }

                _process = new Process();
                _process.StartInfo.FileName = _xrayPath;
                _process.StartInfo.Arguments = $"run -c \"{configPath}\"";
                _process.StartInfo.UseShellExecute = false;
                _process.StartInfo.RedirectStandardOutput = true;
                _process.StartInfo.RedirectStandardError = true;
                _process.StartInfo.CreateNoWindow = true;

                _process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        WriteXrayLog(e.Data);
                        OnLog?.Invoke(e.Data);
                    }
                };

                _process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        WriteXrayLog(e.Data);
                        OnLog?.Invoke(e.Data);
                    }
                };

                _process.EnableRaisingEvents = true;
                _process.Exited += (s, e) =>
                {
                    WriteXrayLog("Xray exited.");
                    OnCrashed?.Invoke();
                };

                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                WriteXrayLog($"Exception: {ex}");
                throw;
            }

            return Task.CompletedTask;
        }


        public void Start(string configPath)
        {
            _ = StartAsync(configPath);
        }


        public void Stop()
        {
            try
            {
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill();
                    WriteXrayLog("Xray stopped.");
                }
            }
            catch (Exception ex)
            {
                WriteXrayLog($"Stop Exception: {ex}");
            }
        }


        private void WriteXrayLog(string text)
        {
            try
            {
                if (File.Exists(_xrayLogPath))
                {
                    var lines = File.ReadAllLines(_xrayLogPath);

                    if (lines.Length >= 500)
                    {
                        int removeCount = lines.Length - 499;
                        var trimmed = new string[lines.Length - removeCount];
                        Array.Copy(lines, removeCount, trimmed, 0, trimmed.Length);
                        File.WriteAllLines(_xrayLogPath, trimmed);
                    }
                }

                File.AppendAllText(_xrayLogPath, text + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
