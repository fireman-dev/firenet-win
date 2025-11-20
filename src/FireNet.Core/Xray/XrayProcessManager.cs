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

        public XrayProcessManager()
        {
            // مسیر پوشه xray-core
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            _xrayPath = Path.Combine(appData, "FireNet", "xray-core", "xray.exe");

            if (!File.Exists(_xrayPath))
                throw new FileNotFoundException("xray.exe not found in FireNet/xray-core/");
        }

        // -------------------------------------------------------
        // اجرای Xray با config.json
        // -------------------------------------------------------
        public bool Start(string configFile)
        {
            if (!File.Exists(configFile))
                throw new FileNotFoundException("config.json not found: " + configFile);

            // اگر قبلاً اجراست
            Stop();

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _xrayPath,
                    Arguments = $"-config \"{configFile}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = Path.GetDirectoryName(_xrayPath)
                };

                _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

                _process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        OnLog?.Invoke(e.Data);
                };

                _process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        OnLog?.Invoke("[ERROR] " + e.Data);
                };

                _process.Exited += (s, e) =>
                {
                    if (_process.ExitCode != 0)
                        OnCrashed?.Invoke();
                };

                bool started = _process.Start();

                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                return started;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke("Failed to start Xray: " + ex.Message);
                return false;
            }
        }

        // -------------------------------------------------------
        // توقف پروسه Xray
        // -------------------------------------------------------
        public void Stop()
        {
            try
            {
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill(true);
                    _process.Dispose();
                }
            }
            catch { }

            _process = null;
        }

        // -------------------------------------------------------
        // چک کردن اینکه آیا Xray در حال اجراست؟
        // -------------------------------------------------------
        public bool IsRunning()
        {
            return _process != null && !_process.HasExited;
        }

        // -------------------------------------------------------
        // گرفتن PID
        // -------------------------------------------------------
        public int? GetPid()
        {
            if (_process != null && !_process.HasExited)
                return _process.Id;

            return null;
        }
    }
}
