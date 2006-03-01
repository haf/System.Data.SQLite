using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace SQLiteProvider
{
    class SQLiteSiteMapProvider : StaticSiteMapProvider
    {
        SiteMapNode _root;
        Dictionary<long, SiteMapNode> _nodes = new Dictionary<long, SiteMapNode>();
        #region CommonProviderComponents
        private string eventSource = "SQLiteSiteMapProvider";
        private string connectionString;

        private bool _WriteExceptionsToEventLog = false;
        public bool WriteExceptionsToEventLog
        {
            get { return _WriteExceptionsToEventLog; }
            set { _WriteExceptionsToEventLog = value; }
        }

        private string _ApplicationName;
        private long _AppID;
        public string ApplicationName
        {
            get { return _ApplicationName; }
            set
            {
                _ApplicationName = value;
                _AppID = ProviderUtility.GetApplicationID(connectionString, value);
            }
        }

        private bool _initialized = false;
        public virtual bool IsInitialized
        {
            get { return _initialized; }
        }
        #endregion

        public override void Initialize(string name, NameValueCollection config)
        {

            lock (this)
            {
                if (_initialized)
                    return;

                //
                // Initialize values from web.config.
                //

                if (config == null)
                    throw new ArgumentNullException("config");

                if (name == null || name.Length == 0)
                    name = "SQLiteSiteMapProvider";

                if (String.IsNullOrEmpty(config["description"]))
                {
                    config.Remove("description");
                    config.Add("description", "SQLite SiteMap Privider");
                }

                /*
                if (!String.IsNullOrEmpty(config["updateFileName"]))
                {
                    _validationFileName = HttpContext.Current.Server.MapPath(String.Format("~/App_Data/{0}", config["updateFileName"]));
                    if (!File.Exists(_validationFileName))
                    {
                        File.Create(_validationFileName).Close();
                    }
                    _validationDate = File.GetLastWriteTime(_validationFileName);

                    FileSystemWatcher watcher = new FileSystemWatcher();
                    watcher.Changed += new FileSystemEventHandler(this.OnSiteMapChanged);
                    watcher.EnableRaisingEvents = true;

                }*/
                // Initialize the abstract base class.
                base.Initialize(name, config);

                _WriteExceptionsToEventLog = ProviderUtility.GetExceptionDesitination(config["writeExceptionsToEventLog"]);
                connectionString = ProviderUtility.GetConnectionString(config["connectionStringName"]);
                ApplicationName = ProviderUtility.GetApplicationName(config["applicationName"]);

            }


        }

        public override SiteMapNode BuildSiteMap()
        {
            lock (this)
            {
                if (_root != null)
                    return _root;

                SQLiteConnection conn = new SQLiteConnection(connectionString);
                SQLiteCommand cmd = new SQLiteCommand(SiteMapSql.GetNodes, conn);
                cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;

                long ID;
                string title;
                string description;
                string url;
                long parent;

                SiteMapNode n = null;
                SiteMapNode parentNode = null;

                try
                {
                    conn.Open();
                    SQLiteDataReader r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        ID = r.GetInt64(0);
                        
                        title = r.IsDBNull(1) ? null : r.GetString(1).Trim();
                        description = r.IsDBNull(2) ? null : r.GetString(2).Trim();
                        url = r.IsDBNull(3) ? null : r.GetString(3).Trim();
                        _root = new SiteMapNode(this, ID.ToString(), url, title, description);
                        _root.Roles = GetNodeRoles(ID, conn);
                        base.AddNode(_root, null);
                        _nodes.Add(ID, _root);

                    }
                    while (r.Read())
                    {
                        ID = r.GetInt64(0);

                        title = r.IsDBNull(1) ? null : r.GetString(1).Trim();
                        description = r.IsDBNull(2) ? null : r.GetString(2).Trim();
                        url = r.IsDBNull(3) ? null : r.GetString(3).Trim();
                        parent = r.GetInt64(4);

                        parentNode = GetParent(parent);
                        n = new SiteMapNode(this, ID.ToString(), url, title, description);
                        n.Roles = GetNodeRoles(ID, conn);

                        base.AddNode(n, parentNode);
                        _nodes.Add(ID, n);
                    }

                }
                catch (Exception ex)
                {
                    ProviderUtility.HandleException(ex, eventSource, "BuildSiteMap", _WriteExceptionsToEventLog);
                }
                finally
                {
                    conn.Close();
                }
                return _root;
            }

        }

        protected override SiteMapNode GetRootNodeCore()
        {
            lock (this)
            {
                BuildSiteMap();
                return _root;
            }
        }

        private SiteMapNode GetParent(long pid)
        {
            if (!_nodes.ContainsKey(pid))
                throw new System.Configuration.Provider.ProviderException("Invalid Parent ID");
            return _nodes[pid];
        }

        private IList GetNodeRoles(long ID, SQLiteConnection conn)
        {
            SQLiteCommand cmd = new SQLiteCommand(SiteMapSql.GetNodeRoles, conn);
            cmd.Parameters.Add("$AppID", DbType.Int64).Value = _AppID;
            cmd.Parameters.Add("$NodeID", DbType.Int64).Value = ID;
            ArrayList result = new ArrayList();

            SQLiteDataReader r = cmd.ExecuteReader();
            while (r.Read())
            {
                result.Add(r.GetString(0));
            }
            return (IList)result;
        }
    }
}
