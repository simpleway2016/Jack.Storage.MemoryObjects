#if NET46
using System.Data.SQLite;
#else
using Microsoft.Data.Sqlite;
using SQLiteConnection = Microsoft.Data.Sqlite.SqliteConnection;
#endif

using System;
using System.Collections.Generic;

using System.IO;
using System.Reflection;
using System.Text;

namespace Jack.Storage.MemoryObjects
{
    class StorageDB:IDisposable
    {
        SQLiteConnection _sqlCon;
        SQLiteConnection _sqlConForDelete;
        PropertyInfo _pro;
        public StorageDB(string filepath,PropertyInfo property)
        {
            _pro = property;
            if( System.IO.Directory.Exists(Path.GetDirectoryName(filepath)) == false )
            {
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            }

               _sqlCon = new SQLiteConnection($"Data Source=\"{filepath}\"");
            _sqlCon.Open();
            FixFileSize();
            if(isTableExist("main") == false)
            {
                CreateTable(_pro.PropertyType);
            }

            _sqlConForDelete = new SQLiteConnection($"Data Source=\"{filepath}\"");
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
        void CreateTable(Type keytype)
        {
            string type = "INTEGER";
           if(keytype == typeof(long))
            {
                type = "BIGINT";
            }
            else if (keytype == typeof(string))
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

        public void ReadData<T>(Action<T> callback)
        {
            using (var cmd = _sqlCon.CreateCommand())
            {
                cmd.CommandText = $"select * from [main]";
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string content = (string)reader["Content"];
                    callback(Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content));
                }
                reader.Close();
            }
        }
        public object Exec(string sql)
        {
            using (var cmd = _sqlCon.CreateCommand())
            {
                cmd.CommandText = sql;
                return cmd.ExecuteScalar();
            }
        }
        public void Handle<T>(System.Collections.IEnumerable list)
        {
            var tran = _sqlCon.BeginTransaction();
            try
            {
                using (var cmd = _sqlCon.CreateCommand())
                {
                    cmd.Transaction = tran;
                    foreach (OpAction<T> item in list)
                    {
                        var data = item.Data;
                        
                        var key = _pro.GetValue(data);

                        if (item.Type == ActionType.Add)
                        {
                            cmd.CommandText = $"insert into [main] (key,Content,CreateTime) values (@p0,@p1,@p2)";
                            var p = cmd.CreateParameter();
                            p.ParameterName = "p0";
                            p.Value = key;
                            cmd.Parameters.Add(p);

                            p = cmd.CreateParameter();
                            p.ParameterName = "p1";
                            try
                            {
                                p.Value = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                            }
                            catch (Exception ex)
                            {
                                
                                p.Value = ex.Message;
                            }
                            cmd.Parameters.Add(p);

                            p = cmd.CreateParameter();
                            p.ParameterName = "p2";
                            p.Value = DateTime.Now;
                            cmd.Parameters.Add(p);

                           
                        }
                        else if (item.Type == ActionType.Update)
                        {
                            cmd.CommandText = $"update [main] set Content=@p1 where key=@p0";

                            var p = cmd.CreateParameter();
                            p.ParameterName = "p0";
                            p.Value = key;
                            cmd.Parameters.Add(p);

                            p = cmd.CreateParameter();
                            p.ParameterName = "p1";
                            try
                            {
                                p.Value = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                            }
                            catch (Exception ex)
                            {
                                p.Value = ex.Message;
                            }
                            cmd.Parameters.Add(p);

                        }
                        else if(item.Type == ActionType.Remove)
                        {
                            cmd.CommandText = $"delete from [main] where key=@p0";
                            var p = cmd.CreateParameter();
                            p.ParameterName = "p0";
                            p.Value = key;
                            cmd.Parameters.Add(p);
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