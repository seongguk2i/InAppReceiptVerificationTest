using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace InAppReceiptVerificationTest
{
    class CIOSReceiptVerificationMng
    {
        public const string IOS_PRODUCT_URL = "https://buy.itunes.apple.com/verifyReceipt";
        public const string IOS_SENDBOX_URL = "https://sandbox.itunes.apple.com/verifyReceipt";
        public const string PACKAGE_NAME = "your Bundle Identifier"; //번들 ID를 입력합니다.

        public const int IOS_RV_SUCCESS = 0;    //ios영수증 검증 결과 성공
        public const int IOS_RV_FAIL_RETRY = 1;    //샌드박스에서 재검증필요
        public const int IOS_RV_FAIL = -1;    //검증 실패

        /*ios영수증 검증
        itemID = 해당 영수증으로 결제한 아이템의 ID
        receiptData = 영수증 데이터
        bProduct = 프로덕트에서 검증할지 샌드박스에서 검증할지, 기본적으로 프로덕트에서 검증하고 리턴값이 IOS_RV_FAIL_RETRY  인경우에 샌드박스에서 검증한다.
        */
        public static int VerifyIOSReceipt(ref string itemID, string receiptData, bool bProduct)
        {
            try
            {
                itemID = null;

                // Verify the receipt with Apple
                string postString = string.Format("{{ \"receipt-data\" : \"{0}\" }}", receiptData);
                ASCIIEncoding ascii = new ASCIIEncoding();
                byte[] postBytes = ascii.GetBytes(postString);
                HttpWebRequest request;

                if (bProduct)
                    request = WebRequest.Create(IOS_PRODUCT_URL) as HttpWebRequest;
                else
                    request = WebRequest.Create(IOS_SENDBOX_URL) as HttpWebRequest;
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = postBytes.Length;
                Stream postStream = request.GetRequestStream();
                postStream.Write(postBytes, 0, postBytes.Length);
                postStream.Close();

                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                StringBuilder sb = new StringBuilder();
                byte[] buf = new byte[8192];
                Stream resStream = response.GetResponseStream();
                string tempString = null;

                int count = 0;
                do
                {
                    count = resStream.Read(buf, 0, buf.Length);

                    if (count != 0)
                    {
                        tempString = Encoding.ASCII.GetString(buf, 0, count);
                        sb.Append(tempString);
                    }
                } while (count > 0);

                var fd = JObject.Parse(sb.ToString());
                try
                {
                    resStream.Close();
                    response.Close();
                }
                catch
                {
                }

                string strResult = fd["status"].ToString();

                // Receipt not valid
                if (strResult != "0")
                {
                    if (strResult == "21007")
                        return IOS_RV_FAIL_RETRY;

                    // Error out
                    return IOS_RV_FAIL;
                }

                // Product ID does not match what we expected
                var receipt = fd["receipt"];

                /*
                if (String.Compare(receipt["product_id"].ToString().Replace("\"", "").Trim(), itemID.Trim(), true) != 0)
                {
                    // Error out

                    return IOS_RV_FAIL;
                }
                * */

                //제품 ID정보를 저장함
                itemID = receipt["product_id"].ToString().Replace("\"", "").Trim();

                // This product was not sold by the right app
                if (String.Compare(receipt["bid"].ToString().Replace("\"", "").Trim(), PACKAGE_NAME, true) != 0)
                {
                    // Error out
                    return IOS_RV_FAIL;
                }

                /*
                // This transaction didn't occur within 24 hours in either direction; somebody is reusing a receipt
                DateTime transDate = DateTime.SpecifyKind(DateTime.Parse(receipt["purchase_date"].ToString().Replace("\"", "").Replace("Etc/GMT", "")), DateTimeKind.Utc);
                TimeSpan delay = DateTime.UtcNow - transDate;
                if (delay.TotalHours > 24 || delay.TotalHours < -24)
                {
                    // Error out
                    return false;
                }
                */
                // Perform the purchase -- all my purchases are server-side only, which is a very secure way of doing things
                // Success!
            }

            catch// (Exception ex)
            {
                // We crashed and burned -- do something intelligent
                return IOS_RV_FAIL;
            }

            return IOS_RV_SUCCESS;
        }



        public static bool CheckReceipt(string strItemID, string strReceipt)
        {
            //일단 프로덕션에서 검증을 해봅니다.
            int ret = CIOSReceiptVerificationMng.VerifyIOSReceipt(ref strItemID, strReceipt, true);

            //성공
            if (ret == IOS_RV_SUCCESS)
                return true;

            //만일 샌드 박스용영수증이라면 센드박스에서 다시 검증합니다.

            if (ret == IOS_RV_FAIL_RETRY)
            {

                ret = CIOSReceiptVerificationMng.VerifyIOSReceipt(ref strItemID, strReceipt, false);

                if (ret != IOS_RV_SUCCESS)

                    return false;

                return true;
            }

            //영수증 검증실패(정상구매가 아니거나 애플 서버연결에 실패 했을 수 있습니다.)

            return false;
        }
    }
}
