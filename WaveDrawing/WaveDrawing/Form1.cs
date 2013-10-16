using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NAudio.Wave;

namespace WaveDrawing
{
	public partial class Form1 : Form
	{
		AudioFileReader audioFile = null;

		public Form1()
		{
			InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			var res = openFileDialog1.ShowDialog();
			if (res != System.Windows.Forms.DialogResult.OK)
				return;

			fileNameL.Text = openFileDialog1.FileName;
			LoadAudioFile(openFileDialog1.FileName);
			UpdateWaveView();
		}

		private void Form1_SizeChanged(object sender, EventArgs e)
		{
			timer1.Enabled = false;
			timer1.Enabled = true;
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			timer1.Enabled = false;
			UpdateWaveView();
		}

		public void LoadAudioFile(string fname)
		{
			if (audioFile != null)
				audioFile.Dispose();
			audioFile = new AudioFileReader(fname);
		}

		public void UpdateWaveView()
		{
			if (pictureBox1.Image != null)
			{
				pictureBox1.Image.Dispose();
				pictureBox1.Image = null;
			}

			if (audioFile != null)
			{
				audioFile.Position = 0;
				pictureBox1.Image = DrawWaveform(audioFile, pictureBox1.Width, pictureBox1.Height);
			}
		}

		private static Bitmap DrawWaveform(AudioFileReader reader, int width, int height, int style = 0)
		{		
			// calculate number of samples
			long nSamples = reader.Length / ((reader.WaveFormat.BitsPerSample * reader.WaveFormat.Channels) / 8);
			if (nSamples < 2)
				return null;

			// drawing position/scaling factors
			int yBase = height; 
			double yScale = -(height - 3);

			if (style == 1)
			{
				yBase = height / 2;
				yScale = -((double)height - 3) / 2;
			}

			double sampleWidth = width / (double)nSamples;
			double currPosition = 0;

			Bitmap res = new Bitmap(width, height);
			
			using (Graphics g = Graphics.FromImage(res))
			using (Pen linePen = new Pen(Color.Red))
			using (Brush fillBrush = new SolidBrush(Color.Red))
			{
				//g.Clear(Color.Black);

				// Data for current column
				int currColumn = 0;
				float minVal = float.PositiveInfinity, maxVal = float.NegativeInfinity;

				// Data for previous column
				int prevColumn = 0;
				int prevMinY = 0, prevMaxY = 0;

				// Buffer for reading samples
				float[] buffer = new float[8192];
				int readCount;

				while ((readCount = reader.Read(buffer, 0, 8192)) > 0)
				{
					// Merge stereo samples to mono
					if (reader.WaveFormat.Channels == 2)
					{
						for (int i = 0, o = 0; i < readCount; i += 2, o++)
							buffer[o] = (buffer[i] + buffer[i + 1]) / 2;
						readCount >>= 1;
					}

					// process samples
					foreach (float sample in buffer.Take(readCount))
					{
						minVal = Math.Min(minVal, sample);
						maxVal = Math.Max(maxVal, sample);
						currPosition += sampleWidth;

						// on column change, draw to bitmap
						if ((int)currPosition > currColumn)
						{
							if (!float.IsInfinity(minVal) && !float.IsInfinity(maxVal))
							{
								// calculate Y coordinates for min & max
								int minY = 0, maxY = 0;
								if (style == 0)
								{
									minY = yBase;
									maxY = (int)(yBase + yScale * Math.Max(Math.Abs(minVal), Math.Abs(maxVal)));
								}
								else if (style == 1)
								{
									minY = (int)(yBase + yScale * minVal);
									maxY = (int)(yBase + yScale * maxVal);
								}

								if (sampleWidth > 1)
								{
									// more columns than samples, use polygon drawing to fill gapes
									g.FillPolygon(fillBrush, new Point[] { 
										new Point(prevColumn, prevMinY), new Point(prevColumn, prevMaxY),
										new Point(currColumn, maxY), new Point(currColumn, minY) });
								}
								else
								{
									// more samples than columns, draw lines only
									g.DrawLine(linePen, currColumn, minY, currColumn, maxY);
								}

								// save current data to previous
								prevColumn = currColumn;
								prevMinY = minY;
								prevMaxY = maxY;
							}

							// update column number and reset accumulators
							currColumn = (int)currPosition;
							minVal = float.PositiveInfinity;
							maxVal = float.NegativeInfinity;
						}
					}
				}
			}

			return res;
		}
	}
}
