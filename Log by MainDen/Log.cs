using System;
using System.IO;
using System.Linq;

namespace MainDen.Modules.IO
{
    public class LogException : Exception
    {
        public LogException() : base() { }
        public LogException(string message) : base(message) { }
        public LogException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class LogWriteException : LogException
    {
        public LogWriteException() : base() { output = Log.Outputs.None; }
        public LogWriteException(string message) : base(message) { output = Log.Outputs.None; }
        public LogWriteException(string message, Exception innerException) : base(message, innerException) { output = Log.Outputs.None; }
        public LogWriteException(Log.Outputs output, string message) : base(message) { this.output = output; }
        public LogWriteException(Log.Outputs output, string message, Exception innerException) : base(message, innerException) { this.output = output; }
        public LogWriteException(Log.Outputs output) : this(output, $"Unable write to {output}.") { }
        public LogWriteException(Log.Outputs output, Exception innerException) : this(output, $"Unable write to {output}.", innerException) { }
        private readonly Log.Outputs output;
        public Log.Outputs Output { get => output; }
    }

    public class LogSettingsException : LogException
    {
        public LogSettingsException() : base() { }
        public LogSettingsException(string message) : base(message) { }
        public LogSettingsException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class Log
    {
        public enum Sender
        {
            Log = 0,
            User = 1,
            Error = 2,
            Debug = 3,
        }

        [Flags]
        public enum Outputs
        {
            None = 0,
            Custom = 1,
            Console = 2,
            File = 4,
        }
        
        private readonly object _lSettings = new object();
        
        private string _FilePathFormat = ".\\log\\log_{0:yyyy-MM-dd}.txt";

        private string _MessageFormat = "({0} {1:yyyy-MM-dd HH:mm:ss}) {2}\n";
        
        private bool _WriteToCustom = true;
        
        private bool _WriteToConsole = true;
        
        private bool _WriteToFile = true;

        private bool _AllowWriteNullMessages = false;

        private bool _IgnoreWriteExceptions = false;

        private bool _AutoDisableWriteOutputs = true;

        private Action<string> _Custom;
        
        public string FilePathFormat
        {
            get
            {
                lock (_lSettings)
                    return _FilePathFormat;
            }
            set
            {
                if (value is null)
                    throw new ArgumentNullException();

                try
                {
                    var filePath = GetFilePath(value, DateTime.Now);

                    lock (_lSettings)
                        _FilePathFormat = value;
                }
                catch (Exception e)
                {
                    throw new LogSettingsException("Invalid file path format.", e);
                }
            }
        }

        public string MessageFormat
        {
            get
            {
                lock (_lSettings)
                    return _MessageFormat;
            }
            set
            {
                if (value is null)
                    throw new ArgumentNullException();

                try
                {
                    GetLogMessage(value, Sender.Log, DateTime.Now, "");

                    lock (_lSettings)
                        _MessageFormat = value;
                }
                catch (Exception e)
                {
                    throw new LogSettingsException("Invalid message format.", e);
                }
            }
        }

        public bool WriteToCustom
        {
            get
            {
                lock (_lSettings)
                    return _WriteToCustom;
            }
            set
            {
                lock (_lSettings)
                    _WriteToCustom = value;
            }
        }
        
        public bool WriteToConsole
        {
            get
            {
                lock (_lSettings)
                    return _WriteToConsole;
            }
            set
            {
                lock (_lSettings)
                    _WriteToConsole = value;
            }
        }
        
        public bool WriteToFile
        {
            get
            {
                lock (_lSettings)
                    return _WriteToFile;
            }
            set
            {
                lock (_lSettings)
                    _WriteToFile = value;
            }
        }
        
        public bool AllowWriteNullMessages
        {
            get
            {
                lock (_lSettings)
                    return _AllowWriteNullMessages;
            }
            set
            {
                lock (_lSettings)
                    _AllowWriteNullMessages = value;
            }
        }

        public bool IgnoreWriteExceptions
        {
            get
            {
                lock (_lSettings)
                    return _IgnoreWriteExceptions;
            }
            set
            {
                lock (_lSettings)
                    _IgnoreWriteExceptions = value;
            }
        }
        
        public bool AutoDisableWriteOutputs
        {
            get
            {
                lock (_lSettings)
                    return _AutoDisableWriteOutputs;
            }
            set
            {
                lock (_lSettings)
                    _AutoDisableWriteOutputs = value;
            }
        }

        public event Action<string> Custom
        {
            add
            {
                if (value is null)
                    throw new ArgumentNullException();

                lock (_lSettings)
                    _Custom += value;
            }
            remove
            {
                if (value is null)
                    throw new ArgumentNullException();

                lock (_lSettings)
                    _Custom -= value;
            }
        }

        public string GetFilePath(DateTime dateTime)
        {
            return GetFilePath(FilePathFormat, dateTime);
        }
        
        public string GetLogMessage(Sender sender, DateTime dateTime, string message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            return GetLogMessage(MessageFormat, sender, dateTime, message);
        }

        private void WriteBase(string logMessage, string filePath)
        {
            lock (_lSettings)
            {
                if (logMessage is null)
                    throw new ArgumentNullException(nameof(logMessage));
                if (filePath is null)
                    throw new ArgumentNullException(nameof(filePath));

                Outputs outputs = Outputs.None;

                if (_WriteToCustom)
                    try
                    {
                        _Custom?.Invoke(logMessage);
                    }
                    catch
                    {
                        outputs |= Outputs.Custom;
                    }

                if (_WriteToConsole)
                    try
                    {
                        Console.Write(logMessage);
                    }
                    catch
                    {
                        outputs |= Outputs.Console;
                    }

                if (_WriteToFile)
                    try
                    {
                        CreateLogDirectory(filePath);
                        File.AppendAllText(filePath, logMessage);
                    }
                    catch
                    {
                        outputs |= Outputs.File;
                    }

                if (!_IgnoreWriteExceptions && outputs != Outputs.None)
                {
                    if (_AutoDisableWriteOutputs)
                    {
                        if (outputs.HasFlag(Outputs.Custom))
                            _WriteToCustom = false;
                        if (outputs.HasFlag(Outputs.Console))
                            _WriteToConsole = false;
                        if (outputs.HasFlag(Outputs.File))
                            _WriteToFile = false;
                    }

                    throw new LogWriteException(outputs);
                }
            }
        }
        
        public void WriteCustomLogMessage(string customLogMessage)
        {
            lock (_lSettings)
            {
                DateTime dateTime = DateTime.Now;

                if (customLogMessage is null)
                    throw new ArgumentNullException(nameof(customLogMessage));

                string filePath = GetFilePath(dateTime);
                WriteBase(customLogMessage, filePath);
            }
        }

        public void Write(Sender sender, DateTime dateTime, string message)
        {
            lock (_lSettings)
            {
                if (message is null)
                    if (_AllowWriteNullMessages)
                        message = "null";
                    else
                        throw new ArgumentNullException(nameof(message));

                string logMessage = GetLogMessage(sender, dateTime, message);
                string filePath = GetFilePath(dateTime);
                WriteBase(logMessage, filePath);
            }
        }
        
        public void Write(string message, Sender sender = Sender.Log)
        {
            lock (_lSettings)
            {
                DateTime dateTime = DateTime.Now;

                if (message is null)
                    if (_AllowWriteNullMessages)
                        message = "null";
                    else
                        throw new ArgumentNullException(nameof(message));

                string logMessage = GetLogMessage(sender, dateTime, message);
                string filePath = GetFilePath(dateTime);
                WriteBase(logMessage, filePath);
            }
        }

        private static readonly object _lStaticSettings = new object();
        
        private static Log _Default;
        
        public static Log Default
        {
            get
            {
                lock (_lStaticSettings)
                    return _Default ?? (_Default = new Log());
            }
        }

        public static void CreateLogDirectory(string filePath)
        {
            if (filePath is null)
                throw new ArgumentNullException(nameof(filePath));

            if (File.Exists(filePath))
                return;

            var paths = filePath.Split('\\').SkipLast(1);
            var directory = string.Join('\\', paths);
            Directory.CreateDirectory(directory);
        }

        public static string GetFilePath(string filePathFormat, DateTime dateTime)
        {
            if (filePathFormat is null)
                throw new ArgumentNullException(nameof(filePathFormat));

            return string.Format(filePathFormat, dateTime);
        }
        
        public static string GetLogMessage(string messageFormat, Sender sender, DateTime dateTime, string message)
        {
            if (messageFormat is null)
                throw new ArgumentNullException(nameof(messageFormat));

            if (message is null)
                throw new ArgumentNullException(nameof(message));

            return string.Format(
                messageFormat,
                sender,
                dateTime,
                message);
        }
    }
}