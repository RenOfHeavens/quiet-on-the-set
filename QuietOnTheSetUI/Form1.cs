using Microsoft.Win32;
using NAudio.CoreAudioApi;
using QuietOnTheSetUI.Properties;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace QuietOnTheSetUI
{
    public partial class Form1 : Form
    {
        readonly MMDeviceEnumerator MMDE = new MMDeviceEnumerator();
        readonly MMDevice mmDevice;
        private bool _isLocked = false;
        private string _password;
        private int _maxVolume;
        private bool _exitAllowed = false;

        // Removes the app from Alt+Tab window if minimized
        protected override CreateParams CreateParams
        {
            get
            {
                var Params = base.CreateParams;
                if (FormWindowState.Minimized == WindowState)
                {
                    Params.ExStyle |= 0x80;
                }
                return Params;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            if (FormWindowState.Minimized == WindowState)
            {
                base.OnShown(e);
                Hide();
            }
        }

        public Form1()
        {
            InitializeComponent();

            try
            {
                checkBox1.Checked = Convert.ToBoolean(Properties.Settings.Default["StartAutomatically"]);
                checkBox2.Checked = Convert.ToBoolean(Properties.Settings.Default["StartMinimized"]);
            }
            catch (Exception)
            {
                checkBox1.Checked = false;
                checkBox2.Checked = false;
            }

            Icon = QuietOnTheSetUI.Properties.Resources.appicon;
            notifyIcon1.Icon = QuietOnTheSetUI.Properties.Resources.appicon;
            mmDevice = MMDE.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            volumeTrackBar.ValueChanged += VolumeTrackBar_ValueChanged;
            mmDevice.AudioEndpointVolume.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;
            _maxVolume = Convert.ToInt16(Properties.Settings.Default["MaxVolume"]);
            _isLocked = Convert.ToBoolean(Properties.Settings.Default["IsLocked"]);
            _password = Properties.Settings.Default["UnlockCode"].ToString();
            notifyIcon1.BalloonTipTitle = $"Quiet on the Set";
            volumeTrackBar.Value = _maxVolume;
            currentVolumeLabel.Text = Convert.ToInt16(mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100).ToString();

            if (checkBox2.Checked)
            {
                //  Hides the app completely
                Form1_FormClosing(null, new FormClosingEventArgs(new CloseReason(), true));

                //  The volume is automatically locked if the app is minimized 
                Properties.Settings.Default["IsLocked"] = true;
                Properties.Settings.Default.Save();
            }

            if (_isLocked)
            {
                LockVolume(true);
            }
            else
            {
                UnlockVolume();
            }

            FormClosing += Form1_FormClosing;
            Resize += Form1_Resize;

            UpdateFooter();
        }

        private void UpdateFooter()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            DateTime buildDate = new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime;

            footerLabel.Text = $"v{version} was built {buildDate:g}";
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == WindowState)
            {
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(500);
                Hide();
            }
            else if (FormWindowState.Normal == WindowState)
            {
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                notifyIcon1.Visible = false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_exitAllowed == false)
            {
                e.Cancel = true;
                WindowState = FormWindowState.Minimized;
            }
        }

        internal void LockVolume(bool initializing = false)
        {
            _isLocked = true;
            lockButton.Text = "Unlock";
            volumeTrackBar.Enabled = false;
            if (!initializing)
            {
                _maxVolume = volumeTrackBar.Value;
                _password = passwordTextBox.Text;
                Properties.Settings.Default["MaxVolume"] = _maxVolume.ToString();
                Properties.Settings.Default["IsLocked"] = true;
                Properties.Settings.Default["UnlockCode"] = passwordTextBox.Text;
                Properties.Settings.Default.Save();
            }
            passwordTextBox.Text = string.Empty;
            confirmPasswordTextBox.Text = string.Empty;
            if (_password.Length > 0) { lockButton.Enabled = false; }
            exitButton.Visible = false;
            notifyIcon1.BalloonTipText = BalloonTipText;
            notifyIcon1.Text = BalloonTipText;
            SetMaxVolume();
        }
        internal void UnlockVolume()
        {
            _isLocked = false;
            lockButton.Text = "Lock";
            volumeTrackBar.Enabled = true;
            Properties.Settings.Default["IsLocked"] = false;
            Properties.Settings.Default["UnlockCode"] = string.Empty;
            Properties.Settings.Default.Save();
            passwordTextBox.Text = string.Empty;
            confirmPasswordTextBox.Text = string.Empty;
            exitButton.Visible = true;
            notifyIcon1.BalloonTipText = BalloonTipText;
            notifyIcon1.Text = BalloonTipText;
            _password = string.Empty;
        }

        private string BalloonTipText
        {
            get
            {
                if (_isLocked)
                {
                    return $"Maximum volume locked at {volumeTrackBar.Value}";
                }
                else
                {
                    return $"No maximum volume is currently set";
                }
            }
        }

        private void SetMaxVolume()
        {
            if (mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar > (_maxVolume / 100f))
            {
                mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar = _maxVolume / 100f;
            }
        }

        private void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            var newVolume = Convert.ToInt16(data.MasterVolume * 100);
            if (_isLocked && newVolume > _maxVolume)
            {
                SetMaxVolume();
            }
            if (currentVolumeLabel.InvokeRequired)
            {
                currentVolumeLabel.Invoke(new MethodInvoker(delegate { currentVolumeLabel.Text = newVolume.ToString(); }));
            }
        }

        private void VolumeTrackBar_ValueChanged(object sender, EventArgs e)
        {
            maxVolumeLabel.Text = volumeTrackBar.Value.ToString();
        }

        private void VolumeTrackBar_Scroll(object sender, EventArgs e)
        {
            var trackBar = (TrackBar)sender;
            maxVolumeLabel.Text = trackBar.Value.ToString();
        }

        private void LockButton_Click(object sender, EventArgs e)
        {
            if (_isLocked)
            {
                UnlockVolume();
            }
            else
            {
                LockVolume();
            }
        }

        private void PasswordTextBox_TextChanged(object sender, EventArgs e)
        {
            ValidatePasswords();
        }

        private void ConfirmPasswordTextBox_TextChanged(object sender, EventArgs e)
        {
            ValidatePasswords();
        }

        internal void ValidatePasswords()
        {
            if (_isLocked)
            {
                lockButton.Enabled = passwordTextBox.Text.Equals(confirmPasswordTextBox.Text) && passwordTextBox.Text.Equals(_password);
            }
            else
            {
                lockButton.Enabled = passwordTextBox.Text.Equals(confirmPasswordTextBox.Text);
            }
        }

        private void ShowPasswordCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            var isChecked = ((CheckBox)sender).Checked;
            passwordTextBox.UseSystemPasswordChar = !isChecked;
            confirmPasswordTextBox.UseSystemPasswordChar = !isChecked;
        }

        private void NotifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Show must be called before setting WindowState,
            // otherwise the window loses its size and position
            Show();
            WindowState = FormWindowState.Normal;
            MaxmizedFromTray();
        }

        private void MaxmizedFromTray()
        {
            notifyIcon1.Visible = false;
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            var response = MessageBox.Show("This will completely shut down the volume control so users can set the volume as loud as they want. Are you sure you want to exit?", "Warning", MessageBoxButtons.YesNo);
            if (response == DialogResult.Yes)
            {
                _exitAllowed = true;
                Application.Exit();
            }
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (checkBox1.Checked)
            {
                rk.SetValue("QuietOnTheSet", Application.ExecutablePath.ToString());
            }
            else
            {
                rk.DeleteValue("QuietOnTheSet", false);
            }

            Properties.Settings.Default["StartAutomatically"] = checkBox1.Checked;
            Properties.Settings.Default.Save();
        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default["StartMinimized"] = checkBox2.Checked;
            Properties.Settings.Default.Save();
        }
    }
}
