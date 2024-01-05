using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_Artist_V2
{
    public class Logger
    {
        public static void Log(string message)
        {
            FileStream fs = null;
            if (!File.Exists(DataCenter.LogFilePath))
            {
                using (fs = new FileStream(DataCenter.LogFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    sw.BaseStream.Seek(0, SeekOrigin.End);
                    string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{message}";
                    sw.WriteLine(logMessage);
                }
                DataCenter.SetAccessRights(DataCenter.LogFilePath);
            }
            else
            {
                using (fs = new FileStream(DataCenter.LogFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    sw.BaseStream.Seek(0, SeekOrigin.End);
                    string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{message}";
                    sw.WriteLine(logMessage);
                }
            }
        }

        public static void Log_CMD(string message)
        {
            FileStream fs = null;
            if (!File.Exists(DataCenter.LogCMDFilePath))
            {
                using (fs = new FileStream(DataCenter.LogCMDFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    sw.BaseStream.Seek(0, SeekOrigin.End);
                    string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{message}";
                    sw.WriteLine(logMessage);
                }
                DataCenter.SetAccessRights(DataCenter.LogCMDFilePath);
            }
            else
            {
                using (fs = new FileStream(DataCenter.LogCMDFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    sw.BaseStream.Seek(0, SeekOrigin.End);
                    string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{message}";
                    sw.WriteLine(logMessage);
                }
            }
        }
    }
}


