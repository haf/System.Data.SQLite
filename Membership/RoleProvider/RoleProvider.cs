using System.Web.Security;
using System.Configuration.Provider;
using System.Collections.Specialized;
using System.Collections.Generic;
using System;
using System.Data;
using System.Data.SQLite;
using System.Configuration;
using System.Diagnostics;
using System.Web;
using System.Globalization;



namespace SQLiteProvider
{

    public sealed partial class SQLiteRoleProvider : RoleProvider
    {


        private string eventSource = "SQLiteRoleProvider";

        private string connectionString;
        private bool _WriteExceptionsToEventLog = false;
        private string _ApplicationName;
        private long _AppID;

        private bool _initialized = false;
        private object _InitLock = new Object();
        private object _AppLock = new Object();

        public bool WriteExceptionsToEventLog
        {
            get { return _WriteExceptionsToEventLog; }
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            bool TempInit = _initialized;
            if (_initialized)
                return;

            lock (_InitLock)
            {

                if (config == null)
                    throw new ArgumentNullException("config");

                if (name == null || name.Length == 0)
                    name = "SQLiteRoleProvider";

                if (String.IsNullOrEmpty(config["description"]))
                {
                    config.Remove("description");
                    config.Add("description", "SQLite Role Privider");
                }

                // Initialize the abstract base class.
                base.Initialize(name, config);
                _WriteExceptionsToEventLog = ProviderUtility.GetExceptionDesitination(config["writeExceptionsToEventLog"]);
                connectionString = ProviderUtility.GetConnectionString(config["connectionStringName"]);
                ApplicationName = ProviderUtility.GetApplicationName(config["applicationName"]);

                _initialized = true;
            }
        }
        public override string ApplicationName
        {
            get { return _ApplicationName; }
            set
            {
                lock (_AppLock)
                {
                    _ApplicationName = value;
                    _AppID = ProviderUtility.GetApplicationID(connectionString, value);
                }
            }
        }
        public override void AddUsersToRoles(string[] usernames, string[] rolenames)
        {
            foreach (string rolename in rolenames)
            {
                if (!RoleExists(rolename))
                {
                    throw new ProviderException("Role name not found.");
                }
            }

            foreach (string username in usernames)
            {
                foreach (string rolename in rolenames)
                {
                    if (IsUserInRole(username, rolename))
                    {
                        throw new ProviderException("User is already in role.");
                    }
                }
            }


            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(RoleSql.AddUserToRole, conn);

            SQLiteParameter userParm = cmd.Parameters.Add("$Username", DbType.String);
            SQLiteParameter roleParm = cmd.Parameters.Add("$Rolename", DbType.String);
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            SQLiteTransaction tran = null;

            try
            {
                conn.Open();
                tran = conn.BeginTransaction();
                cmd.Transaction = tran;

                foreach (string username in usernames)
                {
                    foreach (string rolename in rolenames)
                    {
                        userParm.Value = username;
                        roleParm.Value = rolename;
                        cmd.ExecuteNonQuery();
                    }
                }

                tran.Commit();
            }
            catch (SQLiteException e)
            {
                try
                {
                    tran.Rollback();
                }
                catch { }

                ProviderUtility.HandleException(e, eventSource, "AddUsersToRoles", WriteExceptionsToEventLog);

            }
            finally
            {
                conn.Close();
            }
        }
        public override void CreateRole(string rolename)
        {
            if (RoleExists(rolename))
            {
                throw new ProviderException("Role name already exists.");
            }

            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(RoleSql.CreateRole, conn);

            cmd.Parameters.Add("$Rolename", DbType.String).Value = rolename;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            try
            {
                conn.Open();

                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "CreateRole", WriteExceptionsToEventLog);

            }
            finally
            {
                conn.Close();
            }
        }
        public override bool DeleteRole(string rolename, bool throwOnPopulatedRole)
        {
            if (!RoleExists(rolename))
            {
                throw new ProviderException("Role does not exist.");
            }

            if (throwOnPopulatedRole && GetUsersInRole(rolename).Length > 0)
            {
                throw new ProviderException("Cannot delete a populated role.");
            }

            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(RoleSql.DeleteRole, conn);

            cmd.Parameters.Add("$Rolename", DbType.String).Value = rolename;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;


            SQLiteCommand cmd2 = new SQLiteCommand(RoleSql.DeleteRoleFromMap, conn);

            cmd2.Parameters.Add("$Rolename", DbType.String).Value = rolename;
            cmd2.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            SQLiteTransaction tran = null;

            try
            {
                conn.Open();
                tran = conn.BeginTransaction();
                cmd.Transaction = tran;
                cmd2.Transaction = tran;

                cmd2.ExecuteNonQuery();
                cmd.ExecuteNonQuery();

                tran.Commit();
            }
            catch (SQLiteException e)
            {
                try
                {
                    tran.Rollback();
                }
                catch { }

                ProviderUtility.HandleException(e, eventSource, "DeleteRole", WriteExceptionsToEventLog);
            }
            finally
            {
                conn.Close();
            }

            return true;
        }
        public override string[] GetAllRoles()
        {
            List<String> names = new List<string>();
            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(RoleSql.GetAllRoles, conn);

            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            SQLiteDataReader reader = null;

            try
            {
                conn.Open();

                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    names.Add(reader.GetString(0));
                }
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "GetAllRoles", WriteExceptionsToEventLog);
            }
            finally
            {
                if (reader != null) { reader.Close(); }
                conn.Close();
            }


            return names.ToArray();
        }
        public override string[] GetRolesForUser(string username)
        {
            List<string> roles = new List<string>();

            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(RoleSql.GetRolesForUser, conn);

            cmd.Parameters.Add("$Username", DbType.String).Value = username;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            SQLiteDataReader reader = null;

            try
            {
                conn.Open();

                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    roles.Add(reader.GetString(0));
                }
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "GetRolesForUser", WriteExceptionsToEventLog);
            }
            finally
            {
                if (reader != null) { reader.Close(); }
                conn.Close();
            }


            return roles.ToArray();
        }
        public override string[] GetUsersInRole(string rolename)
        {
            List<String> users = new List<string>();

            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(RoleSql.GetUsersInRole, conn);

            cmd.Parameters.Add("$Rolename", DbType.String).Value = rolename;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            SQLiteDataReader reader = null;

            try
            {
                conn.Open();

                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(reader.GetString(0));
                }
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "GetUsersInRole", WriteExceptionsToEventLog);
            }
            finally
            {
                if (reader != null) { reader.Close(); }
                conn.Close();
            }
            return users.ToArray();
        }
        public override bool IsUserInRole(string username, string rolename)
        {
            long count = 0;

            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(RoleSql.IsUserInRole, conn);

            cmd.Parameters.Add("$Username", DbType.String).Value = username;
            cmd.Parameters.Add("$Rolename", DbType.String).Value = rolename;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            try
            {
                conn.Open();
                count = (long)cmd.ExecuteScalar();
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "IsUserInRole", WriteExceptionsToEventLog);
            }
            finally
            {
                conn.Close();
            }

            return (count != 0);
        }
        public override void RemoveUsersFromRoles(string[] usernames, string[] rolenames)
        {
            foreach (string rolename in rolenames)
            {
                if (!RoleExists(rolename))
                {
                    throw new ProviderException("Role name not found.");
                }
            }

            foreach (string username in usernames)
            {
                foreach (string rolename in rolenames)
                {
                    if (!IsUserInRole(username, rolename))
                    {
                        throw new ProviderException("User is not in role.");
                    }
                }
            }


            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(RoleSql.DeleteUserFromRole, conn);

            SQLiteParameter userParm = cmd.Parameters.Add("$Username", DbType.String);
            SQLiteParameter roleParm = cmd.Parameters.Add("$Rolename", DbType.String);
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            SQLiteTransaction tran = null;

            try
            {
                conn.Open();
                tran = conn.BeginTransaction();
                cmd.Transaction = tran;

                foreach (string username in usernames)
                {
                    foreach (string rolename in rolenames)
                    {
                        userParm.Value = username;
                        roleParm.Value = rolename;
                        cmd.ExecuteNonQuery();
                    }
                }

                tran.Commit();
            }
            catch (SQLiteException e)
            {
                try
                {
                    tran.Rollback();
                }
                catch { }
                ProviderUtility.HandleException(e, eventSource, "RemoveUsersFromRoles", WriteExceptionsToEventLog);

            }
            finally
            {
                conn.Close();
            }
        }
        public override bool RoleExists(string rolename)
        {
            long count = 0;

            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(RoleSql.RoleExists, conn);

            cmd.Parameters.Add("$Rolename", DbType.String).Value = rolename;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            try
            {
                conn.Open();
                count = (long)cmd.ExecuteScalar();
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "RoleExists", WriteExceptionsToEventLog);


            }
            finally
            {
                conn.Close();
            }

            return (count != 0);
        }
        public override string[] FindUsersInRole(string rolename, string usernameToMatch)
        {
            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(RoleSql.FindUsersInRole, conn);
            cmd.Parameters.Add("$Username", DbType.String).Value = usernameToMatch;
            cmd.Parameters.Add("$Rolename", DbType.String).Value = rolename;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            List<String> users = new List<string>();
            SQLiteDataReader reader = null;

            try
            {
                conn.Open();

                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(reader.GetString(0));
                }
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "FindUsersInRole", WriteExceptionsToEventLog);                

            }
            finally
            {
                if (reader != null) { reader.Close(); }

                conn.Close();
            }

            return users.ToArray();
        }

    }
}


