using MAM.Views.AdminPanelViews;
using Microsoft.WindowsAppSDK.Runtime.Packages;
using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml;

namespace MAM.Data
{
    public class DataAccess
    {
        //private readonly string _connectionString = "server=localhost;uid=root;pwd=root;database=mam;Connection Timeout=30;";
        public static string ConnectionString { get; set; }//= "server=localhost;uid=root;pwd=root;database=mam;Connection Timeout=30;";
        public static List<string> ConnectionStringList { get; set; }
        private XamlRoot xamlRoot;
        public DataAccess()
        {
            if(App.MainAppWindow!=null)
                xamlRoot = App.MainAppWindow.Content.XamlRoot;
        }
        // Constructor to initialize with a connection string
        public DataAccess(string connectionString)
        {
            ConnectionString = connectionString;
        }

        // Open MySQL connection
        private MySqlConnection OpenConnection()
        {
            var connection = new  MySqlConnection(ConnectionString);
            try
            {
                connection.Open();
            }
            catch (Exception ex)
            { 
               // GlobalClass.Instance.ShowErrorDialogAsync($"Failed to connect to the database: {ex.Message}");
                Debug.WriteLine($"Failed to connect to the database: {ex.Message}");
                //throw;  // Re-throw to propagate the error upwards if necessary
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
        public async Task<MySqlDataReader> ExecuteReaderStoredProcedure(string procedureName, List<MySqlParameter> parameters=null)
        {
            MySqlConnection connection = OpenConnection();
           
                if (connection.State == ConnectionState.Open)
                {
                    var command = new MySqlCommand(procedureName, connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    // Add parameters to prevent SQL injection
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
                        }
                    }
                    try
                    {
                        var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
                        return reader;
                    }

                    catch (Exception ex)
                    {
                        CloseConnection(connection);
                        Debug.WriteLine($"Error executing query: {ex.Message}");
                        return null;
                    }
                }
                return null;
        }
        public async Task<int> ExecuteNonQueryStoredProcedure(string procedureName, Dictionary<string, object> parameters)
        {
            try
            {
                using (var connection = OpenConnection())
                {
                    if(connection.State==ConnectionState.Open)
                    {
                        using (var command = new MySqlCommand(procedureName, connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            // Add parameters if any
                            if (parameters != null)
                            {
                                foreach (var param in parameters)
                                {
                                    command.Parameters.AddWithValue(param.Key, param.Value);
                                }
                            }
                            return await command.ExecuteNonQueryAsync();
                        }
                    }
                    else return 0;
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Invalid UserId "+ex.ToString());
                return 0;
            }
        }
        //public (int, string, string,int,string) GetUserCredentials(string userName)
        //{
        //    string query = "SELECT u.user_id, u.password_hash, u.password_salt,ug.group_id,ug.group_name FROM user u inner join mam.user_roles ur on u.user_id = ur.user_id inner join mam.user_group ug on ug.group_id = ur.group_id ug WHERE user_name = @userName";
        //    var parameters = new List<MySqlParameter> { new("@userName", userName) };
            
        //    var reader = ExecuteReader(query, parameters);

        //    if (reader.Read())
        //    {
        //        return (
        //            Convert.ToInt32(reader["user_id"]),
        //            reader["password_hash"].ToString(),
        //            reader["password_salt"].ToString(),
        //            Convert.ToInt32(reader["group_id"].ToString()),
        //            reader["group_name"].ToString()
        //        );
        //    }
        //    reader.DisposeAsync();
        //    return (-1,string.Empty, string.Empty,-1, string.Empty);
        //}
        public async Task<(int, string, string, int, string)> GetUserCredentials(string userName)
        {
            string query = @"SELECT u.user_id, u.password_hash, u.password_salt, ug.group_id, ug.group_name
                            FROM user u
                            INNER JOIN user_roles ur ON u.user_id = ur.user_id
                            INNER JOIN user_group ug ON ug.group_id = ur.group_id
                            WHERE user_name = @userName";

            var parameters = new List<MySqlParameter> { new("@userName", userName) };

            using var reader = ExecuteReader(query, parameters);
            if (reader!=null && await reader.ReadAsync())
            {
                return (
                    Convert.ToInt32(reader["user_id"]),
                    reader["password_hash"].ToString(),
                    reader["password_salt"].ToString(),
                    Convert.ToInt32(reader["group_id"]),
                    reader["group_name"].ToString()
                );
            }

            return (-1, string.Empty, string.Empty, -1, string.Empty);
        }


        public int GetId(string query, List<MySqlParameter> parameters = null)
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
                            command.Parameters.AddWithValue(param.ParameterName, param.Value);
                        }
                        var res = command.ExecuteScalar();
                        if (res != null)
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
        public string GetString(string query, Dictionary<string, object> parameters = null)
        {
            string result=string.Empty;
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
                        if (res != null)
                            result = res.ToString();                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error executing query: {ex.Message}");
                        throw;
                    }
                }
                CloseConnection(connection);
            }
            return result;
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
                                command.Parameters.Add(new MySqlParameter(param.Key, param.Value));
                            }
                        }
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
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
            string query = "SELECT u.user_id, u.first_name, u.last_name, u.email, u.user_name, u.password_hash," +
                           "u.ad_user, u.active," +
                           "EXISTS(" +
                                   "SELECT 1 " +
                                   "FROM user_roles ur2 " +
                                   "JOIN user_group ug2 ON ur2.group_id = ug2.group_id " +
                                   "WHERE ur2.user_id = u.user_id AND ug2.group_name = 'Admin' " +
                                  ") AS IsAdmin " +
                           "FROM user u;";

            //string query = "SELECT u.user_id, u.first_name, u.last_name, u.email, u.user_name, u.password_hash, u.ad_user, u.active, " +
            //    "CASE WHEN ug.group_name = 'Admin' THEN TRUE ELSE FALSE END AS IsAdmin " +
            //    "FROM user u " +
            //    " JOIN user_roles ur ON u.user_id = ur.user_id " +
            //    " JOIN user_group ug ON ug.group_id = ur.group_id";

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
                                    Password = reader.GetString("password_hash"),
                                    IsADUser = reader.GetBoolean("ad_user"),
                                    IsActive = reader.GetBoolean("active"),
                                    IsAdmin = reader.GetBoolean("IsAdmin"),
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

        public List<string> GetUserGroups()
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
        public async Task<(int affectedRows, int lastInsertedId,string errorMessage)> ExecuteNonQuery(string query, List<MySqlParameter> parameters)
        {
            int affectedRows = 0;
            int lastInsertedId = -1;
            int errorCode = 0;
            string errorMessage=string.Empty;
            using (MySqlConnection connection = OpenConnection())
            {
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        // Add parameters to the command
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters.ToArray());
                        }

                        affectedRows = await command.ExecuteNonQueryAsync();

                        // Only retrieve LastInsertedId for INSERT queries
                        if (query.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                        {
                            lastInsertedId = Convert.ToInt32(command.LastInsertedId);
                        }
                    }
                    catch (MySqlException ex)
                    {
                        switch (ex.Number)
                        {
                            case 1062: // Duplicate primary key / unique constraint violation
                                errorMessage = "This item already exists. Please use a unique value.";
                                await GlobalClass.Instance.ShowDialogAsync("Duplicate primary key error: Primary key or unique constraint violation.",xamlRoot);
                                errorCode = 1062;
                                break;

                            case 1451: // Foreign key constraint error
                                errorMessage = "This item is assigned to one or more items and can't be deleted.";
                                await GlobalClass.Instance.ShowDialogAsync("Foreign key error: It is referenced in another table.", xamlRoot);
                                errorCode = 1451;
                                break;
                            case 1644:
                                errorMessage = "Please ensure the name is unique and try again.";
                                await GlobalClass.Instance.ShowDialogAsync("Duplicate root category not allowed.", xamlRoot);
                                errorCode = 1644;
                                break;

                            default:
                                errorMessage = "A database error ocuured.";
                                await GlobalClass.Instance.ShowDialogAsync($"A MySQL error occurred: {ex.Message}",xamlRoot);
                                errorCode = ex.Number;
                                break;
                        }
                        return (-1, lastInsertedId,errorMessage);
                    }
                    catch (Exception ex)
                    {
                        await GlobalClass.Instance.ShowDialogAsync($"An unexpected error occurred: {ex.Message}",xamlRoot);
                        errorCode = -1;
                        return (0, lastInsertedId, errorMessage);
                    }
                    finally
                    {
                        // Ensure connection is closed properly
                        CloseConnection(connection);
                    }
                }
            }

            return (affectedRows, lastInsertedId, errorMessage);
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
                        CloseConnection(connection);
                        Debug.WriteLine($"Error executing query: {ex.Message}");
                        throw;
                    }
                }
            }
        }
        public MySqlDataReader ExecuteReader(string query, List<MySqlParameter> parameters)
        {
             MySqlConnection connection = OpenConnection();
            if (connection.State == ConnectionState.Open)
            {
                MySqlCommand command = new MySqlCommand(query, connection);
                try
                {
                    // Add parameters to prevent SQL injection
                    foreach (var param in parameters)
                    {
                        command.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
                    }
                    return command.ExecuteReader(CommandBehavior.CloseConnection);

                }
                catch (Exception ex)
                {
                    CloseConnection(connection);
                    Debug.WriteLine($"Error executing query: {ex.Message}");
                    throw;
                }
            }
            else
                return null;
        }
        public async Task<int> UpdateRecord(string table, string idColumn, int idValue, Dictionary<string, object> columnValues)
        {
            int updatedRows = 0;

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

                // Add ID parameter separately
                parameters.Add(new MySqlParameter("@Id", idValue));

                // Execute query and deconstruct the tuple result
                var (affectedRows, _,errorMessage) = await ExecuteNonQuery(queryBuilder.ToString(), parameters);

                // Assign affectedRows to updatedRows
                updatedRows = affectedRows;

                // Optional: Handle error codes if needed
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    Console.WriteLine($"Database error occurred with code: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                updatedRows = -1;
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

             return updatedRows;
        }

      
        public bool Delete(string table, string param, object value, out string errorMessage,out int errorCode)
        {
            errorMessage = string.Empty;
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
                        errorCode = 0;
                        return rowsAffected > 0;
                    }
                    catch (MySqlException ex) when (ex.Number == 1451) // Foreign key constraint error
                    {
                        errorMessage = ex.Message;
                        Debug.WriteLine($"Error executing query: {ex.Message}");
                        errorCode = 1451;
                        return false;
                    }
                    catch (Exception ex)
                    {
                        errorMessage = ex.Message;
                        Debug.WriteLine($"Error executing query: {ex.Message}");
                        errorCode = -1;
                        return false;
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
