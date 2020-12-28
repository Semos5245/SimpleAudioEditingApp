using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MMAudioProject
{
    public partial class Form1 : Form
    {
        WaveStream reader;
        WaveOut waveOut;
        bool deleteStarted = false;
        bool isFromSelected = false;
        int deleteXFrom = 0;
        int deleteXTo = 0;
        string filePath;

        private Color processingColor = Color.DarkOrange;
        public Form1()
        {
            InitializeComponent();
            waveOut = new WaveOut();
            pPlayOptions.Enabled = false;

        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Choose an audio file",
                Filter = "Audio Files|*.mp3;*.wav"
            };

            SetStatus("Getting audio file...", processingColor);

            if (ofd.ShowDialog() == DialogResult.Cancel)
            { SetStatus("", Color.White); return; }

            filePath = ofd.FileName;

            reader = new Mp3FileReader(ofd.FileName);

            lstFileDetails.Items.Clear();

            lstFileDetails.Items.Add(new ListViewItem(new string[] {
                "SampleRate" , reader.WaveFormat.SampleRate.ToString()
            }));

            lstFileDetails.Items.Add(new ListViewItem(new string[]{
                "BitsPerSample" , reader.WaveFormat.BitsPerSample.ToString()
            }));

            lstFileDetails.Items.Add(new ListViewItem(new string[]{
                "Channels" , reader.WaveFormat.Channels.ToString()
            }));

            lstFileDetails.Items.Add(new ListViewItem(new string[]{
                "Encoding" , (reader as Mp3FileReader).Mp3WaveFormat.Encoding.ToString()
            }));

            lstFileDetails.Items.Add(new ListViewItem(new string[]{
                "AverageBytesPerSecond" , (reader as Mp3FileReader).Mp3WaveFormat.AverageBytesPerSecond.ToString()
            }));
            lstFileDetails.Items.Add(new ListViewItem(new string[]{
                "File Duration" , reader.TotalTime.ToString()
            }));
            lstFileDetails.Items.Add(new ListViewItem(new string[]{
                "Total Samples" , (reader.WaveFormat.SampleRate * reader.TotalTime.TotalSeconds).ToString()
            }));

            pPlayOptions.Enabled = true;

            DrawSound(reader);

            SetStatus("File Loaded...", Color.Green);
        }

        private void btnStartPause_Click(object sender, EventArgs e)
        {
            if (waveOut.PlaybackState.Equals(PlaybackState.Stopped))
            {
                PlayFile(reader);
                Invoke(new Action(async () => await DrawSoundLine()));
                btnStartPause.Text = "Pause";
            }
            else if (waveOut.PlaybackState.Equals(PlaybackState.Playing))
            {
                waveOut.Pause();
                btnStartPause.Text = "Resume";
            }
            else
            {
                waveOut.Resume();
                btnStartPause.Text = "Pause";
            }
        }

        private void btnBaclward_Click(object sender, EventArgs e)
        {
            reader.Skip(-1);
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            reader.Skip(1);
        }

        private void trimToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!deleteStarted)
            {
                deleteStarted = true;
                pictureBox1.Cursor = Cursors.Arrow;
            }
        }

        private void onPlaybackStopped(object sender, StoppedEventArgs e)
        {
            btnStartPause.Text = "| |";
            waveOut.PlaybackStopped -= onPlaybackStopped;
        }

        private void onMouseMove(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image == null)
                return;

            using (Graphics g = pictureBox1.CreateGraphics())
            {
                pictureBox1.Refresh();
                g.DrawLine(deleteStarted ? Pens.Red : Pens.DarkOrange, e.X, 0, e.X, pictureBox1.Image.Height);
            }

        }

        private async void onMouseClick(object sender, MouseEventArgs e)
        {
            if (!deleteStarted)
            {
                double percentage = e.X / (double)pictureBox1.Width;
                double seconds = reader.TotalTime.TotalMilliseconds * percentage;
                reader.CurrentTime = new TimeSpan(0, 0, 0, 0, (int)(seconds));
            }
            else
            {
                Bitmap image = new Bitmap(pictureBox1.Image);
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.DrawLine(Pens.Red, e.X, 0, e.X, image.Height);
                }

                pictureBox1.Image = image;

                if (!isFromSelected)
                {
                    deleteXFrom = e.X;
                    isFromSelected = true;
                }
                else
                {
                    deleteXTo = e.X;
                    
                    var min = Math.Min(deleteXFrom, deleteXTo);
                    var max = Math.Max(deleteXFrom, deleteXTo);

                    deleteXFrom = min;
                    deleteXTo = max;

                    double percentageStart = deleteXFrom / (double)pictureBox1.Width;
                    double percentageEnd = deleteXTo / (double)pictureBox1.Width;

                    var startMilliseconds = reader.TotalTime.TotalMilliseconds * percentageStart;
                    var endMilliseconds = reader.TotalTime.TotalMilliseconds * percentageEnd;
                    var from = TimeSpan.FromMilliseconds(startMilliseconds);
                    var to = TimeSpan.FromMilliseconds(endMilliseconds);

                    //List<byte> bytes = new List<byte>();
                    //Mp3Frame frame;
                    //var mp3Reader = new Mp3FileReader(filePath);
                    //while ((frame = mp3Reader.ReadNextFrame()) != null)
                    //{
                    //    if (mp3Reader.CurrentTime < from || mp3Reader.CurrentTime > to)
                    //        foreach (var data in frame.RawData)
                    //        {
                    //            bytes.Add(data);
                    //        }
                    //}

                    //var rawBytes = bytes.ToArray();

                    //reader = new RawSourceWaveStream(rawBytes, 0, rawBytes.Length, reader.WaveFormat);

                    reader = TrimWavFile(reader, TimeSpan.FromMilliseconds(startMilliseconds), TimeSpan.FromMilliseconds(endMilliseconds));
                    reader = TrimWavFile(reader, TimeSpan.FromMilliseconds(startMilliseconds), TimeSpan.FromMilliseconds(endMilliseconds));


                    deleteStarted = false;
                    isFromSelected = false;
                    pictureBox1.Cursor = Cursors.Default;
                    DrawSound(reader);
                }
            }
        }

        private async void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "Choose a folder"
            };

            SetStatus("Getting save path...", processingColor);

            if (sfd.ShowDialog() == DialogResult.Cancel)
            { SetStatus("", Color.White); return; }

            SetStatus("Saving...", processingColor);

            await SaveFile(reader, sfd.FileName);

            SetStatus("File Saved Successfully.", Color.Green);
        }

        private void drawToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (reader == null)
                return;

            SetStatus("Drawing file waves...", processingColor);

            DrawSound(reader);

            SetStatus("File has been drawn", Color.Green);
        }

        private void reverseSoundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (reader == null)
                return;

            SetStatus("Reversing...", processingColor);

            reader = ReverseSound(reader);

            SetStatus("Sound Rerversed...", Color.Green);
        }

        private void mixSoundsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (reader == null)
                return;

            SetStatus("Getting file to mix...", processingColor);

            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Choose an audio",
                Filter = "Audio Files|*.mp3;*.wav"
            };

            if (ofd.ShowDialog() == DialogResult.Cancel)
            { SetStatus("", Color.White); return; }

            SetStatus("Mixing sounds...", processingColor);

            //Load the second sound file to be mixed
            WaveStream mixReader = new Mp3FileReader(ofd.FileName);

            //Check the details of the loaded file
            if (reader.WaveFormat.SampleRate != mixReader.WaveFormat.SampleRate ||
                reader.WaveFormat.BitsPerSample != mixReader.WaveFormat.BitsPerSample ||
                reader.WaveFormat.Channels != mixReader.WaveFormat.Channels)
            { SetStatus("Incompatible wave formats...", Color.Red); return; }

            //Mix the sounds
            reader = MixSounds(reader, mixReader);

            SetStatus("Sounds have been mixed...", Color.Green);
        }

        /// <summary>
        /// Handles when the start button at the speed is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (reader == null)
                return;

            SetStatus("Altering sound speed...", Color.Yellow);

            reader = AlterSoundSpeed(reader, double.Parse(this.txtSpeed.Text));

            SetStatus("Sound speed has been altered", Color.Green);

            DrawSound(reader);
        }

        /// <summary>
        /// Handles when the start button at the Re-Sampling is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (reader == null)
                return;

            SetStatus("Re-sampling...", Color.Yellow);

            reader = ReSampleSound(reader, int.Parse(this.txtSampleRate.Text));

            SetStatus("Sound File has been re-sampled", Color.Green);

            DrawSound(reader);
        }

        /// <summary>
        /// Handles when the merge sounds button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mergeSoundsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (reader == null)
                return;

            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "MP3 file|*.mp3"
            };

            if (ofd.ShowDialog() == DialogResult.Cancel) return;

            WaveStream readerToMerge = new Mp3FileReader(ofd.FileName);

            SetStatus("Merging...", processingColor);

            reader = MergeSounds(reader, readerToMerge);

            SetStatus("File Merged..", Color.Green);

            DrawSound(reader);
        }

        #region Private Helper Functions

        /// <summary>
        /// Saves the currently loaded and manipulated file to the specified path
        /// </summary>
        /// <param name="path">Path to save the file to</param>
        private async Task SaveFile(WaveStream reader, string path)
        {
            using (WaveFileWriter writer = new WaveFileWriter(path + ".wav", reader.WaveFormat))
            {
                var data = GetSoundData(reader);
                byte[] buffer = new byte[data.Length * 2];
                int i = 0;
                foreach (var item in data)
                {
                    var soundBytes = BitConverter.GetBytes(item);

                    buffer[i++] = soundBytes[0];
                    buffer[i++] = soundBytes[1];
                }
                await writer.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        private WaveStream ReverseSound(WaveStream reader)
        {
            var soundData = GetSoundData(reader);
            soundData = soundData.Reverse().ToArray();
            var bData = new byte[soundData.Length * 2];
            Buffer.BlockCopy(soundData, 0, bData, 0, bData.Length);
            return new RawSourceWaveStream(bData, 0, bData.Length, reader.WaveFormat);
        }

        private short[] GetSoundData(WaveStream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            BinaryReader bReader = new BinaryReader(stream);
            List<short> result = new List<short>();
            while (stream.Position < stream.Length)
            {
                if (stream.CanRead)
                    result.Add(bReader.ReadInt16());
            }
            return result.ToArray();
        }

        private void DrawSound(WaveStream stream)
        {
            short[] data = GetSoundData(stream);
            int samplesPerPoint = data.Length / pictureBox1.Width;
            int y = pictureBox1.Height / 2;
            Bitmap b = new Bitmap(pictureBox1.Width, pictureBox1.Height, PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(b))
            {
                for (int i = 1; i < pictureBox1.Width; i++)
                {
                    short s = data[i * samplesPerPoint];

                    //Making the height of the wave according to the picture height
                    int yy = s * (pictureBox1.Height / 2) / short.MaxValue;

                    //Reversing the axis cuz of the reversed computer nature
                    yy = pictureBox1.Height / 2 - yy;
                    g.DrawLine(Pens.White, i - 1, y, i, yy);
                }
            }

            pictureBox1.Image = b;
        }

        private async Task DrawSoundLine()
        {
            while (waveOut.PlaybackState.Equals(PlaybackState.Playing))
            {

                using (Graphics g = pictureBox1.CreateGraphics())
                {
                    pictureBox1.Refresh();

                    double second = reader.CurrentTime.TotalMilliseconds;
                    double percentage = second / reader.TotalTime.TotalMilliseconds;
                    int x = (int)(pictureBox1.Width * percentage);

                    g.DrawLine(new Pen(Color.White, 3), x, 0, x, pictureBox1.Height);
                }
                await Task.Delay(2);
            }
        }

        /// <summary>
        /// Alters the speed of the passed wave stream
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="speed">Speed to scale to</param>
        /// <returns></returns>
        private WaveStream AlterSoundSpeed(WaveStream reader, double speed)
        {
            var data = GetSoundData(reader);
            int newLength = (int)(data.Length * speed);

            var newData = new short[newLength];

            for (int i = 0; i < newData.Length; i++)
            {
                int index = (int)((long)i * data.Length / newData.Length);
                newData[i] = data[index];
            }

            data = newData;
            byte[] bData = new byte[data.Length * 2];
            Buffer.BlockCopy(data, 0, bData, 0, bData.Length);
            return new RawSourceWaveStream(bData, 0, bData.Length, reader.WaveFormat);
        }

        /// <summary>
        /// Resamples the passed wave stream to the specified rate
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="newSampleRate">Rate to re-sample to</param>
        /// <returns></returns>
        private WaveStream ReSampleSound(WaveStream reader, int newSampleRate)
        {
            var data = GetSoundData(reader);
            double speed = (double)newSampleRate / reader.WaveFormat.SampleRate;
            int newLength = (int)(data.Length * speed);

            var newData = new short[newLength];

            for (int i = 0; i < newData.Length; i++)
            {
                int index = (int)((long)i * data.Length / newData.Length);
                newData[i] = data[index];
            }

            data = newData;
            byte[] bData = new byte[data.Length * 2];
            Buffer.BlockCopy(data, 0, bData, 0, bData.Length);
            WaveFormat format = new WaveFormat(newSampleRate, 16, 2);
            return new RawSourceWaveStream(bData, 0, bData.Length, format);
        }

        private WaveStream MixSounds(WaveStream reader1, WaveStream reader2)
        {
            var sound1Data = GetSoundData(reader1);
            var sound2Data = GetSoundData(reader2);

            var maxLength = Math.Max(sound1Data.Length, sound2Data.Length);

            short[] resultingSoundData = new short[maxLength];

            var resultData = new byte[maxLength * 2];

            for (int i = 0; i < maxLength; i++)
            {
                var firstFound = false;
                int firstSample = 0;
                int secondSample = 0;
                int resultSample = 0;

                if (i < sound1Data.Length)
                {
                    firstFound = true;
                    firstSample = sound1Data[i];
                    firstSample += 32768;
                }

                if (i < sound2Data.Length)
                {
                    secondSample = sound2Data[i];
                    secondSample += 32768;

                    if (firstFound)
                    {
                        if (firstSample < 32768 || secondSample < 32768)
                            resultSample = firstSample * secondSample / 32768;
                        else
                            resultSample = 2 * (firstSample + secondSample) - (firstSample * secondSample) / 32768 - 65536;

                        resultSample = resultSample == 65536 ? 65535 : resultSample;
                        resultSample -= 32768;

                        resultingSoundData[i] = (short)resultSample;
                    }
                    else
                    {
                        resultingSoundData[i] = sound2Data[i];
                    }
                }
                else
                {
                    resultingSoundData[i] = sound1Data[i];
                }
            }

            for (int i = 0, j = 0; i < resultingSoundData.Length; i++)
            {
                var bytes = BitConverter.GetBytes(resultingSoundData[i]);

                resultData[j++] = bytes[0];
                resultData[j++] = bytes[1];
            }

            return new RawSourceWaveStream(resultData, 0, resultData.Length, reader1.WaveFormat);
        }

        private WaveStream MergeSounds(WaveStream reader1, WaveStream reader2)
        {
            var data1 = GetSoundData(reader1);
            var data2 = GetSoundData(reader2);

            var resultData = new byte[data1.Length * 2 + data2.Length * 2];

            int resultIndex = 0;
            for (int i = 0; i < data1.Length; i++)
            {
                var bytes = BitConverter.GetBytes(data1[i]);

                resultData[resultIndex++] = bytes[0];
                resultData[resultIndex++] = bytes[1];
            }

            for (int i = 0; i < data2.Length; i++)
            {
                var bytes = BitConverter.GetBytes(data2[i]);

                resultData[resultIndex++] = bytes[0];
                resultData[resultIndex++] = bytes[1];
            }

            return new RawSourceWaveStream(resultData, 0, resultData.Length, reader1.WaveFormat);
        }

        private WaveStream TrimWavFile(WaveStream reader, TimeSpan cutFromStart, TimeSpan cutFromEnd)
        {
            //Get sound bytes
            var soundBytesList = new List<byte>();
            using (BinaryReader bReader = new BinaryReader(reader))
            {
                reader.Seek(0, SeekOrigin.Begin);
                while (soundBytesList.Count < reader.Length)
                {
                    soundBytesList.Add(bReader.ReadByte());
                }
            }
            var soundBytesArray = soundBytesList.ToArray();

            //Get the bytes to read per milli second
            int bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000;

            //Get the starting position  
            int startPos = (int)cutFromStart.TotalMilliseconds * bytesPerMillisecond;

            //Get the ending position
            int endBytes = (int)cutFromEnd.TotalMilliseconds * bytesPerMillisecond;

            List<byte> result = new List<byte>();

            for (int i = 0; i < soundBytesArray.Length; i++)
            {
                if (i < startPos || i > endBytes)
                    result.Add(soundBytesArray[i]);
            }

            var bArray = result.ToArray();
            //var totalTime = reader.TotalTime.TotalMilliseconds - (cutFromStart.TotalMilliseconds + cutFromEnd.TotalMilliseconds);
            return new RawSourceWaveStream(bArray, 0, bArray.Length, reader.WaveFormat.AsStandardWaveFormat());
        }

        private void PlayFile(WaveStream reader)
        {
            reader.Seek(0, SeekOrigin.Begin);
            waveOut.Init(reader);
            waveOut.Play();
            waveOut.PlaybackStopped += onPlaybackStopped;
        }

        private void SetStatus(string text, Color foreground)
        {
            txtStatus.Text = text.Trim();
            txtStatus.ForeColor = foreground;
        }

        #endregion
    }
}
