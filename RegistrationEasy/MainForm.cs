using System;
using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Windows.Forms;
using System.Diagnostics;
using AutoDeployTool.Services;
using System.Management;
using System.Security.Cryptography;

namespace invocation
{
    public class MainForm : Form
    {
        private TextBox txtMachineCode;
        private TextBox txtRegistrationCode;
        private Button btnRegister;
        private Button btnPurchase;
        private Label lblResultHeader;
        private Label lblMachineId;
        private Label lblPeriodType;
        private Label lblCreateTime;
        private Label lblExpiredTime;

        public MainForm()
        {
            Text = "注册激活验证";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(640, 360);
            MaximizeBox = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(16),
                AutoSize = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));

            var lblMachineCode = new Label { Text = "机器码", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
            txtMachineCode = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };

            var lblRegistrationCode = new Label { Text = "注册码", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
            txtRegistrationCode = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };

            btnRegister = new Button { Text = "验证注册码", Anchor = AnchorStyles.Left, AutoSize = true };
            btnRegister.Click += BtnRegister_Click;
            btnPurchase = new Button { Text = "购买注册码", Anchor = AnchorStyles.Left, AutoSize = true };
            btnPurchase.Click += (_, __) =>
            {
                var uri = ConfigProvider.Get().URI;
                if (string.IsNullOrWhiteSpace(uri))
                {
                    MessageBox.Show(this, "未配置购买链接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                try
                {
                    Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"无法打开链接：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            lblResultHeader = new Label { Text = "解码结果", AutoSize = true, Font = new Font(Font, FontStyle.Bold) };
            lblMachineId = new Label { Text = "机器码：-", AutoSize = true };
            lblPeriodType = new Label { Text = "时长模式：-", AutoSize = true };
            lblCreateTime = new Label { Text = "创建时间：-", AutoSize = true };
            lblExpiredTime = new Label { Text = "过期时间：-", AutoSize = true };

            layout.Controls.Add(lblMachineCode, 0, 0);
            layout.Controls.Add(txtMachineCode, 1, 0);
            layout.Controls.Add(lblRegistrationCode, 0, 1);
            layout.Controls.Add(txtRegistrationCode, 1, 1);
            var actionPanel = new FlowLayoutPanel { Anchor = AnchorStyles.Left, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            actionPanel.Controls.Add(btnPurchase);
            actionPanel.Controls.Add(btnRegister);
            layout.Controls.Add(actionPanel, 1, 2);
            layout.Controls.Add(lblResultHeader, 0, 4);
            layout.SetColumnSpan(lblResultHeader, 2);
            layout.Controls.Add(lblMachineId, 0, 5);
            layout.SetColumnSpan(lblMachineId, 2);
            layout.Controls.Add(lblPeriodType, 0, 6);
            layout.SetColumnSpan(lblPeriodType, 2);
            layout.Controls.Add(lblCreateTime, 0, 7);
            layout.SetColumnSpan(lblCreateTime, 2);
            layout.Controls.Add(lblExpiredTime, 0, 8);
            layout.SetColumnSpan(lblExpiredTime, 2);

            while (layout.RowCount <= 8)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowCount++;
            }

            Controls.Add(layout);

            Load += (_, __) =>
            {
                try
                {
                    txtMachineCode.Text = GetLocalMachineId();
                }
                catch
                {
                    txtMachineCode.Text = Environment.MachineName;
                }
            };
        }

        private void BtnRegister_Click(object? sender, EventArgs e)
        {
            var machineCode = (txtMachineCode.Text ?? string.Empty).Trim();
            var regCode = (txtRegistrationCode.Text ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(regCode))
            {
                MessageBox.Show(this, "请输入注册码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!TryDecodeRegistrationCode(regCode, out var info, out var error))
            {
                MessageBox.Show(this, $"注册码无效：{error}", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }



            if (!string.Equals(info.MachineID, machineCode, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(this, "机器码不匹配", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (DateTime.UtcNow > info.ExpiredTime.ToUniversalTime())
            {
                MessageBox.Show(this, "已过期，请联系支持续期", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            lblMachineId.Text = $"机器码：{info.MachineID}";
            lblPeriodType.Text = $"时长模式：{info.PeriodType}";
            lblCreateTime.Text = $"创建时间：{info.CreateTime:yyyy-MM-dd HH:mm:ss}";
            lblExpiredTime.Text = $"过期时间：{info.ExpiredTime:yyyy-MM-dd HH:mm:ss}";

            MessageBox.Show(this, "注册成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static string GetLocalMachineId()
        {
            try
            {
                var cpu = GetHardwareInfo("Win32_Processor", "ProcessorId");
                var board = GetHardwareInfo("Win32_BaseBoard", "SerialNumber");
                var disk = GetHardwareInfo("Win32_DiskDrive", "SerialNumber");

                // 如果硬件信息获取失败，使用备用方案
                if (string.IsNullOrWhiteSpace(cpu) && string.IsNullOrWhiteSpace(board))
                {
                    return FormatMachineId(Environment.MachineName);
                }

                var rawId = $"{cpu}|{board}|{disk}";
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawId));
                // 取前8字节，转为16进制字符串 (16字符)
                var hex = BitConverter.ToString(hash, 0, 8).Replace("-", "");
                return FormatMachineId(hex);
            }
            catch
            {
                return FormatMachineId(Environment.MachineName);
            }
        }

        private static string GetHardwareInfo(string table, string property)
        {
            try
            {
                // Windows 平台特定检查
                if (OperatingSystem.IsWindows())
                {
                    using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {table}");
                    foreach (var obj in searcher.Get())
                    {
                        var val = obj[property]?.ToString();
                        if (!string.IsNullOrWhiteSpace(val)) return val.Trim();
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        private static string FormatMachineId(string id)
        {
            // 确保全是字母数字，移除非法字符
            var sb = new StringBuilder();
            foreach (var c in id)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToUpperInvariant(c));
            }
            var cleanId = sb.ToString();

            // 补足16位
            while (cleanId.Length < 16) cleanId += "0";
            if (cleanId.Length > 16) cleanId = cleanId.Substring(0, 16);

            // 分组显示 XXXX-XXXX-XXXX-XXXX
            return $"{cleanId.Substring(0, 4)}-{cleanId.Substring(4, 4)}-{cleanId.Substring(8, 4)}-{cleanId.Substring(12, 4)}";
        }

        private static bool TryDecodeRegistrationCode(string regCode, out RegistrationInfo info, out string error)
        {
            info = default!;
            error = string.Empty;

            string decoded = string.Empty;
            regCode = regCode.Trim();
            try
            {
                decoded = EncryptService.DecryptText(regCode);
            }
            catch
            {
                error = $"Base64解码失败";
                return false;
            }

            try
            {
                var text = decoded.Trim();
                if (text.StartsWith("{"))
                {
                    var json = JsonSerializer.Deserialize<RegistrationInfo>(text, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (json is null) throw new Exception("JSON为空");
                    info = json;
                    info.ExpiredTime = info.CreateTime.AddMonths(info.PeriodType);
                    return ValidateInfo(info, out error);
                }
                else
                {
                    var parts = text.Split('|');
                    if (parts.Length < 4) throw new Exception($"字段不足，已解码：{text}");
                    if (!DateTime.TryParse(parts[2], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var create))
                        throw new Exception($"创建时间格式错误：{parts[2]}");
                    if (!DateTime.TryParse(parts[3], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var expire))
                        throw new Exception($"过期时间格式错误：{parts[3]}");
                    if (!int.TryParse(parts[1], out var periodType))
                        throw new Exception($"时长模式格式错误：{parts[1]}");
                    info = new RegistrationInfo
                    {
                        MachineID = parts[0],
                        PeriodType = periodType,
                        CreateTime = create,
                        ExpiredTime = expire
                    };
                    return ValidateInfo(info, out error);
                }
            }
            catch (Exception ex)
            {
                error = $"内容解析失败：{ex.Message}";
                return false;
            }
        }

        private static string NormalizeBase64(string s)
        {
            var t = (s ?? string.Empty).Trim();
            t = t.Replace("-", "+").Replace("_", "/");
            t = t.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace(" ", string.Empty);
            var mod = t.Length % 4;
            if (mod != 0) t = t.PadRight(t.Length + (4 - mod), '=');
            return t;
        }

        private static bool ValidateInfo(RegistrationInfo info, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(info.MachineID)) { error = "机器码缺失"; return false; }
            // PeriodType is int, no whitespace check needed
            if (info.ExpiredTime <= info.CreateTime) { error = "过期时间不合法"; return false; }
            return true;
        }
    }

    public class RegistrationInfo
    {
        [JsonPropertyName("machineId")]
        public string MachineID { get; set; } = string.Empty;

        [JsonPropertyName("period")]
        public int PeriodType { get; set; }

        [JsonPropertyName("ts")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime CreateTime { get; set; }

        public DateTime ExpiredTime { get; set; }
    }

    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        private readonly string _format = "yyyy-MM-dd HH:mm:ss";
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (string.IsNullOrWhiteSpace(str)) return default;
            return DateTime.ParseExact(str, _format, CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_format));
        }
    }
}
