using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System;
using System.Drawing.Drawing2D;

namespace GK_Projekt2
{
    partial class MainForm
    {
        private ToolStrip toolStrip;
        private ToolStripButton loadButton;
        private ToolStripButton saveButton;
        private ToolStripLabel rgbLabel;
        private ToolStripControlHost scaleControlHost;
        private TrackBar scaleTrackBar;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;

        private PictureBox pictureBox;
        private Bitmap currentImage;
        private float scale = 1.0f; // Initial zoom
        private const float minScale = 0.1f;
        private const float maxScale = 800.0f;
        private Point origin = new Point(0, 0);
        private bool isDragging = false;
        private Point dragStartPoint;

        private PPMImage ppmImage = new PPMImage();


        private void InitializeComponent()
        {
            this.Text = "Projekt 2";
            this.Size = new Size(1000, 700);
            toolStrip = new ToolStrip();

            loadButton = new ToolStripButton("Wczytaj");
            loadButton.Click += LoadImageButton_Click;
            saveButton = new ToolStripButton("Zapisz");
            saveButton.Click += SaveImageButton_Click;
            rgbLabel = new ToolStripLabel("RGB: (0, 0, 0)");

            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            statusStrip.Items.Add(statusLabel);

            toolStrip.Items.Add(loadButton);
            toolStrip.Items.Add(saveButton);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(rgbLabel);

            scaleTrackBar = new TrackBar
            {
                Minimum = 1,
                Maximum = 30,
                Value = 1,
                TickStyle = TickStyle.BottomRight,
                TickFrequency = 1,
                Width = 150
            };
            scaleTrackBar.Scroll += ScaleTrackBar_Scroll;
            scaleControlHost = new ToolStripControlHost(scaleTrackBar);
            toolStrip.Items.Add(scaleControlHost);

            pictureBox = new PictureBox
            {
                Location = new Point(10, 50),
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            pictureBox.MouseWheel += PictureBox_MouseWheel;
            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.MouseUp += PictureBox_MouseUp;
            pictureBox.Paint += PictureBox_Paint;

            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            statusStrip.Items.Add(statusLabel);

            this.Controls.Add(pictureBox);
            this.Controls.Add(toolStrip);
            this.Controls.Add(statusStrip);
            this.Controls.Add(scaleTrackBar);
            pictureBox.Focus(); // do eventow myszka
        }

        /// <summary>
        /// Wymagana zmienna projektanta.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void DisplayErrorMessage(string message)
        {
            statusLabel.Text = message;
            statusLabel.ForeColor = Color.Red;
        }

        private void LoadImageButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Pliki obrazów (*.ppm;*.jpg)|*.ppm;*.jpg";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = dialog.FileName;
                string extension = Path.GetExtension(filePath).ToLower();

                if (extension == ".ppm")
                {
                    string magicNumber = "";
                    using (var reader = new StreamReader(filePath))
                    {
                        magicNumber = reader.ReadLine()?.Trim();
                    }
                    if (magicNumber == "P3")
                    {
                        try
                        {
                            currentImage = ppmImage.ReadP3(filePath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Nie udało się załadować obrazu: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        pictureBox.Invalidate();
                    }
                    else if (magicNumber == "P6")
                    {
                        try
                        {
                            currentImage = ppmImage.ReadP6(filePath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Nie udało się załadować obrazu: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        pictureBox.Invalidate();
                    }
                    else
                    {
                        MessageBox.Show("Nieprawidłowy plik PPM.");
                    }
                }
                else if (extension == ".jpg")
                {
                    currentImage = new Bitmap(filePath);
                    pictureBox.Invalidate();
                }
            }
        }

        private void SaveImageButton_Click(object sender, EventArgs e)
        {
            if (currentImage == null)
            {
                MessageBox.Show("Nie udało się załadować obrazu.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Obraz JPEG|*.jpg";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string input = Microsoft.VisualBasic.Interaction.InputBox("Podaj jakość obrazu (0-100):", "Poziom kompresji", "75");
                    if (long.TryParse(input, out long imageQuality) && imageQuality >= 0 && imageQuality <= 100)
                    {
                        try
                        {
                            BitmapSaver.SaveJpeg(currentImage, saveFileDialog.FileName, imageQuality);
                            MessageBox.Show("Obraz zapisany pomyślnie!", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Błąd podczas zapisu obrazu: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        finally
                        {
                            currentImage.Dispose();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Nieprawidłowy poziom jakości. Proszę podać między 1 a 100", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }


        private void PictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (currentImage == null)
                return;

            float oldScale = scale;
            if (e.Delta > 0 && scale < maxScale) scale *= 1.1f;
            else if (e.Delta < 0 && scale > minScale) scale /= 1.1f;

            float scaleAdjustment = scale / oldScale;
            origin.X = (int)(e.X - (e.X - origin.X) * scaleAdjustment);
            origin.Y = (int)(e.Y - (e.Y - origin.Y) * scaleAdjustment);
            pictureBox.Invalidate();
        }

        private Bitmap PPMToBitmap(PPMImage ppmImage)
        {
            Bitmap bitmap = new Bitmap(ppmImage.Width, ppmImage.Height);
            for (int y = 0; y < ppmImage.Height; y++)
            {
                for (int x = 0; x < ppmImage.Width; x++)
                {
                    int index = (y * ppmImage.Width + x) * 3;
                    Color color = Color.FromArgb(
                        ppmImage.Pixels[index + 2],
                        ppmImage.Pixels[index],
                        ppmImage.Pixels[index + 1]);
                    bitmap.SetPixel(x, y, color);
                }
            }
            return bitmap;
        }

        private void ScaleTrackBar_Scroll(object sender, EventArgs e)
        {
            scale = scaleTrackBar.Value;
            pictureBox.Invalidate();
        }


        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (currentImage != null)
            {
                isDragging = true;
                dragStartPoint = new Point(e.X - origin.X, e.Y - origin.Y);
            }
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                origin = new Point(e.X - dragStartPoint.X, e.Y - dragStartPoint.Y);
                pictureBox.Invalidate();
            }
            else if (currentImage != null && scale > 1.0f)
            {
                int imgX = (int)((e.X - origin.X) / scale);
                int imgY = (int)((e.Y - origin.Y) / scale);
                try
                {
                    if (imgX >= 0 && imgX < currentImage.Width && imgY >= 0 && imgY < currentImage.Height)
                    {
                        Color pixelColor = currentImage.GetPixel(imgX, imgY);
                        rgbLabel.Text = $"RGB: ({pixelColor.R}, {pixelColor.G}, {pixelColor.B})";
                    }
                }
                catch (Exception exc)
                {
                    Console.WriteLine($"{exc.Message}");
                }
            }
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (currentImage != null)
            {
                e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;

                Rectangle destRect = new Rectangle(origin.X, origin.Y, (int)(currentImage.Width * scale), (int)(currentImage.Height * scale));
                e.Graphics.DrawImage(currentImage, destRect);
            }
        }
    }
}

