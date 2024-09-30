using System;
using System.Xml;

namespace MyOrderMaster.Models
{
    public static class User
    {
        public static string ID { get; set; }
        public static string Password { get; set; }
        public static string FutureAccount { get; set; }
        public static string StockAccount { get; set; }

        private const string userPath = @"Assets\user.xml";

        public static void GetUserInfo()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(userPath);

            XmlNode nodeId = doc.SelectSingleNode("/user/loginID");
            ID = nodeId.InnerText.Trim();
            
            XmlNode nodePassword = doc.SelectSingleNode("/user/password");
            Password = nodePassword.InnerText.Trim();
        }

        public static void SaveUserInfo()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(userPath);

            XmlNode nodeId = doc.SelectSingleNode("/user/loginID");
            XmlNode nodePassword = doc.SelectSingleNode("/user/password");

            nodeId.InnerText = ID;
            nodePassword.InnerText = Password;

            doc.Save(userPath);
        }
    }
}
