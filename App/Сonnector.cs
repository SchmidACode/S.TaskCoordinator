using System;
using System.Data;
using System.Data.SQLite;

namespace App
{
	internal class Connector
	{
		public string ConnectionString { get; }
		public Connector()
		{
			ConnectionString = "Data Source=MyApp.db;Version=3;";
		}
		public bool IsExists(string tableName)
		{
			using (var connection = new SQLiteConnection(ConnectionString))
			{
				try
				{
					connection.Open();
					string query = $"SELECT COUNT(*) FROM {tableName}";
					using (var command = new SQLiteCommand(query, connection)) {
						var result = command.ExecuteScalar();
						if (result != null && result != DBNull.Value) {
							int rowCount = Convert.ToInt32(result);
							return rowCount > 0;
						}
						return false;
					}
				}
				catch (Exception ex) {
					Console.WriteLine($"Error: {ex.Message}");
					return false;
				}
			}
		}
		public DataTable LoadSortedDataFromDB(string query)
		{
			DataTable table = new DataTable();
			using (var connection = new SQLiteConnection(ConnectionString)) {
				try
				{
					connection.Open();

					using (var adapter = new SQLiteDataAdapter(query, connection)) {
						adapter.Fill(table);
					}
				}
				catch (Exception ex) {
					throw new Exception("Ошибка при загрузке данных из базы данных", ex);
				}
			}
			return table;
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

