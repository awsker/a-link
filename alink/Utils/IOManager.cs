using System;
using System.Collections.Generic;
using System.IO;
using alink.Models;

namespace alink.Utils
{
    public static class IOManager
    {

        private const string memoryOffsetFilename = "memory_offsets.txt";
        private const string netSettingsFilename = "netsettings.txt";
        private const string rulesConfigsFolderName = "rules";

        private static string currentDirectory { get { return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location); } }
        private static string memoryOffsetFileFullPath { get { return Path.Combine(currentDirectory, memoryOffsetFilename); } }
        private static string netSettingsFileFullPath { get { return Path.Combine(currentDirectory, netSettingsFilename); } }
        private static string rulesConfigFolderFullPath { get { return Path.Combine(currentDirectory, rulesConfigsFolderName); } }

        public static IEnumerable<MemoryOffset> ReadMemoryOffsetsFromFile(string filename = null)
        {
            try
            {
                var list = new List<MemoryOffset>();
                if (File.Exists(filename ?? memoryOffsetFileFullPath))
                {
                    using (var sr = new StreamReader(filename ?? memoryOffsetFileFullPath))
                    {
                        string row;
                        while ((row = sr.ReadLine()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(row))
                                continue;
                            list.Add(new MemoryOffset(row));
                        }
                        sr.BaseStream.Close();
                        sr.Close();
                    }
                }
                return list;
            }
            catch (Exception)
            {
                return new MemoryOffset[0];
            }
        }

        public static void WriteMemoryOffsetsToFile(IEnumerable<MemoryOffset> memoryOffsets, string filename = null)
        {
            using (var sw = new StreamWriter(filename ?? memoryOffsetFileFullPath, false))
            {
                foreach (var mo in memoryOffsets)
                {
                    sw.WriteLine(mo.Serialize());
                }
            }
        }

        public static IEnumerable<RulesConfig> ReadRulesConfigsFromLocation(string location = null)
        {
            try
            {
                var list = new List<RulesConfig>();

                if (Directory.Exists(rulesConfigFolderFullPath))
                {
                    foreach (var file in Directory.GetFiles(rulesConfigFolderFullPath, "*.txt"))
                    {
                        var newRule = readRulesConfig(file);
                        if (newRule != null)
                            list.Add(newRule);
                    }
                }

                return list;
            }
            catch (Exception)
            {
                return new RulesConfig[0];
            }
        }

        private static RulesConfig readRulesConfig(string filePath)
        {
            try
            {
                RulesConfig rule = null;

                if (File.Exists(filePath))
                {
                    using (var sr = new StreamReader(filePath))
                    {
                        string row;
                        while ((row = sr.ReadLine()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(row))
                                continue;
                            if (rule == null)
                                rule = new RulesConfig(row) {Filename = filePath};
                            else
                                rule.Rules.Add(new MemoryRule(row));
                        }

                        sr.BaseStream.Close();
                        sr.Close();
                    }
                }
                return rule;
            }
            catch (Exception)
            {
                return null;
            }
        }

        
        public static void WriteRulesConfig(RulesConfig config, string location = null)
        {
            var filename = generateFilename(config, location ?? rulesConfigFolderFullPath);
            config.Filename = filename;

            Directory.CreateDirectory(location ?? rulesConfigFolderFullPath);

            using (var sw = new StreamWriter(filename, false))
            {
                sw.WriteLine(config.Description);
                foreach (var rule in config.Rules)
                {
                    sw.WriteLine(rule.Serialize());
                }
            }
        }

        public static NetSettings ReadNetSettings(string filename = null)
        {
            NetSettings settings = null;
            try
            {
                if (File.Exists(filename ?? netSettingsFileFullPath))
                {
                    using (var sr = new StreamReader(filename ?? netSettingsFileFullPath))
                    {
                        string row = sr.ReadLine();
                        settings = new NetSettings(row);
                        sr.BaseStream.Close();
                        sr.Close();
                    }
                }
                return settings;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void WriteNetSettings(NetSettings settings, string filename = null)
        {
            using (var sw = new StreamWriter(filename ?? netSettingsFileFullPath, false))
            {
                sw.WriteLine(settings.Serialize());
            }
        }

        private static string generateFilename(RulesConfig config, string location)
        {
            if (config.Filename != null)
                return config.Filename;
            var startName = config.Description.Replace(" ", "_") + ".txt";
            var path = Path.Combine(location, startName);
            int startInt = 1;
            while (File.Exists(path))
            {
                path = Path.Combine(location, startName + "_" + ++startInt + ".txt");
            }
            return path;
        }
    }
}
