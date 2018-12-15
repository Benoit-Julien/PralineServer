using System;
using System.IO;
using System.Text;
using System.Threading;

namespace PA {
    public class Logger {
        public static string LogName;

        private static IFormatProvider FormatProvider = Thread.CurrentThread.CurrentCulture;
        
        private static object _syncObject = new object();

        public static void WriteLine(string txt) {
            Write(txt + "\n");
        }

        public static void WriteLine(object obj) {
            WriteLine(obj.ToString());
        }

        public static void WriteLine(string format, params object[] args) {
            Write(format + "\n", args);
        }

        public static void Write(string txt) {
            var now = DateTime.Now;
            lock (_syncObject) {
                using (StreamWriter stream = File.AppendText(LogName)) {
                    stream.Write("[" + now.ToShortDateString() + " " + now.ToLongTimeString() + "] : ");
                    stream.Write(txt);
                }
            }
        }

        public static void Write(object obj) {
            Write(obj.ToString());
        }

        public static void Write(string format, params object[] args) {
            Write(string.Format(FormatProvider, format, args));
        }
    }
}