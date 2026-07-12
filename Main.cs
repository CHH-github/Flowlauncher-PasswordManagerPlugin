using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlowLauncher.Plugin;
using FlowLauncher.Plugin.SharedModels;
using PasswordManagerPlugin.Models;

namespace PasswordManagerPlugin
{
    public class Main : IPlugin, IAsyncPlugin
    {
        private PasswordManager _passwordManager;
        private PluginInitContext _context;

        public Task InitAsync(PluginInitContext context)
        {
            _context = context;
            _passwordManager = new PasswordManager();
            return Task.CompletedTask;
        }

        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            var results = new List<Result>();
            
            if (string.IsNullOrEmpty(query.Search))
            {
                // 空查询显示所有密码
                var allPasswords = _passwordManager.GetAllPasswords();
                foreach (var entry in allPasswords)
                {
                    results.Add(CreatePasswordResult(entry));
                }
                
                // 添加帮助命令
                results.Add(new Result
                {
                    Title = "📖 使用帮助",
                    SubTitle = "p add 名称 账号 密码 - 添加或更新密码",
                    IcoPath = "Images\\icon.png",
                    Action = _ =>
                    {
                        _context.API.ShowMsg("密码管理器", 
                            "使用说明：\n" +
                            "p add 名称 账号 密码  - 添加或更新密码\n" +
                            "p 名称              - 查看密码\n" +
                            "p 名称 delete       - 删除密码\n" +
                            "p all              - 列出所有密码");
                        return true;
                    }
                });
                
                return results;
            }

            var parts = query.Search.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length > 0 && parts[0].ToLower() == "add" && parts.Length >= 4)
            {
                // 添加密码: p add 名称 账号 密码
                var name = parts[1];
                var username = parts[2];
                var password = string.Join(" ", parts.Skip(3)); // 支持密码包含空格
                
                if (_passwordManager.AddPassword(name, username, password))
                {
                    results.Add(new Result
                    {
                        Title = $"✅ 密码已保存",
                        SubTitle = $"名称: {name}, 账号: {username}",
                        IcoPath = "Images\\icon.png",
                        Action = _ =>
                        {
                            _context.API.ShowMsg("成功", $"密码 '{name}' 已保存");
                            return true;
                        }
                    });
                }
                else
                {
                    results.Add(new Result
                    {
                        Title = "❌ 添加失败",
                        SubTitle = "请检查参数格式: p add 名称 账号 密码",
                        IcoPath = "Images\\icon.png"
                    });
                }
                return results;
            }

            if (parts.Length > 1 && parts[1].ToLower() == "delete")
            {
                // 删除密码: p 名称 delete
                var name = parts[0];
                if (_passwordManager.DeletePassword(name))
                {
                    results.Add(new Result
                    {
                        Title = $"✅ 已删除",
                        SubTitle = $"密码 '{name}' 已删除",
                        IcoPath = "Images\\icon.png",
                        Action = _ =>
                        {
                            _context.API.ShowMsg("成功", $"密码 '{name}' 已删除");
                            return true;
                        }
                    });
                }
                else
                {
                    results.Add(new Result
                    {
                        Title = "❌ 删除失败",
                        SubTitle = $"未找到密码 '{name}'",
                        IcoPath = "Images\\icon.png"
                    });
                }
                return results;
            }

            if (parts[0].ToLower() == "all")
            {
                // 列出所有密码
                var allPasswords = _passwordManager.GetAllPasswords();
                foreach (var entry in allPasswords)
                {
                    results.Add(CreatePasswordResult(entry));
                }
                
                if (!results.Any())
                {
                    results.Add(new Result
                    {
                        Title = "📭 暂无密码",
                        SubTitle = "使用 p add 名称 账号 密码 添加密码",
                        IcoPath = "Images\\icon.png"
                    });
                }
                return results;
            }

            // 搜索密码
            var searchName = parts[0];
            var entry = _passwordManager.GetPassword(searchName);
            if (entry != null)
            {
                results.Add(CreatePasswordResult(entry));
            }
            else
            {
                // 模糊搜索
                var fuzzyMatches = _passwordManager.GetAllPasswords()
                    .Where(e => e.Name.ToLower().Contains(searchName.ToLower()))
                    .ToList();

                if (fuzzyMatches.Any())
                {
                    foreach (var match in fuzzyMatches)
                    {
                        results.Add(CreatePasswordResult(match));
                    }
                }
                else
                {
                    results.Add(new Result
                    {
                        Title = "🔍 未找到密码",
                        SubTitle = $"未找到名称包含 '{searchName}' 的密码",
                        IcoPath = "Images\\icon.png",
                        Action = _ =>
                        {
                            _context.API.ShowMsg("未找到", $"密码 '{searchName}' 不存在");
                            return true;
                        }
                    });
                }
            }

            return results;
        }

        private Result CreatePasswordResult(PasswordEntry entry)
        {
            return new Result
            {
                Title = $"🔑 {entry.Name}",
                SubTitle = $"账号: {entry.Username} | 点击复制账号/密码",
                IcoPath = "Images\\icon.png",
                ContextData = entry,
                Action = _ =>
                {
                    // 默认点击复制密码
                    CopyToClipboard(entry.Password);
                    _context.API.ShowMsg("已复制", $"密码 '{entry.Name}' 已复制到剪贴板");
                    return true;
                },
                AutoCompleteText = entry.Name,
                TitleHighlightData = new List<int>()
            };
        }

        private void CopyToClipboard(string text)
        {
            try
            {
                // 在FlowLauncher中使用API复制到剪贴板
                _context.API.CopyToClipboard(text);
            }
            catch (Exception ex)
            {
                _context.API.ShowMsg("错误", $"复制失败: {ex.Message}");
            }
        }

        // 保持对旧版本API的兼容
        public List<Result> Query(Query query)
        {
            return QueryAsync(query, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}