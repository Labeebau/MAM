using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Net;
using MAM.Data;
using Microsoft.UI.Xaml;
using System.Runtime.Versioning;
using System.Security.AccessControl;

public class NetworkConnection : IDisposable
{
    private readonly string _networkName;

    private NetworkConnection(string networkName)
    {
        _networkName = networkName;
    }
    public static async Task<NetworkConnection?> CreateAsync(
    string networkName,
    NetworkCredential credentials,
    XamlRoot xamlRoot)
    {
        if (string.IsNullOrWhiteSpace(networkName) || !networkName.StartsWith(@"\\"))
        {
            await GlobalClass.Instance.ShowDialogAsync(
                $"Invalid network path: {networkName}. It must be a full UNC path like \\\\host\\sharename.",
                xamlRoot);
            return null;
        }

        var netResource = new NetResource
        {
            Scope = ResourceScope.GlobalNetwork,
            ResourceType = ResourceType.Disk,
            DisplayType = ResourceDisplaytype.Share,
            RemoteName = networkName
        };

        const int maxRetries = 3;
        const int delayMs = 2000;

        string? userName = credentials.UserName;
        string? domain = credentials.Domain;
        string? fullUserName = !string.IsNullOrWhiteSpace(domain)
            ? $@"{domain}\{userName}"
            : userName;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            int result = WNetAddConnection2(
                netResource,
                string.IsNullOrWhiteSpace(credentials.Password) ? null : credentials.Password,
                string.IsNullOrWhiteSpace(userName) ? null : fullUserName,
                0);

            if (result == 0)
            {
                return new NetworkConnection(networkName);
            }

            LogNetworkError(result, networkName, attempt);

            if (attempt < maxRetries)
            {
                await Task.Delay(delayMs);
            }
            else
            {
                string errorMessage = GetNetworkErrorMessage(result);
                await GlobalClass.Instance.ShowDialogAsync(
                    $"Can't connect to {networkName}\n\n{errorMessage}", xamlRoot);
            }
        }

        return null;
    }

    
    private static string GetNetworkErrorMessage(int errorCode)
    {
        return errorCode switch
        {
            5 => "Access denied. Please check your username and password.",
            53 => "The network path was not found.",
            64 => "The specified network name is no longer available.",
            67 => "The network name cannot be found.",
            86 => "The specified network password is not correct.",
            1219 => "Multiple connections to the same network resource are not allowed. Disconnect existing connections first.",
            1326 => "Logon failure: unknown user name or bad password.",
            2202 => "The network name cannot be found. Make sure the server and share name are correct and accessible.",
            _ => $"Unknown error occurred. Error code: {errorCode}"
        };
    }

    private static void LogNetworkError(int errorCode, string networkName, int attempt)
    {
        string logPath = "network_errors.log";
        string message = $"[{DateTime.Now}] Attempt {attempt} - Failed to connect to '{networkName}'. Error {errorCode}: {GetNetworkErrorMessage(errorCode)}";

        try
        {
            File.AppendAllText(logPath, message + Environment.NewLine);
        }
        catch
        {
            // Suppress logging failures
        }
    }

    public void Dispose()
    {
        WNetCancelConnection2(_networkName, 0, true);
    }

[DllImport("mpr.dll")]
    private static extern int WNetAddConnection2(NetResource netResource,
        string password, string username, int flags);

    [DllImport("mpr.dll")]
    private static extern int WNetCancelConnection2(string name, int flags, bool force);

    [StructLayout(LayoutKind.Sequential)]
    private class NetResource
    {
        public ResourceScope Scope;
        public ResourceType ResourceType;
        public ResourceDisplaytype DisplayType;
        public int Usage;
        public string LocalName;
        public string RemoteName;
        public string Comment;
        public string Provider;
    }

    private enum ResourceScope : int
    {
        Connected = 1,
        GlobalNetwork,
        Remembered,
        Recent,
        Context
    }

    private enum ResourceType : int
    {
        Any = 0,
        Disk = 1,
        Print = 2
    }

    private enum ResourceDisplaytype : int
    {
        Generic = 0x0,
        Domain = 0x01,
        Server = 0x02,
        Share = 0x03,
        File = 0x04,
        Group = 0x05
    }
}
