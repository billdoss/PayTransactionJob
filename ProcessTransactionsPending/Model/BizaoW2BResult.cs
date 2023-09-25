using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessTransactionsPending.Model
{
    public class Meta
    {
        public string type { get; set; }
        public string source { get; set; }
        public string channel { get; set; }
    }
    class BizaoW2BResult
    {
        public Meta meta { get; set; }
        public string status { get; set; }
        public string amount { get; set; }
        public string order_id { get; set; }
        public string currency { get; set; }
        public string reference { get; set; }
        public string date { get; set; }
        public string state { get; set; }
        public string country_code { get; set; }
        public string user_msisdn { get; set; }
        public string intTransaction_id { get; set; }
        public string extTransaction_id { get; set; }
    }
}
