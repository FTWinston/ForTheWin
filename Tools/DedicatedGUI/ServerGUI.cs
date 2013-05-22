using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FTW.Engine.Shared;
using System.Threading;
using System.IO;
using System.Reflection;

namespace DedicatedGUI
{
    public partial class ServerGUI : Form
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ServerGUI());
        }

        ServerBase server;
        public ServerGUI()
        {
            InitializeComponent();

            TextWriter tw = new TextBoxStreamWriter(txtOutput);
            Console.SetOut(tw);
            Console.SetError(tw);

            server = ServerBase.CreateReflection();

            Config config = Config.ReadFile(settingsFilename);
            if (config == null)
            {
                config = server.CreateDefaultConfig();
                config.SaveToFile(settingsFilename);
            }

            server.Start(true, config);

            this.Text = server.Name;
        }

        const string settingsFilename = "settings.yml";

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
            if (server != null && server.IsRunning)
                server.Stop();
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
