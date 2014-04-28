/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/

#if USE_ENTITY_FRAMEWORK_6
namespace System.Data.SQLite.EF6
#else
namespace System.Data.SQLite.Linq
#endif
{
    using System;
    using System.Data.Common;

#if USE_ENTITY_FRAMEWORK_6
    using System.Data.Entity.Core.Common;
#endif

    /// <summary>
    /// SQLite implementation of <see cref="DbProviderFactory" />.
    /// </summary>
    public sealed class SQLiteProviderFactory :
        DbProviderFactory, IServiceProvider, IDisposable
    {
        #region Public Static Data
        /// <summary>
        /// Static instance member which returns an instanced
        /// <see cref="SQLiteProviderFactory" /> class.
        /// </summary>
        public static readonly SQLiteProviderFactory Instance =
            new SQLiteProviderFactory();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        public SQLiteProviderFactory()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Data.Common.DbProviderFactory Overrides
        /// <summary>
        /// Creates and returns a new <see cref="SQLiteCommand" /> object.
        /// </summary>
        /// <returns>The new object.</returns>
        public override DbCommand CreateCommand()
        {
            CheckDisposed();
            return new SQLiteCommand();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates and returns a new <see cref="SQLiteCommandBuilder" /> object.
        /// </summary>
        /// <returns>The new object.</returns>
        public override DbCommandBuilder CreateCommandBuilder()
        {
            CheckDisposed();
            return new SQLiteCommandBuilder();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates and returns a new <see cref="SQLiteConnection" /> object.
        /// </summary>
        /// <returns>The new object.</returns>
        public override DbConnection CreateConnection()
        {
            CheckDisposed();
            return new SQLiteConnection();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates and returns a new <see cref="SQLiteConnectionStringBuilder" />
        /// object.
        /// </summary>
        /// <returns>The new object.</returns>
        public override DbConnectionStringBuilder CreateConnectionStringBuilder()
        {
            CheckDisposed();
            return new SQLiteConnectionStringBuilder();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates and returns a new <see cref="SQLiteDataAdapter" /> object.
        /// </summary>
        /// <returns>The new object.</returns>
        public override DbDataAdapter CreateDataAdapter()
        {
            CheckDisposed();
            return new SQLiteDataAdapter();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates and returns a new <see cref="SQLiteParameter" /> object.
        /// </summary>
        /// <returns>The new object.</returns>
        public override DbParameter CreateParameter()
        {
            CheckDisposed();
            return new SQLiteParameter();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IServiceProvider Members
        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType">
        /// An object that specifies the type of service object to get.
        /// </param>
        /// <returns>
        /// A service object of type serviceType -OR- a null reference if
        /// there is no service object of type serviceType.
        /// </returns>
        public object GetService(
            Type serviceType
            )
        {
            if ((serviceType == typeof(ISQLiteSchemaExtensions)) ||
                (serviceType == typeof(DbProviderServices)))
            {
                return SQLiteProviderServices.Instance;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed)
            {
                throw new ObjectDisposedException(
                    typeof(SQLiteProviderFactory).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                //if (disposing)
                //{
                //    ////////////////////////////////////
                //    // dispose managed resources here...
                //    ////////////////////////////////////
                //}

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~SQLiteProviderFactory()
        {
            Dispose(false);
        }
        #endregion
    }
}
