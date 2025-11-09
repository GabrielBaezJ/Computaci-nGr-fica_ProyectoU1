using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace ReproductorMúsica
{
    public partial class frmMusicPlayer : Form
    {
        // Windows Media Player COM object (kept as dynamic to avoid early binding)
        private dynamic player = null;
        private bool isPlaying = false;

        public frmMusicPlayer()
        {
            InitializeComponent();

            // Wire events
            this.btnUpload.Click += btnUpload_Click;
            this.btnPlayPause.Click += btnPlayPause_Click;
        }

        private void frmMusicPlayer_Load(object sender, EventArgs e)
        {

        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "MP3 files (*.mp3)|*.mp3";
                ofd.Multiselect = false;
                ofd.Title = "Seleccionar archivo MP3";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string file = ofd.FileName;

                    try
                    {
                        // Show filename in textbox
                        this.txtFileName.Text = Path.GetFileName(file);

                        // Create Windows Media Player COM instance if needed
                        if (player == null)
                        {
                            Type wmpType = Type.GetTypeFromProgID("WMPlayer.OCX");
                            if (wmpType == null)
                            {
                                MessageBox.Show("Windows Media Player no está disponible en este equipo.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            player = Activator.CreateInstance(wmpType);
                        }

                        // Set URL and play
                        player.URL = file;
                        player.controls.play();
                        isPlaying = true;

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("No se pudo reproducir el archivo: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnPlayPause_Click(object sender, EventArgs e)
        {
            try
            {
                if (player == null)
                    return;

                if (isPlaying)
                {
                    player.controls.pause();
                    isPlaying = false;
                }
                else
                {
                    player.controls.play();
                    isPlaying = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al reproducir/pausar: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
