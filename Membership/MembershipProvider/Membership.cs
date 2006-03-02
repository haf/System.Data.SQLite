using System.Web.Security;
using System.Web.Profile;
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

        //
        // Global connection string, generated password length, generic exception message, event log info.
        //

        private int newPasswordLength = 8;
        private string eventSource = "SQLiteMembership";

        private string connectionString;
        private bool _WriteExceptionsToEventLog;
        private MachineKeySection machineKey;
        private string _ApplicationName;
        private long _AppID;
        private bool _EnablePasswordReset;
        private bool _EnablePasswordRetrieval;
        private bool _RequiresQuestionAndAnswer;
        private bool _RequiresUniqueEmail;
        private int _MaxInvalidPasswordAttempts;
        private int _PasswordAttemptWindow;
        private MembershipPasswordFormat _PasswordFormat;
     

        public bool WriteExceptionsToEventLog
        {
            get { return _WriteExceptionsToEventLog; }
            set { _WriteExceptionsToEventLog = value; }
        }






        public override bool ChangePassword(string username, string oldPwd, string newPwd)
        {
            if (!ValidateUser(username, oldPwd))
                return false;


            ValidatePasswordEventArgs args =
              new ValidatePasswordEventArgs(username, newPwd, true);

            OnValidatingPassword(args);

            if (args.Cancel)
                if (args.FailureInformation != null)
                    throw args.FailureInformation;
                else
                    throw new MembershipPasswordException("Change password canceled due to new password validation failure.");

            
            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(MembershipSql.ChangePassword , conn);

             
            cmd.Parameters.Add("$Password", DbType.String).Value = EncodePassword(newPwd);
            cmd.Parameters.Add("$Username", DbType.String).Value = username;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            int rowsAffected = 0;

            try
            {
                conn.Open();

                rowsAffected = cmd.ExecuteNonQuery();
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "ChangePassword", WriteExceptionsToEventLog);

            }
            finally
            {
                conn.Close();
            }

            if (rowsAffected > 0)
            {
                return true;
            }

            return false;
        }
        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPwdQuestion, string newPwdAnswer)
        {
            if (!ValidateUser(username, password))
                return false;

            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(MembershipSql.ChangePasswordQA, conn);

            cmd.Parameters.Add("$Question", DbType.String).Value = newPwdQuestion;
            cmd.Parameters.Add("$Answer", DbType.String).Value = EncodePassword(newPwdAnswer);
            cmd.Parameters.Add("$Username", DbType.String).Value = username;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;


            int rowsAffected = 0;

            try
            {
                conn.Open();

                rowsAffected = cmd.ExecuteNonQuery();
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "ChangePasswordQuestionAndAnswer", WriteExceptionsToEventLog);
            }
            finally
            {
                conn.Close();
            }

            if (rowsAffected > 0)
            {
                return true;
            }

            return false;
        }
        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            ValidatePasswordEventArgs args =
              new ValidatePasswordEventArgs(username, password, true);

            OnValidatingPassword(args);

            if (args.Cancel)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }



            if (RequiresUniqueEmail && GetUserNameByEmail(email) != "")
            {
                status = MembershipCreateStatus.DuplicateEmail;
                return null;
            }

            MembershipUser u = GetUser(username, false);

            if (u == null)
            {
                DateTime createDate = DateTime.Now;

                if (providerUserKey != null)
                {
                    status = MembershipCreateStatus.InvalidProviderUserKey;
                    return null;
                }

                SQLiteConnection conn = new SQLiteConnection(connectionString);
                SQLiteCommand cmd = new SQLiteCommand(MembershipSql.CreateUser, conn);

                cmd.Parameters.Add("$Username", DbType.String).Value = username;
                cmd.Parameters.Add("$Password", DbType.String).Value = EncodePassword(password);
                cmd.Parameters.Add("$Email", DbType.String).Value = email;
                cmd.Parameters.Add("$PasswordQuestion", DbType.String).Value = passwordQuestion;
                cmd.Parameters.Add("$PasswordAnswer", DbType.String).Value = EncodePassword(passwordAnswer);
                cmd.Parameters.Add("$IsApproved", DbType.Boolean).Value = isApproved;
                cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;
                cmd.Parameters.Add("$IsLockedOut", DbType.Boolean).Value = false;

                try
                {
                    conn.Open();

                    int recAdded = cmd.ExecuteNonQuery();

                    if (recAdded > 0)
                    {
                        status = MembershipCreateStatus.Success;
                    }
                    else
                    {
                        status = MembershipCreateStatus.UserRejected;
                    }
                }
                catch (SQLiteException e)
                {
                    try
                    {
                        ProviderUtility.HandleException(e, eventSource, "CreateUser", WriteExceptionsToEventLog);
                    }
                    catch { }


                    status = MembershipCreateStatus.ProviderError;
                }
                finally
                {
                    conn.Close();
                }


                return GetUser(username, false);
            }
            else
            {
                status = MembershipCreateStatus.DuplicateUserName;
            }


            return null;
        }
        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(MembershipSql.DeleteUser , conn);

            cmd.Parameters.Add("$Username", DbType.String).Value = username;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            int rowsAffected = 0;

            try
            {
                if (deleteAllRelatedData)
                {
                    Roles.RemoveUserFromRoles(username, Roles.GetRolesForUser(username));
                    
                    // Process commands to delete all data for the user in the database.
                }
                conn.Open();
                rowsAffected = cmd.ExecuteNonQuery();


            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "DeleteUser", WriteExceptionsToEventLog);
                    
            }
            finally
            {
                conn.Close();
            }

            if (rowsAffected > 0)
                return true;

            return false;
        }
        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(MembershipSql.GetAppUsers, conn);
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;
            cmd.Parameters.Add("$Count", DbType.Int32).Value = pageSize;
            cmd.Parameters.Add("$Skip", DbType.Int32).Value = pageSize * pageIndex;
            MembershipUserCollection users = new MembershipUserCollection();
            SQLiteDataReader r = null;

            int recordCount = 0;

            try
            {
                conn.Open();
                r = cmd.ExecuteReader();
                while (r.Read())
                {
                    users.Add(this.GetUserFromReader(r));
                    recordCount++;
                }
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "GetAllUsers", WriteExceptionsToEventLog);
                
            }
            finally
            {
                totalRecords = recordCount;
                if (r != null) { r.Close(); }

                conn.Close();
            }
            return users;
        }
        public override int GetNumberOfUsersOnline()
        {
            TimeSpan onlineSpan = new TimeSpan(0, System.Web.Security.Membership.UserIsOnlineTimeWindow, 0);
            DateTime compareTime = DateTime.Now.Subtract(onlineSpan);

            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(MembershipSql.GetUsersOnline , conn);

            cmd.Parameters.Add("$CompareDate", DbType.DateTime).Value = compareTime;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            int numOnline = 0;

            try
            {
                conn.Open();

                numOnline = (int)cmd.ExecuteScalar();
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "GetNumberOfUsersOnline", WriteExceptionsToEventLog);
                    
            }
            finally
            {
                conn.Close();
            }

            return numOnline;
        }
        public override string GetPassword(string username, string answer)
        {
            if (!EnablePasswordRetrieval)
            {
                throw new ProviderException("Password Retrieval Not Enabled.");
            }

            if (PasswordFormat == MembershipPasswordFormat.Hashed)
            {
                throw new ProviderException("Cannot retrieve Hashed passwords.");
            }

            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(MembershipSql.GetPassword , conn);

            cmd.Parameters.Add("$Username", DbType.String).Value = username;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            string password = "";
            string passwordAnswer = "";
            SQLiteDataReader reader = null;

            try
            {
                conn.Open();

                reader = cmd.ExecuteReader(CommandBehavior.SingleRow);

                if (reader.HasRows)
                {
                    reader.Read();

                    if (reader.GetBoolean(2))
                        throw new MembershipPasswordException("The supplied user is locked out.");

                    password = reader.GetString(0);
                    passwordAnswer = reader.GetString(1);
                }
                else
                {
                    throw new MembershipPasswordException("The supplied user name is not found.");
                }
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "GetPassword", WriteExceptionsToEventLog);
                    
            }
            finally
            {
                if (reader != null) { reader.Close(); }
                conn.Close();
            }


            if (RequiresQuestionAndAnswer && !CheckPassword(answer, passwordAnswer))
            {
                UpdateFailureCount(username, "passwordAnswer");

                throw new MembershipPasswordException("Incorrect password answer.");
            }


            if (PasswordFormat == MembershipPasswordFormat.Encrypted)
            {
                password = UnEncodePassword(password);
            }

            return password;
        }
        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            SQLiteConnection conn = new SQLiteConnection(connectionString);

            string sql = "Select Count(*) from User where Username = $Username AND AppID = $AppID;";
            SQLiteCommand userExistsCmd = new SQLiteCommand(sql,conn );
            SQLiteCommand cmd = new SQLiteCommand(MembershipSql.GetUserByName , conn);
            SQLiteCommand updateCmd = new SQLiteCommand(MembershipSql.UpdateUserAccessTimeByName, conn);

            userExistsCmd.Parameters.Add("$Username", DbType.String).Value = username;
            userExistsCmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            updateCmd.Parameters.Add("$Username", DbType.String).Value = username;
            updateCmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;
            
            cmd.Parameters.Add("$Username", DbType.String).Value = username;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            MembershipUser u = null;
            SQLiteDataReader reader = null;

            try
            {
                conn.Open();

                Object o = userExistsCmd.ExecuteScalar();

                long count = (o == DBNull.Value ? 0 : (long)o);


                reader = cmd.ExecuteReader();
                reader.Read();
                if (count != 0)
                {
                    u = GetUserFromReader(reader);

                    if (userIsOnline)
                    {
                        updateCmd.ExecuteNonQuery();
                    }
                }

            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "GetUser(String, Boolean)", WriteExceptionsToEventLog);

                    
            }
            finally
            {
                if (reader != null) { reader.Close(); }

                conn.Close();
            }

            return u;
        }
        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(MembershipSql.GetUserByID , conn);
            SQLiteCommand updateCmd = new SQLiteCommand(MembershipSql.UpdateAccessTimeByID, conn);


            updateCmd.Parameters.Add("$UserID", DbType.Int64).Value = providerUserKey;
            cmd.Parameters.Add("$UserID", DbType.Int64).Value = providerUserKey;

            MembershipUser u = null;
            SQLiteDataReader reader = null;

            try
            {
                conn.Open();

                reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    reader.Read();
                    u = GetUserFromReader(reader);

                    if (userIsOnline)
                    {

                        updateCmd.ExecuteNonQuery();
                    }
                }

            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "GetUser(Object, Boolean)", WriteExceptionsToEventLog);

                    

            }
            finally
            {
                if (reader != null) { reader.Close(); }

                conn.Close();
            }

            return u;
        }
        private MembershipUser GetUserFromReader(SQLiteDataReader reader)
        {
            object providerUserKey = reader.GetValue(0);
            string username = reader.GetString(1);
            string email = reader.GetString(2);
            string passwordQuestion = (reader.GetValue(3) != DBNull.Value ? reader.GetString(3) : "");
            string comment = (reader.GetValue(4) != DBNull.Value ? reader.GetString(4) : "");
            bool isApproved = reader.GetBoolean(5);
            bool isLockedOut = reader.GetBoolean(6);
            DateTime creationDate = reader.GetDateTime(7);
            DateTime lastLoginDate = (reader.GetValue(8) != DBNull.Value ? reader.GetDateTime(8) : new DateTime() );
            DateTime lastActivityDate = reader.GetDateTime(9);
            DateTime lastPasswordChangedDate = reader.GetDateTime(10);
            DateTime lastLockedOutDate = (reader.GetValue(11) != DBNull.Value ? reader.GetDateTime(11) : new DateTime() );
            
            MembershipUser u = new MembershipUser(this.Name,
                                                  username,
                                                  providerUserKey,
                                                  email,
                                                  passwordQuestion,
                                                  comment,
                                                  isApproved,
                                                  isLockedOut,
                                                  creationDate,
                                                  lastLoginDate,
                                                  lastActivityDate,
                                                  lastPasswordChangedDate,
                                                  lastLockedOutDate);

            return u;
        }
        public override bool UnlockUser(string username)
        {
            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(MembershipSql.UnlockUser , conn);

            
            cmd.Parameters.Add("$Username", DbType.String).Value = username;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            int rowsAffected = 0;

            try
            {
                conn.Open();

                rowsAffected = cmd.ExecuteNonQuery();
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "UnlockUser", WriteExceptionsToEventLog);
                    
            }
            finally
            {
                conn.Close();
            }

            if (rowsAffected > 0)
                return true;

            return false;
        }
        public override string GetUserNameByEmail(string email)
        {
            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(MembershipSql.GetUserNameByEmail , conn);

            cmd.Parameters.Add("$Email", DbType.String).Value = email;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            string username = "";

            try
            {
                conn.Open();
                Object o = cmd.ExecuteScalar();
                username = (o == DBNull.Value ? "" : (string)o);
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "GetUserNameByEmail", WriteExceptionsToEventLog);
                    
            }
            finally
            {
                conn.Close();
            }

            if (username == null)
                username = "";

            return username;
        }
        public override string ResetPassword(string username, string answer)
        {
            if (!EnablePasswordReset)
            {
                throw new NotSupportedException("Password reset is not enabled.");
            }

            if (answer == null && RequiresQuestionAndAnswer)
            {
                UpdateFailureCount(username, "passwordAnswer");

                throw new ProviderException("Password answer required for password reset.");
            }

            string newPassword =
              System.Web.Security.Membership.GeneratePassword(newPasswordLength, MinRequiredNonAlphanumericCharacters);


            ValidatePasswordEventArgs args =
              new ValidatePasswordEventArgs(username, newPassword, true);

            OnValidatingPassword(args);

            if (args.Cancel)
                if (args.FailureInformation != null)
                    throw args.FailureInformation;
                else
                    throw new MembershipPasswordException("Reset password canceled due to password validation failure.");


            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(MembershipSql.QueryPasswordReset , conn);
            SQLiteCommand updateCmd = new SQLiteCommand(MembershipSql.ResetPassword, conn);


            cmd.Parameters.Add("$Username", DbType.String).Value = username;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            updateCmd.Parameters.Add("$Password", DbType.String).Value = EncodePassword(newPassword);
            updateCmd.Parameters.Add("$Username", DbType.String).Value = username;
            updateCmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;


            int rowsAffected = 0;
            string passwordAnswer = "";
            SQLiteDataReader reader = null;

            try
            {
                conn.Open();

                reader = cmd.ExecuteReader(CommandBehavior.SingleRow);

                if (reader.HasRows)
                {
                    reader.Read();

                    if (reader.GetBoolean(1))
                        throw new MembershipPasswordException("The supplied user is locked out.");

                    passwordAnswer = reader.GetString(0);
                }
                else
                {
                    throw new MembershipPasswordException("The supplied user name is not found.");
                }

                if (RequiresQuestionAndAnswer && !CheckPassword(answer, passwordAnswer))
                {
                    UpdateFailureCount(username, "passwordAnswer");

                    throw new MembershipPasswordException("Incorrect password answer.");
                }

                rowsAffected = updateCmd.ExecuteNonQuery();
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "ResetPassword", WriteExceptionsToEventLog);

            }
            finally
            {
                if (reader != null) { reader.Close(); }
                conn.Close();
            }

            if (rowsAffected > 0)
            {
                return newPassword;
            }
            else
            {
                throw new MembershipPasswordException("User not found, or user is locked out. Password not Reset.");
            }
        }
        public override void UpdateUser(MembershipUser user)
        {
            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(MembershipSql.UpdateUser, conn);

            cmd.Parameters.Add("$Email", DbType.String).Value = user.Email;
            cmd.Parameters.Add("$Comment", DbType.String).Value = user.Comment;
            cmd.Parameters.Add("$IsApproved", DbType.Boolean).Value = user.IsApproved;
            cmd.Parameters.Add("$Username", DbType.String).Value = user.UserName;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;


            try
            {
                conn.Open();

                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "UpdateUser", WriteExceptionsToEventLog);
                
            }
            finally
            {
                conn.Close();
            }
        }
        public override bool ValidateUser(string username, string password)
        {
            bool isValid = false;

            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(MembershipSql.ValidateUser , conn);
            SQLiteCommand updateCmd = new SQLiteCommand(MembershipSql.UpdateLastLoginDate, conn);


            updateCmd.Parameters.Add("$Username", DbType.String).Value = username;
            updateCmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID; 

            cmd.Parameters.Add("$Username", DbType.String).Value = username;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            SQLiteDataReader reader = null;
            bool isApproved = false;
            string pwd = "";

            try
            {
                conn.Open();

                reader = cmd.ExecuteReader(CommandBehavior.SingleRow);

                if (reader.HasRows)
                {
                    reader.Read();
                    pwd = reader.GetString(0);
                    isApproved = reader.GetBoolean(1);
                }
                else
                {
                    return false;
                }

                reader.Close();

                if (CheckPassword(password, pwd))
                {
                    if (isApproved)
                    {
                        isValid = true;
                        updateCmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    conn.Close();

                    UpdateFailureCount(username, "password");
                }
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "ValidateUser", WriteExceptionsToEventLog);
                    
            }
            finally
            {
                if (reader != null) { reader.Close(); }
                conn.Close();
            }

            return isValid;
        }
        private void UpdateFailureCount(string username, string failureType)
        {
            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand queryCmd = new SQLiteCommand(MembershipSql.QueryFailureCount , conn);
            SQLiteCommand updateCmd = new SQLiteCommand();
            SQLiteCommand lockoutCmd = new SQLiteCommand(MembershipSql.LockOutUser, conn);


            updateCmd.Connection = conn;
            updateCmd.Parameters.Add("$Username", DbType.String).Value = username;
            updateCmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;
            updateCmd.Parameters.Add("$Count", DbType.Int32);

            queryCmd.Parameters.Add("$Username", DbType.String).Value = username;
            queryCmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;
            
            lockoutCmd.Parameters.Add("$IsLockedOut", DbType.Boolean).Value = true;
            lockoutCmd.Parameters.Add("$Username", DbType.String).Value = username;
            lockoutCmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

            SQLiteDataReader reader = null;
            DateTime windowStart = new DateTime();
            int failureCount = 0;

            try
            {
                conn.Open();

                reader = queryCmd.ExecuteReader(CommandBehavior.SingleRow);

                if (reader.HasRows)
                {
                    reader.Read();

                    if (failureType == "password")
                    {
                        failureCount = reader.GetInt32(0);
                        windowStart = reader.GetDateTime(1);
                    }

                    if (failureType == "passwordAnswer")
                    {
                        failureCount = reader.GetInt32(2);
                        windowStart = reader.GetDateTime(3);
                    }
                }

                reader.Close();

                DateTime windowEnd = windowStart.AddMinutes(PasswordAttemptWindow);

                if (failureCount == 0 || DateTime.Now > windowEnd)
                {
                    // First password failure or outside of PasswordAttemptWindow. 
                    // Start a new password failure count from 1 and a new window starting now.

                    if (failureType == "password")
                        updateCmd.CommandText = MembershipSql.UpdatePasswordFailureCountStart;

                    if (failureType == "passwordAnswer")
                        updateCmd.CommandText = MembershipSql.UpdateAnswerFailureCountStart;

                    updateCmd.Parameters["$Count"].Value = 1;

                    if (updateCmd.ExecuteNonQuery() < 0)
                        throw new ProviderException("Unable to update failure count and window start.");
                }
                else
                {
                    if (failureCount++ >= MaxInvalidPasswordAttempts)
                    {

                        if (lockoutCmd.ExecuteNonQuery() < 0)
                            throw new ProviderException("Unable to lock out user.");
                    }
                    else
                    {
                        // Password attempts have not exceeded the failure threshold. Update
                        // the failure counts. Leave the window the same.

                        if (failureType == "password")
                            updateCmd.CommandText = MembershipSql.UpdatePasswordFailureCount;

                        if (failureType == "passwordAnswer")
                            updateCmd.CommandText = MembershipSql.UpdateAnswerFailureCount;

                        updateCmd.Parameters["$Count"].Value = failureCount;


                        if (updateCmd.ExecuteNonQuery() < 0)
                            throw new ProviderException("Unable to update failure count.");
                    }
                }
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "UpdateFailureCount", WriteExceptionsToEventLog);

                    
            }
            finally
            {
                if (reader != null) { reader.Close(); }
                conn.Close();
            }
        }
        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {

            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(MembershipSql.FindUsersByName, conn);

            MembershipUserCollection users = new MembershipUserCollection();
            SQLiteDataReader r = null;
            cmd.Parameters.Add("$UsernameSearch", DbType.String).Value = usernameToMatch;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;
            cmd.Parameters.Add("$Count", DbType.Int32).Value = pageSize;
            cmd.Parameters.Add("$Skip", DbType.Int32).Value = pageIndex * pageSize;

            int recordCount = 0;

            try
            {
                conn.Open();
                r = cmd.ExecuteReader();
                while (r.Read())
                {
                    users.Add(this.GetUserFromReader(r));
                    recordCount++;
                }
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "FindUsersByName", WriteExceptionsToEventLog);
                
            }
            finally
            {
                totalRecords = recordCount;
                if (r != null) { r.Close(); }

                conn.Close();
            }
            return users;
        }
        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {

            
            SQLiteConnection conn = new SQLiteConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand(MembershipSql.FindUsersByEmail, conn);

            MembershipUserCollection users = new MembershipUserCollection();
            SQLiteDataReader r = null;
            cmd.Parameters.Add("$EmailSearch", DbType.String).Value = emailToMatch;
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;
            cmd.Parameters.Add("$Count", DbType.Int32).Value = pageSize;
            cmd.Parameters.Add("$Skip", DbType.Int32).Value = pageIndex * pageSize;

            int recordCount = 0;

            try
            {
                conn.Open();
                r = cmd.ExecuteReader();
                while (r.Read())
                {
                    users.Add(this.GetUserFromReader(r));
                    recordCount++;
                }
            }
            catch (SQLiteException e)
            {
                ProviderUtility.HandleException(e, eventSource, "FindUsersByEmail", WriteExceptionsToEventLog);
                
            }
            finally
            {
                totalRecords = recordCount;
                if (r != null) { r.Close(); }

                conn.Close();
            }
            return users;


        }



    }
}