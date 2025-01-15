using MHServerEmu.Core.Helpers;

namespace MHServerEmu.Core.Logging.Targets
{
    /// <summary>
    /// Outputs <see cref="LogMessage"/> instances to a <see cref="FileStream"/>.
    /// </summary>
    public class FileTarget : LogTarget, IDisposable
    {
        private FileStream _fileStream;
        private StreamWriter _writer;

        /// <summary>
        /// Constructs a new <see cref="FileTarget"/> instance with the specified parameters and initializes a <see cref="FileStream"/> to output to.
        /// </summary>
        public FileTarget(LogTargetSettings settings, string fileName, bool reset = false) : base(settings)
        {
            string logDirectory = Path.Combine(FileHelper.ServerRoot, "Logs");
            if (Directory.Exists(logDirectory) == false)
                Directory.CreateDirectory(logDirectory);

            _fileStream = new(Path.Combine(logDirectory, fileName), reset ? FileMode.Create : FileMode.Append, FileAccess.Write, FileShare.Read);
            _writer = new(_fileStream) { AutoFlush = true };
        }

        /// <summary>
        /// Outputs a <see cref="LogMessage"/> instance to the <see cref="FileStream"/>.
        /// </summary>
        public override void ProcessLogMessage(in LogMessage message)
        {
            if (_disposed == false)
                _writer.WriteLine(message.ToString(IncludeTimestamps));
        }

        #region IDisposable Implementation

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _writer.Close();
                _writer.Dispose();
                _fileStream.Close();
                _fileStream.Dispose();
            }

            _writer = null;
            _fileStream = null;

            _disposed = true;
        }

        ~FileTarget() { Dispose(false); }

        #endregion
    }
}
