using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessTransactionsPending.Model
{
    class ErrHandler
    {

        public static void LogError(string errorMessage)
        {
            try
            {
                string path = "Error/" + DateTime.Today.ToString("dd-MM-yy") + ".txt";
                //Check for the file exists, or create a new file     
                if (!File.Exists(System.IO.Path.GetFullPath(path)))
                {
                    File.Create(System.IO.Path.GetFullPath(path)).Close();
                }
                using (StreamWriter w = File.AppendText(System.IO.Path.GetFullPath(path)))
                {        // using the stream writer class write       
                    // log message in a file.        
                    w.WriteLine("\r\nLog Entry : ");
                    w.WriteLine("{0}", DateTime.Now.ToString(CultureInfo.InvariantCulture));
                    string err = "Error Message:" + errorMessage;
                    w.WriteLine(err);
                    w.WriteLine("____________________________________________________________________");
                    w.Flush();
                    w.Close();
                }
            }
            catch (Exception ex)
            {
                //LogError(ex.StackTrace);
            }
        }
        public static void WriteError(string errorMessage)
        {
            try
            {
                string loc = Path.GetDirectoryName("~/Error/" + DateTime.Today.ToString("dd-MM-yy"));
                if (!Directory.Exists(loc))
                {
                    Directory.CreateDirectory(loc);

                }




                //   string path = loc + "/" + emailtype + ".txt";// StartUpPath + "/Logs/" + DateTime.Today.ToString("dd-MM-yy") + "-" + emailtype + ".txt";





                string path = loc + "/" + DateTime.Today.ToString("dd-MM-yy") + ".txt";
                if (!File.Exists(path))
                {
                    File.Create(path).Close();
                }
                using (StreamWriter w = File.AppendText(path))
                {
                    w.WriteLine("\r\nLog Entry : ");
                    w.WriteLine("{0}", DateTime.Now.ToString(CultureInfo.InvariantCulture));
                    //w.WriteLine(ui.customername); 
                    string err = "Error in: DEPOSIT ATM. Error Message:" + errorMessage;
                    w.WriteLine(err);
                    w.WriteLine("_______________________________________");
                    w.Flush();
                    w.Close();
                }
            }
            catch (System.Threading.ThreadAbortException ex) { }
            catch (Exception ex)
            {
                WriteError(ex.Message);
            }
        }

    }
}
