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
            internal string _rda;
            public string rda { get { return _rda; } set { _rda = value; } }
            internal int _qsoCount;
            public int qsoCount { get { return _qsoCount; } set { _qsoCount = value; } }
            internal int _csCount;
            public int csCount { get { return _csCount; } set { _csCount = value; } }
        }

        BindingList<Entry> blStats = new BindingList<Entry>();
        BindingSource bsStats;

        public FStats(List<QSO> lQSO)
        {
            InitializeComponent();

            bsStats = new BindingSource(blStats, null);
            dgvStats.AutoGenerateColumns = false;
            dgvStats.DataSource = bsStats;

            lQSO
                .GroupBy(x => x.rda)
                .Select(cx => new Entry
                {
                    _rda = cx.First().rda,
                    _qsoCount = cx.Count(),
                    _csCount = cx.GroupBy( x => x.cs ).Count()
                })
                .OrderBy( x => x.rda )
                .ToList()
                .ForEach( x => blStats.Add(x) );

            dgvStats.Refresh();

        }
    }
}
