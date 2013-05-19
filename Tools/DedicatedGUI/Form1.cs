using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FTW.Engine.Shared;
using Game.Server;
using System.Threading;
using System.IO;
using System.Reflection;

namespace DedicatedGUI
{
    public partial class Form1 : Form
    {
        GameServer game;
        public Form1()
        {
            InitializeComponent();

            TextWriter tw = new TextBoxStreamWriter(txtOutput);
            Console.SetOut(tw);
            Console.SetError(tw);

            game = new GameServer(true, true);

            Config config = Config.ReadFile(settingsFilename);
            if (config == null)
            {
                config = game.CreateDefaultConfig();
                config.SaveToFile(settingsFilename);
            }

            game.Start(config);

            this.Text = game.Name;
        }

        const string settingsFilename = "settings.yml";
        const int defaultPort = 24680, defaultMaxClients = 8;
        const string defaultServerName = "Some FTW server";

        static Config GetOrCreateConfig(out int port, out int maxClients, out string serverName)
        {
            if (!File.Exists(settingsFilename))
            {
                Assembly a = Assembly.GetExecutingAssembly();
                Stream defaultSettings = a.GetManifestResourceStream(typeof(Program), settingsFilename);
                byte[] buf = new byte[defaultSettings.Length];
                defaultSettings.Read(buf, 0, buf.Length);
                File.WriteAllBytes(settingsFilename, buf);
            }

            Config settings = Config.ReadFile(settingsFilename);
            string strPort = settings.FindValueOrDefault("port", defaultPort.ToString());
            string strMaxClients = settings.FindValueOrDefault("max-clients", defaultPort.ToString());
            serverName = settings.FindValueOrDefault("name", defaultServerName);

            if (!int.TryParse(strPort, out port))
                port = defaultPort;
            if (!int.TryParse(strMaxClients, out maxClients))
                maxClients = defaultMaxClients;

            return settings;
        }

        public class TextBoxStreamWriter : TextWriter
        {
            TextBox _output = null;

            public TextBoxStreamWriter(TextBox output)
            {
                _output = output;
            }

            public override void Write(char value)
            {
                base.Write(value);
                WriteText(value);
            }

            public override Encoding Encoding
            {
                get { return Encoding.Unicode; }
            }

            private delegate void writerDelegate(char c);
            private void WriteText(char c)
            {
                if (_output.InvokeRequired)
                {
                    _output.BeginInvoke(new writerDelegate(WriteText), c);
                    return;
                }
                if ( !_output.IsDisposed )
                    _output.AppendText(c.ToString()); // When character data is written, append it to the text box.
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (game != null && game.IsRunning)
            {// Closing right away would prevent the "server shutting down" disconnect message from being sent out, so delay slightly
                game.Stop();
                Thread.Sleep(100);
            }
        }

        private void txtInput_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                if (txtInput.Text.Trim() != string.Empty)
                {
                    //server.ServerCommand(txtInput.Text.Trim());
                }
                txtInput.Clear();
                e.Handled = true;
            }
        }

        private void txtOutput_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetterOrDigit(e.KeyChar) || char.IsPunctuation(e.KeyChar) || char.IsSymbol(e.KeyChar) || e.KeyChar == ' ')
            {
                txtInput.Text += e.KeyChar;
                txtInput.Focus();
                txtInput.SelectionStart = txtInput.SelectionLength;
            }
        }
    }
}
