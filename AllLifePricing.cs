using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Configuration;
using System.Data.SqlClient;
using System.Security.Cryptography;
using AllLifePricing.Classes;

namespace AllLifePricing
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in both code and config file together.
    public class AllLifePricing : IAllLifePrincing
    {

        SqlConnection sqlConnectionX;
        SqlCommand sqlCommandX;
        SqlParameter sqlParam;
        SqlDataReader sqlDR;

        private static string ComputeHash(string plainText, string hashAlgorithm, byte[] saltBytes)
        {
            // If salt is not specified, generate it.
            if (saltBytes == null)
            {
                // Define min and max salt sizes.
                int minSaltSize = 4;
                int maxSaltSize = 8;

                // Generate a random number for the size of the salt.
                Random random = new Random();
                int saltSize = random.Next(minSaltSize, maxSaltSize);

                // Allocate a byte array, which will hold the salt.
                saltBytes = new byte[saltSize];

                // Initialize a random number generator.
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

                // Fill the salt with cryptographically strong byte values.
                rng.GetNonZeroBytes(saltBytes);
            }

            // Convert plain text into a byte array.
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            // Allocate array, which will hold plain text and salt.
            byte[] plainTextWithSaltBytes =
            new byte[plainTextBytes.Length + saltBytes.Length];

            // Copy plain text bytes into resulting array.
            for (int i = 0; i < plainTextBytes.Length; i++)
                plainTextWithSaltBytes[i] = plainTextBytes[i];

            // Append salt bytes to the resulting array.
            for (int i = 0; i < saltBytes.Length; i++)
                plainTextWithSaltBytes[plainTextBytes.Length + i] = saltBytes[i];

            HashAlgorithm hash;

            // Make sure hashing algorithm name is specified.
            if (hashAlgorithm == null)
                hashAlgorithm = "";

            // Initialize appropriate hashing algorithm class.
            switch (hashAlgorithm.ToUpper())
            {

                case "SHA384":
                    hash = new SHA384Managed();
                    break;

                case "SHA512":
                    hash = new SHA512Managed();
                    break;

                default:
                    hash = new MD5CryptoServiceProvider();
                    break;
            }

            // Compute hash value of our plain text with appended salt.
            byte[] hashBytes = hash.ComputeHash(plainTextWithSaltBytes);

            // Create array which will hold hash and original salt bytes.
            byte[] hashWithSaltBytes = new byte[hashBytes.Length +
            saltBytes.Length];

            // Copy hash bytes into resulting array.
            for (int i = 0; i < hashBytes.Length; i++)
                hashWithSaltBytes[i] = hashBytes[i];

            // Append salt bytes to the result.
            for (int i = 0; i < saltBytes.Length; i++)
                hashWithSaltBytes[hashBytes.Length + i] = saltBytes[i];

            // Convert result into a base64-encoded string.
            string hashValue = Convert.ToBase64String(hashWithSaltBytes);

            // Return the result.
            return hashValue;
        }

        private static bool VerifyHash(string plainText, string hashAlgorithm, string hashValue)
        {
            //plainText is the value the user will enter for the password
            //hashValue is the encrypted password

            // Convert base64-encoded hash value into a byte array.
            byte[] hashWithSaltBytes = Convert.FromBase64String(hashValue);

            // We must know size of hash (without salt).
            int hashSizeInBits, hashSizeInBytes;

            // Make sure that hashing algorithm name is specified.
            if (hashAlgorithm == null)
                hashAlgorithm = "";

            // Size of hash is based on the specified algorithm.
            switch (hashAlgorithm.ToUpper())
            {

                case "SHA384":
                    hashSizeInBits = 384;
                    break;

                case "SHA512":
                    hashSizeInBits = 512;
                    break;

                default: // Must be MD5
                    hashSizeInBits = 128;
                    break;
            }

            // Convert size of hash from bits to bytes.
            hashSizeInBytes = hashSizeInBits / 8;

            // Make sure that the specified hash value is long enough.
            if (hashWithSaltBytes.Length < hashSizeInBytes)
                return false;

            // Allocate array to hold original salt bytes retrieved from hash.
            byte[] saltBytes = new byte[hashWithSaltBytes.Length - hashSizeInBytes];

            // Copy salt from the end of the hash to the new array.
            for (int i = 0; i < saltBytes.Length; i++)
                saltBytes[i] = hashWithSaltBytes[hashSizeInBytes + i];

            // Compute a new hash string.
            string expectedHashString = ComputeHash(plainText, hashAlgorithm, saltBytes);

            // If the computed hash matches the specified hash,
            // the plain text value must be correct.
            return (hashValue == expectedHashString);
        }

        public string GetData(string value)
        {
            return string.Format("You entered: {0}", value);
        }

        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }

        /*
         * 
        public string MyTest(String strValue, int intValue)
        {
            //sqlConnectionX = new SqlConnection(ConfigurationManager.AppSettings["WSSQLConnection"]);
            //sqlConnectionX.Open();

            string test = ComputeHash(strValue, "SHA512", null);
            return test;
            //return strValue + intValue.ToString();
        }

        public string MyTestDecrypt(String strValue, String strValue2)
        {
            string test = string.Empty;

            bool flag = VerifyHash(strValue, "SHA512", strValue2);
            if (flag == true)
            {
                test = "the decrypted value is: " + strValue;
            }
            else
            {
                test = "The password is incorrect";
            }

            return test;
        }               

        
        public DataTable loginSubscriber(String subscriberName, String subscriberPassword, String subscriberCode)
        {
            string s = string.Empty;

            DataTable dt = new DataTable("Result");
            dt.Columns.Add("loginResult", typeof(string));
            dt.Columns.Add("SubcriberID", typeof(string));

            DataRow dr = dt.NewRow();

            try
            {
                Subscriber SubscriberX = new Subscriber();
                SubscriberX.SubscriberName = subscriberName;
                SubscriberX.SubscriberPassword = subscriberPassword;
                SubscriberX.SubscriberCode = subscriberCode;

                Subscriber Subscriber_sys = User_Auth(SubscriberX);

                dr["loginResult"] = Subscriber_sys.ResultMessage;
                if (Subscriber_sys.ResultMessage == "Successful")
                {
                    dr["SubcriberID"] = Subscriber_sys.SubscriberID.ToString();
                }
                else
                {
                    dr["SubcriberID"] = DBNull.Value;
                }
                dt.Rows.Add(dr);
                //s = Subscriber_sys.SubscriberID.ToString();
            }
            catch (Exception ex)
            {                
                //throw;

                dr["loginResult"] = ex.Message;
                dr["SubcriberID"] = DBNull.Value;
                dt.Rows.Add(dr);
            }
            
            return dt;
            //return s;
           
        }

        */

        //public PricingUser Userlogin(PricingUser User_)
        //{
        //    PricingUser DBUser = new PricingUser();
        //    bool blnAreThereErrors = false;

        //    try
        //    {
        //        sqlConnectionX = new SqlConnection(ConfigurationManager.AppSettings["WSSQLConnection"]);
        //        sqlConnectionX.Open();                

        //        sqlCommandX = new SqlCommand();
        //        sqlCommandX.Connection = sqlConnectionX;
        //        sqlCommandX.CommandType = CommandType.StoredProcedure;
        //        sqlCommandX.CommandText = "spx_Pricing_UserAuth";

        //        sqlParam = new SqlParameter("UserName", User_.Username);
        //        sqlCommandX.Parameters.Add(sqlParam);
        //        sqlDR = sqlCommandX.ExecuteReader();

        //        while (sqlDR.Read())
        //        {
        //            DBUser.UserID = sqlDR.GetInt32(0);
        //            DBUser.Username = sqlDR.GetString(1);
        //            DBUser.Password = sqlDR.GetString(2);                    
        //        }
        //        sqlDR.Close();
        //        sqlCommandX.Cancel();
        //        sqlCommandX.Dispose();

        //        //Check the password is correct
        //        bool flag = VerifyHash(User_.Password, "SHA512", DBUser.Password);
        //        if (flag != true)
        //        {
        //            blnAreThereErrors = true;
        //            if (DBUser.Result != null)
        //            {
        //                DBUser.Result += ", User password is incorrect";
        //            }
        //            else
        //            {
        //                DBUser.Result = "User password is incorrect";
        //            }
        //        }
        //        else
        //        {
        //            DBUser.Result = "Success";
        //            DBUser.Password = "";
        //        }

                
        //    }
        //    catch (Exception)
        //    {
        //        //mySubscriber.ResultMessage = ex.Message;
        //    }
        //    finally
        //    {
        //        sqlDR.Close();
        //        sqlDR.Dispose();
        //        sqlConnectionX.Close();
        //    }

        //    return DBUser;
        //}

        //public DataSet Get_UserMenu(int UserID)
        //{
        //    try
        //    {
        //        sqlConnectionX = new SqlConnection(ConfigurationManager.AppSettings["WSSQLConnection"]);
        //        sqlConnectionX.Open();

        //        sqlCommandX = new SqlCommand();
        //        sqlCommandX.Connection = sqlConnectionX;
        //        sqlCommandX.CommandType = CommandType.StoredProcedure;
        //        sqlCommandX.CommandText = "spx_SELECT_UserMenu";

        //        sqlParam = new SqlParameter("UserID", UserID);
        //        sqlCommandX.Parameters.Add(sqlParam);

        //        SqlDataAdapter daX = new SqlDataAdapter(sqlCommandX);
        //        DataSet dsX = new DataSet();

        //        daX.Fill(dsX);

        //        return dsX;
        //    }
        //    finally
        //    {
        //        sqlConnectionX.Close();
        //    }
        //}

        //public DataTable Get_Users()

        //public DataSet Get_Users()
        //{
        //    try
        //    {
        //        sqlConnectionX = new SqlConnection(ConfigurationManager.AppSettings["WSSQLConnection"]);
        //        sqlConnectionX.Open();

        //        sqlCommandX = new SqlCommand();
        //        sqlCommandX.Connection = sqlConnectionX;
        //        sqlCommandX.CommandType = CommandType.StoredProcedure;
        //        sqlCommandX.CommandText = "spx_Select_Users";

        //        //SqlDataReader dr = sqlCommandX.ExecuteReader();
        //        //DataTable dt = new DataTable("Users");
        //        //dt.Load(dr);

        //        //return dt;
        //        SqlDataAdapter daX = new SqlDataAdapter(sqlCommandX);
        //        DataSet dsX = new DataSet();

        //        daX.Fill(dsX);

        //        return dsX;
        //    }
        //    catch (Exception ex)
        //    {
        //        DataTable dt = new DataTable("Result");
        //        dt.Columns.Add("loginResult", typeof(string));
        //        dt.Columns.Add("SubcriberID", typeof(string));
        //        DataRow dr = dt.NewRow();

        //        dr["loginResult"] = ex.Message;
        //        dr["SubcriberID"] = DBNull.Value;
        //        dt.Rows.Add(dr);

        //        DataSet dsEr = new DataSet();
        //        dsEr.Tables.Add(dt);
        //        return dsEr;
        //    }
        //    finally
        //    {
        //        sqlConnectionX.Close();
        //    }
        //}

        //private Subscriber UserAuth(String User_)
        //{
        //    Subscriber mySubscriber = new Subscriber();
        //    bool blnAreThereErrors = false;

        //    try
        //    {
        //        sqlConnectionX = new SqlConnection(ConfigurationManager.AppSettings["WSSQLConnection"]);
        //        sqlConnectionX.Open();

        //        sqlCommandX = new SqlCommand();
        //        sqlCommandX.Connection = sqlConnectionX;
        //        sqlCommandX.CommandType = CommandType.StoredProcedure;
        //        sqlCommandX.CommandText = "spx_Pricing_SubscriberAuth";

        //        sqlParam = new SqlParameter("SubscriberName", SubscriberX.SubscriberName);
        //        sqlCommandX.Parameters.Add(sqlParam);
        //        sqlDR = sqlCommandX.ExecuteReader();

        //        while (sqlDR.Read())
        //        {
        //            mySubscriber.SubscriberID = sqlDR.GetInt32(0);
        //            mySubscriber.SubscriberName = sqlDR.GetString(1);
        //            mySubscriber.SubscriberPassword = sqlDR.GetString(2);
        //            mySubscriber.SubscriberCode = sqlDR.GetValue(3).ToString();
        //            mySubscriber.SubscriberStatus = sqlDR.GetValue(4).ToString();
        //            if (sqlDR.GetValue(5).ToString() == "F")
        //            {
        //                mySubscriber.RetrunRisk = false;
        //            }
        //            else
        //            {
        //                mySubscriber.RetrunRisk = true;
        //            }

        //            if (sqlDR.GetValue(6).ToString() == "F")
        //            {
        //                mySubscriber.RetrunPremium = false;
        //            }
        //            else
        //            {
        //                mySubscriber.RetrunPremium = true;
        //            }

        //            if (sqlDR.GetValue(7).ToString() == "F")
        //            {
        //                mySubscriber.RetrunCover = false;
        //            }
        //            else
        //            {
        //                mySubscriber.RetrunCover = true;
        //            }

        //        }
        //        sqlDR.Close();
        //        sqlCommandX.Cancel();
        //        sqlCommandX.Dispose();

        //        //if (mySubscriber.SubscriberID != 0)
        //        //{
        //        //    //Check the Subscriber code is correct
        //        //    if (mySubscriber.SubscriberCode != SubscriberX.SubscriberCode)
        //        //    {
        //        //        blnAreThereErrors = true;
        //        //        mySubscriber.ResultMessage = "Subscriber code incorrect";
        //        //    }

        //        //    //Check the password is correct
        //        //    bool flag = VerifyHash(SubscriberX.SubscriberPassword, "SHA512", mySubscriber.SubscriberPassword);
        //        //    if (flag != true)
        //        //    {
        //        //        blnAreThereErrors = true;
        //        //        if (mySubscriber.ResultMessage != null)
        //        //        {
        //        //            mySubscriber.ResultMessage += ", Subscriber password is incorrect";
        //        //        }
        //        //        else
        //        //        {
        //        //            mySubscriber.ResultMessage = "Subscriber password is incorrect";
        //        //        }
        //        //    }

        //        //    //Check if the user is enabled
        //        //    if (mySubscriber.SubscriberStatus == "0")
        //        //    {
        //        //        blnAreThereErrors = true;
        //        //        //if (mySubscriber.ResultMessage != null)
        //        //        //{
        //        //        //    mySubscriber.ResultMessage += ", The subscriber is disabled";
        //        //        //}
        //        //        //else
        //        //        //{
        //        //        //    mySubscriber.ResultMessage = "The subscriber is disabled";
        //        //        //}

        //        //        mySubscriber.ResultMessage = "The subscriber is disabled";
        //        //    }
        //        //}
        //        //else
        //        //{
        //        //    blnAreThereErrors = true;
        //        //    mySubscriber.ResultMessage = "The subscriber name does not exist";
        //        //}

        //        //if (blnAreThereErrors == true)
        //        //{
        //        //    mySubscriber.ResultMessage = "Subscriber Authentication failed: " + mySubscriber.ResultMessage;
        //        //}
        //        //else
        //        //{
        //        //    mySubscriber.ResultMessage = "Successful";
        //        //}
        //    }
        //    catch (Exception ex)
        //    {
        //        mySubscriber.ResultMessage = ex.Message;
        //    }
        //    finally
        //    {
        //        sqlDR.Close();
        //        sqlDR.Dispose();
        //        sqlConnectionX.Close();
        //    }

        //    return mySubscriber;
        //}

        private Subscriber Subscriber_Auth(Subscriber SubscriberX)
        {
            Subscriber mySubscriber = new Subscriber();
            bool blnAreThereErrors = false;

            try
            {
                sqlConnectionX = new SqlConnection(ConfigurationManager.AppSettings["WSSQLConnection"]);
                sqlConnectionX.Open();

                sqlCommandX = new SqlCommand();
                sqlCommandX.Connection = sqlConnectionX;
                sqlCommandX.CommandType = CommandType.StoredProcedure;
                sqlCommandX.CommandText = "spx_Pricing_SubscriberAuth";

                sqlParam = new SqlParameter("SubscriberName", SubscriberX.SubscriberName);
                sqlCommandX.Parameters.Add(sqlParam);
                sqlDR = sqlCommandX.ExecuteReader();

                while (sqlDR.Read())
                {
                    mySubscriber.SubscriberID = sqlDR.GetInt32(0);
                    mySubscriber.SubscriberName = sqlDR.GetString(1);
                    mySubscriber.SubscriberPassword = sqlDR.GetString(2);
                    mySubscriber.SubscriberCode = sqlDR.GetValue(3).ToString();
                    mySubscriber.SubscriberStatus = sqlDR.GetValue(4).ToString();
                    if (sqlDR.GetValue(5).ToString() == "F")
                    {
                        mySubscriber.RetrunRisk = false;
                    }
                    else
                    {
                        mySubscriber.RetrunRisk = true;
                    }

                    if (sqlDR.GetValue(6).ToString() == "F")
                    {
                        mySubscriber.RetrunPremium = false;
                    }
                    else
                    {
                        mySubscriber.RetrunPremium = true;
                    }

                    if (sqlDR.GetValue(7).ToString() == "F")
                    {
                        mySubscriber.RetrunCover = false;
                    }
                    else
                    {
                        mySubscriber.RetrunCover = true;
                    }

                }
                sqlDR.Close();
                sqlCommandX.Cancel();
                sqlCommandX.Dispose();

                if (mySubscriber.SubscriberID != 0)
                {
                    //Check the Subscriber code is correct
                    if (mySubscriber.SubscriberCode != SubscriberX.SubscriberCode)
                    {
                        blnAreThereErrors = true;
                        mySubscriber.ResultMessage = "Subscriber code incorrect";
                    }

                    //Check the password is correct
                    bool flag = VerifyHash(SubscriberX.SubscriberPassword, "SHA512", mySubscriber.SubscriberPassword);
                    if (flag != true)
                    {
                        blnAreThereErrors = true;
                        if (mySubscriber.ResultMessage != null)
                        {
                            mySubscriber.ResultMessage += ", Subscriber password is incorrect";
                        }
                        else
                        {
                            mySubscriber.ResultMessage = "Subscriber password is incorrect";
                        }
                    }

                    //Check if the user is enabled
                    if (mySubscriber.SubscriberStatus == "0")
                    {
                        blnAreThereErrors = true;
                        //if (mySubscriber.ResultMessage != null)
                        //{
                        //    mySubscriber.ResultMessage += ", The subscriber is disabled";
                        //}
                        //else
                        //{
                        //    mySubscriber.ResultMessage = "The subscriber is disabled";
                        //}

                        mySubscriber.ResultMessage = "The subscriber is disabled";
                    }
                }
                else
                {
                    blnAreThereErrors = true;
                    mySubscriber.ResultMessage = "The subscriber name does not exist";
                }

                if (blnAreThereErrors == true)
                {
                    mySubscriber.ResultMessage = "Subscriber Authentication failed: " + mySubscriber.ResultMessage;
                }
                else
                {
                    mySubscriber.ResultMessage = "Successful";
                }
            }
            catch (Exception ex)
            {
                mySubscriber.ResultMessage = ex.Message;                
            }
            //finally
            //{
            //    sqlDR.Close();
            //    sqlDR.Dispose();
            //    sqlConnectionX.Close();
            //}

            return mySubscriber;
        }

        public DataTable returnPremium(String subscriberName, String subscriberPassword, String subscriberCode, String productCode, String baseRisk, String riskModifier, String coverValue, String quoteDate, String customerID, String quotationID)
        {
            DataTable dt = new DataTable("Result");
            dt.Columns.Add("loginResult", typeof(string));
            dt.Columns.Add("SubcriberID", typeof(string));

            DataRow dr = dt.NewRow();

            try
            {
                Subscriber SubscriberX = new Subscriber();
                SubscriberX.SubscriberName = subscriberName;
                SubscriberX.SubscriberPassword = subscriberPassword;
                SubscriberX.SubscriberCode = subscriberCode;

                Subscriber Subscriber_sys = Subscriber_Auth(SubscriberX);

                if (Subscriber_sys.ResultMessage == "Subscriber Authentication failed: Subscriber password is incorrect")
                {
                    Subscriber_sys.ResultMessage = "Error: Subscriber Authentication failed: Subscriber password is incorrect";
                    
                    dr["loginResult"] = Subscriber_sys.ResultMessage;
                    if (Subscriber_sys.ResultMessage != "Successful")
                    {
                        dr["SubcriberID"] = DBNull.Value;
                    }
                    dt.Rows.Add(dr);
                }
                else
                {
                    //Check if the Subscriber hass access to the method
                    if (Subscriber_sys.RetrunPremium == true)
                    {
                        #region "Check if the Subscriber has access to the product"
                        bool blnProductAllowed = true;
                        DateTime DTToDate = DateTime.Now;
                        DateTime DTFromDate = DateTime.Now;

                        sqlConnectionX = new SqlConnection(ConfigurationManager.AppSettings["WSSQLConnection"]);
                        sqlConnectionX.Open();

                        sqlCommandX = new SqlCommand();
                        sqlCommandX.Connection = sqlConnectionX;
                        sqlCommandX.CommandType = CommandType.StoredProcedure;
                        sqlCommandX.CommandText = "spx_Pricing_SubscriberProductAccess";

                        sqlParam = new SqlParameter("SubscriberCode", subscriberCode);
                        sqlCommandX.Parameters.Add(sqlParam);
                        sqlParam = new SqlParameter("ProductCode", productCode);
                        sqlCommandX.Parameters.Add(sqlParam);
                        sqlParam = new SqlParameter("quoteDate", quoteDate);
                        sqlCommandX.Parameters.Add(sqlParam);

                        sqlDR = sqlCommandX.ExecuteReader();

                        while (sqlDR.Read())
                        {
                            if (sqlDR.GetValue(0).ToString() == "T")
                            {
                                blnProductAllowed = true;
                            }
                            else
                            {
                                blnProductAllowed = false;
                            }

                            DTFromDate = Convert.ToDateTime(sqlDR.GetValue(1));
                            DTToDate = Convert.ToDateTime(sqlDR.GetValue(2));
                        }

                        sqlDR.Close();
                        sqlDR.Dispose();

                        #endregion

                        if (blnProductAllowed == false)
                        {
                            Subscriber_sys.ResultMessage = "Error: Subscriber does not has access to the product";
                        }
                        else
                        {
                            //check product request is within period parameters
                            if (!(Convert.ToDateTime(quoteDate) > DTFromDate && Convert.ToDateTime(quoteDate) < DTToDate))
                            {
                                Subscriber_sys.ResultMessage = "Error: Product request is outside of period parameters";
                            }
                            else
                            {
                                //Get Premium (and log audit entry)
                                sqlCommandX = new SqlCommand();
                                sqlCommandX.Connection = sqlConnectionX;
                                sqlCommandX.CommandType = CommandType.StoredProcedure;
                                sqlCommandX.CommandText = "spx_PricingReturnPremium";

                                sqlParam = new SqlParameter("SubscriberID", Subscriber_sys.SubscriberID);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("ProductCode", productCode);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("BaseRisk", baseRisk);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("RiskModifierCode", riskModifier);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("CoverValue", coverValue);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("CustomerID", customerID);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("riskQuotationID", quotationID);
                                sqlCommandX.Parameters.Add(sqlParam);
                                //sqlParam = new SqlParameter("quoteDate", quoteDate);
                                //sqlCommandX.Parameters.Add(sqlParam);

                                sqlDR = sqlCommandX.ExecuteReader();
                                DataTable dtResult = new DataTable("Result");
                                dtResult.Load(sqlDR);
                                dt = dtResult;
                            }
                        }
                    }
                    else
                    {
                        if (Subscriber_sys.ResultMessage != "Successful")
                        {
                            Subscriber_sys.ResultMessage += ", subscriber does not have access to method";
                        }
                        else
                        {
                            Subscriber_sys.ResultMessage = "Subscriber Authentication failed: Subscriber does not have access to method";
                        }

                        dr["loginResult"] = Subscriber_sys.ResultMessage;
                        if (Subscriber_sys.ResultMessage != "Successful")
                        {
                            dr["SubcriberID"] = DBNull.Value;
                        }
                        dt.Rows.Add(dr);
                    }
                }

                sqlConnectionX.Close();
                return dt;
            }
            catch (Exception ex)
            {
                //throw;

                dr["loginResult"] = ex.Message;
                dr["SubcriberID"] = DBNull.Value;
                dt.Rows.Add(dr);
                return dt;
            }
            //finally
            //{                
            //    sqlConnectionX.Close();
            //}

        }

        public DataTable returnRisk(String subscriberName, String subscriberPassword, String subscriberCode, String productCode, String baseRisk, String riskModifier, String coverValue, String premiumValue, String quoteDate, String inceptionDate, String effectiveDate, String customerID, String quotationID, String durationModifierCode)
        {
            DataTable dt = new DataTable("Result");
            dt.Columns.Add("loginResult", typeof(string));
            dt.Columns.Add("SubcriberID", typeof(string));

            DataRow dr = dt.NewRow();

            try
            {
                Subscriber SubscriberX = new Subscriber();
                SubscriberX.SubscriberName = subscriberName;
                SubscriberX.SubscriberPassword = subscriberPassword;
                SubscriberX.SubscriberCode = subscriberCode;

                Subscriber Subscriber_sys = Subscriber_Auth(SubscriberX);

                if (Subscriber_sys.ResultMessage == "Subscriber Authentication failed: Subscriber password is incorrect")
                {
                    Subscriber_sys.ResultMessage = "Error: Subscriber Authentication failed: Subscriber password is incorrect";

                    dr["loginResult"] = Subscriber_sys.ResultMessage;
                    if (Subscriber_sys.ResultMessage != "Successful")
                    {
                        dr["SubcriberID"] = DBNull.Value;
                    }
                    dt.Rows.Add(dr);
                }
                else
                {
                    //Check if the Subscriber hass access to the method
                    if (Subscriber_sys.RetrunRisk == true)
                    {

                        #region "Check if the Subscriber has access to the product"
                        bool blnProductAllowed = true;
                        DateTime DTToDate = DateTime.Now;
                        DateTime DTFromDate = DateTime.Now;

                        sqlConnectionX = new SqlConnection(ConfigurationManager.AppSettings["WSSQLConnection"]);
                        sqlConnectionX.Open();

                        sqlCommandX = new SqlCommand();
                        sqlCommandX.Connection = sqlConnectionX;
                        sqlCommandX.CommandType = CommandType.StoredProcedure;
                        sqlCommandX.CommandText = "spx_Pricing_SubscriberProductAccess";

                        sqlParam = new SqlParameter("SubscriberCode", subscriberCode);
                        sqlCommandX.Parameters.Add(sqlParam);
                        sqlParam = new SqlParameter("ProductCode", productCode);
                        sqlCommandX.Parameters.Add(sqlParam);
                        sqlParam = new SqlParameter("quoteDate", quoteDate);
                        sqlCommandX.Parameters.Add(sqlParam);

                        sqlDR = sqlCommandX.ExecuteReader();

                        while (sqlDR.Read())
                        {
                            if (sqlDR.GetValue(0).ToString() == "T")
                            {
                                blnProductAllowed = true;
                            }
                            else
                            {
                                blnProductAllowed = false;
                            }

                            DTFromDate = Convert.ToDateTime(sqlDR.GetValue(1));
                            DTToDate = Convert.ToDateTime(sqlDR.GetValue(2));
                        }

                        sqlDR.Close();
                        sqlDR.Dispose();

                        #endregion

                        if (blnProductAllowed == false)
                        {
                            Subscriber_sys.ResultMessage = "Error: Subscriber does not has access to the product";
                        }
                        else
                        {
                            //check product request is within period parameters
                            if (!(Convert.ToDateTime(quoteDate) > DTFromDate && Convert.ToDateTime(quoteDate) < DTToDate))
                            {
                                Subscriber_sys.ResultMessage = "Error: Product request is outside of period parameters";
                            }
                            else
                            {
                                //Get Premium (and log audit entry)
                                sqlCommandX = new SqlCommand();
                                sqlCommandX.Connection = sqlConnectionX;
                                sqlCommandX.CommandType = CommandType.StoredProcedure;
                                sqlCommandX.CommandText = "spx_PricingReturnRisk";

                                sqlParam = new SqlParameter("SubscriberID", Subscriber_sys.SubscriberID);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("ProductCode", productCode);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("BaseRisk", baseRisk);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("RiskModifierCode", riskModifier);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("CoverValue", coverValue);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("PremiumValue", premiumValue);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("QuoteDate", quoteDate);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("InceptionDate", inceptionDate);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("EffectiveDate", effectiveDate);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("CustomerID", customerID);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("riskQuotationID", quotationID);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("durationModifierCode", durationModifierCode);
                                sqlCommandX.Parameters.Add(sqlParam);

                                sqlDR = sqlCommandX.ExecuteReader();
                                DataTable dtResult = new DataTable("Result");
                                dtResult.Load(sqlDR);
                                dt = dtResult;
                                sqlDR.Close();
                                sqlDR.Dispose();
                            }
                        }
                    }
                    else
                    {
                        if (Subscriber_sys.ResultMessage != "Successful")
                        {
                            Subscriber_sys.ResultMessage += ", subscriber does not have access to method";
                        }
                        else
                        {
                            Subscriber_sys.ResultMessage = "Subscriber Authentication failed: Subscriber does not have access to method";
                        }

                        dr["loginResult"] = Subscriber_sys.ResultMessage;
                        if (Subscriber_sys.ResultMessage != "Successful")
                        {
                            dr["SubcriberID"] = DBNull.Value;
                        }
                        dt.Rows.Add(dr);
                    }
                }

                sqlConnectionX.Close();
                return dt;                
            }
            catch (Exception ex)
            {
                //throw;

                dr["loginResult"] = ex.Message;
                dr["SubcriberID"] = DBNull.Value;
                dt.Rows.Add(dr);
                return dt;
            }
            finally
            {
                sqlConnectionX.Close();
                //System.GC.Collect();
            }

        }

        public DataTable returnCover(String subscriberName, String subscriberPassword, String subscriberCode, String productCode, String baseRisk, String riskModifier, String premiumValue, String quoteDate, String customerID, String quotationID)
        {
            DataTable dt = new DataTable("Result");
            dt.Columns.Add("loginResult", typeof(string));
            dt.Columns.Add("SubcriberID", typeof(string));

            DataRow dr = dt.NewRow();

            try
            {
                Subscriber SubscriberX = new Subscriber();
                SubscriberX.SubscriberName = subscriberName;
                SubscriberX.SubscriberPassword = subscriberPassword;
                SubscriberX.SubscriberCode = subscriberCode;

                Subscriber Subscriber_sys = Subscriber_Auth(SubscriberX);

                if (Subscriber_sys.ResultMessage == "Subscriber Authentication failed: Subscriber password is incorrect")
                {
                    Subscriber_sys.ResultMessage = "Error: Subscriber Authentication failed: Subscriber password is incorrect";

                    dr["loginResult"] = Subscriber_sys.ResultMessage;
                    if (Subscriber_sys.ResultMessage != "Successful")
                    {
                        dr["SubcriberID"] = DBNull.Value;
                    }
                    dt.Rows.Add(dr);
                }
                else
                {
                    //Check if the Subscriber hass access to the method
                    if (Subscriber_sys.RetrunCover == true)
                    {

                        #region "Check if the Subscriber has access to the product"
                        bool blnProductAllowed = true;
                        DateTime DTToDate = DateTime.Now;
                        DateTime DTFromDate = DateTime.Now;

                        sqlConnectionX = new SqlConnection(ConfigurationManager.AppSettings["WSSQLConnection"]);
                        sqlConnectionX.Open();

                        sqlCommandX = new SqlCommand();
                        sqlCommandX.Connection = sqlConnectionX;
                        sqlCommandX.CommandType = CommandType.StoredProcedure;
                        sqlCommandX.CommandText = "spx_Pricing_SubscriberProductAccess";

                        sqlParam = new SqlParameter("SubscriberCode", subscriberCode);
                        sqlCommandX.Parameters.Add(sqlParam);
                        sqlParam = new SqlParameter("ProductCode", productCode);
                        sqlCommandX.Parameters.Add(sqlParam);
                        sqlParam = new SqlParameter("quoteDate", quoteDate);
                        sqlCommandX.Parameters.Add(sqlParam);

                        sqlDR = sqlCommandX.ExecuteReader();

                        while (sqlDR.Read())
                        {
                            if (sqlDR.GetValue(0).ToString() == "T")
                            {
                                blnProductAllowed = true;
                            }
                            else
                            {
                                blnProductAllowed = false;
                            }

                            DTFromDate = Convert.ToDateTime(sqlDR.GetValue(1));
                            DTToDate = Convert.ToDateTime(sqlDR.GetValue(2));
                        }

                        sqlDR.Close();
                        sqlDR.Dispose();

                        #endregion

                        if (blnProductAllowed == false)
                        {
                            Subscriber_sys.ResultMessage = "Error: Subscriber does not has access to the product";
                        }
                        else
                        {
                            //check product request is within period parameters
                            if (!(Convert.ToDateTime(quoteDate) > DTFromDate && Convert.ToDateTime(quoteDate) < DTToDate))
                            {
                                Subscriber_sys.ResultMessage = "Error: Product request is outside of period parameters";
                            }
                            else
                            {
                                //Get Premium (and log audit entry)
                                sqlCommandX = new SqlCommand();
                                sqlCommandX.Connection = sqlConnectionX;
                                sqlCommandX.CommandType = CommandType.StoredProcedure;
                                sqlCommandX.CommandText = "spx_PricingReturnCover";

                                sqlParam = new SqlParameter("SubscriberID", Subscriber_sys.SubscriberID);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("ProductCode", productCode);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("BaseRisk", baseRisk);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("RiskModifierCode", riskModifier);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("PremiumValue", premiumValue);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("CustomerID", customerID);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("riskQuotationID", quotationID);
                                sqlCommandX.Parameters.Add(sqlParam);
                                //sqlParam = new SqlParameter("quoteDate", quoteDate);
                                //sqlCommandX.Parameters.Add(sqlParam);

                                sqlDR = sqlCommandX.ExecuteReader();
                                DataTable dtResult = new DataTable("Result");
                                dtResult.Load(sqlDR);
                                dt = dtResult;
                            }
                        }
                    }
                    else
                    {
                        if (Subscriber_sys.ResultMessage != "Successful")
                        {
                            Subscriber_sys.ResultMessage += ", subscriber does not have access to method";
                        }
                        else
                        {
                            Subscriber_sys.ResultMessage = "Subscriber Authentication failed: Subscriber does not have access to method";
                        }

                        dr["loginResult"] = Subscriber_sys.ResultMessage;
                        if (Subscriber_sys.ResultMessage != "Successful")
                        {
                            dr["SubcriberID"] = DBNull.Value;
                        }
                        dt.Rows.Add(dr);
                    }
                }
                
                sqlConnectionX.Close();

                return dt;
            }
            catch (Exception ex)
            {
                //throw;

                dr["loginResult"] = ex.Message;
                dr["SubcriberID"] = DBNull.Value;
                dt.Rows.Add(dr);
                return dt;
            }
            finally
            {
                sqlConnectionX.Close();
            }
        }

        public DataTable returnEM_Affected_Premium(String subscriberName, String subscriberPassword, String subscriberCode, String productCode, String baseRisk, String riskModifier, String coverValue, String quoteDate, String customerID, String quotationID, String EMloading)
        {
            // EM_Affected_Premium = final premium (without EM loading) + unloaded prem *max( (EM loading – 25),0) / 100
            
            DataTable dt = new DataTable("Result");
            dt.Columns.Add("loginResult", typeof(string));
            dt.Columns.Add("SubcriberID", typeof(string));

            DataRow dr = dt.NewRow();

            try
            {
                Subscriber SubscriberX = new Subscriber();
                SubscriberX.SubscriberName = subscriberName;
                SubscriberX.SubscriberPassword = subscriberPassword;
                SubscriberX.SubscriberCode = subscriberCode;

                Subscriber Subscriber_sys = Subscriber_Auth(SubscriberX);

                if (Subscriber_sys.ResultMessage == "Subscriber Authentication failed: Subscriber password is incorrect")
                {
                    Subscriber_sys.ResultMessage = "Error: Subscriber Authentication failed: Subscriber password is incorrect";

                    dr["loginResult"] = Subscriber_sys.ResultMessage;
                    if (Subscriber_sys.ResultMessage != "Successful")
                    {
                        dr["SubcriberID"] = DBNull.Value;
                    }
                    dt.Rows.Add(dr);
                }
                else
                {
                    //Check if the Subscriber hass access to the method
                    if (Subscriber_sys.RetrunPremium == true)
                    {
                        #region "Check if the Subscriber has access to the product"
                        bool blnProductAllowed = true;
                        DateTime DTToDate = DateTime.Now;
                        DateTime DTFromDate = DateTime.Now;

                        sqlConnectionX = new SqlConnection(ConfigurationManager.AppSettings["WSSQLConnection"]);
                        sqlConnectionX.Open();

                        sqlCommandX = new SqlCommand();
                        sqlCommandX.Connection = sqlConnectionX;
                        sqlCommandX.CommandType = CommandType.StoredProcedure;
                        sqlCommandX.CommandText = "spx_Pricing_SubscriberProductAccess";

                        sqlParam = new SqlParameter("SubscriberCode", subscriberCode);
                        sqlCommandX.Parameters.Add(sqlParam);
                        sqlParam = new SqlParameter("ProductCode", productCode);
                        sqlCommandX.Parameters.Add(sqlParam);
                        sqlParam = new SqlParameter("quoteDate", quoteDate);
                        sqlCommandX.Parameters.Add(sqlParam);

                        sqlDR = sqlCommandX.ExecuteReader();

                        while (sqlDR.Read())
                        {
                            if (sqlDR.GetValue(0).ToString() == "T")
                            {
                                blnProductAllowed = true;
                            }
                            else
                            {
                                blnProductAllowed = false;
                            }

                            DTFromDate = Convert.ToDateTime(sqlDR.GetValue(1));
                            DTToDate = Convert.ToDateTime(sqlDR.GetValue(2));
                        }

                        sqlDR.Close();
                        sqlDR.Dispose();

                        #endregion

                        if (blnProductAllowed == false)
                        {
                            Subscriber_sys.ResultMessage = "Error: Subscriber does not has access to the product";
                        }
                        else
                        {
                            //check product request is within period parameters
                            if (!(Convert.ToDateTime(quoteDate) > DTFromDate && Convert.ToDateTime(quoteDate) < DTToDate))
                            {
                                Subscriber_sys.ResultMessage = "Error: Product request is outside of period parameters";
                            }
                            else
                            {
                                //Get Premium (and log audit entry)
                                sqlCommandX = new SqlCommand();
                                sqlCommandX.Connection = sqlConnectionX;
                                sqlCommandX.CommandType = CommandType.StoredProcedure;
                                sqlCommandX.CommandText = "spx_PricingReturnPremiumEM_Affected";

                                sqlParam = new SqlParameter("SubscriberID", Subscriber_sys.SubscriberID);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("ProductCode", productCode);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("BaseRisk", baseRisk);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("RiskModifierCode", riskModifier);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("CoverValue", coverValue);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("CustomerID", customerID);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("riskQuotationID", quotationID);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("EMloading", EMloading);
                                sqlCommandX.Parameters.Add(sqlParam);

                                sqlDR = sqlCommandX.ExecuteReader();
                                DataTable dtResult = new DataTable("Result");
                                dtResult.Load(sqlDR);
                                dt = dtResult;
                            }
                        }
                    }
                    else
                    {
                        if (Subscriber_sys.ResultMessage != "Successful")
                        {
                            Subscriber_sys.ResultMessage += ", subscriber does not have access to method";
                        }
                        else
                        {
                            Subscriber_sys.ResultMessage = "Subscriber Authentication failed: Subscriber does not have access to method";
                        }

                        dr["loginResult"] = Subscriber_sys.ResultMessage;
                        if (Subscriber_sys.ResultMessage != "Successful")
                        {
                            dr["SubcriberID"] = DBNull.Value;
                        }
                        dt.Rows.Add(dr);
                    }
                }

                sqlConnectionX.Close();
                return dt;
            }
            catch (Exception ex)
            {
                //throw;

                dr["loginResult"] = ex.Message;
                dr["SubcriberID"] = DBNull.Value;
                dt.Rows.Add(dr);
                return dt;
            }
            //finally
            //{                
            //    sqlConnectionX.Close();
            //}

        }

        public DataTable returnEM_Affected_Risk(String subscriberName, String subscriberPassword, String subscriberCode, String productCode, String baseRisk, String riskModifier, String coverValue, String premiumValue, String quoteDate, String inceptionDate, String effectiveDate, String customerID, String quotationID, String durationModifierCode, String EMloading)
        {            
            // EM_Affected_Risk = final risk premium (without EM loading) + unloaded risk premium *max( (EM loading – 25),0) / 100

            DataTable dt = new DataTable("Result");
            dt.Columns.Add("loginResult", typeof(string));
            dt.Columns.Add("SubcriberID", typeof(string));

            DataRow dr = dt.NewRow();

            try
            {
                Subscriber SubscriberX = new Subscriber();
                SubscriberX.SubscriberName = subscriberName;
                SubscriberX.SubscriberPassword = subscriberPassword;
                SubscriberX.SubscriberCode = subscriberCode;

                Subscriber Subscriber_sys = Subscriber_Auth(SubscriberX);

                if (Subscriber_sys.ResultMessage == "Subscriber Authentication failed: Subscriber password is incorrect")
                {
                    Subscriber_sys.ResultMessage = "Error: Subscriber Authentication failed: Subscriber password is incorrect";

                    dr["loginResult"] = Subscriber_sys.ResultMessage;
                    if (Subscriber_sys.ResultMessage != "Successful")
                    {
                        dr["SubcriberID"] = DBNull.Value;
                    }
                    dt.Rows.Add(dr);
                }
                else
                {
                    //Check if the Subscriber hass access to the method
                    if (Subscriber_sys.RetrunRisk == true)
                    {

                        #region "Check if the Subscriber has access to the product"
                        bool blnProductAllowed = true;
                        DateTime DTToDate = DateTime.Now;
                        DateTime DTFromDate = DateTime.Now;

                        sqlConnectionX = new SqlConnection(ConfigurationManager.AppSettings["WSSQLConnection"]);
                        sqlConnectionX.Open();

                        sqlCommandX = new SqlCommand();
                        sqlCommandX.Connection = sqlConnectionX;
                        sqlCommandX.CommandType = CommandType.StoredProcedure;
                        sqlCommandX.CommandText = "spx_Pricing_SubscriberProductAccess";

                        sqlParam = new SqlParameter("SubscriberCode", subscriberCode);
                        sqlCommandX.Parameters.Add(sqlParam);
                        sqlParam = new SqlParameter("ProductCode", productCode);
                        sqlCommandX.Parameters.Add(sqlParam);
                        sqlParam = new SqlParameter("quoteDate", quoteDate);
                        sqlCommandX.Parameters.Add(sqlParam);

                        sqlDR = sqlCommandX.ExecuteReader();

                        while (sqlDR.Read())
                        {
                            if (sqlDR.GetValue(0).ToString() == "T")
                            {
                                blnProductAllowed = true;
                            }
                            else
                            {
                                blnProductAllowed = false;
                            }

                            DTFromDate = Convert.ToDateTime(sqlDR.GetValue(1));
                            DTToDate = Convert.ToDateTime(sqlDR.GetValue(2));
                        }

                        sqlDR.Close();
                        sqlDR.Dispose();

                        #endregion

                        if (blnProductAllowed == false)
                        {
                            Subscriber_sys.ResultMessage = "Error: Subscriber does not has access to the product";
                        }
                        else
                        {
                            //check product request is within period parameters
                            if (!(Convert.ToDateTime(quoteDate) > DTFromDate && Convert.ToDateTime(quoteDate) < DTToDate))
                            {
                                Subscriber_sys.ResultMessage = "Error: Product request is outside of period parameters";
                            }
                            else
                            {
                                //Get Premium (and log audit entry)
                                sqlCommandX = new SqlCommand();
                                sqlCommandX.Connection = sqlConnectionX;
                                sqlCommandX.CommandType = CommandType.StoredProcedure;
                                sqlCommandX.CommandText = "spx_PricingReturnRiskEM_Affected";

                                sqlParam = new SqlParameter("SubscriberID", Subscriber_sys.SubscriberID);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("ProductCode", productCode);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("BaseRisk", baseRisk);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("RiskModifierCode", riskModifier);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("CoverValue", coverValue);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("PremiumValue", premiumValue);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("QuoteDate", quoteDate);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("InceptionDate", inceptionDate);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("EffectiveDate", effectiveDate);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("CustomerID", customerID);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("riskQuotationID", quotationID);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("durationModifierCode", durationModifierCode);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("EMloading", EMloading);
                                sqlCommandX.Parameters.Add(sqlParam);

                                sqlDR = sqlCommandX.ExecuteReader();
                                DataTable dtResult = new DataTable("Result");
                                dtResult.Load(sqlDR);
                                dt = dtResult;
                            }
                        }
                    }
                    else
                    {
                        if (Subscriber_sys.ResultMessage != "Successful")
                        {
                            Subscriber_sys.ResultMessage += ", subscriber does not have access to method";
                        }
                        else
                        {
                            Subscriber_sys.ResultMessage = "Subscriber Authentication failed: Subscriber does not have access to method";
                        }

                        dr["loginResult"] = Subscriber_sys.ResultMessage;
                        if (Subscriber_sys.ResultMessage != "Successful")
                        {
                            dr["SubcriberID"] = DBNull.Value;
                        }
                        dt.Rows.Add(dr);
                    }
                }

                sqlConnectionX.Close();
                return dt;
            }
            catch (Exception ex)
            {
                //throw;

                dr["loginResult"] = ex.Message;
                dr["SubcriberID"] = DBNull.Value;
                dt.Rows.Add(dr);
                return dt;
            }
            //finally
            //{
            //    sqlConnectionX.Close();
            //}

        }

        public DataTable returnEM_Affected_Cover(String subscriberName, String subscriberPassword, String subscriberCode, String productCode, String baseRisk, String riskModifier, String premiumValue, String quoteDate, String customerID, String quotationID, String EMloading)
        {
            // EM_Affected_Cover = Input premium / [(Premium rate) + (Base premium rate) *max( (EM loading – 25),0) / 100]
            DataTable dt = new DataTable("Result");
            dt.Columns.Add("loginResult", typeof(string));
            dt.Columns.Add("SubcriberID", typeof(string));

            DataRow dr = dt.NewRow();

            try
            {
                Subscriber SubscriberX = new Subscriber();
                SubscriberX.SubscriberName = subscriberName;
                SubscriberX.SubscriberPassword = subscriberPassword;
                SubscriberX.SubscriberCode = subscriberCode;

                Subscriber Subscriber_sys = Subscriber_Auth(SubscriberX);

                if (Subscriber_sys.ResultMessage == "Subscriber Authentication failed: Subscriber password is incorrect")
                {
                    Subscriber_sys.ResultMessage = "Error: Subscriber Authentication failed: Subscriber password is incorrect";

                    dr["loginResult"] = Subscriber_sys.ResultMessage;
                    if (Subscriber_sys.ResultMessage != "Successful")
                    {
                        dr["SubcriberID"] = DBNull.Value;
                    }
                    dt.Rows.Add(dr);
                }
                else
                {
                    //Check if the Subscriber hass access to the method
                    if (Subscriber_sys.RetrunCover == true)
                    {

                        #region "Check if the Subscriber has access to the product"
                        bool blnProductAllowed = true;
                        DateTime DTToDate = DateTime.Now;
                        DateTime DTFromDate = DateTime.Now;

                        sqlConnectionX = new SqlConnection(ConfigurationManager.AppSettings["WSSQLConnection"]);
                        sqlConnectionX.Open();

                        sqlCommandX = new SqlCommand();
                        sqlCommandX.Connection = sqlConnectionX;
                        sqlCommandX.CommandType = CommandType.StoredProcedure;
                        sqlCommandX.CommandText = "spx_Pricing_SubscriberProductAccess";

                        sqlParam = new SqlParameter("SubscriberCode", subscriberCode);
                        sqlCommandX.Parameters.Add(sqlParam);
                        sqlParam = new SqlParameter("ProductCode", productCode);
                        sqlCommandX.Parameters.Add(sqlParam);
                        sqlParam = new SqlParameter("quoteDate", quoteDate);
                        sqlCommandX.Parameters.Add(sqlParam);

                        sqlDR = sqlCommandX.ExecuteReader();

                        while (sqlDR.Read())
                        {
                            if (sqlDR.GetValue(0).ToString() == "T")
                            {
                                blnProductAllowed = true;
                            }
                            else
                            {
                                blnProductAllowed = false;
                            }

                            DTFromDate = Convert.ToDateTime(sqlDR.GetValue(1));
                            DTToDate = Convert.ToDateTime(sqlDR.GetValue(2));
                        }

                        sqlDR.Close();
                        sqlDR.Dispose();

                        #endregion

                        if (blnProductAllowed == false)
                        {
                            Subscriber_sys.ResultMessage = "Error: Subscriber does not has access to the product";
                        }
                        else
                        {
                            //check product request is within period parameters
                            if (!(Convert.ToDateTime(quoteDate) > DTFromDate && Convert.ToDateTime(quoteDate) < DTToDate))
                            {
                                Subscriber_sys.ResultMessage = "Error: Product request is outside of period parameters";
                            }
                            else
                            {
                                //Get Premium (and log audit entry)
                                sqlCommandX = new SqlCommand();
                                sqlCommandX.Connection = sqlConnectionX;
                                sqlCommandX.CommandType = CommandType.StoredProcedure;
                                sqlCommandX.CommandText = "spx_PricingReturnCoverEM_Affected";

                                sqlParam = new SqlParameter("SubscriberID", Subscriber_sys.SubscriberID);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("ProductCode", productCode);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("BaseRisk", baseRisk);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("RiskModifierCode", riskModifier);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("PremiumValue", premiumValue);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("CustomerID", customerID);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("riskQuotationID", quotationID);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("EMloading", EMloading);
                                sqlCommandX.Parameters.Add(sqlParam);

                                sqlDR = sqlCommandX.ExecuteReader();
                                DataTable dtResult = new DataTable("Result");
                                dtResult.Load(sqlDR);
                                dt = dtResult;
                            }
                        }
                    }
                    else
                    {
                        if (Subscriber_sys.ResultMessage != "Successful")
                        {
                            Subscriber_sys.ResultMessage += ", subscriber does not have access to method";
                        }
                        else
                        {
                            Subscriber_sys.ResultMessage = "Subscriber Authentication failed: Subscriber does not have access to method";
                        }

                        dr["loginResult"] = Subscriber_sys.ResultMessage;
                        if (Subscriber_sys.ResultMessage != "Successful")
                        {
                            dr["SubcriberID"] = DBNull.Value;
                        }
                        dt.Rows.Add(dr);
                    }
                }

                sqlConnectionX.Close();

                return dt;
            }
            catch (Exception ex)
            {
                //throw;

                dr["loginResult"] = ex.Message;
                dr["SubcriberID"] = DBNull.Value;
                dt.Rows.Add(dr);
                return dt;
            }
            finally
            {
                sqlConnectionX.Close();
            }
        }

        public DataTable returnRiskBand(String subscriberName, String subscriberPassword, String subscriberCode, String productCode, String riskModifier, String quoteDate)
        {
            DataTable dt = new DataTable("Result");
            dt.Columns.Add("loginResult", typeof(string));
            dt.Columns.Add("SubcriberID", typeof(string));

            DataRow dr = dt.NewRow();

            try
            {
                Subscriber SubscriberX = new Subscriber();
                SubscriberX.SubscriberName = subscriberName;
                SubscriberX.SubscriberPassword = subscriberPassword;
                SubscriberX.SubscriberCode = subscriberCode;

                Subscriber Subscriber_sys = Subscriber_Auth(SubscriberX);

                if (Subscriber_sys.ResultMessage == "Subscriber Authentication failed: Subscriber password is incorrect")
                {
                    Subscriber_sys.ResultMessage = "Error: Subscriber Authentication failed: Subscriber password is incorrect";

                    dr["loginResult"] = Subscriber_sys.ResultMessage;
                    if (Subscriber_sys.ResultMessage != "Successful")
                    {
                        dr["SubcriberID"] = DBNull.Value;
                    }
                    dt.Rows.Add(dr);
                }
                else
                {
                    //Check if the Subscriber hass access to the method
                    //if (Subscriber_sys.RetrunCover == true)
                    //{
                        #region "Check if the Subscriber has access to the product"
                        //bool blnProductAllowed = true;
                        //DateTime DTToDate = DateTime.Now;
                        //DateTime DTFromDate = DateTime.Now;

                        //sqlConnectionX = new SqlConnection(ConfigurationManager.AppSettings["WSSQLConnection"]);
                        //sqlConnectionX.Open();

                        //sqlCommandX = new SqlCommand();
                        //sqlCommandX.Connection = sqlConnectionX;
                        //sqlCommandX.CommandType = CommandType.StoredProcedure;
                        //sqlCommandX.CommandText = "spx_Pricing_SubscriberProductAccess";

                        //sqlParam = new SqlParameter("SubscriberCode", subscriberCode);
                        //sqlCommandX.Parameters.Add(sqlParam);
                        //sqlParam = new SqlParameter("ProductCode", productCode);
                        //sqlCommandX.Parameters.Add(sqlParam);
                        //sqlParam = new SqlParameter("quoteDate", quoteDate);
                        //sqlCommandX.Parameters.Add(sqlParam);

                        //sqlDR = sqlCommandX.ExecuteReader();

                        //while (sqlDR.Read())
                        //{
                        //    if (sqlDR.GetValue(0).ToString() == "T")
                        //    {
                        //        blnProductAllowed = true;
                        //    }
                        //    else
                        //    {
                        //        blnProductAllowed = false;
                        //    }

                        //    DTFromDate = Convert.ToDateTime(sqlDR.GetValue(1));
                        //    DTToDate = Convert.ToDateTime(sqlDR.GetValue(2));
                        //}

                        //sqlDR.Close();
                        //sqlDR.Dispose();

                        #endregion

                        //if (blnProductAllowed == false)
                        //{
                        //    Subscriber_sys.ResultMessage = "Error: Subscriber does not has access to the product";
                        //}
                        //else
                        //{
                            ////check product request is within period parameters
                            //if (!(Convert.ToDateTime(quoteDate) > DTFromDate && Convert.ToDateTime(quoteDate) < DTToDate))
                            //{
                            //    Subscriber_sys.ResultMessage = "Error: Product request is outside of period parameters";
                            //}
                            //else
                            //{
                                //Get Premium (and log audit entry)
                                sqlCommandX = new SqlCommand();
                                sqlCommandX.Connection = sqlConnectionX;
                                sqlCommandX.CommandType = CommandType.StoredProcedure;
                                sqlCommandX.CommandText = "spx_PricingReturnRiskBand";

                                sqlParam = new SqlParameter("SubscriberID", Subscriber_sys.SubscriberID);
                                sqlCommandX.Parameters.Add(sqlParam);
                                sqlParam = new SqlParameter("ProductCode", productCode);
                                sqlCommandX.Parameters.Add(sqlParam);                                
                                sqlParam = new SqlParameter("RiskModifierCode", riskModifier);
                                sqlCommandX.Parameters.Add(sqlParam);                               
                                //sqlParam = new SqlParameter("quoteDate", quoteDate);
                                //sqlCommandX.Parameters.Add(sqlParam);

                                sqlDR = sqlCommandX.ExecuteReader();
                                DataTable dtResult = new DataTable("Result");
                                dtResult.Load(sqlDR);
                                dt = dtResult;
                            //}
                        //}
                    //}
                    //else
                    //{
                    //    if (Subscriber_sys.ResultMessage != "Successful")
                    //    {
                    //        Subscriber_sys.ResultMessage += ", subscriber does not have access to method";
                    //    }
                    //    else
                    //    {
                    //        Subscriber_sys.ResultMessage = "Subscriber Authentication failed: Subscriber does not have access to method";
                    //    }

                    //    dr["loginResult"] = Subscriber_sys.ResultMessage;
                    //    if (Subscriber_sys.ResultMessage != "Successful")
                    //    {
                    //        dr["SubcriberID"] = DBNull.Value;
                    //    }
                    //    dt.Rows.Add(dr);
                    //}
                }

                sqlConnectionX.Close();

                return dt;
            }
            catch (Exception ex)
            {
                //throw;

                dr["loginResult"] = ex.Message;
                dr["SubcriberID"] = DBNull.Value;
                dt.Rows.Add(dr);
                return dt;
            }
            finally
            {
                sqlConnectionX.Close();
            }
        }
        
        public DataTable QualifyLife(String subscriberName, String subscriberPassword, String subscriberCode, String AgeNextBirthday, String TobaccoUse, String HbA1cPercent, String BMI, String PantSize, String AlcoholUnitsPerDay, String Occupation)
        {
            DataTable dt = new DataTable("Result");
            dt.Columns.Add("loginResult", typeof(string));
            dt.Columns.Add("SubcriberID", typeof(string));

            DataRow dr = dt.NewRow();

            bool blnLifeAvailable = true;

            try
            {
                Subscriber SubscriberX = new Subscriber();
                SubscriberX.SubscriberName = subscriberName;
                SubscriberX.SubscriberPassword = subscriberPassword;
                SubscriberX.SubscriberCode = subscriberCode;

                Subscriber Subscriber_sys = Subscriber_Auth(SubscriberX);

                if (Subscriber_sys.ResultMessage == "Subscriber Authentication failed: Subscriber password is incorrect")
                {
                    Subscriber_sys.ResultMessage = "Error: Subscriber Authentication failed: Subscriber password is incorrect";

                    dr["loginResult"] = Subscriber_sys.ResultMessage;
                    if (Subscriber_sys.ResultMessage != "Successful")
                    {
                        dr["SubcriberID"] = DBNull.Value;
                    }
                    dt.Rows.Add(dr);
                }
                else
                {
                    //Check if the Subscriber hass access to the method
                    if (Subscriber_sys.RetrunCover == true)
                    {
                        //reset the message
                        Subscriber_sys.ResultMessage = "";

                        if (Convert.ToInt16(AgeNextBirthday) < 18)
                        {
                            blnLifeAvailable = false;
                            if (Subscriber_sys.ResultMessage.Length == 0)
                                Subscriber_sys.ResultMessage = "Age next birthday is below 18";
                            else
                                Subscriber_sys.ResultMessage += ", Age next birthday is below 18";
                        }

                        if (Convert.ToInt16(AgeNextBirthday) > 75)
                        {
                            blnLifeAvailable = false;
                            if (Subscriber_sys.ResultMessage.Length == 0)
                                Subscriber_sys.ResultMessage += "Age next birthday is above 75";
                            else
                                Subscriber_sys.ResultMessage += ", Age next birthday is above 75";
                        }

                        if ((Convert.ToBoolean(TobaccoUse) == true) && (Convert.ToInt16(HbA1cPercent) >= 12))
                        {
                            blnLifeAvailable = false;
                            if (Subscriber_sys.ResultMessage.Length == 0)
                                Subscriber_sys.ResultMessage = "HbA1c is too high (smoker)";
                            else
                                Subscriber_sys.ResultMessage += ", HbA1c is too high (smoker)";
                        }

                        if (Convert.ToInt16(HbA1cPercent) > 14)
                        {
                            blnLifeAvailable = false;
                            if (Subscriber_sys.ResultMessage.Length == 0)
                                Subscriber_sys.ResultMessage = "HbA1c is too high";
                            else
                                Subscriber_sys.ResultMessage += ", HbA1c is too high";
                        }

                        if (BMI != "")
                        {
                            if (Convert.ToDecimal(BMI) > 44)
                            {
                                blnLifeAvailable = false;
                                if (Subscriber_sys.ResultMessage.Length == 0)
                                    Subscriber_sys.ResultMessage = "BMI is too high";
                                else
                                    Subscriber_sys.ResultMessage += ", BMI is too high";
                            }
                        }

                        if (PantSize != "")
                        {
                            if (Convert.ToInt16(PantSize) > 44)
                            {
                                blnLifeAvailable = false;
                                if (Subscriber_sys.ResultMessage.Length == 0)
                                    Subscriber_sys.ResultMessage = "Pant Size is too high";
                                else
                                    Subscriber_sys.ResultMessage += ", Pant Size is too high";
                            }
                        }

                        if (Convert.ToInt16(AlcoholUnitsPerDay) > 5)
                        {
                            blnLifeAvailable = false;
                            if (Subscriber_sys.ResultMessage.Length == 0)
                                Subscriber_sys.ResultMessage = "Alcohol consumption is too high";
                            else
                                Subscriber_sys.ResultMessage += ", Alcohol consumption is too high";  
                        }
                       
                        sqlCommandX = new SqlCommand();
                        sqlCommandX.Connection = sqlConnectionX;
                        sqlCommandX.CommandType = CommandType.StoredProcedure;
                        sqlCommandX.CommandText = "spx_Select_OccupationLimitsByOccupation";

                        sqlParam = new SqlParameter("Occupation", Occupation);
                        sqlCommandX.Parameters.Add(sqlParam);

                        sqlDR = sqlCommandX.ExecuteReader();
                        while (sqlDR.Read())
                        {
                            if (sqlDR.GetBoolean(0) == false)  //sql column 0 = Life
                                if (Subscriber_sys.ResultMessage.Length == 0)
                                    Subscriber_sys.ResultMessage = "Occupation does not allow life cover";
                                else
                                    Subscriber_sys.ResultMessage += ", Occupation does not allow life cover";  
                        }

                        if (Subscriber_sys.ResultMessage.Length == 0)
                            Subscriber_sys.ResultMessage += "Successful";

                        DataTable dt2 = new DataTable("Result");
                        dt2.Columns.Add("Result", typeof(string));

                        DataRow dr2 = dt2.NewRow();

                        dr2["Result"] = Subscriber_sys.ResultMessage;
                        dt2.Rows.Add(dr2);

                        dt = dt2;

                        #region "Old code"
                        #region "Check if the Subscriber has access to the product"
                        //bool blnProductAllowed = true;
                        //DateTime DTToDate = DateTime.Now;
                        //DateTime DTFromDate = DateTime.Now;

                        //sqlConnectionX = new SqlConnection(ConfigurationManager.AppSettings["WSSQLConnection"]);
                        //sqlConnectionX.Open();

                        //sqlCommandX = new SqlCommand();
                        //sqlCommandX.Connection = sqlConnectionX;
                        //sqlCommandX.CommandType = CommandType.StoredProcedure;
                        //sqlCommandX.CommandText = "spx_Pricing_SubscriberProductAccess";

                        //sqlParam = new SqlParameter("SubscriberCode", subscriberCode);
                        //sqlCommandX.Parameters.Add(sqlParam);
                        //sqlParam = new SqlParameter("ProductCode", productCode);
                        //sqlCommandX.Parameters.Add(sqlParam);
                        //sqlParam = new SqlParameter("quoteDate", quoteDate);
                        //sqlCommandX.Parameters.Add(sqlParam);

                        //sqlDR = sqlCommandX.ExecuteReader();

                        //while (sqlDR.Read())
                        //{
                        //    if (sqlDR.GetValue(0).ToString() == "T")
                        //    {
                        //        blnProductAllowed = true;
                        //    }
                        //    else
                        //    {
                        //        blnProductAllowed = false;
                        //    }

                        //    DTFromDate = Convert.ToDateTime(sqlDR.GetValue(1));
                        //    DTToDate = Convert.ToDateTime(sqlDR.GetValue(2));
                        //}

                        //sqlDR.Close();
                        //sqlDR.Dispose();

                        #endregion

                        ////if (blnProductAllowed == false)
                        ////{
                        ////    Subscriber_sys.ResultMessage = "Error: Subscriber does not has access to the product";
                        ////}
                        ////else
                        ////{                      
                        //    //Get Premium (and log audit entry)
                        //    sqlCommandX = new SqlCommand();
                        //    sqlCommandX.Connection = sqlConnectionX;
                        //    sqlCommandX.CommandType = CommandType.StoredProcedure;
                        //    sqlCommandX.CommandText = "spx_PricingReturnCover";

                        //    sqlParam = new SqlParameter("SubscriberID", Subscriber_sys.SubscriberID);
                        //    sqlCommandX.Parameters.Add(sqlParam);
                        //    sqlParam = new SqlParameter("ProductCode", productCode);
                        //    sqlCommandX.Parameters.Add(sqlParam);
                        //    sqlParam = new SqlParameter("BaseRisk", baseRisk);
                        //    sqlCommandX.Parameters.Add(sqlParam);
                        //    sqlParam = new SqlParameter("RiskModifierCode", riskModifier);
                        //    sqlCommandX.Parameters.Add(sqlParam);
                        //    sqlParam = new SqlParameter("PremiumValue", premiumValue);
                        //    sqlCommandX.Parameters.Add(sqlParam);
                        //    sqlParam = new SqlParameter("CustomerID", customerID);
                        //    sqlCommandX.Parameters.Add(sqlParam);
                        //    sqlParam = new SqlParameter("riskQuotationID", quotationID);
                        //    sqlCommandX.Parameters.Add(sqlParam);
                        //    //sqlParam = new SqlParameter("quoteDate", quoteDate);
                        //    //sqlCommandX.Parameters.Add(sqlParam);

                        //    sqlDR = sqlCommandX.ExecuteReader();
                        //    DataTable dtResult = new DataTable("Result");
                        //    dtResult.Load(sqlDR);
                        //    dt = dtResult;                            
                        ////}
                        #endregion
                    }
                    else
                    {
                        if (Subscriber_sys.ResultMessage != "Successful")
                        {
                            Subscriber_sys.ResultMessage += ", subscriber does not have access to method";
                        }
                        else
                        {
                            //Subscriber_sys.ResultMessage = "Subscriber Authentication failed: Subscriber does not have access to method";
                        }

                        dr["loginResult"] = Subscriber_sys.ResultMessage;
                        if (Subscriber_sys.ResultMessage != "Successful")
                        {
                            dr["SubcriberID"] = DBNull.Value;
                        }
                        dt.Rows.Add(dr);
                    }
                }

                sqlConnectionX.Close();

                return dt;
            }
            catch (Exception ex)
            {
                //throw;

                dr["loginResult"] = ex.Message;
                dr["SubcriberID"] = DBNull.Value;
                dt.Rows.Add(dr);
                return dt;
            }
            finally
            {
                sqlConnectionX.Close();
            }
        }

        public DataTable QualifyDisability(String subscriberName, String subscriberPassword, String subscriberCode, String AgeNextBirthday, String TobaccoUse, String HbA1cPercent, String BMI, String PantSize, String AlcoholUnitsPerDay, String Employment, String Qualification, String Income, String SpouseIncome, String RiskBand)
        {
            DataTable dt = new DataTable("Result");
            dt.Columns.Add("loginResult", typeof(string));
            dt.Columns.Add("SubcriberID", typeof(string));

            DataRow dr = dt.NewRow();

            bool blnLifeAvailable = true;

            try
            {
                Subscriber SubscriberX = new Subscriber();
                SubscriberX.SubscriberName = subscriberName;
                SubscriberX.SubscriberPassword = subscriberPassword;
                SubscriberX.SubscriberCode = subscriberCode;

                Subscriber Subscriber_sys = Subscriber_Auth(SubscriberX);

                if (Subscriber_sys.ResultMessage == "Subscriber Authentication failed: Subscriber password is incorrect")
                {
                    Subscriber_sys.ResultMessage = "Error: Subscriber Authentication failed: Subscriber password is incorrect";

                    dr["loginResult"] = Subscriber_sys.ResultMessage;
                    if (Subscriber_sys.ResultMessage != "Successful")
                    {
                        dr["SubcriberID"] = DBNull.Value;
                    }
                    dt.Rows.Add(dr);
                }
                else
                {
                    //Check if the Subscriber hass access to the method
                    if (Subscriber_sys.RetrunCover == true)
                    {
                        //reset the message
                        Subscriber_sys.ResultMessage = "";

                        if (Convert.ToInt16(AgeNextBirthday) < 18)
                        {
                            blnLifeAvailable = false;
                            if (Subscriber_sys.ResultMessage.Length == 0)
                                Subscriber_sys.ResultMessage = "Age next birthday is below 18";
                            else
                                Subscriber_sys.ResultMessage += ", Age next birthday is below 18";
                        }

                        if (Convert.ToInt16(AgeNextBirthday) > 60)
                        {
                            blnLifeAvailable = false;
                            if (Subscriber_sys.ResultMessage.Length == 0)
                                Subscriber_sys.ResultMessage += "Age next birthday is above 60";
                            else
                                Subscriber_sys.ResultMessage += ", Age next birthday is above 60";
                        }

                        if ((Convert.ToBoolean(TobaccoUse) == true) && (Convert.ToInt16(HbA1cPercent) >= 12))
                        {
                            blnLifeAvailable = false;
                            if (Subscriber_sys.ResultMessage.Length == 0)
                                Subscriber_sys.ResultMessage = "HbA1c is too high (smoker)";
                            else
                                Subscriber_sys.ResultMessage += ", HbA1c is too high (smoker)";
                        }

                        if (Convert.ToInt16(HbA1cPercent) > 14)
                        {
                            blnLifeAvailable = false;
                            if (Subscriber_sys.ResultMessage.Length == 0)
                                Subscriber_sys.ResultMessage = "HbA1c is too high";
                            else
                                Subscriber_sys.ResultMessage += ", HbA1c is too high";
                        }

                        if (BMI != "")
                        {
                            if (Convert.ToDecimal(BMI) > 40)
                            {
                                blnLifeAvailable = false;
                                if (Subscriber_sys.ResultMessage.Length == 0)
                                    Subscriber_sys.ResultMessage = "BMI is too high";
                                else
                                    Subscriber_sys.ResultMessage += ", BMI is too high";
                            }
                        }

                        if (PantSize != "")
                        {
                            if (Convert.ToInt16(PantSize) > 40)
                            {
                                blnLifeAvailable = false;
                                if (Subscriber_sys.ResultMessage.Length == 0)
                                    Subscriber_sys.ResultMessage = "Pant Size is too high";
                                else
                                    Subscriber_sys.ResultMessage += ", Pant Size is too high";
                            }
                        }

                        if (Convert.ToInt16(AlcoholUnitsPerDay) > 5)
                        {
                            blnLifeAvailable = false;
                            if (Subscriber_sys.ResultMessage.Length == 0)
                                Subscriber_sys.ResultMessage = "Alcohol consumption is too high";
                            else
                                Subscriber_sys.ResultMessage += ", Alcohol consumption is too high";
                        }

                        if ((Employment.Contains("Unemployed") && SpouseIncome == "0"))
                        {
                            blnLifeAvailable = false;
                            if (Subscriber_sys.ResultMessage.Length == 0)
                                Subscriber_sys.ResultMessage = "Unemployed";
                            else
                                Subscriber_sys.ResultMessage += ", Unemployed";
                        }

                        //sqlCommandX = new SqlCommand();
                        //sqlCommandX.Connection = sqlConnectionX;
                        //sqlCommandX.CommandType = CommandType.StoredProcedure;
                        //sqlCommandX.CommandText = "spx_Select_OccupationLimitsByOccupation";

                        //sqlParam = new SqlParameter("Occupation", Employment);
                        //sqlCommandX.Parameters.Add(sqlParam);

                        //sqlDR = sqlCommandX.ExecuteReader();
                        //while (sqlDR.Read())
                        //{
                        //    if (sqlDR.GetValue(0).ToString() == "0")  //sql column 0 = Life
                        //        if (Subscriber_sys.ResultMessage.Length == 0)
                        //            Subscriber_sys.ResultMessage = "Occupation does not allow life cover";
                        //        else
                        //            Subscriber_sys.ResultMessage += ", Occupation does not allow life cover";
                        //}




                        //if (RiskBand == "Unemployed")
                        //{
                        //    blnLifeAvailable = false;
                        //    if (Subscriber_sys.ResultMessage.Length == 0)
                        //        Subscriber_sys.ResultMessage = "Unemployed";
                        //    else
                        //        Subscriber_sys.ResultMessage += ", Unemployed";
                        //}

                        if (RiskBand == "Silver")
                        {
                            if (Subscriber_sys.ResultMessage.Length == 0)
                                Subscriber_sys.ResultMessage = "Risk band is Silver";
                            else
                                Subscriber_sys.ResultMessage += ", Risk band is Silver";                            
                        }

                        if (RiskBand == "Bronze")
                        {
                            if (Subscriber_sys.ResultMessage.Length == 0)
                                Subscriber_sys.ResultMessage = "Risk band is Bronze";
                            else
                                Subscriber_sys.ResultMessage += ", Risk band is Bronze";     
                        }

                        decimal decIncome = Convert.ToDecimal(Income);
                        int intClass = 0;

                        if ((decIncome >= 0) && (decIncome<10499))
                        {
                            switch (Qualification)
                            {
                                case "No matric":
                                   intClass = 4;
                                   break;
                                case "Matric":
                                   intClass = 4;
                                   break;
                                case "3 or 4 yr. Diploma/3 yr. Degree":
                                   intClass = 3;
                                   break;
                                case "4 yr. Degree/professional qualification":
                                   intClass = 1;
                                   break;
                            }
                        }

                        if ((decIncome >= 10500) && (decIncome < 15749))
                        {
                            switch (Qualification)
                            {
                                case "No matric":
                                    intClass = 4;
                                    break;
                                case "Matric":
                                    intClass = 3;
                                    break;
                                case "3 or 4 yr. Diploma/3 yr. Degree":
                                    intClass = 2;
                                    break;
                                case "4 yr. Degree/professional qualification":
                                    intClass = 1;
                                    break;
                            }
                        }

                        if ((decIncome >= 15750) && (decIncome < 26249))
                        {
                            switch (Qualification)
                            {
                                case "No matric":
                                    intClass = 3;
                                    break;
                                case "Matric":
                                    intClass = 2;
                                    break;
                                case "3 or 4 yr. Diploma/3 yr. Degree":
                                    intClass = 1;
                                    break;
                                case "4 yr. Degree/professional qualification":
                                    intClass = 1;
                                    break;
                            }
                        }

                        if ((decIncome >= 26250) && (decIncome < 41999))
                        {
                            switch (Qualification)
                            {
                                case "No matric":
                                    intClass = 2;
                                    break;
                                case "Matric":
                                    intClass = 2;
                                    break;
                                case "3 or 4 yr. Diploma/3 yr. Degree":
                                    intClass = 1;
                                    break;
                                case "4 yr. Degree/professional qualification":
                                    intClass = 1;
                                    break;
                            }
                        }

                        if (decIncome >= 42000)
                        {
                            switch (Qualification)
                            {
                                case "No matric":
                                    intClass = 2;
                                    break;
                                case "Matric":
                                    intClass = 1;
                                    break;
                                case "3 or 4 yr. Diploma/3 yr. Degree":
                                    intClass = 1;
                                    break;
                                case "4 yr. Degree/professional qualification":
                                    intClass = 1;
                                    break;
                            }
                        }
                        

                        if (Subscriber_sys.ResultMessage.Length == 0)
                            Subscriber_sys.ResultMessage += "Successful";

                        DataTable dt2 = new DataTable("Result");
                        dt2.Columns.Add("Result", typeof(string));
                        dt2.Columns.Add("Class", typeof(string));

                        DataRow dr2 = dt2.NewRow();

                        dr2["Result"] = Subscriber_sys.ResultMessage;
                        dr2["Class"] = intClass;
                        dt2.Rows.Add(dr2);

                        dt = dt2;                        

                        #region "Old code"
                        #region "Check if the Subscriber has access to the product"
                        //bool blnProductAllowed = true;
                        //DateTime DTToDate = DateTime.Now;
                        //DateTime DTFromDate = DateTime.Now;

                        //sqlConnectionX = new SqlConnection(ConfigurationManager.AppSettings["WSSQLConnection"]);
                        //sqlConnectionX.Open();

                        //sqlCommandX = new SqlCommand();
                        //sqlCommandX.Connection = sqlConnectionX;
                        //sqlCommandX.CommandType = CommandType.StoredProcedure;
                        //sqlCommandX.CommandText = "spx_Pricing_SubscriberProductAccess";

                        //sqlParam = new SqlParameter("SubscriberCode", subscriberCode);
                        //sqlCommandX.Parameters.Add(sqlParam);
                        //sqlParam = new SqlParameter("ProductCode", productCode);
                        //sqlCommandX.Parameters.Add(sqlParam);
                        //sqlParam = new SqlParameter("quoteDate", quoteDate);
                        //sqlCommandX.Parameters.Add(sqlParam);

                        //sqlDR = sqlCommandX.ExecuteReader();

                        //while (sqlDR.Read())
                        //{
                        //    if (sqlDR.GetValue(0).ToString() == "T")
                        //    {
                        //        blnProductAllowed = true;
                        //    }
                        //    else
                        //    {
                        //        blnProductAllowed = false;
                        //    }

                        //    DTFromDate = Convert.ToDateTime(sqlDR.GetValue(1));
                        //    DTToDate = Convert.ToDateTime(sqlDR.GetValue(2));
                        //}

                        //sqlDR.Close();
                        //sqlDR.Dispose();

                        #endregion

                        ////if (blnProductAllowed == false)
                        ////{
                        ////    Subscriber_sys.ResultMessage = "Error: Subscriber does not has access to the product";
                        ////}
                        ////else
                        ////{                      
                        //    //Get Premium (and log audit entry)
                        //    sqlCommandX = new SqlCommand();
                        //    sqlCommandX.Connection = sqlConnectionX;
                        //    sqlCommandX.CommandType = CommandType.StoredProcedure;
                        //    sqlCommandX.CommandText = "spx_PricingReturnCover";

                        //    sqlParam = new SqlParameter("SubscriberID", Subscriber_sys.SubscriberID);
                        //    sqlCommandX.Parameters.Add(sqlParam);
                        //    sqlParam = new SqlParameter("ProductCode", productCode);
                        //    sqlCommandX.Parameters.Add(sqlParam);
                        //    sqlParam = new SqlParameter("BaseRisk", baseRisk);
                        //    sqlCommandX.Parameters.Add(sqlParam);
                        //    sqlParam = new SqlParameter("RiskModifierCode", riskModifier);
                        //    sqlCommandX.Parameters.Add(sqlParam);
                        //    sqlParam = new SqlParameter("PremiumValue", premiumValue);
                        //    sqlCommandX.Parameters.Add(sqlParam);
                        //    sqlParam = new SqlParameter("CustomerID", customerID);
                        //    sqlCommandX.Parameters.Add(sqlParam);
                        //    sqlParam = new SqlParameter("riskQuotationID", quotationID);
                        //    sqlCommandX.Parameters.Add(sqlParam);
                        //    //sqlParam = new SqlParameter("quoteDate", quoteDate);
                        //    //sqlCommandX.Parameters.Add(sqlParam);

                        //    sqlDR = sqlCommandX.ExecuteReader();
                        //    DataTable dtResult = new DataTable("Result");
                        //    dtResult.Load(sqlDR);
                        //    dt = dtResult;                            
                        ////}
                        #endregion
                    }
                    else
                    {
                        if (Subscriber_sys.ResultMessage != "Successful")
                        {
                            Subscriber_sys.ResultMessage += ", subscriber does not have access to method";
                        }
                        else
                        {
                            //Subscriber_sys.ResultMessage = "Subscriber Authentication failed: Subscriber does not have access to method";
                        }

                        dr["loginResult"] = Subscriber_sys.ResultMessage;
                        if (Subscriber_sys.ResultMessage != "Successful")
                        {
                            dr["SubcriberID"] = DBNull.Value;
                        }
                        dt.Rows.Add(dr);
                    }
                }

                sqlConnectionX.Close();

                return dt;
            }
            catch (Exception ex)
            {
                //throw;

                dr["loginResult"] = ex.Message;
                dr["SubcriberID"] = DBNull.Value;
                dt.Rows.Add(dr);
                return dt;
            }
            finally
            {
                sqlConnectionX.Close();
            }
        }

        public decimal calculedMax(decimal a, decimal b)
        {
            decimal c = a - b;
            if (c < 0)
            {
                return 0;
            }
            else
            {
                return c;
            }
        }
    }
}

