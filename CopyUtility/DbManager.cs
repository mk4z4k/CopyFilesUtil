using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Configuration;

namespace CopyUtility
{
    public class DbManager
    {
        private const string _getFilePathSql = "select file_storagePath from [ref].[Settings]";
        private const string _getFilesReference = "select distinct file_reference from [file].[file]";

        private string _connectionString;

        public DbManager()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
        }

        /// <summary>
        /// Получаем место хранения фалов из настроек системы
        /// </summary>
        /// <returns></returns>
        public string GetFilePathFromSettings()
        {
            string filePath = "";
            
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlCommand sql = new SqlCommand(_getFilePathSql,conn);
                var res = sql.ExecuteScalar();
                filePath = res.ToString();
            }
            
            return filePath;

        }

        /// <summary>
        /// Получаем пути к файлам на который есть ссылки в бд
        /// </summary>
        /// <returns></returns>
        public List<string> GetFilesReference()
        {
            List<string> files = new List<string>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string cmdText = _getFilesReference;
                SqlCommand sql = new SqlCommand(cmdText, conn);
                using (SqlDataReader reader = sql.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        files.Add(Convert.ToString(reader[0]));
                    }
                }
            }
            return files;
        }

    }
}
