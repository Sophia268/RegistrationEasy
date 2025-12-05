using System;
using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace invocation
{
    public class MainForm : Form
    {
        private TextBox txtMachineCode;
        private TextBox txtRegistrationCode;
        private Button btnRegister;
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

            btnRegister = new Button { Text = "注册", Anchor = AnchorStyles.Left, AutoSize = true };
            btnRegister.Click += BtnRegister_Click;

            lblResultHeader = new Label { Text = "解码结果", AutoSize = true, Font = new Font(Font, FontStyle.Bold) };
            lblMachineId = new Label { Text = "机器ID：-", AutoSize = true };
            lblPeriodType = new Label { Text = "有效期类型：-", AutoSize = true };
            lblCreateTime = new Label { Text = "创建时间：-", AutoSize = true };
            lblExpiredTime = new Label { Text = "过期时间：-", AutoSize = true };

            layout.Controls.Add(lblMachineCode, 0, 0);
            layout.Controls.Add(txtMachineCode, 1, 0);
            layout.Controls.Add(lblRegistrationCode, 0, 1);
            layout.Controls.Add(txtRegistrationCode, 1, 1);
            layout.Controls.Add(btnRegister, 1, 2);
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

            lblMachineId.Text = $"机器ID：{info.MachineID}";
            lblPeriodType.Text = $"有效期类型：{info.PeriodType}";
            lblCreateTime.Text = $"创建时间：{info.CreateTime:yyyy-MM-dd HH:mm:ss}";
            lblExpiredTime.Text = $"过期时间：{info.ExpiredTime:yyyy-MM-dd HH:mm:ss}";

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

            MessageBox.Show(this, "注册成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static string GetLocalMachineId()
        {
            var guid = Registry.GetValue(@"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Cryptography", "MachineGuid", null) as string;
            if (!string.IsNullOrWhiteSpace(guid)) return guid!;
            return Environment.MachineName;
        }

        private static bool TryDecodeRegistrationCode(string regCode, out RegistrationInfo info, out string error)
        {
            info = default!;
            error = string.Empty;

            string decoded;
            try
            {
                var normalized = NormalizeBase64(regCode);
                var raw = Convert.FromBase64String(normalized);
                decoded = Encoding.UTF8.GetString(raw);
            }
            catch (Exception ex)
            {
                error = $"Base64解码失败：{ex.Message}";
                return false;
            }

            try
            {
                if (decoded.TrimStart().StartsWith("{"))
                {
                    var json = JsonSerializer.Deserialize<RegistrationInfo>(decoded, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (json is null) throw new Exception("JSON为空");
                    info = json;
                    return ValidateInfo(info, out error);
                }
                else
                {
                    var parts = decoded.Split('|');
                    if (parts.Length < 4) throw new Exception($"字段不足，已解码：{decoded}");
                    if (!DateTime.TryParse(parts[2], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var create))
                        throw new Exception($"创建时间格式错误：{parts[2]}");
                    if (!DateTime.TryParse(parts[3], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var expire))
                        throw new Exception($"过期时间格式错误：{parts[3]}");
                    info = new RegistrationInfo
                    {
                        MachineID = parts[0],
                        PeriodType = parts[1],
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
            if (string.IsNullOrWhiteSpace(info.MachineID)) { error = "机器ID缺失"; return false; }
            if (string.IsNullOrWhiteSpace(info.PeriodType)) { error = "有效期类型缺失"; return false; }
            if (info.ExpiredTime <= info.CreateTime) { error = "过期时间不合法"; return false; }
            return true;
        }
    }

    public class RegistrationInfo
    {
        public string MachineID { get; set; } = string.Empty;
        public string PeriodType { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public DateTime ExpiredTime { get; set; }
    }
}
