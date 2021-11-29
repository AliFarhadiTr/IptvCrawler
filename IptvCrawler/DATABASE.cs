using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB
{
    public class DATABASE
    {

        private string path;
        private string table;

        public DATABASE(string path)
        {
            this.path = path;
        }

        public DATABASE Table(string table)
        {
            this.table = table;
            return this;
        }

        public int Insert(Dictionary<string, string> data)
        {

            int modified = 0;

            using (SQLiteConnection con = new SQLiteConnection(@"Data Source=" + path + ";Version=3;", true))
            {
                StringBuilder vars1 = new StringBuilder("(");
                StringBuilder vars2 = new StringBuilder("(");

                foreach (var item in data)
                {
                    vars1.Append(item.Key).Append(",");
                    vars2.Append("@" + item.Key).Append(",");
                }
                vars1 = vars1.Remove(vars1.Length - 1, 1);
                vars2 = vars2.Remove(vars2.Length - 1, 1);
                vars1.Append(")");
                vars2.Append(")");

                con.Open();
                string sql = String.Format("INSERT INTO `{0}` {1} VALUES {2};SELECT last_insert_rowid();", this.table, vars1.ToString(), vars2.ToString());

                using (SQLiteCommand cmd = new SQLiteCommand(sql, con))
                {

                    foreach (var item in data)
                    {
                        cmd.Parameters.AddWithValue("@" + item.Key, item.Value);
                    }

                    modified = Convert.ToInt32(cmd.ExecuteScalar());

                }


            }

            return modified;

        }

        public List<object> Fetch(Dictionary<string, string> where,string oprator="=",string concat="AND")
        {

            List<object> data = new List<object>();

            using (SQLiteConnection con = new SQLiteConnection(@"Data Source=" + path + ";Version=3;", true))
            {
                con.Open();

                StringBuilder vars1 = new StringBuilder();
                foreach (var item in where)
                {
                    vars1.Append(item.Key).Append(" ").Append(oprator).Append(" @" + item.Key).Append(" ").Append(concat).Append(" ");
                }

                vars1 = vars1.Remove(vars1.Length - (concat.Length+1), concat.Length);

                string sql = String.Format("SELECT * FROM `{0}` WHERE {1};", this.table, vars1.ToString());
                using (SQLiteCommand cmd = new SQLiteCommand(sql, con))
                {

                    foreach (var item in where)
                    {
                        cmd.Parameters.AddWithValue("@" + item.Key, item.Value);
                    }

                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {

                        while (reader.Read())
                        {

                            data.Add(reader.GetValues());
                            
                        }

                    }

                }


            }

            return data;

        }

        public List<object> Fetch(string id)
        {

            Dictionary<string, string> data = new Dictionary<string, string>();

            data.Add("id", id);

            return Fetch(data);

        }

        public List<object> FetchAll()
        {

            List<object> data = new List<object>();

            using (SQLiteConnection con = new SQLiteConnection(@"Data Source=" + path + ";Version=3;", true))
            {
                con.Open();

                string sql = String.Format("SELECT * FROM `{0}`;", this.table);
                using (SQLiteCommand cmd = new SQLiteCommand(sql, con))
                {

                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {

                        while (reader.Read())
                        {

                            data.Add(reader.GetValues());

                        }

                    }

                }


            }

            return data;

        }

        public bool Exist(Dictionary<string, string> where, string oprator = "=", string concat = "AND")
        {

            List<object> data = new List<object>();

            using (SQLiteConnection con = new SQLiteConnection(@"Data Source=" + path + ";Version=3;", true))
            {
                con.Open();

                StringBuilder vars1 = new StringBuilder();
                foreach (var item in where)
                {
                    vars1.Append(item.Key).Append(" ").Append(oprator).Append(" @" + item.Key).Append(" ").Append(concat).Append(" ");
                }

                vars1 = vars1.Remove(vars1.Length - (concat.Length + 1), concat.Length);

                string sql = String.Format("SELECT * FROM `{0}` WHERE {1};", this.table, vars1.ToString());
                using (SQLiteCommand cmd = new SQLiteCommand(sql, con))
                {

                    foreach (var item in where)
                    {
                        cmd.Parameters.AddWithValue("@" + item.Key, item.Value);
                    }

                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {

                        while (reader.Read())
                        {

                            data.Add(reader.GetValues());

                        }

                    }

                }


            }

            return data.Count!=0;

        }

        public void Delete(Dictionary<string, string> where, string oprator = "=", string concat = "AND")
        {

            using (SQLiteConnection con = new SQLiteConnection(@"Data Source=" + path + ";Version=3;", true))
            {
                con.Open();

                StringBuilder vars1 = new StringBuilder();
                foreach (var item in where)
                {
                    vars1.Append(item.Key).Append(" ").Append(oprator).Append(" @" + item.Key).Append(" ").Append(concat).Append(" ");
                }

                vars1 = vars1.Remove(vars1.Length - (concat.Length + 1), concat.Length);

                string sql = String.Format("DELETE FROM `{0}` WHERE {1};", this.table, vars1.ToString());
                using (SQLiteCommand cmd = new SQLiteCommand(sql, con))
                {

                    foreach (var item in where)
                    {
                        cmd.Parameters.AddWithValue("@" + item.Key, item.Value);
                    }

                    cmd.ExecuteNonQuery();

                }


            }

        }

        public void Delete(string id)
        {

            Dictionary<string, string> data = new Dictionary<string, string>();

            data.Add("id", id);

            Delete(data);

        }

        public void Update(Dictionary<string, string> data,Dictionary<string, string> where, string oprator = "=", string concat = "AND")
        {
            using (SQLiteConnection con = new SQLiteConnection(@"Data Source=" + path + ";Version=3;", true))
            {
                con.Open();

                StringBuilder vars1 = new StringBuilder();
                int i = 0;
                foreach (var item in where)
                {
                    vars1.Append(item.Key).Append(" ").Append(oprator).Append(" @param_tmp" + (i++).ToString()).Append(" ").Append(concat).Append(" ");
                }

                vars1 = vars1.Remove(vars1.Length - (concat.Length + 1), concat.Length);

                StringBuilder vars2 = new StringBuilder();
                foreach (var item in data)
                {
                    vars2.Append(item.Key).Append("=").Append("@" + item.Key).Append(",");
                }

                vars2 = vars2.Remove(vars2.Length - 1, 1);

                string sql = String.Format("UPDATE `{0}` SET {1} WHERE {2};", this.table, vars2.ToString(),vars1.ToString());
                using (SQLiteCommand cmd = new SQLiteCommand(sql, con))
                {

                    foreach (var item in data)
                    {
                        cmd.Parameters.AddWithValue("@" + item.Key, item.Value);
                    }

                    i = 0;
                    foreach (var item in where)
                    {
                        cmd.Parameters.AddWithValue("@param_tmp" + (i++).ToString(), item.Value);
                    }

                    cmd.ExecuteNonQuery();

                }


            }
        }

        public void Update(Dictionary<string, string> data, string id)
        {
            Dictionary<string, string> where = new Dictionary<string, string>();

            where.Add("id",id);
            Update(data,where);
        }

    }
}
