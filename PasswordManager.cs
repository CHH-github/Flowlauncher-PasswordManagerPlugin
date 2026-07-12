using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using PasswordManagerPlugin.Models;

namespace PasswordManagerPlugin
{
    public class PasswordManager
    {
        private readonly string _dataPath;
        private List<PasswordEntry> _entries;
        private static readonly object _lock = new object();

        public PasswordManager()
        {
            // 创建数据目录
            var dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PasswordManagerPlugin"
            );
            
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            _dataPath = Path.Combine(dataDir, "passwords.json");
            _entries = LoadEntries();
        }

        private List<PasswordEntry> LoadEntries()
        {
            lock (_lock)
            {
                if (File.Exists(_dataPath))
                {
                    try
                    {
                        var json = File.ReadAllText(_dataPath, Encoding.UTF8);
                        return JsonConvert.DeserializeObject<List<PasswordEntry>>(json) ?? new List<PasswordEntry>();
                    }
                    catch
                    {
                        return new List<PasswordEntry>();
                    }
                }
                return new List<PasswordEntry>();
            }
        }

        private void SaveEntries()
        {
            lock (_lock)
            {
                var json = JsonConvert.SerializeObject(_entries, Formatting.Indented);
                File.WriteAllText(_dataPath, json, Encoding.UTF8);
            }
        }

        public bool AddPassword(string name, string username, string password)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return false;
            }

            var existing = _entries.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                // 更新现有条目
                existing.Username = username;
                existing.Password = password;
                existing.UpdatedAt = DateTime.Now;
            }
            else
            {
                // 添加新条目
                _entries.Add(new PasswordEntry
                {
                    Name = name,
                    Username = username,
                    Password = password,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }

            SaveEntries();
            return true;
        }

        public PasswordEntry? GetPassword(string name)
        {
            return _entries.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public List<PasswordEntry> GetAllPasswords()
        {
            return _entries.ToList();
        }

        public bool DeletePassword(string name)
        {
            var entry = _entries.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (entry != null)
            {
                _entries.Remove(entry);
                SaveEntries();
                return true;
            }
            return false;
        }
    }
}