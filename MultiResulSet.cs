using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BaseDb
{
    public class MultiResultSet
    {
        private readonly string _sql;
        private object[] _parameters;
        private List<Func<SqlDataReader, IEnumerable>> _resultSets;
        private SqlConnection _Conn;
        private int _CmdTimeout;

        public MultiResultSet(SqlConnection Conn, int cmdTimeout, string sql, params object[] parameters)
        {
            this._sql = sql;
            this._parameters = parameters;
            this._Conn = Conn;
            this._resultSets = new List<Func<SqlDataReader, IEnumerable>>();
        }

        public MultiResultSet With<TResult>()
        {
            _resultSets.Add((reader) =>
            {

                IList<TResult> result = new List<TResult>();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        TResult obj = Activator.CreateInstance<TResult>();
                        foreach (PropertyInfo item in obj.GetType().GetProperties())
                        {
                            item.SetValue(obj, reader[item.Name].GetType() == typeof(DBNull) ? null : reader[item.Name], null);
                        }
                        result.Add(obj);
                    }
                }
                return result;
            });

            return this;
        }

        public IEnumerable<IEnumerable> Execute()
        {
            var Results = new List<IEnumerable>();
            try
            {
                this._Conn.Open();
                using (var cmd = new SqlCommand())
                {
                    cmd.CommandText = this._sql;
                    cmd.Connection = this._Conn;
                    cmd.CommandTimeout = this._CmdTimeout;
                    if (this._parameters.Count() > 0) cmd.Parameters.AddRange(this._parameters);

                    using (var dr = cmd.ExecuteReader())
                    {
                        foreach (var resultSet in _resultSets)
                        {
                            Results.Add(resultSet(dr));
                            dr.NextResult();
                        }
                    }

                    return Results;
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                this._Conn.Close();
            }
        }


    }
}
