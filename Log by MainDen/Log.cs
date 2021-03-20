using System;
using System.IO;

namespace MainDen.Modules
{
    public class Log
    {
        public class LogException : Exception
        {
            public LogException() : base() { }
            public LogException(string message) : base(message) { }
            public LogException(string message, Exception innerException) : base(message, innerException) { }
        }
        public class LogWriteException : LogException
        {
            public LogWriteException() : base() { }
            public LogWriteException(string message) : base(message) { }
            public LogWriteException(string message, Exception innerException) : base(message, innerException) { }
        }
        public class LogSettingsException : LogException
        {
            public LogSettingsException() : base() { }
            public LogSettingsException(string message) : base(message) { }
            public LogSettingsException(string message, Exception innerException) : base(message, innerException) { }
        }
        public Log() { }
        public enum Sender
        {
            Log = 0,
            User = 1,
            Error = 2,
            Debug = 3,
        }
        [Flags]
        private enum Output
        {
            None = 0,
            Custom = 1,
            Console = 2,
            File = 4,
        }
        private readonly object lSettings = new object();
        private string _FilePathFormat = ".\\log_{0:yyyy-MM-dd}.txt";
        public string FilePathFormat
        {
            get
            {
                lock (lSettings)
                    return _FilePathFormat;
            }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));
                try
                {
                    File.Create(GetFilePath(value, DateTime.Now)).Dispose();
                    lock (lSettings)
                        _FilePathFormat = value;
                }
                catch (Exception e) { throw new LogSettingsException("Unable to create log file.", e); }
            }
        }
        public string GetFilePath(DateTime fileDateTime)
        {
            lock (lSettings)
                return string.Format(_FilePathFormat, fileDateTime);
        }
        private string _MessageFormat = "({0} {1:yyyy-MM-dd HH:mm:ss}) {2}\n";
        public string MessageFormat
        {
            get
            {
                lock (lSettings)
                    return _MessageFormat;
            }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));
                try
                {
                    GetMessage(value, Sender.Log, DateTime.Now, "Message");
                    lock (lSettings)
                        _MessageFormat = value;
                } catch (Exception e) { throw new LogSettingsException("Invalid message format.", e); }
            }
        }
        private string _MessageDetailsFormat = "({0} {1:yyyy-MM-dd HH:mm:ss}) {2}\n(Details)\n{3}\n";
        public string MessageDetailsFormat
        {
            get
            {
                lock (lSettings)
                    return _MessageDetailsFormat;
            }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));
                try
                {
                    GetMessage(value, Sender.Log, DateTime.Now, "Message", "Details");
                    lock (lSettings)
                        _MessageDetailsFormat = value;
                } catch (Exception e) { throw new LogSettingsException("Invalid message + details format.", e); }
            }
        }
        public string GetMessage(Sender sender, DateTime dateTime, string message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));
            lock (lSettings)
                return string.Format(
                    _MessageFormat,
                    sender,
                    dateTime,
                    message);
        }
        public string GetMessage(Sender sender, DateTime dateTime, string message, string details)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));
            if (details is null)
                throw new ArgumentNullException(nameof(details));
            lock (lSettings)
                return string.Format(
                    _MessageDetailsFormat,
                    sender,
                    dateTime,
                    message,
                    details);
        }
        private bool _WriteToCustom = true;
        public bool WriteToCustom
        {
            get
            {
                lock (lSettings)
                    return _WriteToCustom;
            }
            set
            {
                lock (lSettings)
                    _WriteToCustom = value;
            }
        }
        private bool _WriteToConsole = true;
        public bool WriteToConsole
        {
            get
            {
                lock (lSettings)
                    return _WriteToConsole;
            }
            set
            {
                lock (lSettings)
                    _WriteToConsole = value;
            }
        }
        private bool _WriteToFile = true;
        public bool WriteToFile
        {
            get
            {
                lock (lSettings)
                    return _WriteToFile;
            }
            set
            {
                lock (lSettings)
                    _WriteToFile = value;
            }
        }
        private Action<string> _CustomWrite;
        public event Action<string> CustomWrite
        {
            add
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));
                lock (lSettings)
                    _CustomWrite += value;
            }
            remove
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));
                lock (lSettings)
                    _CustomWrite -= value;
            }
        }
        private Output output;
        private void WriteBase(string logMessage, string filePath)
        {
            if (logMessage is null)
                throw new ArgumentNullException(nameof(logMessage));
            if (filePath is null)
                throw new ArgumentNullException(nameof(filePath));
            lock (lSettings)
            {
                output = Output.None;
                if (WriteToCustom)
                    try
                    {
                        _CustomWrite?.Invoke(logMessage);
                    } catch { output |= Output.Custom; }
                if (WriteToConsole)
                    try
                    {
                        Console.Write(logMessage);
                    } catch { output |= Output.Console; }
                if (WriteToFile)
                    try
                    {
                        File.AppendAllText(filePath, logMessage);
                    } catch { output |= Output.File; }
                if (output != Output.None)
                    throw new LogWriteException($"Unable write to {output}.");
            }
        }
        public void WriteCustom(string logMessage)
        {
            if (logMessage is null)
                throw new ArgumentNullException(nameof(logMessage));
            lock (lSettings)
            {
                string filePath = GetFilePath(DateTime.Now);
                WriteBase(logMessage, filePath);
            }
        }
        public void Write(Sender sender, DateTime dateTime, string message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));
            lock (lSettings)
            {
                string logMessage = GetMessage(sender, dateTime, message);
                string filePath = GetFilePath(dateTime);
                WriteBase(logMessage, filePath);
            }
        }
        public void Write(Sender sender, DateTime dateTime, string message, string details)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));
            if (details is null)
                throw new ArgumentNullException(nameof(details));
            lock (lSettings)
            {
                string logMessage = GetMessage(sender, dateTime, message, details);
                string filePath = GetFilePath(dateTime);
                WriteBase(logMessage, filePath);
            }
        }
        public void Write(string message, Sender sender = Sender.Log)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));
            lock (lSettings)
            {
                DateTime dateTime = DateTime.Now;
                Write(sender, dateTime, message);
            }
        }
        public void Write(string message, string details, Sender sender = Sender.Log)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));
            if (details is null)
                throw new ArgumentNullException(nameof(details));
            lock (lSettings)
            {
                DateTime dateTime = DateTime.Now;
                Write(sender, dateTime, message, details);
            }
        }
        public static string GetFilePath(string filePathFormat, DateTime fileDateTime)
        {
            if (filePathFormat is null)
                throw new ArgumentNullException(nameof(filePathFormat));
            return string.Format(filePathFormat, fileDateTime);
        }
        public static string GetMessage(string messageFormat, Sender sender, DateTime dateTime, string message)
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
        public static string GetMessage(string messageDetailsFormat, Sender sender, DateTime dateTime, string message, string details)
        {
            if (messageDetailsFormat is null)
                throw new ArgumentNullException(nameof(messageDetailsFormat));
            if (message is null)
                throw new ArgumentNullException(nameof(message));
            if (details is null)
                throw new ArgumentNullException(nameof(details));
            return string.Format(
                messageDetailsFormat,
                sender,
                dateTime,
                message,
                details);
        }
        private static readonly Log _Default = new Log();
        public static Log Default
        {
            get => _Default;
        }
    }
}