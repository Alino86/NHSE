﻿using System;
using System.Windows.Forms;
using NHSE.Core;
using NHSE.Injection;
using NHSE.WinForms.Properties;

namespace NHSE.WinForms
{
    public partial class SysBotUI : Form
    {
        private readonly Func<byte[]> ByteFetch;
        private readonly Action<byte[]> ByteSet;
        private readonly InjectionType Type;
        private readonly SysBot Bot = new SysBot();
        private readonly Settings Settings = Settings.Default;

        public SysBotUI(Action<byte[]> read, Func<byte[]> write, InjectionType type)
        {
            InitializeComponent();
            ByteFetch = write;
            ByteSet = read;
            Type = type;
            RamOffset.Text = GetOffset(type).ToString("X8");

            TB_IP.Text = Settings.SysBotIP;
            TB_Port.Text = Settings.SysBotPort.ToString();

            PopPrompt();
        }

        private void PopPrompt()
        {
            if (Settings.SysBotPrompted)
                return;

            const string info = "This SysBot reads and writes RAM directly to your game when called to Read/Write.";
            const string reqd = "Using this functionality requires the sys-botbase sysmodule running on the console." +
                                "Your console must be on the same network as the PC running this program.";
            WinFormsUtil.Alert(info, reqd);
            Settings.SysBotPrompted = true;
            Settings.Save();
        }

        private uint GetOffset(InjectionType type)
        {
            var settings = Settings;
            return type switch
            {
                _ => settings.SysBotPouchOffset,
            };
        }

        private void SetOffset(InjectionType type, uint value)
        {
            var settings = Settings;
            settings.SysBotPouchOffset = type switch
            {
                _ => value,
            };
            settings.Save();
        }

        private void B_Connect_Click(object sender, EventArgs e)
        {
            var ip = TB_IP.Text;
            var port = TB_Port.Text;
            if (!int.TryParse(port, out var p))
                p = 6000;

            try
            {
                Bot.Connect(ip, p);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                WinFormsUtil.Error(ex.Message);
                return;
            }

            var settings = Settings;
            settings.SysBotIP = ip;
            settings.SysBotPort = p;
            settings.Save();

            GB_Inject.Enabled = true;
        }

        private void B_WriteCurrent_Click(object sender, EventArgs e)
        {
            var offset = StringUtil.GetHexValue(RamOffset.Text);
            if (offset == 0)
            {
                WinFormsUtil.Error("Incorrect hex offset.");
                return;
            }

            try
            {
                var data = ByteFetch();
                Bot.WriteBytes(data, offset);
                SetOffset(Type, offset);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                WinFormsUtil.Error(ex.Message);
            }
        }

        private void B_ReadCurrent_Click(object sender, EventArgs e)
        {
            var offset = StringUtil.GetHexValue(RamOffset.Text);
            if (offset == 0)
            {
                WinFormsUtil.Error("Incorrect hex offset.");
                return;
            }

            try
            {
                var data = ByteFetch();
                var result = Bot.ReadBytes(offset, data.Length);
                ByteSet(result);
                SetOffset(Type, offset);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                WinFormsUtil.Error(ex.Message);
            }
        }
    }
}
