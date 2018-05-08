using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UDPListenerNS;
using StorableFormState;
using System.Xml.Serialization;
using SerializationNS;
using System.Runtime.Serialization;
using GPSReaderNS;
using System.IO;
using System.Diagnostics;

namespace dxpClient
{
    public partial class FMain : FormWStorableState<DXpConfig>
    {
        UDPListener udpListener = new UDPListener();
        QSOFactory qsoFactory;
        HTTPService http;
        GPSReader gpsReader = new GPSReader();
        BindingList<QSO> blQSO = new BindingList<QSO>();
        Dictionary<string, BindingList<QSO>> qsoIndex = new Dictionary<string, BindingList<QSO>>();
        BindingSource bsQSO;
        string qsoFilePath = Application.StartupPath + "\\qso.dat";


        public FMain()
        {
#if DEBUG
            TextWriterTraceListener[] listeners = new TextWriterTraceListener[] {
            new TextWriterTraceListener("debug.log"),
            new TextWriterTraceListener(Console.Out)};
            Debug.Listeners.AddRange(listeners);
            Trace.Listeners.AddRange(listeners);
            Trace.AutoFlush = true;
            Debug.AutoFlush = true;
#endif
            config = new XmlConfigNS.XmlConfig<DXpConfig>();
            qsoFactory = new QSOFactory( config.data );
            InitializeComponent();
            udpListener.DataReceived += UDPDataReceived;
            udpListener.StartListener(12060);
            bsQSO = new BindingSource(blQSO, null);
            dgvQSO.AutoGenerateColumns = false;
            dgvQSO.DataSource = bsQSO;
            if (config.data.dgvQSOColumnsWidth != null && config.data.dgvQSOColumnsWidth.Count() == dgvQSO.ColumnCount)
                for (var co = 0; co < dgvQSO.ColumnCount; co++)
                    dgvQSO.Columns[co].Width = config.data.dgvQSOColumnsWidth[co];
            else
            {
                config.data.dgvQSOColumnsWidth = new int[dgvQSO.ColumnCount];
                for (var co = 0; co < dgvQSO.ColumnCount; co++)
                    config.data.dgvQSOColumnsWidth[co] = dgvQSO.Columns[co].Width;
                config.write();
            }
            bool qsoEr;
            List<QSO> storedQSOs = ProtoBufSerialization.ReadListItems<QSO>(qsoFilePath, out qsoEr);
            if (storedQSOs.Count > 0)
            {
                foreach (QSO qso in storedQSOs)
                {
                    if (qso.freq.IndexOf('.') == -1)
                    {
                        qso.freq = QSO.formatFreq(qso.freq);
                        qsoEr = true;
                    }
                    blQSO.Insert(0, qso);
                    updateQsoIndex(qso);
                }
                QSO lastQSO = storedQSOs.Last();
                if (lastQSO.rda == config.data.rda)
                    qsoFactory.no = lastQSO.no + 1;
            }
            if (qsoEr)
                ProtoBufSerialization.WriteList(qsoFilePath, storedQSOs, false);
            config.data.rafaChanged += rafaChanged;
            config.data.rdaChanged += rdaChanged;
            gpsReader.locationChanged += locationChanged;
            startGPSReader();
            http = new HTTPService("http://73.ru/dxped/uwsgi/qso", gpsReader, config.data);
            http.connectionStateChanged += onHTTPConnection;
        }

        private void rdaChanged(object sender, EventArgs e)
        {
            qsoFactory.no = blQSO.Where(x => x.rda == config.data.rda).Count() + 1;
        }

        private void updateQsoIndex( QSO qso )
        {
            if (!qsoIndex.ContainsKey(qso.cs))
                qsoIndex[qso.cs] = new BindingList<QSO>();
            qsoIndex[qso.cs].Insert(0, qso);
        }

        private void onHTTPConnection(object sender, EventArgs e)
        {
            DoInvoke(() =>
           {
               if (http.connected)
               {
                   slConnection.Text = "Connected";
                   slConnection.ForeColor = Color.Green;
               } 
               else
               {
                   slConnection.Text = "No connection";
                   slConnection.ForeColor = Color.Red;
               }
           });

        }

        private void startGPSReader()
        {
            if (config.data.gpsReaderWirelessGW)
                gpsReader.listenWirelessGW();
            else
            {
                List<SerialDeviceInfo> ports = GPSReader.listSerialDevices();
                SerialDeviceInfo port = ports.FirstOrDefault(x => x.deviceID == config.data.gpsReaderDeviceID);
                if (port != null)
                {
                    string portName = port.portName;
                    gpsReader.listenPort(portName);
                }
            }
        }

        private void locationChanged( object sender, EventArgs e )
        {
            string newLoc = DXpConfig.qth(gpsReader.coords);
            if ( newLoc != config.data.loc)
            {
                config.data.loc = newLoc;
                config.write();
            }
            slCoords.Text = gpsReader.coords.ToString();
            slLoc.Text = config.data.loc;
            if (config.data.rafa != null)
                slLoc.Text += " RAFA " + config.data.rafa;
        }

        private void rafaChanged( object sender, EventArgs e)
        {
            showBalloon("New RAFA " + config.data.rafa);
            config.write();
        }

        private void showBalloon( string text )
        {
            DoInvoke(() =>
           {
               NotifyIcon notifyIcon = new NotifyIcon();
               notifyIcon.Visible = true;
               notifyIcon.Icon = SystemIcons.Information;
               notifyIcon.BalloonTipTitle = "DXpedition";
               notifyIcon.BalloonTipText = text;
               notifyIcon.ShowBalloonTip(0);
               notifyIcon.BalloonTipClosed += delegate (object sender, EventArgs e)
               {
                   notifyIcon.Dispose();
               };
           });
        }

        private async void UDPDataReceived(object sender, DataReceivedArgs e)
        {
            string data = Encoding.UTF8.GetString(e.data);
            System.Diagnostics.Debug.WriteLine(data);
            QSO qso = qsoFactory.create(data);
            if (qso == null)
                return;
            //System.Diagnostics.Debug.WriteLine(qso.toJSON());
            dgvQSOInsert(qso);
            ProtoBufSerialization.Write<QSO>(qsoFilePath, qso, true);
            await http.postQso(qso);
        }

        private void dgvQSOInsert( QSO qso )
        {
            DoInvoke(() => {
                blQSO.Insert(0, qso);
                updateQsoIndex(qso);
                DataGridViewRow r = dgvQSO.Rows[0];
                setRowColors(r, Color.White, Color.SteelBlue);
                Task.Run( async () => 
                {
                    await Task.Delay(5000);
                    DoInvoke(() => 
                    {
                        setRowColors(r, dgvQSO.DefaultCellStyle.ForeColor, dgvQSO.DefaultCellStyle.BackColor);
                        dgvQSO.Refresh();
                    });
                });
                dgvQSO.FirstDisplayedScrollingRowIndex = 0;
                dgvQSO.Refresh();
            });
        }

        private void setRowColors( DataGridViewRow r, Color f, Color b)
        {
            for (int co = 0; co < dgvQSO.ColumnCount; co++)
            {
                r.Cells[co].Style.BackColor = b;
                r.Cells[co].Style.ForeColor = f;
            }
        }

        private void FMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            udpListener.DataReceived -= UDPDataReceived;
            udpListener.StopListener();
            Application.DoEvents();
        }

        private void DgvQSO_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            config.data.dgvQSOColumnsWidth[e.Column.Index] = e.Column.Width;
            config.write();
        }

        private void miSettings_Click(object sender, EventArgs e)
        {
            FSettings fs = new FSettings( config.data);
            if ( fs.ShowDialog(this) == DialogResult.OK )
            {
                config.data.rda = fs.rda.Trim( ' ' );
                config.data.wff = fs.wwf.Trim( ' ' );                
                if (config.data.gpsReaderDeviceID != fs.gpsReaderDeviceID || config.data.gpsReaderWirelessGW != fs.gpsReaderWirelessGW)
                {
                    config.data.gpsReaderWirelessGW = fs.gpsReaderWirelessGW;
                    config.data.gpsReaderDeviceID = fs.gpsReaderDeviceID;
                    startGPSReader();
                }
                config.write();
            }
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ( folderBrowserDialog.ShowDialog() == DialogResult.OK )
            {
                blQSO
                    .GroupBy(x => x.rda)
                    .ToList()
                    .ForEach(l =>
                   {
                       try
                       {
                           string fileName = Path.Combine(folderBrowserDialog.SelectedPath, l.First().rda + ".adi");
                           using (StreamWriter sw = new StreamWriter(fileName))
                           {
                               /*
                               sw.WriteLine("Nr;My Call;Ur Call;Ur RST;My RST;Freq;Mode;RDA;RAFA;Loc;WFF");
                               foreach (QSO qso in blQSO)
                                   sw.WriteLine(qso.no.ToString() + ";" + qso.myCS + ";" + qso.cs + ";" + qso.rcv + ";" + qso.snt + ";" + qso.freq + ";" + qso.mode + ";" + qso.rda + ";" + qso.rafa + ";" +
                                       qso.loc + ";" + qso.wff);
                                       */
                               DateTime ts = DateTime.UtcNow;
                               sw.WriteLine("ADIF Export from DxpClient");
                               sw.WriteLine("Logs generated @ {0:yyyy-MM-dd HH:mm:ssZ}", ts);
                               sw.WriteLine("<EOH>");
                               foreach (QSO qso in l)
                                   sw.WriteLine(qso.adif());


                           }
                       }
                       catch (Exception ex)
                       {
                           System.Diagnostics.Debug.WriteLine(ex.ToString());
                           MessageBox.Show("Can not export to text file: " + ex.ToString(), "DXpedition", MessageBoxButtons.OK, MessageBoxIcon.Error);
                       }


                   });
            }
        }

        private void FMain_Load(object sender, EventArgs e)
        {
            if (blQSO.Count > 0)
            {
                dgvQSO.FirstDisplayedScrollingRowIndex = 0;
                dgvQSO.Refresh();
            }
        }

        private void tbCSFilter_TextChanged(object sender, EventArgs e)
        {
            if (tbCSFilter.Text != "")
                tbCSFilter.Text = tbCSFilter.Text.ToUpper();
            if (miFilter.Checked && tbCSFilter.Text != "")
            {
                if (qsoIndex.ContainsKey(tbCSFilter.Text))
                    bsQSO.DataSource = qsoIndex[tbCSFilter.Text];
                else
                    MessageBox.Show("Callsign not found!");
            }
            else if (!miFilter.Checked)
                bsQSO.DataSource = blQSO;
            else
                miFilter.Checked = false;
            miFilter.BackColor = miFilter.Checked ? SystemColors.MenuHighlight : DefaultBackColor;
        }

        private void miStats_Click(object sender, EventArgs e)
        {
            FStats fs = new FStats(blQSO.ToList());
            fs.Show();
        }
    }

    [DataContract]
    public class DXpConfig : StorableFormConfig
    {

        [XmlIgnoreAttribute]
        private string _rda;
        [XmlIgnoreAttribute]
        private string _loc;
        [XmlIgnoreAttribute]
        public EventHandler<EventArgs> rdaChanged;
        [XmlIgnore]
        string _gpsReaderDeviceID;
        public string gpsReaderDeviceID {
            get { return _gpsReaderDeviceID; }
            set { _gpsReaderDeviceID = value; }
        }
        public bool gpsReaderWirelessGW;

        [XmlIgnore]
        Dictionary<string, string> rafaData = new Dictionary<string, string>();
        [XmlIgnore]
        string _rafa;
        [DataMember]
        public string rafa { get { return _rafa; } set { _rafa = value; } }
        [XmlIgnore]
        public EventHandler<EventArgs> rafaChanged;

        

        public int[] dgvQSOColumnsWidth;
        [DataMember]
        public string rda {
            get { return _rda; }
            set
            {
                if ( value != _rda )
                {
                    _rda = value;                    
                    rdaChanged?.Invoke(this, new EventArgs());
                }
            }
        }
        [DataMember]
        public string loc
        {
            get { return _loc; }
            set
            {
                if (value != _loc)
                {
                    _loc = value;
                    string newRafa = rafaData.ContainsKey(_loc) ? rafaData[_loc] : null;
                    if ( newRafa != rafa)
                    {
                        rafa = newRafa;
                        rafaChanged?.Invoke(this, new EventArgs());
                    }
                }
            }
        }
        [DataMember]
        public string wff;

        public DXpConfig() : base() {
            try
            {
                using (StreamReader sr = new StreamReader(Application.StartupPath + "\\rafa.csv"))
                {
                    do
                    {
                        string line = sr.ReadLine();
                        string[] lineData = line.Split(';');
                        if ( lineData[0] ==  "" ) 
                        {
                            string[] keys = lineData[3].Split(',');
                            foreach (string key in keys)
                                rafaData[key] = lineData[1];
                        }
                    } while (sr.Peek() >= 0);

                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
                MessageBox.Show("Rafa data could not be loaded: " + e.ToString(), "DXpedition", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        public string toJSON()
        {
            return JSONSerializer.Serialize<DXpConfig>(this);
        }

        

        public static string qth(Coords c)
        {
            double lat = c.lat;
            double lng = c.lng;
            string qth = "";
            lat += 90;
            lng += 180;
            lat = lat / 10 + 0.0000001;
            lng = lng / 20 + 0.0000001;
            qth += (char)(65 + lng);
            qth += (char)(65 + lat);
            lat = 10 * (lat - Math.Truncate(lat));
            lng = 10 * (lng - Math.Truncate(lng));
            qth += (char)(48 + lng);
            qth += (char)(48 + lat);
            lat = 24 * (lat - Math.Truncate(lat));
            lng = 24 * (lng - Math.Truncate(lng));
            qth += (char)(65 + lng);
            qth += (char)(65 + lat);
            lat = 10 * (lat - Math.Truncate(lat));
            lng = 10 * (lng - Math.Truncate(lng));
            /*            qth += (char)(48 + lng) + (char)(48 + lat);
                        lat = 24 * (lat - Math.Truncate(lat));
                        lng = 24 * (lng - Math.Truncate(lng));
                        qth += (char)(65 + lng) + (char)(65 + lat);*/
            System.Diagnostics.Debug.WriteLine(qth);
            return qth;
        } // returnQth()


    }
}
