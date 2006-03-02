using System.Web.Security;
using System.Configuration.Provider;
using System.Collections.Specialized;
using System;
using System.Data;
using System.Data.SQLite;
using System.Configuration;
using System.Diagnostics;
using System.Web;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Web.Configuration;




namespace SQLiteProvider
{

    public sealed partial class SQLiteMembershipProvider : MembershipProvider
    {
        private Object _appLock = new Object();
        public override string ApplicationName
        {
            get { return _ApplicationName; }
            set
            {
                lock (_appLock)
                {
                    _ApplicationName = value;
                    _AppID = ProviderUtility.GetApplicationID(connectionString, value);
                }
            }
        }
        public override bool EnablePasswordReset
        {
            get { return _EnablePasswordReset; }
        }


        public override bool EnablePasswordRetrieval
        {
            get { return _EnablePasswordRetrieval; }
        }


        public override bool RequiresQuestionAndAnswer
        {
            get { return _RequiresQuestionAndAnswer; }
        }


        public override bool RequiresUniqueEmail
        {
            get { return _RequiresUniqueEmail; }
        }


        public override int MaxInvalidPasswordAttempts
        {
            get { return _MaxInvalidPasswordAttempts; }
        }


        public override int PasswordAttemptWindow
        {
            get { return _PasswordAttemptWindow; }
        }


        public override MembershipPasswordFormat PasswordFormat
        {
            get { return _PasswordFormat; }
        }

        private int _MinRequiredNonAlphanumericCharacters;

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return _MinRequiredNonAlphanumericCharacters; }
        }

        private int _MinRequiredPasswordLength;

        public override int MinRequiredPasswordLength
        {
            get { return _MinRequiredPasswordLength; }
        }

        private string _PasswordStrengthRegularExpression;

        public override string PasswordStrengthRegularExpression
        {
            get { return _PasswordStrengthRegularExpression; }
        }
    }
}