using System;
using System.IO;
using System.Text;
using System.Threading;

namespace PA {
    public abstract class ALogger {
        private static IFormatProvider _formatProvider = Thread.CurrentThread.CurrentCulture;

        public abstract void Write(string txt);
        public abstract void WriteError(string txt);

        public void WriteLine(string txt) {
            Write(txt + "\n");
        }

        public void WriteLine(object obj) {
            WriteLine(obj.ToString());
        }

        public void WriteLine(string format, params object[] args) {
            Write(format + "\n", args);
        }

        public void Write(object obj) {
            Write(obj.ToString());
        }

        public void Write(string format, params object[] args) {
            Write(string.Format(_formatProvider, format, args));
        }
        
        public void WriteLineError(string txt) {
            WriteError(txt + "\n");
        }

        public void WriteLineError(object obj) {
            WriteLineError(obj.ToString());
        }

        public void WriteLineError(string format, params object[] args) {
            Write(format + "\n", args);
        }

        public void WriteError(object obj) {
            WriteError(obj.ToString());
        }

        public void WriteError(string format, params object[] args) {
            WriteError(string.Format(_formatProvider, format, args));
        }
    }

    public class FileLogger : ALogger {
        public string LogName;

        private object _syncObject;

        public FileLogger(string logName) {
            LogName = logName;
            _syncObject = new object();
        }

        public override void Write(string txt) {
            var now = DateTime.Now;
            lock (_syncObject) {
                using (StreamWriter stream = File.AppendText(LogName)) {
                    stream.Write("[" + now.ToShortDateString() + " " + now.ToLongTimeString() + "] : ");
                    stream.Write(txt);
                }
            }
        }

        public override void WriteError(string txt) {
            Write(txt);
        }
    }

    public class Logger {
        private static ALogger _logger;

        public static void SetLogger(ALogger logger) {
            _logger = logger;
        }

        public static void WriteLine(string txt) {
            _logger.WriteLine(txt);
        }

        public static void WriteLine(object obj) {
            _logger.WriteLine(obj);
        }

        public static void WriteLine(string format, params object[] args) {
            _logger.WriteLine(format, args);
        }
        
        public static void WriteLineError(string txt) {
            _logger.WriteLineError(txt);
        }

        public static void WriteLineError(object obj) {
            _logger.WriteLineError(obj);
        }

        public static void WriteLineError(string format, params object[] args) {
            _logger.WriteLineError(format, args);
        }

        public static void Write(string txt) {
            _logger.Write(txt);
        }
        
        public static void Write(object obj) {
            _logger.Write(obj);
        }

        public static void Write(string format, params object[] args) {
            _logger.Write(format, args);
        }

        public static void WriteError(string txt) {
            _logger.WriteError(txt);
        }
        
        public void WriteError(object obj) {
            _logger.WriteError(obj);
        }

        public void WriteError(string format, params object[] args) {
            _logger.WriteError(format, args);
        }
    }
}