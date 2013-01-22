using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Threading;
using AudioPlay.SoundTouch;
using NAudio.Wave;
using MessageBox = System.Windows.MessageBox;

namespace AudioPlay.Pages
{
    /// <summary>
    /// Interaction logic for PlayPage.xaml
    /// </summary>
    public partial class PlayPage : Page
    {
        public PlayPage()
        {
            InitializeComponent();
            Loaded += PlayPageLoaded;
        }
        
        #region Const Datas

        private const int Latency = 125;
        private const string MP3Extension = ".mp3";

        #endregion

        #region Private Members
        
        private string _filePath = "";
        private int BUFFER_SIZE = 1024 * 10;
        private static bool tempoChanged = false;
        //private static bool pitchChanged;
        
        private CusBufferedWaveProvider provider;

        private Mp3FileReader reader;
        private BlockAlignReductionStream blockAlignReductionStream;
        private static WaveChannel32 waveChannel;

        //private static SoundTouchNet.SoundStretcher stretcher;
        private static SoundTouchAPI soundTouch;
        private TimeStretchProfile timeStretchProfile;

        private static IWavePlayer player;

        private Thread _playThread;

        private object PropertiesLock = new object();

        private TimeSpan totalTime;
        private DispatcherTimer timer;
        private bool isDragging = false;

        private bool stopWorker;

        //private static WaveFileWriter waveFileWriter;

        #endregion

        #region Private Methods

        private void PlayPageLoaded(object sender, RoutedEventArgs e)
        {
            IsPlaying = null;
        }


        private void Init()
        {
            if (!CheckFile(_filePath))
            {
                return;
            }

            TextBlock_FileName.Text = System.IO.Path.GetFileName(_filePath);

            // 初始化 reader
            reader = new Mp3FileReader(_filePath);

            // 初始化 provider
            blockAlignReductionStream = new BlockAlignReductionStream(reader);
            waveChannel = new WaveChannel32(blockAlignReductionStream);
            Dispatcher.Invoke(new Action(() => { waveChannel.Volume = Volume; }));

            //waveFileWriter = new WaveFileWriter("./test.mp3", waveChannel.WaveFormat);

            provider = new CusBufferedWaveProvider(waveChannel.WaveFormat);

            // 初始化 player
            //player = new WasapiOut(global::NAudio.CoreAudioApi.AudioClientShareMode.Shared, Latency);
            //player = new WaveOut();
            player = new DirectSoundOut(Latency);
            player.Init(provider);


            //// 初始化 stretcher
            //stretcher = new SoundStretcher(reader.WaveFormat.SampleRate, reader.WaveFormat.Channels);
            //stretcher.Tempo = 1f;
            //stretcher.Pitch = 1f;
            //stretcher.Rate = 1f;

            // 初始化 soundTouch
            soundTouch = new SoundTouchAPI();
            soundTouch.CreateInstance();

            soundTouch.SetSampleRate(waveChannel.WaveFormat.SampleRate);
            soundTouch.SetChannels(waveChannel.WaveFormat.Channels);
            soundTouch.SetTempoChange(0f);
            soundTouch.SetPitchSemiTones(0f);
            soundTouch.SetRateChange(0f);

            soundTouch.SetTempo(TempoValue);
            InitTimeStretchProfile();
            soundTouch.SetSetting(SoundTouch.SoundTouchAPI.SoundTouchSettings.SETTING_SEQUENCE_MS, 0);


            _playThread = new Thread(ProcessWave);
            _playThread.Name = "PlayThread";
        }

        private void ProcessWave()
        {
            //MsToBytes(Latency);
            byte[] inputBuffer = new byte[BUFFER_SIZE * sizeof(float)];
            byte[] soundTouchOutBuffer = new byte[BUFFER_SIZE * sizeof(float)];

            ByteAndFloatsConverter convertInputBuffer = new ByteAndFloatsConverter { Bytes = inputBuffer };
            ByteAndFloatsConverter convertOutputBuffer = new ByteAndFloatsConverter { Bytes = soundTouchOutBuffer };

            byte[] buffer = new byte[BUFFER_SIZE];
            bool finished = false;
            int bytesRead = 0;
            stopWorker = false;
            while ( !stopWorker && waveChannel.Position < waveChannel.Length)
            {
                //bytesRead = waveChannel.Read(buffer, 0, BUFFER_SIZE);
                bytesRead = waveChannel.Read(convertInputBuffer.Bytes, 0, convertInputBuffer.Bytes.Length);
                //bytesRead = reader.Read(convertInputBuffer.Bytes, 0, convertInputBuffer.Bytes.Length);

                SetSoundSharpValues();
                
                int floatsRead = bytesRead / ((sizeof(float)) * waveChannel.WaveFormat.Channels);
                soundTouch.PutSamples(convertInputBuffer.Floats, (uint)floatsRead);
                
                uint receivecount;

                do
                {// 榨干SoundTouch里面的数据
                    uint outBufferSizeFloats = (uint)convertOutputBuffer.Bytes.Length / (uint)(sizeof(float) * waveChannel.WaveFormat.Channels);

                    receivecount = soundTouch.ReceiveSamples(convertOutputBuffer.Floats, outBufferSizeFloats);

                    #region Test: write buffers into test.mp3
                    //waveFileWriter.Write(convertOutputBuffer.Bytes, 0, convertOutputBuffer.Bytes.Length);
                    //bool finish = false;
                    //if (finish)
                    //{
                    //    waveFileWriter.Close();
                    //}
                    #endregion

                    if (receivecount > 0)
                    {
                        provider.AddSamples(convertOutputBuffer.Bytes, 0, (int)receivecount * sizeof(float) * reader.WaveFormat.Channels, reader.CurrentTime); ;
                        //provider.AddSamples(convertOutputBuffer.Bytes, 0, convertOutputBuffer.Bytes.Length, reader.CurrentTime); ;

                        while (provider.BuffersCount > 3)
                        {
                            Thread.Sleep(10);
                        } 
                    }

                    //if (finished && bytesRead == 0)
                    //{
                    //    break;
                    //} 
                } while (!stopWorker && receivecount != 0);
            } 

            reader.Close();
        }

        private void SetSoundSharpValues()
        {
            if (tempoChanged)
            {
                float tempo = TempoValue;
                soundTouch.SetTempo(tempo);
                tempoChanged = false;
                ApplySoundTouchTimeStretchProfile();
            }

            //if (pitchChanged)
            //{
            //    float pitch = this.Pitch;
            //    // Assign updated pitch
            //    // m_soundTouchSharp.SetPitchOctaves(pitch);
            //    soundTouch.SetPitchSemiTones(pitch);
            //    pitchChanged = false;
            //}
        }

        private bool CheckFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }
            else
            {
                if (System.IO.Path.GetExtension(filePath) == MP3Extension)
                {
                    return true;
                }
                else
                {
                    MessageBox.Show("目前仅支持MP3格式文件");
                    return false;
                }
            }
        }

        private void InitTimeStretchProfile()
        {
            TimeStretchProfile = new TimeStretchProfile();
            TimeStretchProfile.AAFilterLength = 128;
            TimeStretchProfile.Description = "Optimum for Music and Speech";
            TimeStretchProfile.Id = "Practice#_Optimum";
            TimeStretchProfile.Overlap = 20;
            TimeStretchProfile.SeekWindow = 80;
            TimeStretchProfile.Sequence = 20;
            TimeStretchProfile.UseAAFilter = true;

            soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_USE_AA_FILTER, timeStretchProfile.UseAAFilter ? 1 : 0);
            soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_AA_FILTER_LENGTH, timeStretchProfile.AAFilterLength);
            soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_OVERLAP_MS, timeStretchProfile.Overlap);
            soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_SEQUENCE_MS, timeStretchProfile.Sequence);
            soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_SEEKWINDOW_MS, timeStretchProfile.SeekWindow);
        }

        private void ApplySoundTouchTimeStretchProfile()
        {
            // "Disable" sound touch AA and revert to Automatic settings at regular tempo (to remove side effects)
            if (Math.Abs(TempoValue - 1) < 0.001)
            {
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_USE_AA_FILTER, 0);
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_AA_FILTER_LENGTH, 0);
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_OVERLAP_MS, 0);
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_SEQUENCE_MS, 0);
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_SEEKWINDOW_MS, 0);
            }
            else
            {
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_USE_AA_FILTER, timeStretchProfile.UseAAFilter ? 1 : 0);
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_AA_FILTER_LENGTH, timeStretchProfile.AAFilterLength);
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_OVERLAP_MS, timeStretchProfile.Overlap);
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_SEQUENCE_MS, timeStretchProfile.Sequence);
                soundTouch.SetSetting(SoundTouchAPI.SoundTouchSettings.SETTING_SEEKWINDOW_MS, timeStretchProfile.SeekWindow);
            }
        }

        public TimeStretchProfile TimeStretchProfile
        {
            get
            {
                lock (PropertiesLock) { return timeStretchProfile; }
            }
            set
            {
                lock (PropertiesLock)
                {
                    timeStretchProfile = value;
                }
            }
        }


        private int MsToBytes(int ms)
        {
            int bytes = ms * (provider.WaveFormat.AverageBytesPerSecond / 1000);
            bytes -= bytes % provider.WaveFormat.BlockAlign;
            BUFFER_SIZE = bytes;
            return bytes;
        }

        private void StopPlay()
        {
            if (player == null)
            {
                return;
            }
            Dispatcher.Invoke(new Action(() => { IsPlaying = null; }));
        }

        private void InitTime()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(200);
            timer.Tick += new EventHandler(timer_Tick);

            totalTime = reader.TotalTime;
            string totalTimeString = string.Format("{0:00}:{1:00}:{2:00}",
                totalTime.Hours, totalTime.Minutes, totalTime.Seconds);
            Run_TotalTime.Text = totalTimeString;

            Slider_Position.Maximum = totalTime.TotalSeconds;
            Slider_Position.SmallChange = 0.1;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (!isDragging)
            {
                Slider_Position.Value = waveChannel.CurrentTime.TotalSeconds;
            }
        }

        #endregion

        #region Dispose

        internal void Dispose()
        {
            _playThread.Abort();
            soundTouch.Dispose();
            blockAlignReductionStream.Dispose();
            reader.Dispose();
            waveChannel.Dispose();
            player.Dispose();
            //waveFileWriter.Dispose();
        }

        #endregion
        
        #region UI Event

        private void Button_Open_Click(object sender, RoutedEventArgs e)
        {
            if (reader != null)
            {
                Dispose();
                StopPlay();
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            _filePath = openFileDialog.FileName;

            Init();
            InitTimeStretchProfile();
            InitTime();
            timer.Start();
            _playThread.Start();
            //player.Play();
            IsPlaying = true;
        }

        private void Slider_Position_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            isDragging = true;
        }

        private void Slider_Position_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            isDragging = false;
            waveChannel.CurrentTime = TimeSpan.FromSeconds(Slider_Position.Value);
        }

        private void Slider_Position_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TimeSpan curTime = new TimeSpan(0, 0, 0, Convert.ToInt32(Slider_Position.Value));
            string currentTimeString = string.Format("{0:00}:{1:00}:{2:00}",
                curTime.Hours, curTime.Minutes, curTime.Seconds);
            Run_CurrentTime.Text = currentTimeString;
        }

        private void Button_Play_Click(object sender, RoutedEventArgs e)
        {
            if (reader == null)
            {
                return;
            }

            Dispatcher.Invoke(new Action(() =>
            {
                if (IsPlaying == true)
                {
                    IsPlaying = false;
                }
                else if (IsPlaying == false)
                {
                    IsPlaying = true;
                }
            }));
        }

        private void Button_Stop_Click(object sender, RoutedEventArgs e)
        {
            StopPlay();
        }

        private void Button_Test_Click(object sender, RoutedEventArgs e)
        {
        }

        #endregion
    }
}
