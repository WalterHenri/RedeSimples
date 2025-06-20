using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Models;
using Service;

namespace RedeSimples
{
    public partial class NetstatControl : UserControl
    {
        private readonly NetworkCommandService _networkService = new();

        public NetstatControl()
        {
            InitializeComponent();
        }

        private async void BtnRunNetstat_Click(object sender, RoutedEventArgs e)
        {
            BtnRunNetstat.IsEnabled = false;
            TxtStatus.Text = "Executando, por favor aguarde...";
            NetstatGrid.ItemsSource = null;

            try
            {
                string arguments = BuildNetstatArguments();
                string rawOutput = await _networkService.GetNetstatAsync(arguments);
                var entries = await ParseNetstatOutputAsync(rawOutput);

                string filter = TxtFilter.Text;
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    entries = entries.Where(entry =>
                        entry.LocalAddress.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                        entry.ForeignAddress.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                        (entry.State?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        entry.ProcessName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                        entry.PID.ToString().Contains(filter)
                    ).ToList();
                }

                NetstatGrid.ItemsSource = entries;
                TxtStatus.Text = $"Concluído. {entries.Count} resultados encontrados.";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Ocorreu um erro.";
                MessageBox.Show($"Falha ao executar o comando: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnRunNetstat.IsEnabled = true;
            }
        }

        private string BuildNetstatArguments()
        {
            var args = "";
            if (ChkShowAll.IsChecked == true) args += "a";
            if (ChkNumeric.IsChecked == true) args += "n";
            if (ChkShowProcess.IsChecked == true) args += "o";

            return string.IsNullOrEmpty(args) ? "" : $"-{args}";
        }

        private Task<List<NetstatEntry>> ParseNetstatOutputAsync(string rawOutput)
        {
            return Task.Run(() =>
            {
                var entries = new List<NetstatEntry>();
                var lines = rawOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var regex = new Regex(@"^\s*(TCP|UDP)\s+([\d\.:\[\]]+:\S+)\s+([\d\.:\[\]\*]+:\S+)\s*(\w+)?\s*(\d+)?\s*$", RegexOptions.Compiled);

                foreach (var line in lines)
                {
                    if (!line.Trim().StartsWith("TCP") && !line.Trim().StartsWith("UDP")) continue;

                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    var entry = new NetstatEntry
                    {
                        Protocol = match.Groups[1].Value,
                        LocalAddress = match.Groups[2].Value,
                        ForeignAddress = match.Groups[3].Value,
                        State = match.Groups[4].Value,
                        PID = !string.IsNullOrEmpty(match.Groups[5].Value) ? int.Parse(match.Groups[5].Value) : 0
                    };

                    if (entry.PID > 0)
                    {
                        try
                        {
                            var process = Process.GetProcessById(entry.PID);
                            entry.ProcessName = process.ProcessName;
                        }
                        catch (ArgumentException) { entry.ProcessName = "Processo Finalizado"; }
                        catch (Win32Exception) { entry.ProcessName = "Acesso Negado"; }
                        catch (InvalidOperationException) { entry.ProcessName = "Processo Finalizado"; }
                    }
                    else
                    {
                        entry.ProcessName = "N/A";
                    }
                    entries.Add(entry);
                }
                return entries;
            });
        }
    }
}