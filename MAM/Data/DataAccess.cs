using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;
using System.Text;
using Microsoft.UI.Xaml;
using MAM.Views.AdminPanelViews;

namespace MAM.Data
{
    public class DataAccess
    {
        public static string ConnectionString { get; set; }
        private readonly XamlRoot _xamlRoot;

        public DataAccess()
        {
            if (App.MainAppWindow != null)
                _xamlRoot = App.MainAppWindow.Content.XamlRoot;
        }

        public DataAccess(string connectionString)
        {
            ConnectionString = connectionString;
        }

        #region Connection Handling
        private async Task<MySqlConnection?> OpenConnectionAsync(int maxRetries = 3, int delayMs = 1000)
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                Debug.WriteLine("[DB] Connection string not set.");
                return null;
            }

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var connection = new MySqlConnection(ConnectionString);

                try
                {
                    await connection.OpenAsync();
                    return connection; // ✅ success
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] Connection attempt {attempt} failed: {ex.Message}");

                    if (attempt == maxRetries)
                    {
                        Debug.WriteLine("[DB] Max retries reached. Returning null.");
                        return null;
                    }

                    await Task.Delay(delayMs);
                }
            }

            return null;
        }
        #endregion
        public async Task<MySqlDataReader?> ExecuteReaderStoredProcedure(string procedureName, List<MySqlParameter>? parameters = null)
        {
            var connection = await OpenConnectionAsync();
            if (connection == null)
            {
                Debug.WriteLine("[DB] No connection available.");
                return null; // return null so caller can handle
            }

            var command = new MySqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

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
                Debug.WriteLine($"Error executing SP {procedureName}: {ex.Message}");
                connection.Close();
                return null;
            }
        }
        #region ExecuteNonQuery (Insert/Update/Delete)
        public async Task<(int affectedRows, int lastInsertedId, string errorMessage)> ExecuteNonQuery(
            string query, List<MySqlParameter>? parameters = null)
        {
            try
            {
                await using var connection = await OpenConnectionAsync();
                if (connection == null) return (0, -1, "Database connection failed.");

                await using var command = new MySqlCommand(query, connection);

                if (parameters != null)
                    command.Parameters.AddRange(parameters.ToArray());

                int affectedRows = await command.ExecuteNonQueryAsync();
                int lastInsertedId = query.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase)
                    ? Convert.ToInt32(command.LastInsertedId)
                    : -1;

                return (affectedRows, lastInsertedId, string.Empty);
            }
            catch (MySqlException ex)
            {
                Debug.WriteLine($"[DB Error] {ex.Message}");
                return (0, -1, ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB Error] {ex.Message}");
                return (0, -1, "Unexpected error: " + ex.Message);
            }
        }
        #endregion

        #region ExecuteScalar
        public async Task<object?> ExecuteScalar(string query, Dictionary<string, object>? parameters = null)
        {
            try
            {
                await using var connection = await OpenConnectionAsync();
                if (connection == null) return null;

                await using var command = new MySqlCommand(query, connection);
                if (parameters != null)
                {
                    foreach (var param in parameters)
                        command.Parameters.AddWithValue(param.Key, param.Value);
                }

                return await command.ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB Scalar Error] {ex.Message}");
                return null;
            }
        }
        #endregion

        #region ExecuteReader
        public async Task<MySqlDataReader?> ExecuteReader(string query, List<MySqlParameter>? parameters = null)
        {
            try
            {
                var connection = await OpenConnectionAsync();
                if (connection == null) return null;

                var command = new MySqlCommand(query, connection);
                if (parameters != null)
                {
                    foreach (var param in parameters)
                        command.Parameters.Add(param);
                }

                return await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB Reader Error] {ex.Message}");
                return null;
            }
        }
        #endregion
        /// <summary>
        /// Executes a stored procedure that does not return a result set.
        /// Returns the number of affected rows.
        /// </summary>
        public async Task<int> ExecuteNonQueryStoredProcedure(
            string procedureName, List<MySqlParameter>? parameters = null)
        {
            try
            {
                await using var connection = await OpenConnectionAsync();
                await using var command = new MySqlCommand(procedureName, connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                if (parameters != null)
                {
                    foreach (var param in parameters)
                        command.Parameters.AddWithValue(param.ParameterName, param.Value ?? DBNull.Value);
                }

                return await command.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Debug.WriteLine($"[DB] Stored procedure {procedureName} failed: {ex.Message}");
                return 0;
            }
        }
        public async Task<int> GetId(string query, List<MySqlParameter> parameters = null)
        {
            int id = -1;
            try
            {
                await using var connection = await OpenConnectionAsync();
                if (connection == null) return id;

                await using var command = new MySqlCommand(query, connection);
                if (parameters != null)
                    command.Parameters.AddRange(parameters.ToArray());
                        var res = command.ExecuteScalar();
                        if (res != null)
                            id = Convert.ToInt32(res);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB Data Error] {ex.Message}");
            }
            return id;
        }
        public async Task<(int userId, string passwordHash, string passwordSalt, int groupId, string groupName)>
    GetUserCredentials(string userName)
        {
            string query = @"
        SELECT u.user_id, u.password_hash, u.password_salt, ug.group_id, ug.group_name
        FROM user u
        INNER JOIN user_roles ur ON u.user_id = ur.user_id
        INNER JOIN user_group ug ON ug.group_id = ur.group_id
        WHERE user_name = @userName";

            var parameters = new List<MySqlParameter> { new("@userName", userName) };

            try
            {
                using var reader = await ExecuteReader(query, parameters);
                if (reader == null)  // connection failed
                    return (-1, string.Empty, string.Empty, -1, string.Empty);

                if (await reader.ReadAsync())
                {
                    return (
                        reader.GetInt32("user_id"),
                        reader["password_hash"]?.ToString() ?? string.Empty,
                        reader["password_salt"]?.ToString() ?? string.Empty,
                        reader.GetInt32("group_id"),
                        reader["group_name"]?.ToString() ?? string.Empty
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB GetUserCredentials Error] {ex.Message}");
            }

            return (-1, string.Empty, string.Empty, -1, string.Empty);
        }

        #region GetDataAsync (DataTable)
        public async Task<DataTable> GetDataAsync(string query, List<MySqlParameter>? parameters = null)
        {
            DataTable dt = new();
            try
            {
                await using var connection = await OpenConnectionAsync();
                if (connection == null) return dt;

                await using var command = new MySqlCommand(query, connection);
                if (parameters != null)
                    command.Parameters.AddRange(parameters.ToArray());

                using var reader = await command.ExecuteReaderAsync();
                dt.Load(reader);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB Data Error] {ex.Message}");
            }
            return dt;
        }
        #endregion

        #region GetUsers (example entity mapping)
        public async Task<List<User>> GetUsers()
        {
            List<User> users = new();
            string query = @"SELECT u.user_id, u.first_name, u.last_name, u.email, u.user_name, u.password_hash,
                             u.ad_user, u.active,
                             EXISTS(SELECT 1 FROM user_roles ur2 
                                    JOIN user_group ug2 ON ur2.group_id = ug2.group_id 
                                    WHERE ur2.user_id = u.user_id AND ug2.group_name = 'Admin') AS IsAdmin
                             FROM user u;";

            try
            {
                await using var connection = await OpenConnectionAsync();
                if (connection == null) return users;

                await using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    users.Add(new User
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
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB Users Error] {ex.Message}");
            }

            return users;
        }
        #endregion
        public async Task<int> UpdateRecord(
    string table,
    string idColumn,
    int idValue,
    Dictionary<string, object> columnValues)
        {
            if (string.IsNullOrWhiteSpace(table))
                throw new ArgumentException("Table name cannot be null or empty.", nameof(table));

            if (string.IsNullOrWhiteSpace(idColumn))
                throw new ArgumentException("Id column cannot be null or empty.", nameof(idColumn));

            if (columnValues == null || columnValues.Count == 0)
                throw new ArgumentException("Column values cannot be null or empty.", nameof(columnValues));

            try
            {
                // Build query dynamically
                var queryBuilder = new StringBuilder($"UPDATE {table} SET ");
                var parameters = new List<MySqlParameter>();

                foreach (var kvp in columnValues)
                {
                    string columnName = kvp.Key;
                    object value = kvp.Value;

                    queryBuilder.Append($"{columnName} = @{columnName}, ");
                    parameters.Add(new MySqlParameter($"@{columnName}", value ?? DBNull.Value));
                }

                // Remove trailing ", " and add WHERE clause
                queryBuilder.Length -= 2;
                queryBuilder.Append($" WHERE {idColumn} = @Id");

                parameters.Add(new MySqlParameter("@Id", idValue));

                // Execute update
                await using var connection = await OpenConnectionAsync();
                await using var command = new MySqlCommand(queryBuilder.ToString(), connection);

                foreach (var param in parameters)
                    command.Parameters.Add(param);

                return await command.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Debug.WriteLine($"[DB] Update failed on table {table}: {ex.Message}");
                return -1; // indicate failure
            }
        }

        #region Delete
        public async Task<(int rowsAffected, string errorMessage, int errorCode)> Delete(
     string table, string param, object value)
        {
            string errorMessage = string.Empty;
            int errorCode = 0;
            int rowsAffected = 0;

            // Use parameterized query to prevent SQL injection
            string query = $"DELETE FROM {table} WHERE {param} = @{param};";

            try
            {
                await using var connection = await OpenConnectionAsync();

                // ✅ Check for null or closed connection
                if (connection == null || connection.State != ConnectionState.Open)
                {
                    return (0, "Database connection could not be established.", -100);
                }

                await using var command = new MySqlCommand(query, connection);

                // Add parameter properly
                command.Parameters.AddWithValue($"@{param}", value);

                rowsAffected = await command.ExecuteNonQueryAsync();

                return (rowsAffected, errorMessage, errorCode);
            }
            catch (MySqlException ex) when (ex.Number == 1451) // Foreign key constraint error
            {
                errorMessage = ex.Message;
                Debug.WriteLine($"[Delete Error] {ex.Message}");
                errorCode = 1451;
                return (rowsAffected, errorMessage, errorCode);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Debug.WriteLine($"[Delete Error] {ex.Message}");
                errorCode = -1;
                return (rowsAffected, errorMessage, errorCode);
            }
        }

        #endregion
    }
}










//using MAM.Views.AdminPanelViews;
//using Microsoft.WindowsAppSDK.Runtime.Packages;
//using MySql.Data.MySqlClient;
//using System.Data;
//using System.Diagnostics;
//using System.Text;
//using System.Xml.Linq;
//using System.Xml;
//using Microsoft.UI.Xaml.Controls.Primitives;
//using Microsoft.UI.Xaml;

//namespace MAM.Data
//{
//    public class DataAccess
//    {
//        //private readonly string _connectionString = "server=localhost;uid=root;pwd=root;database=mam;Connection Timeout=30;";
//        public static string ConnectionString { get; set; }//= "server=localhost;uid=root;pwd=root;database=mam;Connection Timeout=30;";
//        public static List<string> ConnectionStringList { get; set; }
//        private XamlRoot xamlRoot;
//        public DataAccess()
//        {
//            if(App.MainAppWindow!=null)
//                xamlRoot = App.MainAppWindow.Content.XamlRoot;
//        }
//        // Constructor to initialize with a connection string
//        public DataAccess(string connectionString)
//        {
//            ConnectionString = connectionString;
//        }

//        // Open MySQL connection
//        private async Task<MySqlConnection> OpenConnectionAsync(int maxRetries = 3, int delayMilliseconds = 1000)
//        {
//            var connection = new MySqlConnection(ConnectionString);

//            for (int attempt = 1; attempt <= maxRetries; attempt++)
//            {
//                try
//                {
//                    await connection.OpenAsync();
//                    return connection; // success
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[DB] Connection attempt {attempt} failed: {ex.Message}");

//                    if (attempt < maxRetries)
//                    {
//                        await Task.Delay(delayMilliseconds);
//                    }
//                }
//            }
//            Debug.WriteLine("[DB] Could not establish a database connection after retries.");
//            return null; // return null instead of crashing
//        }

//        // Close MySQL connection
//        private void CloseConnection(MySqlConnection connection)
//        {
//            if (connection != null && connection.State == System.Data.ConnectionState.Open)
//            {
//                connection.Close();
//            }
//        }
//        public async Task<MySqlDataReader?> ExecuteReaderStoredProcedure(string procedureName, List<MySqlParameter>? parameters = null)
//        {
//            var connection = await OpenConnectionAsync();
//            if (connection == null)
//            {
//                Debug.WriteLine("[DB] No connection available.");
//                return null; // return null so caller can handle
//            }

//            var command = new MySqlCommand(procedureName, connection)
//            {
//                CommandType = CommandType.StoredProcedure
//            };

//            if (parameters != null)
//            {
//                foreach (var param in parameters)
//                {
//                    command.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
//                }
//            }

//            try
//            {
//                var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
//                return reader;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Error executing SP {procedureName}: {ex.Message}");
//                connection.Close();
//                return null;
//            }
//        }

//        //public async Task<MySqlDataReader> ExecuteReaderStoredProcedure(string procedureName, List<MySqlParameter> parameters=null)
//        //{
//        //    MySqlConnection connection =await OpenConnectionAsync();

//        //        if (connection.State == ConnectionState.Open)
//        //        {
//        //            var command = new MySqlCommand(procedureName, connection)
//        //            {
//        //                CommandType = CommandType.StoredProcedure
//        //            };
//        //            // Add parameters to prevent SQL injection
//        //            if (parameters != null)
//        //            {
//        //                foreach (var param in parameters)
//        //                {
//        //                    command.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
//        //                }
//        //            }
//        //            try
//        //            {
//        //                var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
//        //                return reader;
//        //            }

//        //            catch (Exception ex)
//        //            {
//        //                CloseConnection(connection);
//        //                Debug.WriteLine($"Error executing query: {ex.Message}");
//        //                return null;
//        //            }
//        //        }
//        //        return null;
//        //}
//        public async Task<int> ExecuteNonQueryStoredProcedure(string procedureName, List<MySqlParameter> parameters)
//        {
//            try
//            {
//                await using (var connection =await OpenConnectionAsync())
//                {
//                    if(connection.State==ConnectionState.Open)
//                    {
//                        using (var command = new MySqlCommand(procedureName, connection))
//                        {
//                            command.CommandType = CommandType.StoredProcedure;
//                            // Add parameters if any
//                            if (parameters != null)
//                            {
//                                foreach (var param in parameters)
//                                {
//                                    command.Parameters.AddWithValue(param.ParameterName, param.Value);
//                                }
//                            }
//                            return await command.ExecuteNonQueryAsync();
//                        }
//                    }
//                    else return 0;
//                }
//            }
//            catch (MySqlException ex)
//            {
//                Console.WriteLine("Invalid UserId "+ex.ToString());
//                return 0;
//            }
//        }

//        public async Task<(int, string, string, int, string)> GetUserCredentials(string userName)
//        {
//            string query = @"SELECT u.user_id, u.password_hash, u.password_salt, ug.group_id, ug.group_name
//                            FROM user u
//                            INNER JOIN user_roles ur ON u.user_id = ur.user_id
//                            INNER JOIN user_group ug ON ug.group_id = ur.group_id
//                            WHERE user_name = @userName";

//            var parameters = new List<MySqlParameter> { new("@userName", userName) };

//            using var reader =await ExecuteReader(query, parameters);
//            if (reader!=null && await reader.ReadAsync())
//            {
//                return (
//                    Convert.ToInt32(reader["user_id"]),
//                    reader["password_hash"].ToString(),
//                    reader["password_salt"].ToString(),
//                    Convert.ToInt32(reader["group_id"]),
//                    reader["group_name"].ToString()
//                );
//            }

//            return (-1, string.Empty, string.Empty, -1, string.Empty);
//        }


//        public async Task<int> GetId(string query, List<MySqlParameter> parameters = null)
//        {
//            int id = -1;
//            await using (MySqlConnection connection =await OpenConnectionAsync())
//            {
//                using (MySqlCommand command = new MySqlCommand(query, connection))
//                {

//                    try
//                    {
//                        foreach (var param in parameters)
//                        {
//                            command.Parameters.AddWithValue(param.ParameterName, param.Value);
//                        }
//                        var res = command.ExecuteScalar();
//                        if (res != null)
//                            id = Convert.ToInt32(res);
//                    }
//                    catch (Exception ex)
//                    {
//                        Debug.WriteLine($"Error executing query: {ex.Message}");
//                        throw;
//                    }
//                }
//                CloseConnection(connection);
//            }
//            return id;

//        }
//        public async Task<string> GetString(string query, Dictionary<string, object> parameters = null)
//        {
//            string result=string.Empty;
//            await using (MySqlConnection connection =await OpenConnectionAsync())
//            {
//                using (MySqlCommand command = new MySqlCommand(query, connection))
//                {

//                    try
//                    {
//                        foreach (var param in parameters)
//                        {
//                            command.Parameters.AddWithValue(param.Key, param.Value);
//                        }
//                        var res = command.ExecuteScalar();
//                        if (res != null)
//                            result = res.ToString();                    }
//                    catch (Exception ex)
//                    {
//                        Debug.WriteLine($"Error executing query: {ex.Message}");
//                        throw;
//                    }
//                }
//                CloseConnection(connection);
//            }
//            return result;
//        }

//        public async Task<DataTable> GetDataAsync(string query, List<MySqlParameter> parameters = null)
//        {
//            DataTable dataTable = new DataTable();
//            await using (MySqlConnection connection =await OpenConnectionAsync())
//            {
//                using (MySqlCommand command = new MySqlCommand(query, connection))
//                {
//                    try
//                    {
//                        if (parameters != null)
//                        {
//                            foreach (var param in parameters)
//                            {
//                                command.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
//                            }
//                        }
//                        using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
//                        {
//                            if (reader.HasRows)
//                                dataTable.Load(reader);
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        Debug.WriteLine($"Error executing query: {ex.Message}");
//                        throw;
//                    }
//                }
//                CloseConnection(connection);
//            }
//            return dataTable;
//        }
//        // Method to retrieve data (example: for fetching rows)
//        public async Task<List<User>> GetUsers()
//        {
//            List<User> UserList = new List<User>();
//            string query = "SELECT u.user_id, u.first_name, u.last_name, u.email, u.user_name, u.password_hash," +
//                           "u.ad_user, u.active," +
//                           "EXISTS(" +
//                                   "SELECT 1 " +
//                                   "FROM user_roles ur2 " +
//                                   "JOIN user_group ug2 ON ur2.group_id = ug2.group_id " +
//                                   "WHERE ur2.user_id = u.user_id AND ug2.group_name = 'Admin' " +
//                                  ") AS IsAdmin " +
//                           "FROM user u;";

//            await using (MySqlConnection connection =await OpenConnectionAsync())
//            {
//                using (MySqlCommand command = new MySqlCommand(query, connection))
//                {
//                    try
//                    {
//                        using (MySqlDataReader reader = command.ExecuteReader())
//                        {
//                            while (reader.Read())
//                            {
//                                // Assume columns in the table are: ID, Name, Age
//                                UserList.Add(new User
//                                {
//                                    UserId = reader.GetInt32("user_id"),
//                                    FirstName = reader.GetString("first_name"),
//                                    LastName = reader.GetString("last_name"),
//                                    Email = reader.GetString("email"),
//                                    UserName = reader.GetString("user_name"),
//                                    Password = reader.GetString("password_hash"),
//                                    IsADUser = reader.GetBoolean("ad_user"),
//                                    IsActive = reader.GetBoolean("active"),
//                                    IsAdmin = reader.GetBoolean("IsAdmin"),
//                                });
//                            }
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        Debug.WriteLine($"Error executing query: {ex.Message}");
//                        throw;
//                    }
//                }

//                CloseConnection(connection);
//            }

//            return UserList;
//        }

//        public async Task<List<string>> GetUserGroups()
//        {
//            List<string> UserGroupList = new List<string>();
//            string query = "select group_name from user_group";

//            await using (MySqlConnection connection =await OpenConnectionAsync())
//            {
//                using (MySqlCommand command = new MySqlCommand(query, connection))
//                {
//                    try
//                    {
//                        using (MySqlDataReader reader = command.ExecuteReader())
//                        {
//                            while (reader.Read())
//                            {
//                                UserGroupList.Add(reader.GetString("group_name"));
//                            }
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        Debug.WriteLine($"Error executing query: {ex.Message}");
//                        throw;
//                    }
//                }
//                CloseConnection(connection);
//            }
//            return UserGroupList;
//        }

//        // Method to execute non-query operations (INSERT/UPDATE/DELETE)
//        public async Task<(int affectedRows, int lastInsertedId,string errorMessage)> ExecuteNonQuery(string query, List<MySqlParameter> parameters)
//        {
//            int affectedRows = 0;
//            int lastInsertedId = -1;
//            int errorCode = 0;
//            string errorMessage=string.Empty;
//            await using (MySqlConnection connection =await OpenConnectionAsync())
//            {
//                using (MySqlCommand command = new MySqlCommand(query, connection))
//                {
//                    try
//                    {
//                        // Add parameters to the command
//                        if (parameters != null)
//                        {
//                            command.Parameters.AddRange(parameters.ToArray());
//                        }

//                        affectedRows = await command.ExecuteNonQueryAsync();

//                        // Only retrieve LastInsertedId for INSERT queries
//                        if (query.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
//                        {
//                            lastInsertedId = Convert.ToInt32(command.LastInsertedId);
//                        }
//                    }
//                    catch (MySqlException ex)
//                    {
//                        switch (ex.Number)
//                        {
//                            case 1062: // Duplicate primary key / unique constraint violation
//                                errorMessage = "This item already exists. Please use a unique value.";
//                                await GlobalClass.Instance.ShowDialogAsync("Duplicate primary key error: Primary key or unique constraint violation.",xamlRoot);
//                                errorCode = 1062;
//                                break;

//                            case 1451: // Foreign key constraint error
//                                errorMessage = "This item is assigned to one or more items and can't be deleted.";
//                                await GlobalClass.Instance.ShowDialogAsync("Foreign key error: It is referenced in another table.", xamlRoot);
//                                errorCode = 1451;
//                                break;
//                            case 1644:
//                                errorMessage = "Please ensure the name is unique and try again.";
//                                await GlobalClass.Instance.ShowDialogAsync("Duplicate root category not allowed.", xamlRoot);
//                                errorCode = 1644;
//                                break;

//                            default:
//                                errorMessage = "A database error ocuured.";
//                                await GlobalClass.Instance.ShowDialogAsync($"A MySQL error occurred: {ex.Message}",xamlRoot);
//                                errorCode = ex.Number;
//                                break;
//                        }
//                        return (-1, lastInsertedId,errorMessage);
//                    }
//                    catch (Exception ex)
//                    {
//                        await GlobalClass.Instance.ShowDialogAsync($"An unexpected error occurred: {ex.Message}",xamlRoot);
//                        errorCode = -1;
//                        return (0, lastInsertedId, errorMessage);
//                    }
//                    finally
//                    {
//                        // Ensure connection is closed properly
//                        CloseConnection(connection);
//                    }
//                }
//            }

//            return (affectedRows, lastInsertedId, errorMessage);
//        }



//        public async Task<bool> ExecuteScalar(string query, Dictionary<string, object> parameters)
//        {
//            await using (MySqlConnection connection =await OpenConnectionAsync())
//            {
//                using (MySqlCommand command = new MySqlCommand(query, connection))
//                {
//                    try
//                    {
//                        // Add parameters to prevent SQL injection
//                        foreach (var param in parameters)
//                        {
//                            command.Parameters.AddWithValue(param.Key, param.Value);
//                        }
//                        var result = command.ExecuteScalar();
//                        CloseConnection(connection);
//                        return Convert.ToInt32(result) > 0;
//                    }
//                    catch (Exception ex)
//                    {
//                        CloseConnection(connection);
//                        Debug.WriteLine($"Error executing query: {ex.Message}");
//                        throw;
//                    }
//                }
//            }
//        }
//        public async Task<MySqlDataReader> ExecuteReader(string query, List<MySqlParameter> parameters)
//        {
//             MySqlConnection connection =await OpenConnectionAsync();
//            if (connection.State == ConnectionState.Open)
//            {
//                MySqlCommand command = new MySqlCommand(query, connection);
//                try
//                {
//                    // Add parameters to prevent SQL injection
//                    foreach (var param in parameters)
//                    {
//                        command.Parameters.Add(new MySqlParameter(param.ParameterName, param.Value));
//                    }
//                    return command.ExecuteReader(CommandBehavior.CloseConnection);

//                }
//                catch (Exception ex)
//                {
//                    CloseConnection(connection);
//                    Debug.WriteLine($"Error executing query: {ex.Message}");
//                    throw;
//                }
//            }
//            else
//                return null;
//        }
//        public async Task<int> UpdateRecord(string table, string idColumn, int idValue, Dictionary<string, object> columnValues)
//        {
//            int updatedRows = 0;

//            try
//            {
//                // Construct the update query dynamically based on the provided column names
//                var queryBuilder = new StringBuilder($"UPDATE {table} SET ");
//                var parameters = new List<MySqlParameter>();

//                foreach (var kvp in columnValues)
//                {
//                    string columnName = kvp.Key;
//                    object value = kvp.Value;

//                    // Append the column name to the query with a parameter placeholder
//                    queryBuilder.Append($"{columnName} = @{columnName}, ");

//                    // Add the parameter to the list
//                    parameters.Add(new MySqlParameter($"@{columnName}", value));
//                }

//                // Remove the trailing comma and space, then add the WHERE clause
//                queryBuilder.Length -= 2; // Remove last comma and space
//                queryBuilder.Append($" WHERE {idColumn} = @Id");

//                // Add ID parameter separately
//                parameters.Add(new MySqlParameter("@Id", idValue));

//                // Execute query and deconstruct the tuple result
//                var (affectedRows, _,errorMessage) = await ExecuteNonQuery(queryBuilder.ToString(), parameters);

//                // Assign affectedRows to updatedRows
//                updatedRows = affectedRows;

//                // Optional: Handle error codes if needed
//                if (!string.IsNullOrEmpty(errorMessage))
//                {
//                    Console.WriteLine($"Database error occurred with code: {errorMessage}");
//                }
//            }
//            catch (Exception ex)
//            {
//                updatedRows = -1;
//                Console.WriteLine($"An error occurred: {ex.Message}");
//            }

//             return updatedRows;
//        }

//        public async Task<(int rowsAffected, string errorMessage, int errorCode)> Delete(string table, string param, object value)
//        {
//            string errorMessage = string.Empty;
//            int errorCode = 0;
//            int rowsAffected = 0;

//            // Use parameterized query to prevent SQL injection
//            string query = $"DELETE FROM {table} WHERE {param} = @{param};";

//            try
//            {
//                await using var connection = await OpenConnectionAsync();
//                await using var command = new MySqlCommand(query, connection);

//                // Add parameter properly
//                command.Parameters.AddWithValue($"@{param}", value);

//                rowsAffected = await command.ExecuteNonQueryAsync();

//                return (rowsAffected, errorMessage, errorCode);
//            }
//            catch (MySqlException ex) when (ex.Number == 1451) // Foreign key constraint error
//            {
//                errorMessage = ex.Message;
//                Debug.WriteLine($"[Delete Error] {ex.Message}");
//                errorCode = 1451;
//                return (rowsAffected, errorMessage, errorCode);
//            }
//            catch (Exception ex)
//            {
//                errorMessage = ex.Message;
//                Debug.WriteLine($"[Delete Error] {ex.Message}");
//                errorCode = -1;
//                return (rowsAffected, errorMessage, errorCode);
//            }
//        }
//        public async Task<bool> DeleteAll(string query)
//        {
//            using (MySqlConnection connection =await OpenConnectionAsync())
//            {
//                using (MySqlCommand command = new MySqlCommand(query, connection))
//                {
//                    try
//                    {

//                        var result = command.ExecuteScalar();
//                        CloseConnection(connection);

//                        return Convert.ToInt32(result) > 0;
//                    }
//                    catch (Exception ex)
//                    {
//                        Debug.WriteLine($"Error executing query: {ex.Message}");
//                        throw;
//                    }

//                }
//            }
//        }
//    }
//}



