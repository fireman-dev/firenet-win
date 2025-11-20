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

        public event Action<string> OnLog;
        public event Action OnCrashed;

        public XrayProcessManager(string xrayDirectory)
        {
            _xrayPath = Path.Combine(xrayDirectory, "xray.exe");

            try
            {
                Directory.CreateDirectory("logs");
            }
            catch { }
        }

        public async Task StartAsync(string configPath)
        {
            try
            {
                File.AppendAllText("logs/xray.log",
                    $"[Start] {DateTime.Now} → Starting Xray\nPath: {_xrayPath}\nConfig: {configPath}\n\n");

                if (!File.Exists(_xrayPath))
                {
                    File.AppendAllText("logs/xray.log", "[ERROR] xray.exe not found!\n");
                    throw new FileNotFoundException("xray.exe not found", _xrayPath);
                }

                if (!File.Exists(configPath))
                {
                    File.AppendAllText("logs/xray.log", "[ERROR] config.json not found!\n");
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
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        File.AppendAllText("logs/xray.log", "[OUT] " + e.Data + "\n");
                        OnLog?.Invoke(e.Data);
                    }
                };

                _process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        File.AppendAllText("logs/xray.log", "[ERR] " + e.Data + "\n");
                        OnLog?.Invoke(e.Data);
                    }
                };

                _process.EnableRaisingEvents = true;
                _process.Exited += (s, e) =>
                {
                    File.AppendAllText("logs/xray.log",
                        $"[Crash] Xray exited unexpectedly at {DateTime.Now}\n");
                    OnCrashed?.Invoke();
                };

                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                File.AppendAllText("logs/xray.log",
                    $"[FATAL] Error starting Xray → {DateTime.Now}\n{ex}\n\n");
                throw;
            }
        }

        public void Stop()
        {
            try
            {
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill();
                    File.AppendAllText("logs/xray.log",
                        $"[Stop] Xray stopped at {DateTime.Now}\n\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("logs/xray.log",
                    $"[ERROR] Stop failed\n{ex}\n\n");
            }
        }
    }
}
