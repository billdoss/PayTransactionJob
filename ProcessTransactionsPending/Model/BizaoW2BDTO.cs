using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessTransactionsPending.Model
{
    class BizaoW2BDTO
    {
        public int Id { get; set; }
        public string BeneficiaryFirstName { get; set; }
        public string BeneficiaryLastName { get; set; }
        public string Msisdn { get; set; }
        public double Amount { get; set; }
        public string AccountDebited { get; set; }
        public string AccountCredited { get; set; }
        public string OrderId { get; set; }
        public string Operator { get; set; }
        public string Reference { get; set; }
    }
}
