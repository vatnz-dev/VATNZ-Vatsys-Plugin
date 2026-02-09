using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Xml.Linq;
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
        private readonly Dictionary<string, string> callsignToName =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public VATNZPlugin()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "vatSysPluginDebug"
            );

            Directory.CreateDirectory(folder);
            debugPath = Path.Combine(folder, "vatnz_plugin_debug.txt");
            File.WriteAllText(debugPath, $"[{DateTime.Now}] VATNZPlugin loaded\n");

            LoadSectorsFromXml();

            Audio.VSCSFrequenciesChanged += OnVSCSFrequenciesChanged;
            Audio.FrequencyErrorStateChanged += OnVSCSFrequenciesChanged;
            Network.PrimaryFrequencyChanged += OnVSCSFrequenciesChanged;

            var startupTimer = new Timer(1000);
            startupTimer.Elapsed += (s, e) =>
            {
                startupTimer.Stop();
                UpdateExtensions();
            };
            startupTimer.Start();
        }

        public void OnFDRUpdate(FDR updated) { }
        public void OnRadarTrackUpdate(RadarTrack updated) { }

        private string FindNZProfilePath()
        {
            string profilesRoot = Path.Combine(
                Helpers.GetFilesFolder(),
                "Profiles"
            );

            foreach (var folder in Directory.GetDirectories(profilesRoot))
            {
                string sectorsPath = Path.Combine(folder, "Sectors.xml");
                if (!File.Exists(sectorsPath))
                    continue;

                var doc = XDocument.Load(sectorsPath);

                bool isNZ = doc
                    .Descendants("Sector")
                    .Any(x =>
                    {
                        var cs = (string)x.Attribute("Callsign") ?? "";
                        return cs.StartsWith("NZ", StringComparison.OrdinalIgnoreCase)
                               && cs.EndsWith("_CTR", StringComparison.OrdinalIgnoreCase)
                               && x.Element("Volumes") != null;
                    });

                if (isNZ)
                {
                    Log($"Using profile folder: {folder}");
                    return folder;
                }
            }

            return null;
        }

        private void LoadSectorsFromXml()
        {
            string profilePath = FindNZProfilePath();
            if (profilePath == null)
                return;

            string path = Path.Combine(profilePath, "Sectors.xml");
            if (!File.Exists(path))
                return;

            var doc = XDocument.Load(path);

            var sectors = doc
                .Descendants("Sector")
                .Where(x =>
                {
                    var cs = (string)x.Attribute("Callsign") ?? "";
                    return cs.StartsWith("NZ", StringComparison.OrdinalIgnoreCase)
                           && cs.EndsWith("_CTR", StringComparison.OrdinalIgnoreCase)
                           && x.Element("Volumes") != null;
                });

            callsignToName.Clear();

            foreach (var s in sectors)
            {
                string callsign = (string)s.Attribute("Callsign");
                string name = (string)s.Attribute("Name");

                if (string.IsNullOrWhiteSpace(callsign) || string.IsNullOrWhiteSpace(name))
                    continue;

                callsignToName[callsign.Trim()] = name.Trim();
            }

            Log($"Loaded {callsignToName.Count} NZ CTR sectors");
        }

        private void OnVSCSFrequenciesChanged(object sender, EventArgs e)
        {
            UpdateExtensions();
        }

        private void UpdateExtensions()
        {
            if (Network.Me == null || string.IsNullOrEmpty(Network.Me.Callsign))
                return;

            if (!Network.Me.Callsign.EndsWith("_CTR", StringComparison.OrdinalIgnoreCase))
                return;


            var freqs = Audio.VSCSFrequencies;
            if (freqs.Count == 0)
                return;

            var extending = new List<string>();

            foreach (var freq in freqs)
            {
                if (!freq.Transmit)
                    continue;

                if (!freq.Name.EndsWith("_CTR", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.Equals(freq.Name, Network.Me.Callsign, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!callsignToName.TryGetValue(freq.Name, out string shortName))
                    continue;

                string vsFreqStr = Conversions.FrequencyToString(freq.Frequency);
                extending.Add($"{shortName} {vsFreqStr}");
            }

            string extendingText = extending.Any()
                ? $"Extending {string.Join(", ", extending)}"
                : string.Empty;

            UpdateControllerInfo(extendingText);
        }

        private void UpdateControllerInfo(string extending)
        {
            var info = Network.ControllerInfo;
            if (info == null)
                return;

            var newInfo = new List<string>();

            foreach (var line in info)
            {
                if (!line.StartsWith("Extending", StringComparison.OrdinalIgnoreCase))
                    newInfo.Add(line);
            }

            if (!string.IsNullOrEmpty(extending))
                newInfo.Add(extending);

            if (newInfo.Count > 5)
                newInfo = newInfo.Skip(newInfo.Count - 5).ToList();

            Network.ControllerInfo = newInfo.ToArray();

            Log($"Updated controller info: {extending}");
        }

        private void Log(string msg)
        {
            File.AppendAllText(debugPath, $"[{DateTime.Now}] {msg}\n");
        }
    }
}
