using ProcessTransactionsPending.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace ProcessTransactionsPending
{
    class Program
    {
        static void Main(string[] args)
        {

            //   Timer timer1 = new Timer();
            int curMinute = DateTime.Now.Minute;
            int lastMinute = DateTime.Now.AddMinutes(-1).Minute;


            int timeIntervalInSecs = Convert.ToInt32(ConfigurationManager.AppSettings["timeIntervalInSecs"]);

            string myHost = Dns.GetHostName();
            string myHostIP = Dns.GetHostByName(myHost).AddressList[0].ToString().Trim();
            string authIP = ConfigurationManager.AppSettings["MachineIP"].Trim();
            if (myHostIP.CompareTo(authIP) == 0)
            {
                // check one instance is running
                if (Process.GetProcessesByName("ProcessTransactionsPending").Length > 1)
                {
                    Console.WriteLine("Multiple instances not allowed. \n Only an Instance is allowed to run");
                    Console.Read();
                    Stop();
                    //return;
                }
            }
            else
            {
                Console.WriteLine("Application not allowed to run on this IP. Ensure correct IP is set in the config");
                //return;
                Stop();
            }
            /*TransactionService transactionService = new TransactionService();
            List<bizaoBeneficiaryDTo> bizaoBeneficiaryDTos = new List<bizaoBeneficiaryDTo>();

            transactionService.getTransactionWithDifferentStatus("PENDING");

            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromMinutes(5);

            var timer = new System.Threading.Timer((e) =>
            {

                //transactionService.GetBizaoTransactions("ERROR OCCURED DURING GETING STATUS / NOT RESPONSE");
                bizaoBeneficiaryDTos = null;
                bizaoBeneficiaryDTos = transactionService.GetBizaoTransactions("ERROR OCCURED DURING GETING STATUS / NOT RESPONSE");

                foreach (var item in bizaoBeneficiaryDTos)
                {
                    string transactionStatus = transactionService.GetTransactionStatus(item.batchNumber);
                    if (transactionStatus == null)
                        transactionService.UpdateBizaoTransactionStatus(item, "ERROR OCCURED DURING GETING STATUS / NOT RESPONSE");
                    else if (transactionStatus.ToUpper() != "FAILED" && transactionStatus.ToUpper() != "SUCCESSFUL")
                        transactionService.UpdateBizaoTransactionStatus(item, "ERROR OCCURED DURING GETING STATUS / NOT RESPONSE");
                    else if (transactionStatus.ToUpper() == "FAILED")
                        transactionService.UpdateBizaoTransactionStatus(item, transactionStatus.ToUpper());
                    else if (transactionStatus.ToUpper() == "SUCCESSFUL")
                        transactionService.UpdateBizaoTransactionStatus(item, transactionStatus.ToUpper());

                    Console.WriteLine("status saved for : " + item.batchNumber + " !!");
                }

            }, null, startTimeSpan, periodTimeSpan);
            */





            /*while (true)
            {

                TransactionService transactionService = new TransactionService();
                List<bizaoBeneficiaryDTo> bizaoBeneficiaryDTos = new List<bizaoBeneficiaryDTo>();
                bizaoBeneficiaryDTos = transactionService.getBizaoTransactions("PENDING");
                //bizaoBeneficiaryDTos = transactionService.getBizaoTransactions("ERROR OCCURED DURING GETING STATUS / NOT RESPONSE");

                foreach (var item in bizaoBeneficiaryDTos)
                {
                    string transactionStatus = transactionService.GetTransactionStatus(item.batchNumber);
                    if (transactionStatus == null)
                        transactionService.UpdateBizaoTransactionStatus(item, "ERROR OCCURED DURING GETING STATUS / NOT RESPONSE");
                    else if (transactionStatus.ToUpper() != "FAILED" && transactionStatus.ToUpper() != "SUCCESSFUL")
                        transactionService.UpdateBizaoTransactionStatus(item, "ERROR OCCURED DURING GETING STATUS / NOT RESPONSE");
                    else if (transactionStatus.ToUpper() == "FAILED")
                        transactionService.UpdateBizaoTransactionStatus(item, transactionStatus.ToUpper());
                    else if (transactionStatus.ToUpper() == "SUCCESSFUL")
                        transactionService.UpdateBizaoTransactionStatus(item, transactionStatus.ToUpper());

                    Console.WriteLine("status saved for : " + item.batchNumber + " !!");
                }




                bizaoBeneficiaryDTos = transactionService.getBizaoTransactions("FAILED");

                foreach (var item in bizaoBeneficiaryDTos)
                {
                    string transactionStatus = transactionService.GetTransactionStatus(item.batchNumber);
                    
                    if (transactionStatus.ToUpper() == "FAILED")
                    {
                        transactionService.UpdateBizaoTransactionStatus(item, transactionStatus.ToUpper());
                        string resp = transactionService.PostToBasis1(
                                transactionService.oldAccountTocompteCaisse(item.AccountCredited),
                                transactionService.oldAccountTocompteCaisse(item.AccountDebited),
                                item.amount,
                                9077,
                                "EXTOURNE " + item.Remarks,
                                "32"
                            );
                    }
                    else if (transactionStatus.ToUpper() == "SUCCESSFUL")
                        transactionService.UpdateBizaoTransactionStatus(item, transactionStatus.ToUpper());


                    transactionService.UpdateBizaoTransactionAsTreated(item, "TREATED");
                    Console.WriteLine("status saved for : " + item.batchNumber + " !!");
                }
            }*/

            /*const string appName = "ProcessTransactionsPending";
            bool createdNew;

            Mutex mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                Console.WriteLine(appName + " is already running! Exiting the application.");
                Console.ReadKey();
                return;
            }*/

            TransactionService transactionService = new TransactionService();

            Thread prepareRefundingTask = new Thread(transactionService.prepareRefunding);

            Thread RecheckStatus = new Thread(transactionService.RecheckStatus);

            Thread refundFailedTransaction = new Thread(transactionService.refundFailedTransaction);

            //Thread creditForB2WOps = new Thread(transactionService.CreditForB2WOps);
            //Thread PayCashDeposit = new Thread(transactionService.PayCashDeposit);
            //Thread LoadAccount = new Thread(transactionService.LoadAccount);

            //Thread refundFailedTransactionTask = new Thread(transactionService.refundFailedTransaction);


            prepareRefundingTask.Start();
            RecheckStatus.Start();
            refundFailedTransaction.Start();

            //if (ConfigurationManager.AppSettings["activateCashDeposit"] == "1")
            //{
            //    PayCashDeposit.Start();
            //    while (DateTime.Now.ToString("HH:mm:ss tt") == "07:00:00 AM")
            //    {
            //        LoadAccount.Start();
            //    }
            //}
            //creditForB2WOps.Start();

            /*int W2BActivate = Convert.ToInt32(ConfigurationManager.AppSettings["activateW2B"]);
            if (W2BActivate == 1)
            {
                creditForB2WOps.Start();
            }*/
        }


        public static void Stop()
        {
            Console.WriteLine("Applet stopped at {0}", System.DateTime.Now.TimeOfDay);
            ErrHandler.LogError("Applet stopped !");

        }
    }
}
