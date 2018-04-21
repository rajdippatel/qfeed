using System.Net;

namespace IQFEED.Client
{
    /// <summary>
    /// IQConnectConfig class to hold connection related configuration.
    /// </summary>
    class IQConnectConfig
    {
        public string CustomerProductId { get; set; } = "enter your ID here";
        public string LoginID { get; set; } = "enter your login here";
        public string Password { get; set; } = "enter your password here";
        public string ProductVersion { get; set; } = "0.1";

        public string StartArguments
        {
            get
            {
                return string.Format("‑product {0} ‑version {1} ‑login {2} ‑password {3} ‑autoconnect ‑savelogininfo", CustomerProductId, ProductVersion, LoginID, Password);
            }
        }

        /// <summary>
        /// IQConnect server address
        /// </summary>
        public IPAddress ServerAddress { get; set; } = IPAddress.Parse("127.0.0.1");
        /// <summary>
        /// IQConnect server port
        /// </summary>
        public int ServerPort { get; set; } = 5009;
    }
}
