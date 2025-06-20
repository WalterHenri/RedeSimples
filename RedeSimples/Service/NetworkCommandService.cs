using System.Diagnostics;
using System.Threading.Tasks;

namespace Service;

public class NetworkCommandService
{
    private async Task<string> RunCommandAsync(string command, string args)
    {
        ProcessStartInfo processInfo = new ProcessStartInfo(command, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true 
        };

        using (Process process = new Process { StartInfo = processInfo })
        {
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!string.IsNullOrEmpty(error))
            {
                return $"Erro: {error}";
            }
            return output;
        }
    }

    public Task<string> PingAsync(string ipAddress)
    {
        return RunCommandAsync("ping", $"-n 1 {ipAddress}");
    }

    public Task<string> GetNetstatAsync(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return RunCommandAsync("netstat", "");
        }
        return RunCommandAsync("netstat", arguments);
    }

    public Task<string> PingAsync(string ipAddress, int pacotes)
    {
        
        return RunCommandAsync("ping", $"-n {pacotes} {ipAddress}");
    }

    public Task<string> TraceRouteAsync(string ipAddress)
    {
        return RunCommandAsync("tracert", $"-d {ipAddress}"); // -d para não resolver nomes, mais rápido
    }

    public Task<string> GetNetstatAsync()
    {
        return RunCommandAsync("netstat", "-ano");
    }

    public Task<string> GetArpTableAsync()
    {
        return RunCommandAsync("arp", "-a");
    }
}