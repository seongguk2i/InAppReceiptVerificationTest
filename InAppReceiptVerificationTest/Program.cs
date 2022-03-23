using System;

namespace InAppReceiptVerificationTest
{
    class Program
    {
        static void Main(string[] args)
        {
            /*{"Payload":"{ \"this\" : \"is a fake receipt\" }","Store":"fake","TransactionID":"45940b8e-2516-435d-9ac4-c4dcd6cdecd7"}*/
            CIOSReceiptVerificationMng.CheckReceipt("heart", "{\"Payload\":\"{ \"this\" : \"is a fake receipt\" }\",\"Store\":\"fake\",\"TransactionID\":\"45940b8e-2516-435d-9ac4-c4dcd6cdecd7\"}");
        }
    }
}
