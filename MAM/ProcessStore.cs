using MAM.Data;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAM
{
    public static class ProcessStore
    {
        private static DataAccess dataAccess=new DataAccess();
        public static ObservableCollection<Process> AllProcesses { get; set; } = new ObservableCollection<Process>();
        public static async Task UpdateProcessStatusInDatabaseAsync(Process process)
        {
            Dictionary<string, object> propsList = new Dictionary<string, object>();
            propsList.Add("start_time", process.StartTime);
            propsList.Add("end_time", process.StartTime);
            propsList.Add("status", process.Status);
            propsList.Add("result", process.Result);
            int result = await dataAccess.UpdateRecord("process", "process_id", process.ProcessId, propsList);
        }
        public static async Task<int> InsertProcessInDatabaseAsync(Process process)
        {
            List<MySqlParameter> propsList = new();
            propsList.Add(new MySqlParameter("@Server", process.Server));
            propsList.Add(new MySqlParameter("@FilePath", process.FilePath));
            propsList.Add(new MySqlParameter("@Type", process.ProcessType));

            propsList.Add(new MySqlParameter("@StartTime", process.StartTime));
            propsList.Add(new MySqlParameter("@Status", process.Status));
            propsList.Add(new MySqlParameter("@Result", process.Result));
            string query = "insert into process (server,file_name,type,start_time,status,result)values(@Server,@FilePath,@Type,@StartTime,@Status,@Result)";
            var (affectedRows, newId, errorCode) = await dataAccess.ExecuteNonQuery(query, propsList);
            return affectedRows > 0 ? newId : -1;
        }
    }
}
