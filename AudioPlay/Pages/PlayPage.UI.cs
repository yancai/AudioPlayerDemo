using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace AudioPlay.Pages
{
    public partial class PlayPage
    {
        public static readonly DependencyProperty TempoProperty =
            DependencyProperty.Register("Tempo", typeof (float), typeof (PlayPage), new PropertyMetadata(1.0f, TempoChangedCallback));

        private static void TempoChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            tempoChanged = true;
            //soundTouch.SetTempo((float) e.NewValue);
        }

        public float Tempo
        {
            get { return (float) GetValue(TempoProperty); }
            set { SetValue(TempoProperty, value); }
        }

        internal float TempoValue
        {
            get
            {
                float value = 0;
                Dispatcher.Invoke(new Action(() => { value = Tempo; }));
                return value;
            }
        }

        public static readonly DependencyProperty VolumeProperty =
            DependencyProperty.Register("Volume", typeof (float), typeof (PlayPage), new PropertyMetadata(0.25f, VolumeChangedCallback));

        private static void VolumeChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (waveChannel != null)
            {
                waveChannel.Volume = (float)e.NewValue;                
            }
        }

        public float Volume
        {
            get { return (float) GetValue(VolumeProperty); }
            set { SetValue(VolumeProperty, value); }
        }

        public static readonly DependencyProperty IsPlayingProperty =
            DependencyProperty.Register("IsPlaying", typeof (bool?), typeof (PlayPage), new PropertyMetadata(null, IsPlayingChangedCallback));

        private static void IsPlayingChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool?)e.NewValue == true)
            {
                player.Play();
            }
            else if((bool?)e.NewValue == false)
            {
                player.Pause();
            }
            else if ((bool?)e.NewValue == null)
            {
                if (player == null)
                {
                    return;
                }
                player.Stop();
            }
        }

        public bool? IsPlaying
        {
            get { return (bool?) GetValue(IsPlayingProperty); }
            set { SetValue(IsPlayingProperty, value); }
        }
    }
}
