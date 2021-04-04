using System;
using System.IO;

namespace MainDen.Modules.IO
{
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
        public enum Output
        {
            None = 0,
            Custom = 1,
            Console = 2,
            File = 4,
        }
        public class LogException : Exception
        {
            public LogException() : base() { }
            public LogException(string message) : base(message) { }
            public LogException(string message, Exception innerException) : base(message, innerException) { }
        }
        public class LogWriteException : LogException
        {
            public LogWriteException(string message) : base(message) { output = Output.None; }
            public LogWriteException(string message, Exception innerException) : base(message, innerException) { output = Output.None; }
            public LogWriteException(Output output, string message) : base(message) { this.output = output; }
            public LogWriteException(Output output, string message, Exception innerException) : base(message, innerException) { this.output = output; }
            public LogWriteException(Output output) : this(output, $"Unable write to {output}.") { }
            public LogWriteException(Output output, Exception innerException) : this(output, $"Unable write to {output}.", innerException) { }
            private readonly Output output;
            public Output Output { get => output; }
        }
        public class LogSettingsException : LogException
        {
            public LogSettingsException() : base() { }
            public LogSettingsException(string message) : base(message) { }
            public LogSettingsException(string message, Exception innerException) : base(message, innerException) { }
        }
        private readonly object lSettings = new object();
        private string _FilePathFormat = ".\\log\\log_{0:yyyy-MM-dd}.txt";
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
        private bool _AllowWriteNullMessages = false;
        public bool AllowWriteNullMessages
        {
            get
            {
                lock (lSettings)
                    return _AllowWriteNullMessages;
            }
            set
            {
                lock (lSettings)
                    _AllowWriteNullMessages = value;
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
                if (_AllowWriteNullMessages)
                    logMessage = "null";
                else
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
                    throw new LogWriteException(output);
            }
        }
        public void WriteCustom(string logMessage)
        {
            if (logMessage is null)
                if (_AllowWriteNullMessages)
                    logMessage = "null";
                else
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
                if (_AllowWriteNullMessages)
                    message = "null";
                else
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
                if (_AllowWriteNullMessages)
                    message = "null";
                else
                    throw new ArgumentNullException(nameof(message));
            if (details is null)
                if (_AllowWriteNullMessages)
                    details = "null";
                else
                    throw new ArgumentNullException(nameof(details));
            lock (lSettings)
            {
                string logMessage = GetMessage(sender, dateTime, message, details);
                string filePath = GetFilePath(dateTime);
                WriteBase(logMessage, filePath);
            }
        }
        public void Write(string message, Sender sender)
        {
            if (message is null)
                if (_AllowWriteNullMessages)
                    message = "null";
                else
                    throw new ArgumentNullException(nameof(message));
            lock (lSettings)
            {
                DateTime dateTime = DateTime.Now;
                Write(sender, dateTime, message);
            }
        }
        public void Write(string message)
        {
            Write(message, Sender.Log);
        }
        public void Write(string message, string details, Sender sender)
        {
            if (message is null)
                if (_AllowWriteNullMessages)
                    message = "null";
                else
                    throw new ArgumentNullException(nameof(message));
            if (details is null)
                if (_AllowWriteNullMessages)
                    details = "null";
                else
                    throw new ArgumentNullException(nameof(details));
            lock (lSettings)
            {
                DateTime dateTime = DateTime.Now;
                Write(sender, dateTime, message, details);
            }
        }
        public void Write(string message, string details)
        {
            Write(message, details, Sender.Log);
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
        private static readonly object lStaticSettings = new object();
        private static Log _Default;
        public static Log Default
        {
            get
            {
                lock (lStaticSettings)
                    return _Default ??= new Log();
            }
        }
    }
}