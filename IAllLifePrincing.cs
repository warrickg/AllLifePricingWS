using AllLifePricing.Classes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Web;

namespace AllLifePricing
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IAllLifePrincing
    {
        [OperationContract]
        string GetData(string value);

        [OperationContract]
        CompositeType GetDataUsingDataContract(CompositeType composite);

        /*
        [OperationContract]
        string MyTest(String strValue, int intValue);

        [OperationContract]
        string MyTestDecrypt(String strValue, String strValue2);

        
        [OperationContract]
        DataTable loginSubscriber(String subscriberName, String subscriberPassword, String subscriberCode);
        
        [OperationContract]
        PricingUser Userlogin(PricingUser User_);        

        [OperationContract]
        DataSet Get_UserMenu(int UserID);

        [OperationContract]
        DataSet Get_Users();
        */

        [OperationContract]
        DataTable returnPremium(String subscriberName, String subscriberPassword, String subscriberCode, String productCode, String baseRisk, String riskModifier, String coverValue, String quoteDate, String customerID, String quotationID);

        [OperationContract]
        DataTable returnRisk(String subscriberName, String subscriberPassword, String subscriberCode, String productCode, String baseRisk, String riskModifier, String coverValue, String premiumValue, String quoteDate, String inceptionDate, String effectiveDate, String customerID, String quotationID, String durationModifierCode);

        [OperationContract]
        DataTable returnCover(String subscriberName, String subscriberPassword, String subscriberCode, String productCode, String baseRisk, String riskModifier, String premiumValue, String quoteDate, String customerID, String quotationID);

        [OperationContract]
        DataTable returnEM_Affected_Premium(String subscriberName, String subscriberPassword, String subscriberCode, String productCode, String baseRisk, String riskModifier, String coverValue, String quoteDate, String customerID, String quotationID, String EMloading);

        [OperationContract]
        DataTable returnEM_Affected_Risk(String subscriberName, String subscriberPassword, String subscriberCode, String productCode, String baseRisk, String riskModifier, String coverValue, String premiumValue, String quoteDate, String inceptionDate, String effectiveDate, String customerID, String quotationID, String durationModifierCode, String EMloading);

        [OperationContract]
        DataTable returnEM_Affected_Cover(String subscriberName, String subscriberPassword, String subscriberCode, String productCode, String baseRisk, String riskModifier, String premiumValue, String quoteDate, String customerID, String quotationID, String EMloading);


        [OperationContract]
        DataTable returnRiskBand(String subscriberName, String subscriberPassword, String subscriberCode, String productCode, String riskModifier, String quoteDate);

        [OperationContract]
        DataTable QualifyLife(String subscriberName, String subscriberPassword, String subscriberCode, String AgeNextBirthday, String TobaccoUse, String HbA1cPercent, String BMI, String PantSize, String AlcoholUnitsPerDay, String Occupation);

        [OperationContract]
        DataTable QualifyDisability(String subscriberName, String subscriberPassword, String subscriberCode, String AgeNextBirthday, String TobaccoUse, String HbA1cPercent, String BMI, String PantSize, String AlcoholUnitsPerDay, String Employment, String Qualification, String Income, String SpouseIncome, String RiskBand);

        //private static string CreateSalt(int size)
        //{
        //    // Generate a cryptographic random number using the cryptographic
        //    // service provider
        //    RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
        //    byte[] buff = new byte[size];
        //    rng.GetBytes(buff);
        //    // Return a Base64 string representation of the random number
        //    return Convert.ToBase64String(buff);
        //}

        //private static string CreatePasswordHash(string pwd, string salt)
        //{
        //    //string saltAndPwd = String.Concat(pwd, salt);
        //    //string hashedPwd = FormsAuthentication.HashPasswordForStoringInConfigFile(saltAndPwd, "SHA1");
        //    //hashedPwd = String.Concat(hashedPwd, salt);

        //    string hashedPwd = FormsAuthentication.HashPasswordForStoringInConfigFile(salt + pwd, "SHA1");

        //    return hashedPwd;
        //}

        // TODO: Add your service operations here
    }

    // Use a data contract as illustrated in the sample below to add composite types to service operations.
    // You can add XSD files into the project. After building the project, you can directly use the data types defined there, with the namespace "AllLifePricing.ContractType".
    [DataContract]
    public class CompositeType
    {
        bool boolValue = true;
        string stringValue = "Hello ";

        [DataMember]
        public bool BoolValue
        {
            get { return boolValue; }
            set { boolValue = value; }
        }

        [DataMember]
        public string StringValue
        {
            get { return stringValue; }
            set { stringValue = value; }
        }
    }
}
