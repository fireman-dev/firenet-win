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

        /// <summary>
        /// آیا پروسه‌ی Xray در حال اجراست؟
        /// </summary>
        public bool IsRunning => _process != null && !_process.HasExited;

        /// <summary>
        /// سازنده‌ی بدون پارامتر (استفاده‌شده در ViewModel ها)
        /// xray-core را کنار exe (پوشه‌ی برنامه) در نظر می‌گیرد.
        /// </summary>
        public XrayProcessManager()
            : this(Path.Combine(AppContext.BaseDirectory, "xray-core"))
        {
        }

        /// <summary>
        /// سازنده با مسیر دایرکتوری xray-core
        /// </summary>
        public XrayProcessManager(string xrayDirectory)
        {
            _xrayPath = Path.Combine(xrayDirectory, "xray.exe");

            try
            {
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "logs"));
            }
            catch
            {
                // ignore
            }
        }

        /// <summary>
        /// نسخه‌ی async برای استارت Xray با لاگ‌گیری
        /// </summary>
        public Task StartAsync(string configPath)
        {
            try
            {
                var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "xray.log");

                WriteXrayLog(output);

                if (!File.Exists(_xrayPath))
                {
                    WriteXrayLog(output);
                    throw new FileNotFoundException("xray.exe not found", _xrayPath);
                }

                if (!File.Exists(configPath))
                {
                    WriteXrayLog(output);
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
                        try
                        {
                            WriteXrayLog(output);
                        }
                        catch { }

                        OnLog?.Invoke(e.Data);
                    }
                };

                _process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        try
                        {
                            WriteXrayLog(output);
                        }
                        catch { }

                        OnLog?.Invoke(e.Data);
                    }
                };

                _process.EnableRaisingEvents = true;
                _process.Exited += (s, e) =>
                {
                    try
                    {
                        WriteXrayLog(output);
                    }
                    catch { }

                    OnCrashed?.Invoke();
                };

                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "xray.log");
                try
                {
                    WriteXrayLog(output);
                }
                catch { }

                throw;
            }

            // چون کار async واقعی نداریم، فقط CompletedTask برمی‌گردانیم
            return Task.CompletedTask;
        }

        /// <summary>
        /// متد هم‌نام قدیمی که ViewModel ها از آن استفاده می‌کنند.
        /// این متد فقط StartAsync را صدا می‌زند.
        /// </summary>
        public void Start(string configPath)
        {
            // fire-and-forget
            _ = StartAsync(configPath);
        }

        public void Stop()
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "xray.log");

            try
            {
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill();
                    WriteXrayLog(output);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    WriteXrayLog(output);
                }
                catch { }
            }
        }

        private void WriteXrayLog(string text)
        {
            try
            {
                // اگر فایل وجود دارد، ابتدا خطوط را محدود کن
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

                // لاگ جدید را اضافه کن
                File.AppendAllText(_xrayLogPath, text + Environment.NewLine);
            }
            catch
            {
                // خطای لاگ گیری نباید باعث کرش بشه
            }
        }
    }
}
