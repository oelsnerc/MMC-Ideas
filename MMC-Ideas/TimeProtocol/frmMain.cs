﻿//********************************************************************
// frmMain - The Main Form
// (c) Okt 2010 MMC
//********************************************************************
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

//********************************************************************
namespace TimeProtocol
{
    public partial class frmMain : Form
    {
        //************************************************************
        protected Logger m_Log;

        protected int _MouseDownX;
        protected int _MouseDownY;

        //------------------------------------------------------------
        public frmMain()
        {
            InitializeComponent();

            string FileName = Properties.Settings.Default.FileName;
            if (String.IsNullOrEmpty(FileName))
            {
                m_Log = null;
            }
            else
            {
                m_Log = new Logger(FileName);
            }

            ssdSeconds.Value = 0;
            ssdMinutes.Value = 0;
            ssdHours.Value = 0;
        }

        //------------------------------------------------------------
        public bool canRun()
        {
            if (m_Log != null)
                return true;

            if (dlgFileSave.ShowDialog() != DialogResult.OK)
                return false;

            m_Log = new Logger(dlgFileSave.FileName);
            
            Properties.Settings.Default.FileName = dlgFileSave.FileName;
            Properties.Settings.Default.PositionX = this.Location.X;
            Properties.Settings.Default.PositionY = this.Location.Y;
            
            return true;
        }

        //------------------------------------------------------------
        private void frmMain_Load(object sender, EventArgs e)
        {
            showTime(!Properties.Settings.Default.ShowDuration);

            Point newLocation = new Point(Properties.Settings.Default.PositionX,
                                          Properties.Settings.Default.PositionY);
            this.Location = newLocation;

            m_Log.log_start("Start");
            SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(SystemEvents_PowerModeChanged);
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
            tmrClock.Enabled = true;

            setLogEnabled(true);
        }

        //------------------------------------------------------------
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;

            Properties.Settings.Default.PositionX = this.Location.X;
            Properties.Settings.Default.PositionY = this.Location.Y;
            Properties.Settings.Default.FileName = m_Log.FileName;
            Properties.Settings.Default.ShowDuration = mnuTimeDuration.Checked;

            Properties.Settings.Default.Save();

            m_Log.log_end("End");
            m_Log.Close();
        }

        //************************************************************
        public void setLogEnabled(bool Enabled)
        {
            m_Log.isEnabled = Enabled;
            if (Enabled)
            {
                btnRun.ImageIndex = 1;
                SystemTray.Text = "Time protocol: Started!";
            }
            else
            {
                btnRun.ImageIndex = 0;
                SystemTray.Text = "Time protocol: Stopped!";
            }
        }

        //************************************************************
        public void showTime(bool CurrentTime)
        {
            Properties.Settings.Default.ShowDuration = !CurrentTime;
            mnuTimeDuration.Checked = !CurrentTime;
            mnuTimeCurrent.Checked = CurrentTime;
        }


        //************************************************************
        public void SystemEvents_PowerModeChanged(Object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    m_Log.log_start("Resume");
                    break;
                case PowerModes.StatusChange:
                    // m_Log.log("PowerChange");  // ignore this one due to flooding
                    break;
                case PowerModes.Suspend:
                    m_Log.log_end("Suspend");
                    break;
                default:
                    m_Log.log("Unknown PowerModeChange event");
                    break;
            }
        }

        //------------------------------------------------------------
        public void SystemEvents_SessionSwitch(Object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.ConsoleConnect:
                    m_Log.log("ConsoleConnect");
                    break;
                case SessionSwitchReason.ConsoleDisconnect:
                    m_Log.log("Console Disconnect");
                    break;
                case SessionSwitchReason.RemoteConnect:
                    m_Log.log("RemoteConnect");
                    break;
                case SessionSwitchReason.RemoteDisconnect:
                    m_Log.log("RemoteDisconnect");
                    break;
                case SessionSwitchReason.SessionLock:
                    m_Log.log("SessionLock");
                    break;
                case SessionSwitchReason.SessionLogoff:
                    m_Log.log("SessionLogoff");
                    break;
                case SessionSwitchReason.SessionLogon:
                    m_Log.log("SessionLogon");
                    break;
                case SessionSwitchReason.SessionRemoteControl:
                    m_Log.log("SessionRemoteControl");
                    break;
                case SessionSwitchReason.SessionUnlock:
                    m_Log.log("SessionUnlock");
                    break;
                default:
                    m_Log.log("Unknown Session Switch event");
                    break;
            }
        }

        //------------------------------------------------------------
        private void btnRun_Click(object sender, EventArgs e)
        {
            setLogEnabled(btnRun.ImageIndex == 0);
        }

        //------------------------------------------------------------
        private void tmrClock_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            if (mnuTimeDuration.Checked)
            {
                TimeSpan dur = now - m_Log.LastStart;
                ssdHours.Value = dur.Hours;
                ssdMinutes.Value = dur.Minutes;
                ssdSeconds.Value = dur.Seconds;
            }
            else
            {
                ssdHours.Value = now.Hour;
                ssdMinutes.Value = now.Minute;
                ssdSeconds.Value = now.Second;
            }
        }

        //------------------------------------------------------------
        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        //------------------------------------------------------------
        private void frmMain_DoubleClick(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            Hide();
        }

        //------------------------------------------------------------
        private void SystemTray_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
        }

        //************************************************************
        // Menu items
        //------------------------------------------------------------
        private void mnuNewFile_Click(object sender, EventArgs e)
        {
            string FileName = m_Log.FileName;
            dlgFileSave.InitialDirectory = Path.GetDirectoryName(FileName);
            dlgFileSave.FileName = Path.GetFileName(FileName);
            if (dlgFileSave.ShowDialog() == DialogResult.OK)
            {
                m_Log.FileName = dlgFileSave.FileName;
            }
        }

        //------------------------------------------------------------
        private void mnuOpen_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(m_Log.FileName);
        }

        //------------------------------------------------------------
        private void mnuTime_Click(object sender, EventArgs e)
        {
            showTime(sender == mnuTimeCurrent);
            tmrClock_Tick(sender, e);
        }

        //************************************************************
        // now the Mouse movement
        private void frmMain_MouseDown(object sender, MouseEventArgs e)
        {
            _MouseDownX = e.X;
            _MouseDownY = e.Y;
        }

        private void frmMain_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // actually we want to use this.Location.Offset(e.X - _MouseDownX, e.Y - _MouseDownY);
                // but this will not call the "Setter"
                // so we're working on the reference and call the "Setter" explicitely
                Point Cur = this.Location;
                Cur.Offset(e.X - _MouseDownX, e.Y - _MouseDownY);
                this.Location = Cur;
            }
        }
    }
}

//********************************************************************
// END OF FILE frmMain
//********************************************************************
