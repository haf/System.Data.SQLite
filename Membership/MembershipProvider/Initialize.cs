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
        public override void Initialize(string name, NameValueCollection config)
        {
            
            //
            // Initialize values from web.config.
            //

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


            _MaxInvalidPasswordAttempts = Convert.ToInt32(GetConfigValue(config["maxInvalidPasswordAttempts"], "5"));
            _PasswordAttemptWindow = Convert.ToInt32(GetConfigValue(config["passwordAttemptWindow"], "10"));
            _MinRequiredNonAlphanumericCharacters = Convert.ToInt32(GetConfigValue(config["minRequiredNonAlphanumericCharacters"], "1"));
            _MinRequiredPasswordLength = Convert.ToInt32(GetConfigValue(config["minRequiredPasswordLength"], "7"));
            _PasswordStrengthRegularExpression = Convert.ToString(GetConfigValue(config["passwordStrengthRegularExpression"], ""));
            _EnablePasswordReset = Convert.ToBoolean(GetConfigValue(config["enablePasswordReset"], "true"));
            _EnablePasswordRetrieval = Convert.ToBoolean(GetConfigValue(config["enablePasswordRetrieval"], "false"));
            _RequiresQuestionAndAnswer = Convert.ToBoolean(GetConfigValue(config["requiresQuestionAndAnswer"], "false"));
            _RequiresUniqueEmail = Convert.ToBoolean(GetConfigValue(config["requiresUniqueEmail"], "true"));
            
            string temp_format = config["passwordFormat"];
            if (temp_format == null)
            {
                temp_format = "Hashed";
            }

            switch (temp_format)
            {
                case "Hashed":
                    _PasswordFormat = MembershipPasswordFormat.Hashed;
                    break;
                case "Encrypted":
                    _PasswordFormat = MembershipPasswordFormat.Encrypted;
                    break;
                case "Clear":
                    _PasswordFormat = MembershipPasswordFormat.Clear;
                    break;
                default:
                    throw new ProviderException("Password format not supported.");
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
                    throw new ProviderException("Hashed or Encrypted passwords " +
                                                "are not supported with auto-generated keys.");

            // Application Name has to be last since we use it to get our AppID
            this.ApplicationName = GetConfigValue(config["applicationName"],
                                System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
        }


        //
        // A helper function to retrieve config values from the configuration file.
        //

        private string GetConfigValue(string configValue, string defaultValue)
        {
            if (String.IsNullOrEmpty(configValue))
                return defaultValue;

            return configValue;
        }
    }
}