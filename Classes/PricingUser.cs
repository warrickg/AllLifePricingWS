using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllLifePricing.Classes
{
    public class PricingUser
    {
        private int userID;
        public int UserID
        {
            get { return userID; }
            set { userID = value; }
        }

        private string username;
        public string Username
        {
            get { return username; }
            set { username = value; }
        }

        private string password;
        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        private string result;
        public string Result
        {
            get { return result; }
            set { result = value; }
        }
    }
}
