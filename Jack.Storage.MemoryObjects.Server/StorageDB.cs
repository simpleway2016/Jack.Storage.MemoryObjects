using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Way.Lib;

namespace Jack.Storage.MemoryObjects.Server
{
    class StorageDB:IDisposable
    {
        SqliteConnection _sqlCon;
        SqliteConnection _sqlConForDelete;
        string _propertyName;
        Way.Lib.CLog _log;
        Type _keytype;
        string FilePath { get; }
        public StorageDB(string filepath,string propertyName,string propertyType)
        {
            this.FilePath = filepath;
            _log = new Way.Lib.CLog(Path.GetFileName(filepath) + ".Error" , false);
               _propertyName = propertyName;
            _keytype = typeof(long).Assembly.GetType(propertyType);

            if (System.IO.Directory.Exists(Path.GetDirectoryName(filepath)) == false)
            {
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            }

            _sqlCon = new SqliteConnection($"Data Source=\"{filepath}\"");
            _sqlCon.Open();
            FixFileSize();
            if(isTableExist("main") == false)
            {
                CreateTable();
            }

            _sqlConForDelete = new SqliteConnection($"Data Source=\"{filepath}\"");
            _sqlConForDelete.Open();
        }
        bool isTableExist(string tablename)
        {

            var sql = $"SELECT COUNT(*) FROM sqlite_master where type='table' and name='{tablename}'";
            using (var cmd = _sqlCon.CreateCommand())
            {
                cmd.CommandText = sql;
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }


        }
        /// <summary>
        /// 释放多余
        /// </summary>
        public void FixFileSize()
        {
            try
            {
                using (var cmd = _sqlCon.CreateCommand())
                {
                    cmd.CommandText = "VACUUM";
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// 判断表是否包含指定字段
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        bool hasFieldInTable(string tablename, string fieldName)
        {
            try
            {
                var sql = $"SELECT [{fieldName}] FROM [{tablename}] LIMIT 1 OFFSET 0";
                using (var cmd = _sqlCon.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        void AddFieldToTable(string tableName, string fieldName, string fieldType)
        {
            string sql = $@"
ALTER TABLE [{tableName}]
ADD [{fieldName}] {fieldType}
";
            using (var cmd = _sqlCon.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }
        void CreateTable()
        {
            string type = "INTEGER";
           
           if (_keytype == typeof(long))
            {
                type = "BIGINT";
            }
            else if (_keytype == typeof(string))
            {
                type = "TEXT";
            }
            string sql = $@"
CREATE TABLE [main](
    [key] "+ type + @", 
    [CreateTime] DATETIME,
    [Content] TEXT);
";
            string sql2 = @"
CREATE INDEX primary_index ON [main] (
    key ASC
);
";
            var tran = _sqlCon.BeginTransaction();
            using (var cmd = _sqlCon.CreateCommand())
            {
                cmd.Transaction = tran;

                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
                cmd.CommandText = sql2;
                cmd.ExecuteNonQuery();
            }
            tran.Commit();

        }

        public void ReadData(Action<string> callback)
        {
            var tran = _sqlCon.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
            using (var cmd = _sqlCon.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = $"select * from [main]";
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string content = (string)reader["Content"];
                    callback(content);
                }
                reader.Close();
            }
            tran.Commit();
        }
        public object Exec(string sql)
        {
            using (var cmd = _sqlCon.CreateCommand())
            {
                cmd.CommandText = sql;
                return cmd.ExecuteScalar();
            }
        }
        public void Handle(System.Collections.IEnumerable list)
        {
            var tran = _sqlCon.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
            try
            {
                using (var cmd = _sqlCon.CreateCommand())
                {
                    cmd.Transaction = tran;
                    foreach (ContentAction item in list)
                    {
                        var data = item.Data;

                        var key = Convert.ChangeType(item.KeyValue, _keytype);
                        cmd.CommandText = "select exists (SELECT * FROM [main] where key=@key)";
                        var p = cmd.CreateParameter();
                        p.ParameterName = "key";
                        p.Value = key;
                        cmd.Parameters.Add(p);

                        if (item.Type == ActionType.Add && Convert.ToBoolean(cmd.ExecuteScalar()) )
                        {
                            item.Type = ActionType.Update;
                        }

                        if (item.Type == ActionType.Add)
                        {
                            cmd.CommandText = $"insert into [main] (key,Content,CreateTime) values (@key,@p1,@p2)";

                            p = cmd.CreateParameter();
                            p.ParameterName = "p1";
                            p.Value = data;
                            cmd.Parameters.Add(p);

                            p = cmd.CreateParameter();
                            p.ParameterName = "p2";
                            p.Value = DateTime.Now;
                            cmd.Parameters.Add(p);

                           
                        }
                        else if (item.Type == ActionType.Update)
                        {
                            cmd.CommandText = $"update [main] set Content=@p1 where key=@key";

                            p = cmd.CreateParameter();
                            p.ParameterName = "p1";
                            p.Value = data;
                            cmd.Parameters.Add(p);

                        }
                        else if(item.Type == ActionType.Remove)
                        {
                            cmd.CommandText = $"delete from [main] where key=@key";
                        }
                        else if (item.Type == ActionType.DeleteFile)
                        {
                            cmd.CommandText = $"delete from [main]";
                        }
                        cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();
                    }
                }
                tran.Commit();
            }
            catch (Exception ex)
            {
                tran.Rollback();
                _log.Log("write file error. \r\n{0}",ex.ToString());
            }
           
        }

        public void Dispose()
        {
            if(_sqlCon != null)
            {
                _sqlCon.Dispose();
                _sqlConForDelete.Dispose();
                _sqlCon = null;
            }
        }
    }
}
