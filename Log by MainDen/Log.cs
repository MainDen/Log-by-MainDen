using System;
using System.IO;

namespace MainDen.Modules
{
    public class Log
    {
        public Log()
        {
            FormatsCaching();
        }
        public enum Sender : int
        {
            Log = 0,
            User = 1,
            Error = 2,
        }
        private readonly object lSettings = new object();
        private string filePathFormat = @".\log_{0:yyyy-MM-dd}.txt";
        public string FilePathFormat
        {
            get
            {
                lock (lSettings)
                    return filePathFormat;
            }
            set
            {
                if (value is null)
                    return;
                try
                {
                    File.Create(GetFilePath(value, DateTime.Now)).Dispose();
                    lock (lSettings)
                        filePathFormat = value;
                }
                catch { }
            }
        }
        public string GetFilePath(DateTime fileDateTime)
        {
            lock (lSettings)
                return string.Format(filePathFormat, fileDateTime);
        }
        private string messageFormat = @"({0} {1:yyyy-MM-dd HH:mm:ss}) {2}\n";
        private string cachedMessageFormat;
        public string MessageFormat
        {
            get
            {
                lock (lSettings)
                    return messageFormat;
            }
            set
            {
                if (value is null)
                    return;
                try
                {
                    GetMessage(value, Sender.Log, DateTime.Now, "Message");
                    lock (lSettings)
                    {
                        messageFormat = value;
                        cachedMessageFormat = TextConverter.ToMultiLine(messageFormat);
                    }
                }
                catch { }
            }
        }
        private string messageDetailsFormat = @"({0} {1:yyyy-MM-dd HH:mm:ss}) {2}\n(Details)\n{3}\n";
        private string cachedMessageDetailsFormat;
        public string MessageDetailsFormat
        {
            get
            {
                lock (lSettings)
                    return messageDetailsFormat;
            }
            set
            {
                if (value is null)
                    return;
                try
                {
                    GetMessage(value, Sender.Log, DateTime.Now, "Message", "Details");
                    lock (lSettings)
                    {
                        messageDetailsFormat = value;
                        cachedMessageDetailsFormat = TextConverter.ToMultiLine(messageDetailsFormat);
                    }
                }
                catch { }
            }
        }
        private void FormatsCaching()
        {
            lock (lSettings)
            {
                cachedMessageFormat = TextConverter.ToMultiLine(messageFormat);
                cachedMessageDetailsFormat = TextConverter.ToMultiLine(messageDetailsFormat);
            }
        }
        public string GetMessage(Sender sender, DateTime dateTime, string message)
        {
            lock (lSettings)
                return string.Format(
                    cachedMessageFormat ?? "\nMESSAGE FORMAT EXCEPTION\n",
                    sender,
                    dateTime,
                    message ?? "NULL");
        }
        public string GetMessage(Sender sender, DateTime dateTime, string message, string details)
        {
            lock (lSettings)
                return string.Format(
                    (cachedMessageDetailsFormat ?? "\nMESSAGE DETAILS FORMAT EXCEPTION\n"),
                    sender,
                    dateTime,
                    message ?? "NULL",
                    details ?? "NULL");
        }
        private bool writeToCustom = true;
        public bool WriteToCustom
        {
            get
            {
                lock (lSettings)
                    return writeToCustom;
            }
            set
            {
                lock (lSettings)
                    writeToCustom = value;
            }
        }
        private bool writeToConsole = true;
        public bool WriteToConsole
        {
            get
            {
                lock (lSettings)
                    return writeToConsole;
            }
            set
            {
                lock (lSettings)
                    writeToConsole = value;
            }
        }
        private bool writeToFile = true;
        public bool WriteToFile
        {
            get
            {
                lock (lSettings)
                    return writeToFile;
            }
            set
            {
                lock (lSettings)
                    writeToFile = value;
            }
        }
        public event Action<string> CustomWrite;
        public void WriteCustom(string logMessage, string filePath)
        {
            lock (lSettings)
            {
                if (logMessage is null)
                    throw new ArgumentNullException(nameof(logMessage));
                if (filePath is null)
                    throw new ArgumentNullException(nameof(filePath));
                if (WriteToCustom)
                    CustomWrite?.Invoke(logMessage);
                if (WriteToConsole)
                    Console.Write(logMessage);
                if (WriteToFile)
                    File.AppendAllText(filePath, logMessage);
            }
        }
        public void Write(Sender sender, DateTime dateTime, string message)
        {
            lock (lSettings)
            {
                string logMessage = GetMessage(sender, dateTime, message);
                string filePath = GetFilePath(dateTime);
                WriteCustom(logMessage, filePath);
            }
        }
        public void Write(Sender sender, DateTime dateTime, string message, string details)
        {
            lock (lSettings)
            {
                string logMessage = GetMessage(sender, dateTime, message, details);
                string filePath = GetFilePath(dateTime);
                WriteCustom(logMessage, filePath);
            }
        }
        public void Write(string message, Sender sender = Sender.Log)
        {
            lock (lSettings)
            {
                DateTime dateTime = DateTime.Now;
                Write(sender, dateTime, message);
            }
        }
        public void Write(string message, string details, Sender sender = Sender.Log)
        {
            lock (lSettings)
            {
                DateTime dateTime = DateTime.Now;
                Write(sender, dateTime, message, details);
            }
        }
        public static string GetFilePath(string filePathFormat, DateTime fileDateTime)
        {
            return string.Format(filePathFormat, fileDateTime);
        }
        public static string GetMessage(string messageFormat, Sender sender, DateTime dateTime, string message)
        {
            return string.Format(
                TextConverter.ToMultiLine(messageFormat ?? "\nNULL MESSAGE FORMAT\n"),
                sender,
                dateTime,
                message ?? "NULL");
        }
        public static string GetMessage(string messageDetailsFormat, Sender sender, DateTime dateTime, string message, string details)
        {
            return string.Format(
                TextConverter.ToMultiLine(messageDetailsFormat ?? "\nNULL MESSAGE DETAILS FORMAT\n"),
                sender,
                dateTime,
                message ?? "NULL",
                details ?? "NULL");
        }
        private static readonly Log def = new Log();
        public static Log Def
        {
            get => def;
        }
    }
    internal static class TextConverter
    {
        public static string ToMultiLine(string singleLineText)
        {
            int len = singleLineText.Length;
            char[] result = singleLineText.ToCharArray();
            bool escaped = false;
            int escapedCount = 0;
            for (int i = 0; i < len; i++)
            {
                if (escaped)
                {
                    if (result[i] == 'n')
                        result[i - escapedCount] = '\n';
                    else if (result[i] == 'r')
                        result[i - escapedCount] = '\r';
                    else if (result[i] == '\\')
                        result[i - escapedCount] = '\\';
                    else
                        result[i - escapedCount] = result[i];
                }
                else if (result[i] == '\\')
                {
                    escaped = true;
                    escapedCount++;
                }
            }
            return new string(result, 0, len - escapedCount);
        }
    }
}