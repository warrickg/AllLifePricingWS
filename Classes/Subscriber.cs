using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllLifePricing.Classes
{
    public class Subscriber
    {
        private int subscriberID;
        public int SubscriberID
        {
            get { return subscriberID; }
            set { subscriberID = value; }
        }

        private string subscriberName;
        public string SubscriberName
        {
            get { return subscriberName; }
            set { subscriberName = value; }
        }

        private string subscriberPassword;
        public string SubscriberPassword
        {
            get { return subscriberPassword; }
            set { subscriberPassword = value; }
        }

        private string subscriberCode;
        public string SubscriberCode
        {
            get { return subscriberCode; }
            set { subscriberCode = value; }
        }

        private string subscriberStatus;
        public string SubscriberStatus
        {
            get { return subscriberStatus; }
            set { subscriberStatus = value; }
        }

        private bool retrunRisk;
        public bool RetrunRisk
        {
            get { return retrunRisk; }
            set { retrunRisk = value; }
        }

        private bool retrunPremium;
        public bool RetrunPremium
        {
            get { return retrunPremium; }
            set { retrunPremium = value; }
        }

        private bool retrunCover;
        public bool RetrunCover
        {
            get { return retrunCover; }
            set { retrunCover = value; }
        }

        private string resultMessage;
        public string ResultMessage
        {
            get { return resultMessage; }
            set { resultMessage = value; }
        }
    }
}
