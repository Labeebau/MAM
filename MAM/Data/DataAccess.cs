using MAM.Views.AdminPanelViews;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAM.Data
{
    public class DataAccess
    {
        private readonly string _connectionString = "server=localhost;uid=root;pwd=root;database=mam";
        public DataAccess()
        {
        }
        // Constructor to initialize with a connection string
        public DataAccess(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Open MySQL connection
        private MySqlConnection OpenConnection()
        {
            var connection = new MySqlConnection(_connectionString);
            try
            {
                connection.Open();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to connect to the database: {ex.Message}");
                throw;  // Re-throw to propagate the error upwards if necessary
            }
            return connection;
        }

        // Close MySQL connection
        private void CloseConnection(MySqlConnection connection)
        {
            if (connection != null && connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
            }
        }
        public int GetId(string query,Dictionary<string,object> parameters=null)
        {
            int id = -1;
            using (MySqlConnection connection = OpenConnection())
            {
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {

                    try
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                        var res = command.ExecuteScalar();
                        if(res!=null)
                            id = Convert.ToInt32(res);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error executing query: {ex.Message}");
                        throw;
                    }
                }
                CloseConnection(connection);
            }
            return id;
        }
        public DataTable GetData(string query, Dictionary<string, object> parameters = null)
        {
            DataTable dataTable = new DataTable();
            using (MySqlConnection connection = OpenConnection())
            {
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                command.Parameters.AddWithValue(param.Key, param.Value);
                            }
                        }
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if(reader.HasRows)
                                dataTable.Load(reader);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error executing query: {ex.Message}");
                        throw;
                    }
                }
                CloseConnection(connection);
            }
            return dataTable;
        }
        // Method to retrieve data (example: for fetching rows)
        public List<User> GetUsers()
        {
            List<User> UserList = new List<User>();
            string query = "SELECT u.user_id, u.first_name ,u.last_name,u.email, u.user_name, u.password,u.ad_user,u.active," +
                      "case when ug.group_name = 'Admin' then True else false end as IsAdmin " +
                      "FROM user u " +
                      "left join user_roles ur on u.user_id = ur.user_id " +
                      "left join user_group ug on ug.group_id = ur.group_id";

            using (MySqlConnection connection = OpenConnection())
            {
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Assume columns in the table are: ID, Name, Age
                                UserList.Add(new User
                                {
                                    UserId = reader.GetInt32("user_id"),
                                    FirstName = reader.GetString("first_name"),
                                    LastName = reader.GetString("last_name"),
                                    Email = reader.GetString("email"),
                                    UserName = reader.GetString("user_name"),
                                    Password = reader.GetString("password"),
                                    IsADUser = reader.GetBoolean("ad_user"),
                                    IsActive = reader.GetBoolean("active"),
                                    IsAdmin = reader.GetBoolean("IsAdmin")
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error executing query: {ex.Message}");
                        throw;
                    }
                }

                CloseConnection(connection);
            }

            return UserList;
        }

        public  List<string> GetUserGroups()
        {
            List<string> UserGroupList = new List<string>();
            string query = "select group_name from user_group";

            using (MySqlConnection connection = OpenConnection())
            {
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                UserGroupList.Add(reader.GetString("group_name"));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error executing query: {ex.Message}");
                        throw;
                    }
                }

                CloseConnection(connection);
            }

            return UserGroupList;
        }

        // Method to execute non-query operations (INSERT/UPDATE/DELETE)

        public int ExecuteNonQuery(string query, Dictionary<string, object> parameters, out int lastInsertedId)
        {
            int affectedRows=0;
            lastInsertedId=-1;
            using (MySqlConnection connection = OpenConnection())
            {
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        // Add parameters to prevent SQL injection
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                        affectedRows = command.ExecuteNonQuery();
                        lastInsertedId = Convert.ToInt32(command.LastInsertedId);
                        CloseConnection(connection);
                        return affectedRows;
                    }
                    catch (MySqlException ex)
                    {
                        if (ex.Number == 1062)  // Error codes for primary key and unique constraint violations
                        {
                            Console.WriteLine("Duplicate primary key error: Primary key or unique constraint violation.");
                            return -1;

                        }
                        else
                        {
                            Console.WriteLine($"A MySQL error occurred: {ex.Message}");
                            return 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred: {ex.Message}");
                        return 0;
                    }
                }
            }
        }
        public bool ExecuteScalar(string query, Dictionary<string, object> parameters)
        {
            using (MySqlConnection connection = OpenConnection())
            {
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        // Add parameters to prevent SQL injection
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }

                        var result = command.ExecuteScalar();
                        CloseConnection(connection);

                        return Convert.ToInt32(result) > 0;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error executing query: {ex.Message}");
                        throw;
                    }

                }
            }
        }
        public Task UpdateRecord(string table, string idColumn, int idValue, Dictionary<string, object> columnValues)
        {
            try
            {
                // Construct the update query dynamically based on the provided column names
                var queryBuilder = new StringBuilder($"UPDATE {table} SET ");
                var parameters = new List<MySqlParameter>();

                foreach (var kvp in columnValues)
                {
                    string columnName = kvp.Key;
                    object value = kvp.Value;
                    // Append the column name to the query with a parameter placeholder
                    queryBuilder.Append($"{columnName} = @{columnName}, ");
                    // Add the parameter to the list
                    parameters.Add(new MySqlParameter($"@{columnName}", value));

                }
                // Remove the trailing comma and space, then add the WHERE clause
                queryBuilder.Length -= 2; // Remove last comma and space
                queryBuilder.Append($" WHERE {idColumn} = @Id");
                columnValues.Add("@Id", idValue);
                int id = 0;
                ExecuteNonQuery(queryBuilder.ToString(), columnValues, out id);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return Task.CompletedTask;
        }
        public bool Delete(string table, string param, object value,out string errorMessage)
        {
            errorMessage=string.Empty;
            string query = $"delete from {table} where {param}={value};";
            using (MySqlConnection connection = OpenConnection())
            {
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue(param, value);
                    try
                    {

                        int rowsAffected = command.ExecuteNonQuery();
                        CloseConnection(connection);

                        return rowsAffected> 0;
                    }
                    catch (Exception ex)
                    {
                        errorMessage = ex.Message;
                        Debug.WriteLine($"Error executing query: {ex.Message}");
                        throw;
                    }

                }
            }
        }
        public bool DeleteAll(string query)
        {
            using (MySqlConnection connection = OpenConnection())
            {
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    try
                    {

                        var result = command.ExecuteScalar();
                        CloseConnection(connection);

                        return Convert.ToInt32(result) > 0;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error executing query: {ex.Message}");
                        throw;
                    }

                }
            }
        }
    }
}


//    public string connectionString = "server=localhost;uid=root;pwd=root;database=mam";
//    public DataAccess() 
//    {
//        //ConnectDatabaseAsync();
//    }
//    private async  Task ConnectDatabaseAsync()
//    {
//        DataTable dataTable = new DataTable();
//        try
//        {
//            using (MySqlConnection Conn = new MySqlConnection(connectionString))
//            {
//                await Conn.OpenAsync();

//            }

//        }
//        catch (Exception ex) { }
//    }
//    public bool ExecuteQuery(string query,string param1,string param2)
//    {
//        try
//        {
//            using (MySqlConnection Conn = new MySqlConnection(connectionString))
//            {
//                 Conn.Open();

//                using (MySqlCommand cmd = new MySqlCommand(query, Conn))
//                {
//                    cmd.Parameters.AddWithValue("@userName", param1);
//                    cmd.Parameters.AddWithValue("@password", param2);
//                    int userCount=Convert.ToInt32(cmd.ExecuteScalar());
//                    return userCount > 0;
//                }
//            }

//        }
//        catch (Exception ex) 
//        {
//            return false;
//        }
//    }
//}
//}
