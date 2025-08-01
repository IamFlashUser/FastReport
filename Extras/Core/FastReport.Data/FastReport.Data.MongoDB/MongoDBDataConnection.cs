﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using MongoDB.Driver;
using System.Data;
using MongoDB.Bson;
using FastReport.Data;
using MongoDB.Driver.Core.Configuration;
using System.Threading.Tasks;
using System.Threading;

namespace FastReport.Data
{
    public partial class MongoDBDataConnection : DataConnectionBase
    {
        public static string dbName = "";

        #region Private Methods
        private static void ExecuteFillDataTable(BsonDocument doc, DataTable dt, DataRow dr, string parent)
        {
            List<KeyValuePair<string, BsonArray>> arrays = new List<KeyValuePair<string, BsonArray>>();
            foreach (string key in doc.Names)
            {
                object value = doc[key];
                string x; 
                if (value is BsonDocument)
                {
                    string newParent = string.IsNullOrEmpty(parent) ? key : parent + "." + key;
                    ExecuteFillDataTable((BsonDocument)value, dt, dr, newParent);
                }               
                else if (value is BsonArray)                   
                    arrays.Add(new KeyValuePair<string, BsonArray>(key, (BsonArray)value));
                else if (value is BsonTimestamp)
                    x = doc[key].AsBsonTimestamp.ToLocalTime().ToString("s");
                else if (value is BsonNull)
                    x = string.Empty;
                else
                {
                    x = value.ToString();
                    string colName = string.IsNullOrEmpty(parent) ? key : parent + "." + key;
                    if (!dt.Columns.Contains(colName))
                        dt.Columns.Add(colName);
                    dr[colName] = value;
                }
            }          
        }
        #endregion

        #region Protected Methods

        /// <inheritdoc/>
        protected DataTable CreateDataTable(DataTable table, bool allRows)
        {
            IMongoDatabase db = CreateDataTableShared();

            var collection = db.GetCollection<BsonDocument>(table.TableName);
            if (!allRows)
            {
                var documents = collection.Find(new BsonDocument()).FirstOrDefault();
                if (documents != null)
                {
                    DataRow dr = table.NewRow();
                    ExecuteFillDataTable(documents, table, dr, string.Empty);
                }
            }
            else
            {
                var documents = collection.Find(new BsonDocument()).ToList();
                foreach (var obj in documents)
                {
                    DataRow dr = table.NewRow();
                    ExecuteFillDataTable(obj, table, dr, string.Empty);
                    table.Rows.Add(dr);
                }
            }          
            return table;
        }

        protected async Task<DataTable> CreateDataTableAsync(DataTable table, bool allRows, CancellationToken token)
        {
            IMongoDatabase db = CreateDataTableShared();

            var collection = db.GetCollection<BsonDocument>(table.TableName);
            if (!allRows)
            {
                var documents = (await collection.FindAsync(new BsonDocument(), cancellationToken: token)).FirstOrDefault();
                if (documents != null)
                {
                    DataRow dr = table.NewRow();
                    ExecuteFillDataTable(documents, table, dr, string.Empty);
                }
            }
            else
            {
                var documents = (await collection.FindAsync(new BsonDocument(), cancellationToken: token)).ToList();
                foreach (var obj in documents)
                {
                    DataRow dr = table.NewRow();
                    ExecuteFillDataTable(obj, table, dr, string.Empty);
                    table.Rows.Add(dr);
                }
            }
            return table;
        }

        private IMongoDatabase CreateDataTableShared()
        {
            MongoClient client = new MongoClient(ConnectionString);
            IMongoDatabase db;
            if (dbName != string.Empty)
            {
                db = client.GetDatabase(dbName);
            }
            else
            {
                MongoUrlBuilder builder = new MongoUrlBuilder(ConnectionString);
                db = client.GetDatabase(builder.DatabaseName);
            }

            return db;
        }

        protected override string GetConnectionStringWithLoginInfo(string userName, string password)
        {
            MongoUrlBuilder builder = new MongoUrlBuilder(ConnectionString);
            builder.Username = userName;
            builder.Password = password;
#if NET45
            string url = builder.ToString();
            if(builder.Scheme == ConnectionStringScheme.MongoDBPlusSrv && builder.Server.Port != 27017)
            {
                string portString = builder.Server.Port.ToString();
                url = url.Remove(url.IndexOf(portString) - 1, 1).Replace(portString, "");
            }
            return url;
#else
            return builder.ToMongoUrl().Url;
#endif
        }
        #endregion

        #region Public Methods

        public override string[] GetTableNames()
        {
            List<string> list = new List<string>();

            MongoClient client = new MongoClient(ConnectionString);
            
            if (String.IsNullOrEmpty(dbName))
            {
                var mongoUrl = new MongoUrl(ConnectionString);
                dbName = mongoUrl.DatabaseName;
            }

            IMongoDatabase db = client.GetDatabase(dbName);
            IAsyncCursor<BsonDocument> collections = db.ListCollections();
            foreach (var item in collections.ToList<BsonDocument>())
            {
                list.Add(item[0].ToString());
            }
            return list.ToArray();
        }

        public override async Task<string[]> GetTableNamesAsync(CancellationToken cancellationToken)
        {
            List<string> list = new List<string>();

            IMongoDatabase db = GetTableNamesShared();
            IAsyncCursor<BsonDocument> collections = await db.ListCollectionsAsync(cancellationToken: cancellationToken);
            foreach (var item in await collections.ToListAsync<BsonDocument>(cancellationToken))
            {
                list.Add(item[0].ToString());
            }
            return list.ToArray();
        }

        private IMongoDatabase GetTableNamesShared()
        {
            MongoClient client = new MongoClient(ConnectionString);

            if (String.IsNullOrEmpty(dbName))
            {
                var mongoUrl = new MongoUrl(ConnectionString);
                dbName = mongoUrl.DatabaseName;
            }

            IMongoDatabase db = client.GetDatabase(dbName);
            return db;
        }

        public override string QuoteIdentifier(string value, DbConnection connection)
        {
            return value;
        }

        /// <inheritdoc/>
        public override void FillTableSchema(DataTable table, string selectCommand,
      CommandParameterCollection parameters)
        {
            CreateDataTable(table, false);
        }

        public override Task FillTableSchemaAsync(DataTable table, string selectCommand, CommandParameterCollection parameters, CancellationToken cancellationToken = default)
        {
            return CreateDataTableAsync(table, false, cancellationToken);
        }

        /// <inheritdoc/>
        public override void FillTableData(DataTable table, string selectCommand,
     CommandParameterCollection parameters)
        {
            CreateDataTable(table, true);
        }

        public override Task FillTableDataAsync(DataTable table, string selectCommand, CommandParameterCollection parameters, CancellationToken cancellationToken = default)
        {
            return CreateDataTableAsync(table, true, cancellationToken);
        }
        #endregion
    }
}
