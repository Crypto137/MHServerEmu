using MHServerEmu.Common.Helpers;

namespace MHServerEmu.Common.Logging.Targets
{
    public class FileTarget : LogTarget, IDisposable
    {
        private readonly object _writeLock = new();

        private FileStream _fileStream;
        private StreamWriter _logStream;

        public FileTarget(bool includeTimestamps, Logger.Level minimumLevel, Logger.Level maximumLevel, string fileName, bool reset = false)
            : base(includeTimestamps, minimumLevel, maximumLevel)
        {         
            string logDirectory = Path.Combine(FileHelper.ServerRoot, "Logs");
            if (!Directory.Exists(logDirectory)) Directory.CreateDirectory(logDirectory);

            _fileStream = new(Path.Combine(logDirectory, fileName), reset ? FileMode.Create : FileMode.Append, FileAccess.Write, FileShare.Read);
            _logStream = new(_fileStream) { AutoFlush = true };
        }

        public override void LogMessage(Logger.Level level, string logger, string message)
        {
            lock (_writeLock)
            {
                string timestamp = IncludeTimestamps ? $"[{DateTime.Now:yyyy.MM.dd HH:mm:ss.fff}] " : "";
                if (_disposed == false) _logStream.WriteLine($"{timestamp}[{level,5}] [{logger}] {message}");
            }
        }

        public override void LogException(Logger.Level level, string logger, string message, Exception exception)
        {
            lock (_writeLock)
            {
                string timestamp = IncludeTimestamps ? $"[{DateTime.Now:yyyy.MM.dd HH:mm:ss.fff}] " : "";
                if (_disposed == false) _logStream.WriteLine($"{timestamp}[{level,5}] [{logger}] {message} - [Exception] {exception}");
            }
        }

        #region IDisposable Implementation

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);          // Take object out the finalization queue to prevent finalization code for it from executing a second time.
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)                      // Only dispose managed resources if we're called from directly or indirectly from user code.
            {
                _logStream.Close();
                _logStream.Dispose();
                _fileStream.Close();
                _fileStream.Dispose();
            }

            _logStream = null;
            _fileStream = null;

            _disposed = true;
        }

        ~FileTarget() { Dispose(false); }       // Finalizer called by the runtime. We should only dispose unmanaged objects and should NOT reference managed ones.

        #endregion
    }
}
