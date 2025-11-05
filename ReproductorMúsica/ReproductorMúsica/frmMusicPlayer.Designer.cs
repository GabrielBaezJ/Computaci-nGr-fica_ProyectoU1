namespace ReproductorMúsica
{
    partial class frmMusicPlayer
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
            this.grpPlayer = new System.Windows.Forms.GroupBox();
            this.btnPlay = new System.Windows.Forms.Button();
            this.btnPause = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.grpArchive = new System.Windows.Forms.GroupBox();
            this.btnUpload = new System.Windows.Forms.Button();
            this.txtNameArchive = new System.Windows.Forms.TextBox();
            this.grpGraphics = new System.Windows.Forms.GroupBox();
            this.picCanvas = new System.Windows.Forms.PictureBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.grpPlayer.SuspendLayout();
            this.grpArchive.SuspendLayout();
            this.grpGraphics.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picCanvas)).BeginInit();
            this.SuspendLayout();
            // 
            // grpPlayer
            // 
            this.grpPlayer.Controls.Add(this.btnStop);
            this.grpPlayer.Controls.Add(this.btnPause);
            this.grpPlayer.Controls.Add(this.btnPlay);
            this.grpPlayer.Location = new System.Drawing.Point(12, 12);
            this.grpPlayer.Name = "grpPlayer";
            this.grpPlayer.Size = new System.Drawing.Size(344, 132);
            this.grpPlayer.TabIndex = 0;
            this.grpPlayer.TabStop = false;
            this.grpPlayer.Text = "Reproducir";
            // 
            // btnPlay
            // 
            this.btnPlay.Location = new System.Drawing.Point(6, 103);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(75, 23);
            this.btnPlay.TabIndex = 0;
            this.btnPlay.Text = "Reproducir";
            this.btnPlay.UseVisualStyleBackColor = true;
            // 
            // btnPause
            // 
            this.btnPause.Location = new System.Drawing.Point(133, 103);
            this.btnPause.Name = "btnPause";
            this.btnPause.Size = new System.Drawing.Size(75, 23);
            this.btnPause.TabIndex = 1;
            this.btnPause.Text = "Pausar";
            this.btnPause.UseVisualStyleBackColor = true;
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(263, 103);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(75, 23);
            this.btnStop.TabIndex = 2;
            this.btnStop.Text = "Detener";
            this.btnStop.UseVisualStyleBackColor = true;
            // 
            // grpArchive
            // 
            this.grpArchive.Controls.Add(this.txtNameArchive);
            this.grpArchive.Controls.Add(this.btnUpload);
            this.grpArchive.Location = new System.Drawing.Point(12, 163);
            this.grpArchive.Name = "grpArchive";
            this.grpArchive.Size = new System.Drawing.Size(344, 100);
            this.grpArchive.TabIndex = 1;
            this.grpArchive.TabStop = false;
            this.grpArchive.Text = "Archivo";
            // 
            // btnUpload
            // 
            this.btnUpload.Location = new System.Drawing.Point(6, 45);
            this.btnUpload.Name = "btnUpload";
            this.btnUpload.Size = new System.Drawing.Size(98, 23);
            this.btnUpload.TabIndex = 0;
            this.btnUpload.Text = "Subir Archivo";
            this.btnUpload.UseVisualStyleBackColor = true;
            // 
            // txtNameArchive
            // 
            this.txtNameArchive.Enabled = false;
            this.txtNameArchive.Location = new System.Drawing.Point(133, 47);
            this.txtNameArchive.Name = "txtNameArchive";
            this.txtNameArchive.Size = new System.Drawing.Size(100, 20);
            this.txtNameArchive.TabIndex = 1;
            // 
            // grpGraphics
            // 
            this.grpGraphics.Controls.Add(this.progressBar1);
            this.grpGraphics.Controls.Add(this.picCanvas);
            this.grpGraphics.Location = new System.Drawing.Point(378, 12);
            this.grpGraphics.Name = "grpGraphics";
            this.grpGraphics.Size = new System.Drawing.Size(410, 303);
            this.grpGraphics.TabIndex = 2;
            this.grpGraphics.TabStop = false;
            this.grpGraphics.Text = "Gráficos";
            // 
            // picCanvas
            // 
            this.picCanvas.Location = new System.Drawing.Point(7, 20);
            this.picCanvas.Name = "picCanvas";
            this.picCanvas.Size = new System.Drawing.Size(397, 240);
            this.picCanvas.TabIndex = 0;
            this.picCanvas.TabStop = false;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(6, 266);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(398, 23);
            this.progressBar1.TabIndex = 1;
            // 
            // frmMusicPlayer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 327);
            this.Controls.Add(this.grpGraphics);
            this.Controls.Add(this.grpArchive);
            this.Controls.Add(this.grpPlayer);
            this.Name = "frmMusicPlayer";
            this.Text = "ProyectoU1";
            this.grpPlayer.ResumeLayout(false);
            this.grpArchive.ResumeLayout(false);
            this.grpArchive.PerformLayout();
            this.grpGraphics.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picCanvas)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpPlayer;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnPause;
        private System.Windows.Forms.Button btnPlay;
        private System.Windows.Forms.GroupBox grpArchive;
        private System.Windows.Forms.Button btnUpload;
        private System.Windows.Forms.TextBox txtNameArchive;
        private System.Windows.Forms.GroupBox grpGraphics;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.PictureBox picCanvas;
    }
}