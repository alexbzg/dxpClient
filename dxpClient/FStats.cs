using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace dxpClient
{
    public partial class FStats : Form
    {
        public class Entry
        {
            internal string _value;
            public string value { get { return _value; } set { _value = value; } }
            internal int _qsoCount;
            public int qsoCount { get { return _qsoCount; } set { _qsoCount = value; } }
            internal int _csCount;
            public int csCount { get { return _csCount; } set { _csCount = value; } }
        }

        public class TempEntry
        {
            public int qsoCount = 0;
            public HashSet<string> csList = new HashSet<string> ();
        }

        BindingList<Entry> blStats = new BindingList<Entry>();
        BindingSource bsStats;

        public FStats(List<QSO> lQSO, string type, List<string> values)
        {
            InitializeComponent();

            bsStats = new BindingSource(blStats, null);
            dgvStats.AutoGenerateColumns = false;
            dgvStats.DataSource = bsStats;
            dgvStats.Columns[0].HeaderText = type;

            if (type == "RDA")
            {
                lQSO
                    .GroupBy(x => x.rda)
                    .Select(cx => new Entry
                    {
                        _value = cx.First().rda,
                        _qsoCount = cx.Count(),
                        _csCount = cx.GroupBy(x => x.cs).Count()
                    })
                    .OrderBy(x => x.value)
                    .ToList()
                    .ForEach(x => blStats.Add(x));
            }

            if (type == "RAFA")
            {
                Dictionary<string, TempEntry> data = new Dictionary<string, TempEntry>();
                lQSO
                    .Where( qso => qso.rafa != null).ToList()
                    .ForEach(qso =>
               {
                   string[] rafas = qso.rafa.Split( new string[] { ", " }, StringSplitOptions.None);
                   foreach (string rafa in rafas)
                   {
                       if (!data.ContainsKey(rafa))
                           data[rafa] = new TempEntry();
                       data[rafa].qsoCount += 1;
                       data[rafa].csList.Add(qso.cs);
                   }
               });
                data.Keys.ToList().OrderBy( k => k ).ToList().ForEach( k => {
                    blStats.Add(new Entry
                    {
                        _value = k,
                        _csCount = data[k].csList.Count,
                        _qsoCount = data[k].qsoCount
                    });
                });
            }

            dgvStats.Refresh();

        }
    }
}
