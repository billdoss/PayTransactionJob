using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using ProcessTransactionsPending.Model;
using Newtonsoft.Json;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;

using Oracle.ManagedDataAccess.Client;
using OracleDataReader = Oracle.ManagedDataAccess.Client.OracleDataReader;
using OracleConnection = Oracle.ManagedDataAccess.Client.OracleConnection;
using OracleCommand = Oracle.ManagedDataAccess.Client.OracleCommand;
using System.IO;

namespace ProcessTransactionsPending
{
    class  TransactionService
    {

        public void prepareRefunding()
        {
            while (true)
            {
                List<bizaoBeneficiaryDTo> bizaoBeneficiaryDTos = new List<bizaoBeneficiaryDTo>();
                bizaoBeneficiaryDTos = getBizaoTransactions("PENDING");
                //bizaoBeneficiaryDTos = transactionService.getBizaoTransactions("ERROR OCCURED DURING GETING STATUS / NOT RESPONSE");

                foreach (var item in bizaoBeneficiaryDTos)
                {
                    if (!item.codeMerchant.Equals("mtn"))
                    {
                        string transactionStatus = GetTransactionStatus(item.batchNumber);
                        if (transactionStatus == null)
                            UpdateBizaoTransactionStatus(item, "ERROR OCCURED DURING GETING STATUS / NOT RESPONSE");
                        else if (transactionStatus.ToUpper() != "FAILED" && transactionStatus.ToUpper() != "SUCCESSFUL")
                            UpdateBizaoTransactionStatus(item, "ERROR OCCURED DURING GETING STATUS / NOT RESPONSE");
                        else if (transactionStatus.ToUpper() == "FAILED")
                            UpdateBizaoTransactionStatus(item, transactionStatus.ToUpper());
                        else if (transactionStatus.ToUpper() == "SUCCESSFUL")
                            UpdateBizaoTransactionStatus(item, transactionStatus.ToUpper());

                        //Console.WriteLine("status saved for : " + item.batchNumber + " !!");
                    }

                }
            }            
        }

        public void CreditForB2WOps()
        {
            while (true)
            {
                List<BizaoW2BDTO> bizaoBeneficiaryDTos = new List<BizaoW2BDTO>();
                bizaoBeneficiaryDTos = GetW2BPendingTrans();
                foreach (var item in bizaoBeneficiaryDTos)
                {
                    string getStatus = GetB2WTransactionStatus(item.OrderId, item.Operator);

                    if (getStatus == "Successful")
                    {
                        string result = PostToBasis1(oldAccountTocompteCaisse(item.AccountDebited), oldAccountTocompteCaisse(item.AccountCredited), item.Amount, 9077, $"WALLET TO BANK IN FAVOUR OF {item.BeneficiaryLastName} {item.BeneficiaryFirstName} INITIATED FROM {item.Msisdn}. REF: {item.OrderId}", "32");

                        string data = $"{oldAccountTocompteCaisse(item.AccountDebited)}, {oldAccountTocompteCaisse(item.AccountCredited)}, {item.Amount}, 9077, WALLET TO BANK IN FAVOUR OF {item.BeneficiaryLastName} {item.BeneficiaryFirstName} INITIATED FROM {item.Msisdn}. REF: {item.OrderId}, 32";
                        // add log
                        LogHandler.WriteLog("\t|==> POST TO BASIS \n\t|==> DATA: " + data + " \n\t|==> RESULT : " + result, "BizaoW2BLogs");
                        // if statement to check if posting is successful
                        if (result == "@ERR7@" || result == "@ERR19@")
                            UpdateW2BBizaoTransactionStatus(item.Id);
                    }
                }
            }
        }

        public void PayCashDeposit()
        {
            while (true)
            {
                //get pending files from path , the system generate a file per transaction
                string filesDirec = @"\\10.0.1.12\borne_de_remise"; //@"C:\CashDeposit\";
                string path2 = @"\\10.0.1.12\Treated\";
                int Expl_code = Convert.ToInt16(ConfigurationManager.AppSettings["cashDepositExplCode"]);
                string Req_code = ConfigurationManager.AppSettings["cashDepositReqCode"];
                string Remarks = ConfigurationManager.AppSettings["cashDepositRemarks"];
                string branch = string.Empty;
                string fromacc = ConfigurationManager.AppSettings["cashDepositTillAcc"];
                string tClearing = ConfigurationManager.AppSettings["atmClearingAccount"];
                string taxAcount = ConfigurationManager.AppSettings["atmDepositTaxAcc"];
                string suspenseAccountATM = ConfigurationManager.AppSettings["atmSuspenseAccount"];

                string f_acct = string.Empty;
                char[] delim = new char[] { '/' };
                string t_to = string.Empty;
                string ResultStr = string.Empty;

                var getbasisDetails = new basisDetails();

                try
                {
                    bool directoryIsAccessible = IsAccessible(filesDirec);
                    if (directoryIsAccessible)
                    {
                        //start here 
                        foreach (string fileName in Directory.GetFiles(filesDirec))
                        {
                            if (File.Exists(fileName))
                            {

                                string onlyFileName = null;
                                // using the method
                                onlyFileName = Path.GetFileName(fileName);

                                //move the file after treating
                                string MoveTo = path2 + onlyFileName;

                                if (!File.Exists(MoveTo))
                                {
                                    // Move the file.
                                    Console.WriteLine("{0} was moved to {1}.", fileName, MoveTo);
                                    ErrHandler.WriteError("{0} was moved to {1}." + fileName + " " + MoveTo);
                                }



                                //read the content of the file 

                                var lines = File.ReadLines(fileName);

                                double amtLine = Convert.ToDouble(0);
                                double subTotalAmt = Convert.ToDouble(0);
                                double sumTotalAmt = Convert.ToDouble(0);
                                var fileData = new filerecord();
                                int countTrans = 0;



                                foreach (string line in lines)
                                {
                                    string[] oneline = line.Split('|');
                                    fileData = new filerecord
                                    {

                                        Id = oneline[0].ToString().Trim(),
                                        transactionDate = oneline[1].ToString().Trim(),
                                        transactionTime = oneline[2].ToString().Trim(),
                                        accountNumber = oneline[3].ToString().Trim(),
                                        cusName = oneline[4].ToString().Trim(),
                                        numberofNotes = oneline[5].ToString().Trim(),
                                        amount = Convert.ToDouble(oneline[6].ToString().Trim()),
                                    };

                                    if (
                           string.IsNullOrEmpty(fileData.Id) ||
                           string.IsNullOrEmpty(fileData.transactionDate) ||
                           string.IsNullOrEmpty(fileData.transactionTime) ||
                           string.IsNullOrEmpty(fileData.accountNumber) ||
                           string.IsNullOrEmpty(fileData.cusName) ||
                           string.IsNullOrEmpty(fileData.numberofNotes) ||
                           fileData.amount <= 0
                                    )
                                    {
                                        ErrHandler.WriteError("there is an error in this file " + Convert.ToString(fileData.Id));
                                        continue;
                                    }

                                    //sum all the record to know how much to credit

                                    amtLine = Convert.ToInt16(fileData.numberofNotes) * fileData.amount;

                                    sumTotalAmt += amtLine;
                                    countTrans++;

                                }


                                DataTable transBatch = new DataTable();
                                //check if file exist in db

                                transBatch = CheckATMDepositTransaction(onlyFileName.Trim());

                                if (transBatch.Rows.Count > 0)
                                {
                                    ErrHandler.WriteError("Batch exist already " + Convert.ToString(onlyFileName));
                                    continue;
                                }
                                else
                                {
                                    // private void InsertATMDepositTransaction(string fileNumber, string AtmId, string fileDate, string fileTime,  string TotalAmount, string AccountNumber, string Notes, string Amount, string AccountName)
                                    InsertATMDepositbatch(onlyFileName, countTrans.ToString(), sumTotalAmt.ToString(), fileData.accountNumber, fileData.cusName);
                                    foreach (string line in lines)
                                    {
                                        var subAmt = Convert.ToInt16(fileData.numberofNotes) * fileData.amount;
                                        InsertATMDepositTransaction(onlyFileName, fileData.Id, fileData.transactionDate, fileData.transactionTime, Convert.ToString(subAmt.ToString()), fileData.accountNumber, fileData.numberofNotes, fileData.amount.ToString(), fileData.cusName);

                                    }
                                }
                                //validate account then credit a

                                string basisAccDetails = GetAccountDetails(fileData.accountNumber, fileData.cusName);
                                if (string.IsNullOrEmpty(basisAccDetails))
                                {
                                    UpdateAtmDepositTrans(onlyFileName, "FAILED");
                                    ErrHandler.WriteError("Invalid account from BASIS for " + fileData.accountNumber);
                                    continue;
                                }
                                else
                                {
                                    string[] basisDetails = basisAccDetails.Split('|');
                                    getbasisDetails = new basisDetails
                                    {


                                        braCode = basisDetails[1].ToString().Trim(),
                                        cusNum = basisDetails[2].ToString().Trim(),
                                        curCode = basisDetails[3].ToString().Trim(),
                                        ledCode = basisDetails[4].ToString().Trim(),
                                        subAcctCode = basisDetails[5].ToString().Trim(),
                                        cusName = basisDetails[0].ToString().Trim(),
                                        sta_code = Convert.ToInt16(basisDetails[7].ToString().Trim()),
                                        mapacctNumber = basisDetails[6].ToString().Trim(),

                                    };

                                }

                                if (getbasisDetails.sta_code != 1)
                                {
                                    UpdateAtmDepositTrans(onlyFileName, "FAILED");
                                    ErrHandler.WriteError("Account status is not active for  " + fileData.accountNumber + " status is " + getbasisDetails.sta_code.ToString());
                                    continue;
                                }

                                t_to = getbasisDetails.braCode.PadLeft(4, '0') + getbasisDetails.cusNum.PadLeft(7, '0') + getbasisDetails.curCode.PadLeft(3, '0') + getbasisDetails.ledCode.PadLeft(4, '0') + getbasisDetails.subAcctCode.PadLeft(3, '0');

                                if (fileData.Id == "001")
                                {
                                    branch = "PLATEAU";
                                }
                                else
                                {
                                    branch = "PLATEAU";
                                }

                                /*
                                 * 
            202/197/1/101/0 - till
            201/0/1/4096/0 - C Clearing D 201/0/1/4095/0 (Suspense) D
            202/0/1/4545/0 - C tax 100
            Customer

                                string fromacc = ConfigurationManager.AppSettings["cashDepositTillAcc"]; 
                    string tClearing = ConfigurationManager.AppSettings["atmClearingAccount"];
                    string taxAcount = ConfigurationManager.AppSettings["atmDepositTaxAcc"];
                    string suspenseAccountATM = ConfigurationManager.AppSettings["atmSuspenseAccount"];
                                 */

                                string Resultcustomer = string.Empty;
                                string[] tempstr = fromacc.Split(delim);
                                f_acct = tempstr[0].PadLeft(4, '0') + tempstr[1].PadLeft(7, '0') + tempstr[2].PadLeft(3, '0') + tempstr[3].PadLeft(4, '0') + tempstr[4].PadLeft(3, '0');

                                //Clearing accounting
                                string[] temClearingA = tClearing.Split(delim);
                                string clearingA = temClearingA[0].PadLeft(4, '0') + temClearingA[1].PadLeft(7, '0') + temClearingA[2].PadLeft(3, '0') + temClearingA[3].PadLeft(4, '0') + temClearingA[4].PadLeft(3, '0');

                                //Suspense Account
                                string[] temsuspenseA = suspenseAccountATM.Split(delim);
                                string suspenseA = temsuspenseA[0].PadLeft(4, '0') + temsuspenseA[1].PadLeft(7, '0') + temsuspenseA[2].PadLeft(3, '0') + temsuspenseA[3].PadLeft(4, '0') + temsuspenseA[4].PadLeft(3, '0');

                                //Tax Account
                                string[] temtaxA = taxAcount.Split(delim);
                                string taxA = temtaxA[0].PadLeft(4, '0') + temtaxA[1].PadLeft(7, '0') + temtaxA[2].PadLeft(3, '0') + temtaxA[3].PadLeft(4, '0') + temtaxA[4].PadLeft(3, '0');

                                string Remarks1 = Remarks + fileData.cusName + " at " + branch;
                                ErrHandler.WriteError("Posting data: " + f_acct + "|" + t_to + "|" + sumTotalAmt + "|" + Expl_code.ToString() + "|" + Remarks1 + "|" + Req_code);
                                string taxFee = "100";
                                double creditAmt = sumTotalAmt - Convert.ToDouble(taxFee);
                                //Debit the till first and credit the clearing ledger and tax
                                ResultStr = PostToBasis1(f_acct, clearingA, creditAmt, Expl_code, Remarks1, Req_code);
                                ErrHandler.WriteError("Posting Response from till to Suspense: " + ResultStr + ",  Remarks : " + Remarks1);
                                if (ResultStr.CompareTo("@ERR7@") == 0 || ResultStr.CompareTo("@ERR19@") == 0)
                                {
                                    //credit tax
                                    string ResultTax = PostToBasis1(f_acct, taxA, Convert.ToDouble(taxFee), Expl_code, Remarks1, Req_code);
                                    ErrHandler.WriteError("Posting Response from till to Tax: " + ResultStr + ",  Remarks : " + Remarks1);
                                    if (ResultTax.CompareTo("@ERR7@") == 0 || ResultTax.CompareTo("@ERR19@") == 0)
                                    {
                                        //debit clearingA credit suspense
                                        string Resultsuspense = PostToBasis1(clearingA, suspenseA, creditAmt, Expl_code, Remarks1, Req_code);
                                        ErrHandler.WriteError("Posting Response from clearing to suspense: " + ResultStr + ",  Remarks : " + Remarks1);

                                        if (Resultsuspense.CompareTo("@ERR7@") == 0 || Resultsuspense.CompareTo("@ERR19@") == 0)
                                        {
                                            //debit suspense credit customer
                                            Resultcustomer = PostToBasis1(suspenseA, t_to, creditAmt, Expl_code, Remarks1, Req_code);
                                            ErrHandler.WriteError("Posting Response from suspense to customer: " + ResultStr + ",  Remarks : " + Remarks1);
                                            if (Resultcustomer.CompareTo("@ERR7@") == 0 || Resultcustomer.CompareTo("@ERR19@") == 0)
                                            {
                                                UpdateAtmDepositTrans(onlyFileName, "SUCCESS");
                                                ErrHandler.WriteError("Posting Response: " + Resultcustomer + ",  Remarks : " + Remarks1);

                                            }
                                            else
                                            {
                                                UpdateAtmDepositTrans(onlyFileName, "FAILED");
                                                ErrHandler.WriteError("Posting Response: " + Resultcustomer + ",  Remarks : " + Remarks1);
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            UpdateAtmDepositTrans(onlyFileName, "FAILED");
                                            ErrHandler.WriteError("Posting Response: " + Resultsuspense + ",  Remarks : " + Remarks1);
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        UpdateAtmDepositTrans(onlyFileName, "FAILED");
                                        ErrHandler.WriteError("Posting Response: " + ResultTax + ",  Remarks : " + Remarks1);
                                        continue;
                                    }

                                }
                                else
                                {
                                    UpdateAtmDepositTrans(onlyFileName, "FAILED");
                                    ErrHandler.WriteError("Posting Response: " + ResultStr + ",  Remarks : " + Remarks1);
                                    continue;
                                }




                                //update the the table 
                                File.Move(fileName, MoveTo);


                                //private void UpdateAtmDepositTrans(string fileNumber, string status)
                            }
                            else
                            {
                                Console.WriteLine("No record to treat ");
                            }

                        }

                        //end here
                    }
                    else
                    {
                        Console.WriteLine("The server is down. Kindly restart");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("The process failed: {0}", e.ToString());
                }
            }
        }

        public bool IsAccessible(string path)
        {
            //get directory info
            DirectoryInfo realpath = new DirectoryInfo(path);
            try
            {
                //if GetDirectories works then is accessible
                realpath.GetDirectories();
                return true;
            }
            catch (Exception)
            {
                //if exception is not accesible
                return false;
            }
        }

        private DataTable CheckATMDepositTransaction(string fileNumber)
        {
            var ConnectionSQLEone = ConfigurationManager.AppSettings["ConStringEone"];
            DataSet ds = new DataSet();
            using (SqlConnection sqlconn = new SqlConnection(ConnectionSQLEone))
            {
                SqlCommand comm = new SqlCommand("CheckATMDeposit_Batch", sqlconn);
                comm.Parameters.AddWithValue("@fileNumber", fileNumber);
                comm.CommandType = CommandType.StoredProcedure;

                SqlDataAdapter adapter = new SqlDataAdapter(comm);
                try
                {
                    //open connection
                    if (sqlconn.State == ConnectionState.Closed)
                    {
                        sqlconn.Open();
                    }
                    adapter.Fill(ds);
                }
                catch (Exception ex)
                {
                    ErrHandler.WriteError(ex.Message);
                }
                finally
                {
                    sqlconn.Close();
                    adapter.Dispose();
                }
            }
            return ds.Tables[0];
        }

        private void InsertATMDepositTransaction(string fileNumber, string AtmId, string fileDate, string fileTime, string TotalAmount, string AccountNumber, string Notes, string Amount, string AccountName)
        {

            var ConnectionSQLEone = ConfigurationManager.AppSettings["ConStringEone"];
            SqlConnection sqlconn = new SqlConnection(ConnectionSQLEone);
            SqlCommand sqlcomm = new SqlCommand("InsertIntoATMDeposit_Transaction", sqlconn);
            sqlcomm.CommandType = CommandType.StoredProcedure;
            sqlcomm.Parameters.AddWithValue("@fileNumber", fileNumber);
            sqlcomm.Parameters.AddWithValue("@AtmId", AtmId);
            sqlcomm.Parameters.AddWithValue("@@fileDate", fileDate);
            sqlcomm.Parameters.AddWithValue("@fileTime", fileTime);
            sqlcomm.Parameters.AddWithValue("@AccountName", AccountName);
            sqlcomm.Parameters.AddWithValue("@AccountNumber", AccountNumber);
            sqlcomm.Parameters.AddWithValue("@Notes", Notes);
            sqlcomm.Parameters.AddWithValue("@Amount", Amount);
            sqlcomm.Parameters.AddWithValue("@TotalAmount", TotalAmount);


            try
            {
                if (sqlconn.State == ConnectionState.Closed)
                {
                    sqlconn.Open();
                }
                sqlcomm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                ErrHandler.WriteError(ex.Message);
            }
            finally
            {
                sqlconn.Close();
            }
        }

        private void InsertATMDepositbatch(string fileNumber, string TransCount, string TotalAmount, string AccountNumber, string AccountName)
        {
            var ConnectionSQLEone = ConfigurationManager.AppSettings["ConStringEone"];
            SqlConnection sqlconn = new SqlConnection(ConnectionSQLEone);
            SqlCommand sqlcomm = new SqlCommand("InsertIntoATMDeposit_Batch", sqlconn);
            sqlcomm.CommandType = CommandType.StoredProcedure;
            sqlcomm.Parameters.AddWithValue("@fileNumber", fileNumber);
            sqlcomm.Parameters.AddWithValue("@TransCount", TransCount);
            sqlcomm.Parameters.AddWithValue("@TotalAmount", TotalAmount);
            sqlcomm.Parameters.AddWithValue("@AccountNumber", AccountNumber);
            sqlcomm.Parameters.AddWithValue("@AccountName", AccountName);
            try
            {
                if (sqlconn.State == ConnectionState.Closed)
                {
                    sqlconn.Open();
                }
                sqlcomm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                ErrHandler.WriteError(ex.Message);
            }
            finally
            {
                sqlconn.Close();
            }
        }

        private void UpdateAtmDepositTrans(string fileNumber, string status)
        {
            var ConnectionSQLEone = ConfigurationManager.AppSettings["ConStringEone"];
            SqlConnection sqlconn = new SqlConnection(ConnectionSQLEone);
            SqlCommand sqlcomm = new SqlCommand("UpdateATMDeposit_Batch", sqlconn);
            sqlcomm.CommandType = CommandType.StoredProcedure;
            sqlcomm.Parameters.AddWithValue("@fileNumber", fileNumber);
            sqlcomm.Parameters.AddWithValue("@Status", status);

            try
            {
                if (sqlconn.State == ConnectionState.Closed)
                {
                    sqlconn.Open();
                }
                sqlcomm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                ErrHandler.WriteError(ex.Message);
            }
            finally
            {
                sqlconn.Close();
            }
        }

        public string GetAccountDetails(string accountNumber, string cusName)
        {
            OracleCommand oracmd = null;
            //   OracleConnection oraconn;
            DataSet dset;
            OracleDataAdapter oradpt;
            OracleDataReader orardr;
            string bra_code = "";// uid.ToString().Substring(0, 3);
            string cus_num = "";// uid.ToString().Substring(3, 6);
            string customername = string.Empty;

            string cusemail = null;
            string query = null;
            //OracleConnection oraconn = new OracleConnection(GTBEncryptLibrary.GTBEncryptLib.DecryptText(ConfigurationManager.AppSettings["BASISConString_eone"]));
            OracleConnection oraconn = new OracleConnection(ConfigurationManager.AppSettings["ConString"]);
            if (oraconn.State != ConnectionState.Open)
            {
                oraconn.Open();
            }

            query = "select a.bra_code, a.cus_num, a.cur_code, a.led_code, a.sub_acct_code, b.map_acc_no, a.sta_code, get_name1(a.bra_code, a.cus_num, a.cur_code, a.led_code, a.sub_acct_code) as cusname " +
                "from account a, map_acct b where b.map_acc_no like '%" + accountNumber + "%' and a.bra_code = b.bra_code and a.cus_num = b.cus_num " +
                "and a.cur_code = 1 and a.led_code = b.led_code and a.sub_acct_code = b.sub_acct_code and get_name1(a.bra_code, a.cus_num, a.cur_code, a.led_code, a.sub_acct_code) like '%"+ cusName + "%'";
            oracmd = new OracleCommand(query, oraconn);
            oracmd.CommandType = CommandType.Text;
            oracmd.Connection = oraconn;
            orardr = oracmd.ExecuteReader();
            if (orardr.HasRows)
            {
                orardr.Read();

                customername = orardr["cusname"].ToString() + "|" + orardr["bra_code"].ToString() + "|" + orardr["cus_num"].ToString() + "|" + orardr["cur_code"].ToString() + "|" + orardr["led_code"].ToString() + "|" + orardr["sub_acct_code"].ToString() + "|" + orardr["map_acc_no"].ToString() + "|" + orardr["sta_code"].ToString();

                orardr.Close();
                return customername;



            }

            /*{
                //  ErrHandler.WriteError(customername + (" : " + (cusemail + "\\n Login notification email was not sent. ")));
            }*/
            oraconn.Close();
            return customername;
        }

        public class filerecord
        {
            public string Id { get; set; }
            public string transactionDate { get; set; }
            public string transactionTime { get; set; }
            public string accountNumber { get; set; }
            public string cusName { get; set; }

            public string numberofNotes { get; set; }
            public double amount { get; set; }


        }

        public class basisDetails
        {
            public string braCode { get; set; }
            public string cusNum { get; set; }
            public string curCode { get; set; }
            public string ledCode { get; set; }
            public string subAcctCode { get; set; }

            public string mapacctNumber { get; set; }
            public int sta_code { get; set; }
            public string cusName { get; set; }


        }

        public void LoadAccount()
        {
            List<CustomerInfos> customers = GetCustomerInfos();
            string filePath = @"\\10.0.1.12\import\COMPTES.txt";
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            TextWriter tw = new StreamWriter(filePath);
            foreach (var customer in customers)
            {
                tw.WriteLine($"{customer.AccountNumber};{customer.Name}");
            }

            tw.Close();
        }

        public List<CustomerInfos> GetCustomerInfos()
        {
            List<CustomerInfos> customers = new List<CustomerInfos>();
            OracleConnection oraconn = new OracleConnection(ConfigurationManager.AppSettings["ConString"]);
            if (oraconn.State != ConnectionState.Open)
            {
                oraconn.Open();
            }

            string query = "select distinct substr(a.map_acc_no, 11, 12) account_num, get_name1(a.bra_code, a.cus_num, a.cur_code, a.led_code, a.sub_acct_code) cus_sho_name from map_acct a, account b where" +
                " a.bra_code = b.bra_code and a.cus_num = b.cus_num and a.cur_code = b.cur_code and a.led_code = b.led_code and a.sub_acct_code = b.sub_acct_code" +
                " and a.led_code not in (10, 20)" +
                " and b.sta_code = 1";

            OracleCommand oracmd = new OracleCommand(query, oraconn);
            oracmd.CommandType = CommandType.Text;
            oracmd.Connection = oraconn;
            OracleDataReader oradr = oracmd.ExecuteReader();

            if (oradr.HasRows)
            {
                while (oradr.Read())
                {
                    CustomerInfos customer = new CustomerInfos
                    {
                        Name = RemoveSpecialCharacters(oradr["cus_sho_name"].ToString()),
                        AccountNumber = oradr["account_num"].ToString()
                    };

                    customers.Add(customer);
                }
            }

            oradr.Close();
            oraconn.Close();

            query = "select a.cus_sho_name, substr(b.map_acc_no, 11, 12) account_num from sec_add a, map_acct b where a.bra_code = b.bra_code and a.cus_num = b.cus_num";

            if (oraconn.State != ConnectionState.Open)
            {
                oraconn.Open();
            }

            oracmd = new OracleCommand(query, oraconn);
            oracmd.CommandType = CommandType.Text;
            oracmd.Connection = oraconn;
            oradr = oracmd.ExecuteReader();

            if (oradr.HasRows)
            {
                while (oradr.Read())
                {
                    CustomerInfos customer = new CustomerInfos
                    {
                        Name = oradr["cus_sho_name"].ToString(),
                        AccountNumber = oradr["account_num"].ToString()
                    };

                    customers.Add(customer);
                }
            }

            oradr.Close();
            oraconn.Close();

            return customers;
        }

        public string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_' || c == ' ' || c == '-' || c == '/' || c == '\'' || c == '(' || c == ')' || c == '&')
                {
                    if(c == '\'')
                    {
                        sb.Append('\'');
                    }
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public class CustomerInfos
        {
            public string Name { get; set; }
            public string AccountNumber { get; set; }
        }

        public List<BizaoW2BDTO> GetW2BPendingTrans()
        {
            List<BizaoW2BDTO> bizaoBeneficiaryDTos = new List<BizaoW2BDTO>();

            SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["bizaoTransactionsConString"]);

            SqlCommand comm;

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("getBizaoB2WTransactions", conn);

            comm.CommandType = CommandType.StoredProcedure;
            SqlDataReader reader;

            reader = comm.ExecuteReader();
            if (reader.HasRows == true)
            {
                while (reader.Read())
                {
                    BizaoW2BDTO bizaoW2BDTO = new BizaoW2BDTO();

                    bizaoW2BDTO.Id = Convert.ToInt32(reader["id"].ToString().Trim());
                    bizaoW2BDTO.BeneficiaryFirstName = reader["beneficiaryFirstName"].ToString().Trim();
                    bizaoW2BDTO.BeneficiaryLastName = reader["beneficiaryLastName"].ToString().Trim();
                    bizaoW2BDTO.Msisdn = reader["beneficiaryMobileNumber"].ToString().Trim();
                    bizaoW2BDTO.AccountCredited = reader["AccountCredited"].ToString().Trim();
                    bizaoW2BDTO.AccountDebited = reader["AccountDebited"].ToString().Trim();
                    bizaoW2BDTO.Amount = Convert.ToDouble(reader["amount"].ToString().Trim());
                    bizaoW2BDTO.Reference = reader["reference"].ToString().Trim();
                    bizaoW2BDTO.OrderId = reader["reference"].ToString().Trim();
                    bizaoW2BDTO.Operator = reader["codeMerchant"].ToString().Trim();

                    bizaoBeneficiaryDTos.Add(bizaoW2BDTO);
                }
            }

            reader.Close();
            conn.Close();

            return bizaoBeneficiaryDTos;

        }

        public string GetB2WTransactionStatus(string orderId, string Operator)
        {
            string channel = Operator == "orang" ? "tpe" : "web";
            string mnoName = Operator == "orang" ? "orange" : Operator.Trim();

            string status = null;
            RestSharp.Deserializers.JsonDeserializer deserial = new RestSharp.Deserializers.JsonDeserializer();
            var client = new RestClient("https://api.bizao.com/mobilemoney/v1/getStatus/" + orderId);
            client.Timeout = -1;
            string token = generateBizaoW2BAccessToken();
            var request = new RestRequest(Method.GET);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Accept", "application/json");
            request.AddHeader("channel", channel);
            request.AddHeader("Cookie", "SERVERID=s1");
            request.AddHeader("country-code", "ci");
            request.AddHeader("mno-name", mnoName);
            request.AddHeader("lang", "fr");
            request.AddHeader("Authorization", "Bearer " + token);
            IRestResponse response = client.Execute(request);

            LogHandler.WriteLog("\t|==> CALLING BIZAO GET STATUS \n\t|==> REQUEST LINK: https://api.bizao.com/mobilemoney/v1/getStatus/" + orderId + " \n\t|==> RESPONSE : " + response.Content, "BizaoW2BLogs");

            HttpStatusCode statusCode = response.StatusCode;
            int numericStatusCode = (int)statusCode;

            if (numericStatusCode.Equals(200))
            {
                BizaoW2BResult w2bResult = deserial.Deserialize<BizaoW2BResult>(response);

                string ResultDataLog = JsonConvert.SerializeObject(w2bResult);


                status = w2bResult.status;
            }
            return status;
        }

        private string generateBizaoW2BAccessToken()
        {
            RestSharp.Deserializers.JsonDeserializer deserial = new RestSharp.Deserializers.JsonDeserializer();

            var client = new RestClient("https://api.bizao.com/token");
            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddHeader("authorization", "Basic d0htajV3Tk1lODNOOUQyNzZjUW5lYUIyTHVRYTpab01JdTFyaGVMcnh2RjhBMFBya1V6WG1Hcm9h");
            request.AddParameter("grant_type", "client_credentials");
            IRestResponse response = client.Execute(request);

            BizaoTokenRequestResponse resp = deserial.Deserialize<BizaoTokenRequestResponse>(response);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return resp.access_token;
            }

            return null;
        }

        public void refundFailedTransaction()
        {
            while (true)
            {
                List<bizaoBeneficiaryDTo> bizaoBeneficiaryDTos = new List<bizaoBeneficiaryDTo>();
                //bizaoBeneficiaryDTos = getBizaoTransactions(status);
                bizaoBeneficiaryDTos = getBizaoTransactions("FAILED");
                foreach (var item in bizaoBeneficiaryDTos)
                {
                    //string transactionStatus = GetTransactionStatus(item.batchNumber);
                    ////if (transactionStatus.ToUpper() == status)
                    //if (transactionStatus.ToUpper() == "FAILED")
                    //{
                    //UpdateBizaoTransactionStatus(item, transactionStatus.ToUpper());
                    if (!item.codeMerchant.Equals("mtn"))
                    {
                        string resp = PostToBasis1(
                                oldAccountTocompteCaisse(item.AccountCredited),
                                oldAccountTocompteCaisse(item.AccountDebited),
                                item.amount,
                                9077,
                                "EXTOURNE " + item.Remarks,
                                "32"
                            );
                        //}
                        //else if (transactionStatus.ToUpper() == "SUCCESSFUL")
                        //    UpdateBizaoTransactionStatus(item, transactionStatus.ToUpper());
                        UpdateBizaoTransactionAsTreated(item, "TREATED");
                        Console.WriteLine(item.batchNumber + " " + item.amount + " PAID !!");
                    }
                    
                }
            }
        }

        public int UpdateW2BBizaoTransactionStatus(int id)
        {
            try
            {
                SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["bizaoTransactionsConString"].ToString());
                SqlCommand commandSql = new SqlCommand("updateW2BBBizaoTransactions", conn); ;

                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }

                commandSql.Parameters.AddWithValue("@id", id);

                commandSql.CommandType = CommandType.StoredProcedure;
                commandSql.ExecuteNonQuery();

                conn.Close();
                conn = null;
                return 1;
            }
            catch (Exception ex)
            {
                //ErrHandler.WriteError(ex.Message);
                return 0;
            }
        }


        public void RevereBackFundWhenIssue()
        {
            //List<bizaoBeneficiaryDTo> bizaoBeneficiaryDTos = new List<bizaoBeneficiaryDTo>();
            //int a = 0;
            //for (int i = 1; i <= 14; i++)
            //{
            //    string resp = PostToBasis1(
            //                oldAccountTocompteCaisse("202/144974/1/11/0"),
            //                oldAccountTocompteCaisse("202/142269/1/6/0"),
            //                //oldAccountTocompteCaisse("202/142269/1/6/0"),
            //                //oldAccountTocompteCaisse("203/124282/1/11/0"),
            //                double.Parse("50000"),
            //                9077,
            //                "EXTOURNE REVERSAL " + "Mobile Money Transfer to Orange Money - REF: |0759189078144974GTBANK20230107051404",
            //                "32"
            //            );
            //}
            //bizaoBeneficiaryDTos = getBizaoTransactions(status);
            //bizaoBeneficiaryDTos = getBizaoTransactions("FAILED");
            //foreach (var item in bizaoBeneficiaryDTos)
            //{
            //    //string transactionStatus = GetTransactionStatus(item.batchNumber);
            //    //if (transactionStatus.ToUpper() == status)
            //    if (item.status.ToUpper() == "FAILED")
            //    {
            //        UpdateBizaoTransactionStatus(item, item.status.ToUpper());
            //        string resp = PostToBasis1(
            //                oldAccountTocompteCaisse(item.AccountCredited),
            //                oldAccountTocompteCaisse(item.AccountDebited),
            //                item.amount,
            //                9077,
            //                "EXTOURNE " + item.Remarks,
            //                "32"
            //            );
            //        if (resp == "@ERR7@" || resp == "@ERR19@")
            //        {
            //            UpdateBizaoTransactionAsTreated(item, "TREATED");
            //        }
            //    }
            //    //else if (item.status.ToUpper() == "SUCCESSFUL")
            //    //    UpdateBizaoTransactionStatus(item, item.status.ToUpper());

            //    Console.WriteLine("status saved for : " + item.batchNumber + " !!");
            //}

            //};
        }


        public void RecheckStatus()
        {
            while(true)
            {
                
                List<bizaoBeneficiaryDTo> bizaoBeneficiaryDTos = new List<bizaoBeneficiaryDTo>();
                bizaoBeneficiaryDTos = getBizaoTransactions("ERROR OCCURED DURING GETING STATUS / NOT RESPONSE");
                //bizaoBeneficiaryDTos = transactionService.getBizaoTransactions("ERROR OCCURED DURING GETING STATUS / NOT RESPONSE");

                foreach (var item in bizaoBeneficiaryDTos)
                {
                    if (!item.codeMerchant.Equals("mtn"))
                    {
                        string transactionStatus = GetTransactionStatus(item.batchNumber);
                    if (transactionStatus == null)
                        UpdateBizaoTransactionStatus(item, "ERROR OCCURED DURING GETING STATUS / NOT RESPONSE");
                    else if (transactionStatus.ToUpper() != "FAILED" && transactionStatus.ToUpper() != "SUCCESSFUL")
                        UpdateBizaoTransactionStatus(item, "ERROR OCCURED DURING GETING STATUS / NOT RESPONSE");
                    else if (transactionStatus.ToUpper() == "FAILED")
                        UpdateBizaoTransactionStatus(item, transactionStatus.ToUpper());
                    else if (transactionStatus.ToUpper() == "SUCCESSFUL")
                        UpdateBizaoTransactionStatus(item, transactionStatus.ToUpper());

                    //Console.WriteLine("status saved for : " + item.batchNumber + " !!");

                    }
                }
                

            };

        }


        private BizaoTokenRequestResponse generateBizaoToken()
        {
            RestSharp.Deserializers.JsonDeserializer deserial = new RestSharp.Deserializers.JsonDeserializer();

            var client = new RestClient("https://api.bizao.com/token?grant_type=client_credentials");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("ACCESS_TOKEN", ConfigurationManager.AppSettings["bizaoAccessToken"]);
            request.AddHeader("Authorization", "Basic " + ConfigurationManager.AppSettings["bizaoAuthorization"]);
            request.AddHeader("Cookie", "route=1664470730.138.1396.536672|81ae3a9a04c06b83bdb4bb4311fcd72d");
            IRestResponse response = client.Execute(request);
            //Console.WriteLine(response.Content);

            BizaoTokenRequestResponse resp = deserial.Deserialize<BizaoTokenRequestResponse>(response);

            return resp;
        }

        public string GetTransactionStatus(string batchNumber)
        {
            string status = null;
            RestSharp.Deserializers.JsonDeserializer deserial = new RestSharp.Deserializers.JsonDeserializer();
            var client = new RestClient("https://api.bizao.com/bulk/v1/getStatus/" + batchNumber);
            client.Timeout = -1;
            string token = generateBizaoToken().access_token;
            var request = new RestRequest(Method.GET);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("channel", "web");
            request.AddHeader("type", "bulk");
            request.AddHeader("Cookie", "lteonMS17=AVGuRvbEYwp+/a0U3c6Ndg$$; SERVERID=s0;SRV=c4929e28-54ed-4c57-bec6-a50f7151ac41; route=1671439555.229.32.693882|81ae3a9a04c06b83bdb4bb4311fcd72d");
            request.AddHeader("country-code", "ci");
            request.AddHeader("lang", "en");
            request.AddHeader("Authorization", "Bearer "+ token);
            IRestResponse response = client.Execute(request);
            HttpStatusCode statusCode = response.StatusCode;
            int numericStatusCode = (int)statusCode;

            if (numericStatusCode.Equals(200))
            {
                bizaoMMResult purchaseResult = deserial.Deserialize<bizaoMMResult>(response);

                string ResultDataLog = JsonConvert.SerializeObject(purchaseResult);
                
                //ErrHandler.WriteError("Log result data here : " + ResultDataLog.ToString());

                if (purchaseResult.Meta.reference != "") //please correct
                {
                    status = purchaseResult.data.Select(m => m.status).FirstOrDefault();                    
                }                
            }
            return status;
        }

        public List<bizaoBeneficiaryDTo> getBizaoTransactions(string status)
        {
            List<bizaoBeneficiaryDTo> bizaoBeneficiaryDTos = new List<bizaoBeneficiaryDTo>();
            string responseString = string.Empty;

            try
            {
                
                SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["bizaoTransactionsConString"]);
                
                SqlCommand comm;

                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }

                comm = new SqlCommand("getBizaoTransactions", conn);

                comm.Parameters.AddWithValue("@status", status);                  
                
                comm.CommandType = CommandType.StoredProcedure;
                SqlDataReader reader;

                reader = comm.ExecuteReader();
                if (reader.HasRows == true)
                {
                    
                    while (reader.Read())
                    {
                        bizaoBeneficiaryDTo bizaoBeneficiaryDTo = new bizaoBeneficiaryDTo();

                        bizaoBeneficiaryDTo.id = reader["id"].ToString().Trim();
                        bizaoBeneficiaryDTo.batchNumber = reader["batchNumber"].ToString().Trim();
                        bizaoBeneficiaryDTo.codeMerchant = reader["codeMerchant"].ToString().Trim();
                        bizaoBeneficiaryDTo.amount = Double.Parse(reader["amount"].ToString().Trim());
                        bizaoBeneficiaryDTo.beneficiaryMobileNumber = reader["beneficiaryMobileNumber"].ToString().Trim();
                        bizaoBeneficiaryDTo.beneficiaryFirstName = reader["beneficiaryFirstName"].ToString().Trim();
                        bizaoBeneficiaryDTo.beneficiaryLastName = reader["beneficiaryLastName"].ToString().Trim();
                        bizaoBeneficiaryDTo.status = reader["status"].ToString().Trim();
                        bizaoBeneficiaryDTo.AccountDebited = reader["AccountDebited"].ToString().Trim();
                        bizaoBeneficiaryDTo.AccountCredited = reader["AccountCredited"].ToString().Trim();
                        bizaoBeneficiaryDTo.Remarks = reader["Remarks"].ToString().Trim();

                        bizaoBeneficiaryDTos.Add(bizaoBeneficiaryDTo);
                    }
                   
                }

                reader.Close();
                reader = null;
                comm = null;
                conn.Close();
                return bizaoBeneficiaryDTos;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        //public void UpdateBizaoTransactionStatus(bizaoBeneficiaryDTo bizaoBeneficiaryDTo, string status)
        //{
        //    List<bizaoBeneficiaryDTo> bizaoBeneficiaryDTos = new List<bizaoBeneficiaryDTo>();
        //    string responseString = string.Empty;

        //    try
        //    {

        //        SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["bizaoTransactionsConString"]);

        //        SqlCommand comm;

        //        if (conn.State != ConnectionState.Open)
        //        {
        //            conn.Open();
        //        }

        //        comm = new SqlCommand("updateBizaoTransactions", conn);

        //        comm.Parameters.AddWithValue("@status", status);
        //        comm.Parameters.AddWithValue("@batchNumber", bizaoBeneficiaryDTo.batchNumber);
        //        comm.Parameters.AddWithValue("@id", bizaoBeneficiaryDTo.id);


        //        comm.CommandType = CommandType.StoredProcedure;
        //        SqlDataReader reader;

        //        reader = comm.ExecuteReader();
        //        if (reader.HasRows == true)
        //        {
        //            bizaoBeneficiaryDTo bizaoBeneficiaryDTo = new bizaoBeneficiaryDTo();
        //            while (reader.Read())
        //            {
        //                bizaoBeneficiaryDTo.id = reader["id"].ToString().Trim();
        //                bizaoBeneficiaryDTo.batchNumber = reader["batchNumber"].ToString().Trim();
        //                bizaoBeneficiaryDTo.amount = Double.Parse(reader["amount"].ToString().Trim());
        //                bizaoBeneficiaryDTo.beneficiaryMobileNumber = reader["beneficiaryMobileNumber"].ToString().Trim();
        //                bizaoBeneficiaryDTo.beneficiaryFirstName = reader["beneficiaryFirstName"].ToString().Trim();
        //                bizaoBeneficiaryDTo.beneficiaryLastName = reader["beneficiaryLastName"].ToString().Trim();
        //                bizaoBeneficiaryDTo.status = reader["status"].ToString().Trim();
        //                bizaoBeneficiaryDTo.AccountDebited = reader["AccountDebited"].ToString().Trim();
        //                bizaoBeneficiaryDTo.AccountCredited = reader["AccountCredited"].ToString().Trim();
        //                bizaoBeneficiaryDTo.Remarks = reader["Remarks"].ToString().Trim();

        //                bizaoBeneficiaryDTos.Add(bizaoBeneficiaryDTo);
        //            }

        //        }

        //        reader.Close();
        //        reader = null;
        //        comm = null;
        //        conn.Close();
        //        return bizaoBeneficiaryDTos;
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }
        //}

        
        public int UpdateBizaoTransactionStatus(bizaoBeneficiaryDTo bizaoBeneficiaryDTo, string status)
        {
            try
            {
                SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["bizaoTransactionsConString"].ToString());
                SqlCommand commandSql = new SqlCommand("updateBizaoTransactions", conn); ;

                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }
                

                commandSql.Parameters.AddWithValue("@status", status);
                commandSql.Parameters.AddWithValue("@batchNumber", bizaoBeneficiaryDTo.batchNumber);
                commandSql.Parameters.AddWithValue("@id", bizaoBeneficiaryDTo.id);

                commandSql.CommandType = CommandType.StoredProcedure;
                commandSql.ExecuteNonQuery();

                conn.Close();
                conn = null;
                return 1;
            }
            catch (Exception ex)
            {
                //ErrHandler.WriteError(ex.Message);
                return 0;
            }
        }

        public int UpdateBizaoTransactionAsTreated(bizaoBeneficiaryDTo bizaoBeneficiaryDTo, string isTreated)
        {
            try
            {
                SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["bizaoTransactionsConString"].ToString());
                SqlCommand commandSql = new SqlCommand("updateBizaoTransactionsAsTreated", conn); ;

                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }


                commandSql.Parameters.AddWithValue("@isTreated", isTreated);
                commandSql.Parameters.AddWithValue("@batchNumber", bizaoBeneficiaryDTo.batchNumber);
                commandSql.Parameters.AddWithValue("@id", bizaoBeneficiaryDTo.id);

                commandSql.CommandType = CommandType.StoredProcedure;
                commandSql.ExecuteNonQuery();

                conn.Close();
                conn = null;
                return 1;
            }
            catch (Exception ex)
            {
                //ErrHandler.WriteError(ex.Message);
                return 0;
            }
        }


        public void getTransactionWithDifferentStatus( string status)
        {
            List<bizaoBeneficiaryDTo> bizaoBeneficiaryDTos = new List<bizaoBeneficiaryDTo>();
            TransactionService transactionService = new TransactionService();
            bizaoBeneficiaryDTos = transactionService.getBizaoTransactions(status);

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

                //Console.WriteLine("status saved for : " + item.batchNumber + " !!");
            }
            
            Console.WriteLine("il ya : " + bizaoBeneficiaryDTos.Count.ToString());
        }

        public string oldAccountTocompteCaisse(string oldAccount)
        {
            char[] delim = new char[] { '/' };
            String[] tempstr;
            String new_account;

            tempstr = oldAccount.Split(delim);

            new_account = tempstr[0].PadLeft(4, '0') + tempstr[1].PadLeft(7, '0') + tempstr[2].PadLeft(3, '0') + tempstr[3].PadLeft(4, '0') + tempstr[4].PadLeft(3, '0');

            return new_account;

        }


        public string PostToBasis1(string Acct_from, string Acct_to, double Tra_amt, int Expl_code, string Remark, string Req_code)
        {
            string Result = string.Empty;

            int orig_bra_code = 999;

            if (Acct_from.Trim() == Acct_to.Trim())
            {
                return "@ERR-74@";
            }


            //Round the amount to 2 decimal places
            Tra_amt = Math.Round(Tra_amt, 2, MidpointRounding.AwayFromZero);
            
            OracleConnection oraconn = new OracleConnection(ConfigurationManager.AppSettings["basis"]);
            Oracle.ManagedDataAccess.Client.OracleCommand oracomm = new Oracle.ManagedDataAccess.Client.OracleCommand("INTERMON.ATM_ENTRIES_GEN", oraconn);
            oracomm.CommandType = CommandType.StoredProcedure;

            try
            {

                if (oraconn.State == ConnectionState.Closed)
                {
                    oraconn.Open();
                }

                oracomm.Parameters.Add("INP_ACCT_FROM", OracleDbType.NVarchar2, 21).Value = Acct_from;
                oracomm.Parameters.Add("INP_ACCT_TO", OracleDbType.NVarchar2, 21).Value = Acct_to;
                oracomm.Parameters.Add("INP_TRA_AMT", OracleDbType.Double, 20).Value = Tra_amt;
                oracomm.Parameters.Add("INP_EXPL_CODE", OracleDbType.Int32, 15).Value = Expl_code;
                oracomm.Parameters.Add("INP_REMARKS", OracleDbType.NVarchar2, 200).Value = Remark;
                oracomm.Parameters.Add("INP_RQST_CODE", OracleDbType.NVarchar2, 15).Value = Req_code;
                oracomm.Parameters.Add("INP_ORIGT_BRA_CODE", OracleDbType.Int32, 15).Value = orig_bra_code;
                oracomm.Parameters.Add("OUT_RETURN_STATUS", OracleDbType.NVarchar2, 100).Direction = ParameterDirection.Output;
                oracomm.ExecuteNonQuery();
                Result = oracomm.Parameters["OUT_RETURN_STATUS"].Value.ToString();

                if (Result.Trim().CompareTo("@ERR7@") == 0 || Result.Trim().CompareTo("@ERR19@") == 0)
                {
                    //  transaction.Commit();
                    // ErrHandler.WriteError("Commit successful for transaction with details INP_ACCT_FROM = " + Acct_from + " || INP_ACCT_TO = " + Acct_to + " || INP_REMARKS = " + Remark + " || OUT_RETURN_STATUS = " + Result);
                    return Result;
                }
                else
                {
                    //  transaction.Rollback();
                    //  ErrHandler.WriteError("Rollback for transaction with details INP_ACCT_FROM = " + Acct_from + " || INP_ACCT_TO = " + Acct_to + " || INP_REMARKS = " + Remark + " || OUT_RETURN_STATUS = " + Result);
                    return Result;
                }
            }
            catch (Exception ex)
            {
                // Result = "-2";
                Result = ex.Message;
                //   transaction.Rollback();
                // ErrHandler.WriteError("Error Posting to Basis, issued a rollback : with details INP_ACCT_FROM = " + Acct_from + " || INP_ACCT_TO = " + Acct_to + " || INP_REMARKS = " + Remark + " || OUT_RETURN_STATUS = " + Result + " ERROR = " + ex.Message);
                return Result;
            }
            finally
            {
                oraconn.Close();
            }
        }
    }
}
