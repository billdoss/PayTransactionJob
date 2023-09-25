using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessTransactionsPending
{
    class LogHandler
    {
        public static void WriteLog(string LogMessage, string directory = null, bool isAccessLog = false, string fileName = null)
        {

            try
            {

                string path = "/JOBS/ProcessTransactionsPending/ProcessTransactionsPending/logs/";

                if (!string.IsNullOrEmpty(fileName) && string.IsNullOrEmpty(directory))
                {
                    path = path + fileName + "_" + DateTime.Today.ToString("dd-MM-yy") + ".txt";
                }
                else if (!string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(directory))
                {
                    path = path + directory + "/" + fileName + "_" + DateTime.Today.ToString("dd-MM-yy") + ".txt";
                }
                else if (string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(directory))
                {
                    path = path + directory + "/" + DateTime.Today.ToString("dd-MM-yy") + ".txt";
                }
                else
                {
                    path = path + DateTime.Today.ToString("dd-MM-yy") + ".txt";
                }

                if (!File.Exists(path))
                {
                    File.Create(path).Close();
                }

                using (StreamWriter w = File.AppendText(path))
                {
                    if (isAccessLog)
                    {
                        w.WriteLine(LogMessage);
                    }
                    else
                    {
                        w.WriteLine("\r\n::::::::::::::::::::::::::::::::: LOG ENTRY :::::::::::::::::::::::::::::::::");
                        w.WriteLine("{0}", DateTime.Now.ToString("dd/MM/yyyy H:m:s"));
                        w.WriteLine("LOG DETAILS");
                        w.WriteLine("\t|==> LOG FROM : GTTransferJob");
                        w.WriteLine(LogMessage);
                    }
                    w.Flush();
                    w.Close();
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
            }
        }
    }
}
