using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BaseDb
{
    public class BaseDb
    {
        private SqlConnection _Conn;
        private string _ConnStr;
        private int _CmdTimeout = 60;

        public BaseDb(string ConnStr = "")
        {
            if (String.IsNullOrWhiteSpace(ConnStr)) throw new ArgumentNullException();
            this._ConnStr = ConnStr;
        }

        public BaseDb() { }

        public SqlConnection Connection
        {
            get {
                if (this._Conn == null) this._Conn = new SqlConnection(this.ConnectionString);
                return this._Conn; 
            }
        }

        public string ConnectionString {
            get
            {
                if (String.IsNullOrWhiteSpace(this._ConnStr)) throw new Exception("Veritabanı bağlantı cümlesi gerekli!!");
                return this._ConnStr;
            }
            set{
                if(String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException();
                this._ConnStr = value;
            }
        }

        public int CommandTimeOut
        {
            get { return this._CmdTimeout; }
            set {
                if (value == null) throw new ArgumentNullException("Command timeout süresi null olamaz!!");
                if (value < 1) throw new ArgumentException("Command timeout süresi en az 15 sanine olmalıdır!!");
                this._CmdTimeout = value;
            }
        }


        
        public DataTable GetSqlResultToDataTable(string sql, params object[] parameters)
        {
            try
            {
                using (var adp = new SqlDataAdapter())
                {
                    this.Connection.Open();
                    adp.SelectCommand = new SqlCommand(sql, this.Connection);
                    adp.SelectCommand.CommandTimeout = this.CommandTimeOut;
                    if (parameters.Count() > 0) adp.SelectCommand.Parameters.AddRange(parameters);
                    DataTable dt = new DataTable();
                    adp.Fill(dt);
                    return dt;

                }
            }
            catch (Exception)
            {
                throw;
            }
            finally {
                this.Connection.Close();
            }
        }


        public DataSet GetSqlResultToDataSet(string sql, params object[] parameters)
        {
            try
            {
                using (var adp = new SqlDataAdapter())
                {
                    this.Connection.Open();
                    adp.SelectCommand = new SqlCommand(sql, this.Connection);
                    adp.SelectCommand.CommandTimeout = this.CommandTimeOut;
                    if (parameters.Count() > 0) adp.SelectCommand.Parameters.AddRange(parameters);
                    DataSet ds = new DataSet();
                    adp.Fill(ds);
                    return ds;
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                this.Connection.Close();
            }
        }


        public int ExecuteSqlInsertUpdateDelete(string sql, params object[] parameters)
        {
            try
            {
                using (var cmd = new SqlCommand())
                {
                    this.Connection.Open();
                    cmd.CommandText = sql;
                    cmd.CommandTimeout = this.CommandTimeOut;
                    cmd.Connection = this.Connection;
                    if (parameters.Count() > 0) cmd.Parameters.AddRange(parameters);
                    
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                this.Connection.Close();
            }
        }


        public T GetSqlScalarResult<T>(string sql, params object[] parameters)
        {
            try
            {
                this.Connection.Open();
                using (var cmd = new SqlCommand())
                {
                    cmd.Connection = this.Connection;
                    cmd.CommandText = sql;
                    cmd.CommandTimeout = this.CommandTimeOut;
                    if (parameters.Count() > 0) cmd.Parameters.AddRange(parameters);

                    return (T)cmd.ExecuteScalar();
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                this.Connection.Close();
            }
        }

       

        public IEnumerable<T> GetSqlSingleResultToType<T>(string sql, params object[] parameters)
        {
            try
            {
                DataTable dt = this.GetSqlResultToDataTable(sql, parameters);

                this.Connection.Open();
                T obj = Activator.CreateInstance<T>();
                PropertyInfo[] objProps = obj.GetType().GetProperties();
                IList<T> Results = new List<T>();
                foreach (DataRow dr in dt.Rows.Cast<DataRow>().ToArray())
                {
                    T objItem = Activator.CreateInstance<T>();
                    foreach (PropertyInfo item in objProps)
                    {
                        if (dt.Columns.Contains(item.Name))
                        {
                            item.SetValue(objItem, dr[item.Name] == DBNull.Value ? null : dr[item.Name], null);
                        }
                    }
                    Results.Add(objItem);
                }

                return Results;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                this.Connection.Close();
            }
        }


        public MultiResultSet GetSqlMultiResultToType(string sql, params object[] parameters)
        {
            try
            {
                return new MultiResultSet(this.Connection, this.CommandTimeOut, sql, parameters);
            }
            catch (Exception)
            {
                throw;
            }

        }


        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed == false)
            {
                if (disposing == true)
                {
                    this._Conn.Dispose();
                }
            }
            _disposed = true;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
