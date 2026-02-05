using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Timers;
using vatsys;
using vatsys.Plugin;
using static vatsys.FDP2;
using static vatsys.RDP;

namespace VATNZPlugin
{
    [Export(typeof(IPlugin))]
    public class VATNZPlugin : IPlugin
    {
        public string Name => "VATNZ Plugin";

        private readonly string debugPath;
        private readonly Timer extensionTimer;

    
        private readonly Dictionary<string, string> freqToSector = new Dictionary<string, string>()
        {
            { "123.9", "OCR" },
            { "119.5", "BAY" },
            { "123.7", "NAK" },
            { "126.2", "OHA" },
            { "129.3", "STH" },
            { "129.4", "KAI" }
        };

        public VATNZPlugin()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "vatSysPluginDebug"
            );

            Directory.CreateDirectory(folder);
            debugPath = Path.Combine(folder, "vatnz_plugin_debug.txt");

            File.WriteAllText(debugPath, $"[{DateTime.Now}] VATNZPlugin loaded\n");

           
            extensionTimer = new Timer(2000);
            extensionTimer.Elapsed += (s, e) => UpdateExtensions();
            extensionTimer.Start();
        }

        public void OnFDRUpdate(FDR updated) { }
        public void OnRadarTrackUpdate(RadarTrack updated) { }

        private void UpdateExtensions()
        {
            try
            {
                
                if (Network.Me == null || string.IsNullOrEmpty(Network.Me.Callsign))
                {
            
                    var controllerInfo = Network.ControllerInfo;
                    if (controllerInfo != null)
                    {
                        var newInfo = controllerInfo
                            .Where(line => !line.StartsWith("Extending"))
                            .ToArray();

                        Network.ControllerInfo = newInfo;
                        Log("Disconnected — cleared extension line");
                    }

                    return;
                }

                var extending = new List<string>();

                foreach (var freq in Audio.VSCSFrequencies)
                {
                    string freqStr = Conversions.FrequencyToString(freq.Frequency);
                    Log($"VSCS: Name={freq.Name}, Freq={freqStr}, Raw={freq.Frequency}");

                    if (!freq.Transmit)
                        continue;

                   
                    if (Audio.VSCSFrequencies.Any(f =>
    f.Transmit &&
    Math.Abs(f.Frequency - freq.Frequency) < 1 &&
    string.Equals(f.Name, Network.Me?.Callsign, StringComparison.OrdinalIgnoreCase)))
                    {
                        Log($"Skipping own frequency {freqStr}");
                        continue;
                    }

                    if (!freqToSector.TryGetValue(freqStr, out string sector))
                        continue;

                    extending.Add($"{sector} {freqStr}");
                }

                string extendingText = extending.Any()
                    ? $"Extending {DoText(extending)}"
                    : string.Empty;

                UpdateControllerInfo(extendingText);
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex}");
            }
        }

      
        private string DoText(List<string> items)
        {
            return string.Join(", ", items);
        }

        private void UpdateControllerInfo(string extending)
        {
            var controllerInfo = Network.ControllerInfo;
            if (controllerInfo == null)
                return;

            var newInfo = new List<string>();

          
            foreach (var line in controllerInfo)
            {
                if (!line.StartsWith("Extending"))
                    newInfo.Add(line);
            }

            if (!string.IsNullOrEmpty(extending))
                newInfo.Add(extending);

            Network.ControllerInfo = newInfo.ToArray();

            Log($"Updated controller info: {extending}");
        }

        private void Log(string message)
        {
            try
            {
                File.AppendAllText(debugPath, $"[{DateTime.Now}] {message}\n");
            }
            catch
            {
               
            }
        }
    }
}