using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessTransactionsPending.Model
{
    class bizaoBeneficiaryDTo
    {
        public string id { get; set; }
        public string beneficiaryFirstName { get; set; }
        public string beneficiaryLastName { get; set; }
        public string beneficiaryAddress { get; set; }
        public string beneficiaryMobileNumber { get; set; }
        public double amount { get; set; }
        public string feesApplicable { get; set; }
        public string mno { get; set; }
        public string batchNumber { get; set; }
        public string AccountDebited { get; set; }
        public string AccountCredited { get; set; }
        public string Remarks { get; set; }
        public string status { get; set; }
        public string codeMerchant { get; set; }
    }
}
