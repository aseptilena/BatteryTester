using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Globalization;

/*  
 *  Display remaining battery charge 
 *  and estimate overall battery life.
 * 
 *  Write results to a text file on the desktop
 * 
 *  Suspend system and display standby during operation
 * 
 *  Matthias Otto
 *  Dec 2008
 * 
 */

namespace BatteryLife
{

    public partial class frmMain : Form
    {
        [FlagsAttribute]
        public enum EXECUTION_STATE : uint
        {
            ES_SYSTEM_REQUIRED = 0x00000001,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_CONTINUOUS = 0x80000000,
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private extern static EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE prState);

        public frmMain()
        {
            InitializeComponent();
        }

        PowerStatus lcPowerStatus = SystemInformation.PowerStatus;
        Timer mTimer = new Timer();
        int mInvokeCount;
        float mStartCharge;
        const int LONG_INTERVAL = 60000;
        const int SHORT_INTERVAL = 300;
        Boolean mCharging;
        string mFileName = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\MyBatteryLife.txt";

        private void btnQuit_Click(object sender, EventArgs e)
        {
            mTimer.Stop();
            Close();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            using (StreamWriter sw = new StreamWriter(mFileName))  // re-write file
            {
                sw.WriteLine("Log started: " + DateTime.Now);
                sw.WriteLine("Time running, Charge remaining, Est. battery life");
            }
            mTimer.Tick += new EventHandler(TimerEventProcessor);
            setTimer(lcPowerStatus.PowerLineStatus == PowerLineStatus.Online);
        }

        private void setTimer(bool prCharging)
        {
            mCharging = prCharging;
            mTimer.Stop();
            if (mCharging)
            {
                lblStatus.Text = "charging";
                lblTime.Text = "";
                lblBattFullLife.Text = "";
                using (StreamWriter sw = new StreamWriter(mFileName, true))
                    sw.WriteLine("charging");
                mTimer.Interval = SHORT_INTERVAL;
            }
            else
            {
                lblStatus.Text = "discharging";
                lblTime.Text = "0 min";
                mStartCharge = lcPowerStatus.BatteryLifePercent;
                lblChargeRemaining.Text = mStartCharge * 100 + "%";
                mInvokeCount = 0;
                mTimer.Interval = LONG_INTERVAL;
            }
            mTimer.Start();
        }

        private void TimerEventProcessor(Object myObject, EventArgs myEventArgs)
        {
            SetThreadExecutionState(EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);

            lblChargeRemaining.Text = lcPowerStatus.BatteryLifePercent * 100 + "%";
            if (lcPowerStatus.PowerLineStatus == PowerLineStatus.Online)
                if (!mCharging)
                    setTimer(true);
                else ;
            else  // battery mode
                if (mCharging)
                    setTimer(false);
                else
                    recordDetails();
        }

        private void recordDetails()
        {
            lblTime.Text = Convert.ToString(Math.Round((float)++mInvokeCount * LONG_INTERVAL / 60000, 1)) + " min";
            //lblTime.Text = String.Format("{0:0.0} min", (float)++mInvokeCount * LONG_INTERVAL / 60000);
            //lblChargeRemaining.Text = lcPowerStatus.BatteryLifePercent * 100 + "%";
            if (mStartCharge > lcPowerStatus.BatteryLifePercent)
            {
                float fullTime = (float)mInvokeCount * LONG_INTERVAL / (mStartCharge - lcPowerStatus.BatteryLifePercent) / 60000.0F;
                lblBattFullLife.Text = Convert.ToString(Math.Round(fullTime, 0)) + " min";
            }

            using (StreamWriter sw = new StreamWriter(mFileName, true))
                sw.WriteLine(lblTime.Text + "," + lblChargeRemaining.Text + "," + lblBattFullLife.Text);
        }

    }
}
