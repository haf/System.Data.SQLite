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

    public sealed partial class SQLiteMembership : MembershipProvider
    {
        private bool _initialized = false;
        private Object _InitLock = new Object();
        public override void Initialize(string name, NameValueCollection config)
        {
            bool te = _initialized;
            if (te)
                return;
            
            lock (_InitLock)
            {

                if (config == null)
                    throw new ArgumentNullException("config");

                if (name == null || name.Length == 0)
                    name = "SQLiteMembershipProvider";

                if (String.IsNullOrEmpty(config["description"]))
                {
                    config.Remove("description");
                    config.Add("description", "SQLite Membership provider");
                }

                // Initialize the abstract base class.
                base.Initialize(name, config);


                _MaxInvalidPasswordAttempts = ConfigAsInt32(config["maxInvalidPasswordAttempts"], 5);
                _PasswordAttemptWindow = ConfigAsInt32(config["passwordAttemptWindow"], 10);
                _MinRequiredNonAlphanumericCharacters = ConfigAsInt32(config["minRequiredNonAlphanumericCharacters"], 0);
                _MinRequiredPasswordLength = ConfigAsInt32(config["minRequiredPasswordLength"], 7);
                _PasswordStrengthRegularExpression = ConfigAsString(config["passwordStrengthRegularExpression"], "");
                _EnablePasswordReset = ConfigAsBoolean(config["enablePasswordReset"], true);
                _EnablePasswordRetrieval = ConfigAsBoolean(config["enablePasswordRetrieval"], false);
                _RequiresQuestionAndAnswer = ConfigAsBoolean(config["requiresQuestionAndAnswer"], false);
                _RequiresUniqueEmail = ConfigAsBoolean(config["requiresUniqueEmail"], true);


                string temp_format = Convert.ToString(ConfigAsString(config["passwordFormat"], "Hashed"));
                try
                {
                    _PasswordFormat = (MembershipPasswordFormat)Enum.Parse(typeof(MembershipPasswordFormat), temp_format);
                }
                catch
                {
                    throw new ProviderException("Invalid Password Format.");
                }


                _WriteExceptionsToEventLog = ProviderUtility.GetExceptionDesitination(config["writeExceptionsToEventLog"]);
                connectionString = ProviderUtility.GetConnectionString(config["connectionStringName"]);
                ApplicationName = ProviderUtility.GetApplicationName(config["applicationName"]);


                // Get encryption and decryption key information from the configuration.
                Configuration cfg =
                  WebConfigurationManager.OpenWebConfiguration(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
                machineKey = (MachineKeySection)cfg.GetSection("system.web/machineKey");

                if (machineKey.ValidationKey.Contains("AutoGenerate"))
                    if (PasswordFormat != MembershipPasswordFormat.Clear)
                        throw new ProviderException("Hashed or Encrypted passwords are not supported with auto-generated keys.");
                _initialized = true;
            }

        }


        //
        // A helper function to retrieve config values from the configuration file.
        //

        private string ConfigAsString(string configValue, string defaultValue)
        {
            if (String.IsNullOrEmpty(configValue))
                return defaultValue;

            return configValue;
        }

        private bool ConfigAsBoolean(string configValue, bool defaultValue){
            if (String.IsNullOrEmpty(configValue))
                return defaultValue;

            return Convert.ToBoolean(configValue);
        }

        private Int32 ConfigAsInt32(string configValue, int defaultValue)
        {
            if (String.IsNullOrEmpty(configValue))
                return defaultValue;

            return Convert.ToInt32(configValue);
        }

    }
}