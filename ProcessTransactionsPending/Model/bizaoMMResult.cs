using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessTransactionsPending.Model
{
    class bizaoMMResult
    {
        public bizaoMetadetails Meta { get; set; }
        public List<dataDetails> data { get; set; }
    }


    public class dataDetails
    {
        public string id { get; set; }
        public string order_id { get; set; }
        public string mno { get; set; }
        public string date { get; set; }
        public string beneficiaryFirstName { get; set; }
        public string beneficiaryLastName { get; set; }
        public string beneficiaryAddress { get; set; }
        public string beneficiaryMobileNumber { get; set; }
        public string toCountry { get; set; }
        public string feesApplicable { get; set; }
        public double amount { get; set; }
        public double fees { get; set; }
        public string status { get; set; }
        public string statusDescription { get; set; }
        [JsonProperty(PropertyName = "intTransaction-Id")]
        public string intTransactionId { get; set; }
        [JsonProperty(PropertyName = "extTransaction-Id")]

        public string extTransactionId { get; set; }

    }

    public class bizaoMetadetails
    {
        public string source { get; set; }
        public string merchantName { get; set; }
        public string type { get; set; }
        public string currency { get; set; }
        public string batchNumber { get; set; }
        public string reference { get; set; }
        public string feesType { get; set; }
        public string lang { get; set; }
        public double totalAmount { get; set; }
        public double totalFees { get; set; }
        public string senderFirstName { get; set; }
        public string senderLastName { get; set; }
        public string senderAddress { get; set; }
        public string senderMobileNumber { get; set; }
        public string fromCountry { get; set; }
        public string comment { get; set; }

    }

    public class ResponsePosting //This returns the posting response
    {
        public int id { get; set; }
        public string date { get; set; }
        public string reference_transaction { get; set; }
        public string reference { get; set; }
        public string code_marchand { get; set; }
        public string marchand { get; set; }
        public string nom_client { get; set; }
        public string telephone { get; set; }
        public string commentaire { get; set; }
        public int montant { get; set; }
        public int frais { get; set; }

        public string error_code { get; set; }
    }
}
