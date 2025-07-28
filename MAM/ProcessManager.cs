using MAM.Data;
using MAM.Utilities;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MAM
{
    public static class ProcessManager
    {
        private static DataAccess dataAccess=new DataAccess();
        public static ObservableCollection<Process> AllProcesses { get; set; } = new ObservableCollection<Process>();
        public static async Task<Process> CreateProcessAsync(string filePath, ProcessType processType, string status = "Initializing", string result = "Waiting")
        {
            var process = new Process(filePath)
            {
                ProcessType = processType,
                StartTime = DateTime.Now,
                Status = status,
                Progress = 0,
                Result = result
            };

            process.ProcessId = await InsertProcessInDatabaseAsync(process);
            AllProcesses.Add(process);
            return process;
        }

        public static async Task CompleteProcessAsync(Process process,string status="Finished", string result = "Finished")
        {
            await UIThreadHelper.RunOnUIThreadAsync(() =>
            {
                process.CompletionTime = DateTime.Now;
                process.Status = "Finished";
                process.Progress = 100;
                process.Result = result;
            });
            await UpdateProcessStatusInDatabaseAsync(process);
        }

        public static async Task FailProcessAsync(Process process, string errorMessage)
        {
            await UIThreadHelper.RunOnUIThreadAsync(() =>
            {
                process.CompletionTime = DateTime.Now;
                process.Status = $"Error: {errorMessage}";
                process.Result = "Failed";
                process.Progress = 0;
            });
            await UpdateProcessStatusInDatabaseAsync(process);
        }
        public static async Task UpdateProcessStatusInDatabaseAsync(Process process)
        {
            Dictionary<string, object> propsList = new Dictionary<string, object>();
            propsList.Add("start_time", process.StartTime);
            propsList.Add("end_time", process.CompletionTime);
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
            var (affectedRows, newId,errorMessage) = await dataAccess.ExecuteNonQuery(query, propsList);
           
            return affectedRows > 0 ? newId : -1;
        }
    }
}
