using System;
using System.Data;
using System.Data.SQLite;

namespace ScheduleTaskCoordinator
{
    internal class Connector
    {
        private string ConnectionString;

        public Connector()
        {
            ConnectionString = "Data Source=ScheduleTaskCoordinator.db;Version=3;";
        }

        public bool IsExists(string tableName)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    string query = $"SELECT COUNT(*) FROM {tableName}";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            int rowCount = Convert.ToInt32(result);
                            return rowCount > 0;
                        }
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return false;
                }
            }
        }

        public DataTable LoadSortedDataFromDB(string query)
        {
            DataTable table = new DataTable();
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                try
                {
                    connection.Open();

                    using (var adapter = new SQLiteDataAdapter(query, connection))
                    {
                        adapter.Fill(table);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Ошибка при загрузке данных из базы данных", ex);
                }
            }
            return table;
        }

        public void ExecuteQuery(string query)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error executing SQL query", ex);
                }
            }
        }

        public DataTable LoadTaskById(int id)
        {
            string query = $"SELECT * FROM Tasks WHERE Id = {id}";
            return LoadSortedDataFromDB(query);
        }

        public void InsertDataToBase(string table, string columns, string values)
        {
            string cmd = $"INSERT INTO {table}({columns}) VALUES ({values})";

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(cmd, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error inserting data into database", ex);
                }
            }
        }

        public void UpdateDataInBase(string table, string column, string value, int id)
        {
            string cmd = $"UPDATE {table} SET {column} = @value WHERE Id = @id";

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(cmd, connection))
                    {
                        command.Parameters.AddWithValue("@value", value);
                        command.Parameters.AddWithValue("@id", id);
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error updating data in database", ex);
                }
            }
        }

        public void DeleteDataFromBase(string table, int ID)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    string cmd = $"DELETE FROM {table} WHERE Id = @ID";
                    using (var command = new SQLiteCommand(cmd, connection))
                    {
                        command.Parameters.AddWithValue("@ID", ID);
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Ошибка при удалении данных из базы данных", ex);
                }
            }
        }
    }
}