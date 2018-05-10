namespace dxpClient
{
    partial class FStats
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.dgvStats = new System.Windows.Forms.DataGridView();
            this.rda = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.qsoCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CS = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStats)).BeginInit();
            this.SuspendLayout();
            // 
            // dgvStats
            // 
            this.dgvStats.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvStats.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.rda,
            this.qsoCount,
            this.CS});
            this.dgvStats.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvStats.Location = new System.Drawing.Point(0, 0);
            this.dgvStats.Name = "dgvStats";
            this.dgvStats.Size = new System.Drawing.Size(363, 308);
            this.dgvStats.TabIndex = 0;
            // 
            // rda
            // 
            this.rda.DataPropertyName = "rda";
            this.rda.HeaderText = "RDA";
            this.rda.Name = "rda";
            // 
            // qsoCount
            // 
            this.qsoCount.DataPropertyName = "qsoCount";
            this.qsoCount.HeaderText = "QSO";
            this.qsoCount.Name = "qsoCount";
            // 
            // CS
            // 
            this.CS.DataPropertyName = "csCount";
            this.CS.HeaderText = "Calls";
            this.CS.Name = "CS";
            // 
            // FStats
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(363, 308);
            this.Controls.Add(this.dgvStats);
            this.Name = "FStats";
            this.Text = "FStats";
            ((System.ComponentModel.ISupportInitialize)(this.dgvStats)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvStats;
        private System.Windows.Forms.DataGridViewTextBoxColumn rda;
        private System.Windows.Forms.DataGridViewTextBoxColumn qsoCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn CS;
    }
}