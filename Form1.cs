using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using RingBuffer;

namespace Serial_Communication
    {
    public partial class Form1 : Form
        {
        RingBuffer<byte> rxbuffer;
        RingBuffer<byte> txbuffer;
        
        FileStream fs_read;
        BinaryWriter sw_read;
        FileStream fs_write;
        BinaryWriter sw_write;

        int counter=0;
        byte[] package = new byte[128];
        bool isTestCompleted = false;
        public Form1()
            {
            InitializeComponent();
            }
        private void Form1_Load(object sender, EventArgs e) 
            {
            updatePorts();
            CheckForIllegalCrossThreadCalls = false;

            

        }
        private void updatePorts()
            {
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
                {
                cmbPortName.Items.Add(port);               
                }
            }
        private SerialPort ComPort = new SerialPort();
        private void connect()
            {
            bool error = false;
            if (cmbPortName.SelectedIndex != -1 & cmbBaudRate.SelectedIndex != -1 & cmbParity.SelectedIndex != -1 & cmbDataBits.SelectedIndex != -1 & cmbStopBits.SelectedIndex != -1)
                {
                ComPort.PortName = cmbPortName.Text;
                ComPort.BaudRate = int.Parse(cmbBaudRate.Text);      
                ComPort.Parity = (Parity)Enum.Parse(typeof(Parity), cmbParity.Text);
                ComPort.DataBits = int.Parse(cmbDataBits.Text);
                ComPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), cmbStopBits.Text);
                //ComPort.Encoding= Encoding.GetEncoding(28591);
                //ComPort.Encoding = new UTF8Encoding(true, true);
                rxbuffer = new RingBuffer<byte>(ComPort.ReadBufferSize);
                txbuffer = new RingBuffer<byte>(ComPort.WriteBufferSize);
                try
                {
                ComPort.Open();
                ComPort.DataReceived += SerialPortDataReceived; 
                }
                catch (UnauthorizedAccessException) { error = true; }
                catch (System.IO.IOException) { error = true; }
                catch (ArgumentException) { error = true; }

                if (error) MessageBox.Show(this, "Could not open the COM port.", "COM Port unavailable", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                }
                else
                {
                MessageBox.Show("Please select all the Serial Port Settings", "Serial Port Interface", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                }
            if (ComPort.IsOpen)
                {
                btnConnect.Text = "Disconnect";
                btnSend.Enabled = true;
                if (!rdText.Checked & !rdHex.Checked)
                    {
                    rdText.Checked = true;
                    }                
                groupBox1.Enabled = false;            
                }
             }
        private void disconnect()
            {
            ComPort.Close();
            btnConnect.Text = "Connect";
            btnSend.Enabled = false;
            groupBox1.Enabled = true;
            }

        private void btnConnect_Click(object sender, EventArgs e)
                                  
            {
            if (ComPort.IsOpen)
                {
                disconnect();
                }
            else
                {
                connect();
                }
            }
        private void btnClear_Click(object sender, EventArgs e)
            {

            txtSend.Clear();
            }
        private void sendData()
            {
            bool error = false;

            if (rdText.Checked == true) 
                {
                for (int i = 0; i < 128; i++)
                {
                    package[i] = (byte)((i + this.counter) % 128);
                }     
                timer1.Start();
                timer2.Start();
                rxbuffer.Clear();
                txbuffer.Clear();
            }
            else                 
                {
                try
                    {
                  byte[] data = HexStringToByteArray(txtSend.Text);
                  ComPort.Write(data, 0, data.Length);
                  txtSend.Clear();
                    }
                catch (FormatException) { error = true; }               
                    catch (ArgumentException) { error = true; }

                if (error) MessageBox.Show(this, "Not properly formatted hex string: " + txtSend.Text + "\n" + "example: E1 FF 1B", "Format Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                                                      
                }
            }
        private byte[] HexStringToByteArray(string s)
            {
            s = s.Replace(" ", "");
            byte[] buffer = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
                buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
            return buffer;
            }
        
        private void btnSend_Click(object sender, EventArgs e)
            {
            fs_read = new FileStream(@"C:\Log\ReadLog.txt", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
            sw_read = new BinaryWriter(fs_read);

            fs_write = new FileStream(@"C:\Log\WriteLog.txt", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
            sw_write = new BinaryWriter(fs_write);

            int sure = int.Parse(txtSend.Text) / 128;
            timer1.Interval = 1000 / sure;
            timer2.Interval = 1000 / sure;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = sure;
            progressBar1.Step = 1;
            sendData();
            }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
            {
            if (ComPort.IsOpen) ComPort.Close();  
            }
    
        private void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {          
            var readByte = ComPort.BytesToRead;
            byte[] read = new byte[readByte];
            ComPort.Read(read, 0, readByte);
            for (int i = 0; i < readByte; i++)
            {
                rxbuffer.Put(read[i]);
            }
        }
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            updatePorts();           
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            int kontrol = int.Parse(txData.Text) / 128;
            if (this.counter>=kontrol)
            {
                timer1.Stop();
                isTestCompleted = true;
            }
            for (int j = 0; j < 128; j++)
            {
                txbuffer.Put(package[j]);
            }
            ComPort.Write(package,0,128);
            counter++;
        }   
        private void timer2_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < rxbuffer.Size; i++)
            {
                sw_read.Write(rxbuffer.Get());
            }
            for (int j = 0; j < txbuffer.Size; j++)
            {
                sw_write.Write(txbuffer.Get());
            }
            if (counter!=0 & progressBar1.Maximum>counter)
            {
                progressBar1.Value = counter;
            }
            if (isTestCompleted)
            {
                timer2.Stop();
                sw_read.Close();
                sw_write.Close();
                fs_write.Close();
                fs_read.Close();
                progressBar1.Value = progressBar1.Maximum;
            }                   
        }
        private void button1_Click(object sender, EventArgs e)
        {
            FileStream fs_writelog = new FileStream(@"C:\Log\WriteLog.txt", FileMode.Open);
            BinaryReader br_write = new BinaryReader(fs_writelog);
            byte[] check_write = new byte[fs_writelog.Length];

            for (int i = 0; i < fs_writelog.Length; i++)
            {
                check_write[i] = br_write.ReadByte();
            }
            FileStream fs_readlog = new FileStream(@"C:\Log\ReadLog.txt", FileMode.Open);
            BinaryReader br_read = new BinaryReader(fs_readlog);
            byte[] check_read = new byte[fs_readlog.Length];
            for (int i = 0; i < fs_readlog.Length; i++)
            {
                check_read[i] = br_read.ReadByte();
            }
            for (int j = 0; j < fs_writelog.Length;)
            {
                if (check_write[j]==check_read[j])
                {
                    j++;
                    label8.ForeColor= System.Drawing.Color.Green;
                    label8.Text = "Başarılı";
                }
                else
                {
                    label8.ForeColor = System.Drawing.Color.Red;
                    label8.Text = "Başarısız";
                    break;
                }
               
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            cmbPortName.SelectedIndex = 0;
            cmbBaudRate.SelectedIndex = 3;
            cmbDataBits.SelectedIndex = 3;
            cmbParity.SelectedIndex= 0;
            cmbStopBits.SelectedIndex = 0;
        }
    }
}
