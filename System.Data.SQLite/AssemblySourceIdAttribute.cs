/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Joe Mistachkin (joe@mistachkin.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/

using System;

namespace System.Data.SQLite
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblySourceIdAttribute : Attribute
    {
        public AssemblySourceIdAttribute(string value)
        {
            sourceId = value;
        }

        ///////////////////////////////////////////////////////////////////////

        private string sourceId;
        public string SourceId
        {
            get { return sourceId; }
        }
    }
}
