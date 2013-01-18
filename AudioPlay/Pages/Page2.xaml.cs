using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AudioPlay.SoundTouch;
using NAudio.Wave;

namespace AudioPlay.Pages
{
    /// <summary>
    /// Interaction logic for Page2.xaml
    /// </summary>
    public partial class Page2 : Page
    {
        private string _filePath;
        private Mp3FileReader reader;
        private BlockAlignReductionStream BARStream;
        private WaveChannel32 waveChannel;
        private SoundTouchAPI soundTouch;
        private CusBufferedWaveProvider provider;
        private DirectSoundOut player;
        private Thread _playThread;

        public Page2()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            _filePath = openFileDialog.FileName;

            Init();

            player.Play();
            _playThread.Start();
        }


        private void Init()
        {
            reader = new Mp3FileReader(_filePath);
            BARStream = new BlockAlignReductionStream(reader);
            waveChannel = new WaveChannel32(BARStream);
            waveChannel.Volume = 0.5f;

            soundTouch = new SoundTouch.SoundTouchAPI();
            soundTouch.CreateInstance();
            soundTouch.SetChannels(2);
            soundTouch.SetSampleRate(waveChannel.WaveFormat.SampleRate);

            provider = new CusBufferedWaveProvider(waveChannel.WaveFormat);

            player = new DirectSoundOut(125);
            player.Init(provider);

            _playThread = new Thread(ProcessAudio);
            _playThread.Name = "ProcessAudio";

        }

        private void ProcessAudio()
        {
            int BUFFER_SIZE = 44000;
            byte[] buffer = new byte[BUFFER_SIZE];
            int bytesRead = 0;

            do
            {
                if (provider.BuffersCount > 50)
                {
                    Thread.Sleep(100);
                    continue;
                }

                bytesRead = waveChannel.Read(buffer, 0, BUFFER_SIZE);
                
                provider.AddSamples(buffer, 0, BUFFER_SIZE, reader.CurrentTime);

            } while (bytesRead !=0);


        }
    }
}
