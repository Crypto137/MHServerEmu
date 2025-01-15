using MHServerEmu.Core.Helpers;

namespace MHServerEmu.Core.Logging.Targets
{
    /// <summary>
    /// Outputs <see cref="LogMessage"/> instances to a <see cref="FileStream"/>.
    /// </summary>
    public class FileTarget : LogTarget, IDisposable
    {
        private bool _splitOutput;
        private StreamWriter[] _writers;

        /// <summary>
        /// Constructs a new <see cref="FileTarget"/> instance with the specified parameters.
        /// </summary>
        public FileTarget(LogTargetSettings settings, string fileName, bool splitOutput, bool append = true) : base(settings)
        {
            _splitOutput = splitOutput;

            string logDirectory = Path.Combine(FileHelper.ServerRoot, "Logs");
            if (Directory.Exists(logDirectory) == false)
                Directory.CreateDirectory(logDirectory);

            FileMode fileMode = append ? FileMode.Append : FileMode.Create;

            if (splitOutput)
            {
                // Create separate writers for each category
                _writers = new StreamWriter[(int)LogCategory.NumCategories];
                for (LogCategory category = 0; category < LogCategory.NumCategories; category++)
                {
                    string filePath = Path.Combine(logDirectory, $"{fileName}_{category}.log");
                    FileStream fs = new(filePath, fileMode, FileAccess.Write, FileShare.Read);
                    _writers[(int)category] = new(fs) { AutoFlush = true };
                }
            }
            else
            {
                // Create a single writer for all categories
                string filePath = Path.Combine(logDirectory, $"{fileName}.log");
                FileStream fs = new(filePath, fileMode, FileAccess.Write, FileShare.Read);
                _writers = [new(fs) { AutoFlush = true }];
            }
        }

        /// <summary>
        /// Outputs a <see cref="LogMessage"/> instance to the <see cref="FileStream"/>.
        /// </summary>
        public override void ProcessLogMessage(in LogMessage message)
        {
            if (_disposed)
                return;

            int index = _splitOutput ? (int)message.Category : 0;
            _writers[index].WriteLine(message.ToString(IncludeTimestamps));
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
                foreach (StreamWriter writer in _writers)
                    writer.Close();

                _writers = Array.Empty<StreamWriter>();
            }

            _disposed = true;
        }

        ~FileTarget() { Dispose(false); }

        #endregion
    }
}
