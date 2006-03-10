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

namespace SQLiteProvider
{
    internal class ProviderUtility
    {

        public static void HandleException(Exception e, string source, string action, bool logging)
        {
            if (logging)
            {
                EventLog log = new EventLog();
                log.Source = source;
                log.Log = "Application";

                string message = "An exception occurred communicating with the data source.\n\n";
                message += "Action: " + action + "\n\n";
                message += "Exception: " + e.ToString();

                log.WriteEntry(message);
                throw new ProviderException("An exception occurred. Please check the Event Log.");
            }
            else
            {
                string msg = String.Format("An exception occured during {0} in {1}. \n Message:{2}", action, source, e.Message );
                throw new ProviderException(msg, e);
            }
        }
        public static long GetApplicationID(String ConnString, string AppName)
        {
            long AppID = 0;
            SQLiteConnection conn = new SQLiteConnection(ConnString);
            SQLiteCommand existsCmd = new SQLiteCommand(ApplicationSql.AppExists, conn);
            SQLiteCommand idCmd = new SQLiteCommand(ApplicationSql.GetAppID, conn);
            SQLiteCommand insertCmd = new SQLiteCommand(ApplicationSql.InsertApp, conn);

            existsCmd.Parameters.Add("$ApplicationName", DbType.String).Value = AppName;
            idCmd.Parameters.Add("$ApplicationName", DbType.String).Value = AppName;
            insertCmd.Parameters.Add("$ApplicationName", DbType.String).Value = AppName;
            try
            {
                conn.Open();
                if (((long)existsCmd.ExecuteScalar()) == 0)
                {
                    insertCmd.ExecuteNonQuery();
                }
                AppID = (long)idCmd.ExecuteScalar();

            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, "Utility", "ApplicationName Property", false);
            }
            finally
            {
                conn.Close();
            }
            return AppID;
        }

        public static string GetConnectionString(string csName)
        {
            string cs = "";
            ConnectionStringSettings css = ConfigurationManager.ConnectionStrings[csName];

            if (css == null || css.ConnectionString.Trim() == "")
            {
                // use default location, creating the DB if need be
                throw new ProviderException("Connection string cannot be blank.");
            }
            else
            {
                cs = css.ConnectionString;
            }
            return cs;
        }
        public static string GetApplicationName(string appName)
        {
            return (String.IsNullOrEmpty(appName)? System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath:appName);
        }
        public static bool GetExceptionDesitination(string exToLog)
        {
            bool res = false;
            if (!String.IsNullOrEmpty(exToLog) && exToLog.ToUpper() == "TRUE")
            {
                res = true;
            }
            return res;
        }
    }
}
