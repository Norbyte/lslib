namespace ConverterApp
{
    partial class ExportItemSelection
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Type = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ItemName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Format = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SuspendLayout();
            // 
            // Type
            // 
            this.Type.Text = "Type";
            this.Type.Width = 120;
            // 
            // Name
            // 
            this.ItemName.Text = "Name";
            this.ItemName.Width = 200;
            // 
            // Format
            // 
            this.Format.Text = "Format";
            this.Format.Width = 120;
            // 
            // ExportItemSelection
            // 
            this.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ItemName,
            this.Type,
            this.Format});
            this.FullRowSelect = true;
            this.View = System.Windows.Forms.View.Details;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ColumnHeader Type;
        private System.Windows.Forms.ColumnHeader ItemName;
        private System.Windows.Forms.ColumnHeader Format;
    }
}
