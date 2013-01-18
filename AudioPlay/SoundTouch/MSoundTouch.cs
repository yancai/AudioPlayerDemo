using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AudioPlay.SoundTouch
{
    class MSoundTouch
    {
        #region Members

        private IntPtr m_handle = IntPtr.Zero;

        public string SoundTouchVersionString { get; private set; }
        public int SoundTouchVersionId { get; private set; }

        #endregion

        #region Private Methods

        /// <summary>
        /// Helper function for validating the SoundTouch as initialized
        /// </summary>
        private void VerifyInstanceInitialized()
        {
            if (m_handle == IntPtr.Zero)
            {
                throw new ApplicationException("SoundTouch as not initialized. Use CreateInstance()");
            }
        }

        #endregion

        #region SoundTouch .NET wrapper API

        /// <summary>
        /// .NET C# Wrapper to the SoundTouch Native C++ Audio library
        /// </summary>
        /// <see cref="http://www.surina.net/soundtouch/index.html"/>
        public void CreateInstance()
        {
            if (m_handle != IntPtr.Zero)
            {
                throw new ApplicationException("SoundSharp Instance was already initialized but not destroyed. Use DestroyInstance().");
            }

            m_handle = soundtouch_createInstance();
        }

        
        /// <summary>
        /// Sets sample rate.
        /// </summary>
        /// <param name="srate"></param>
        public void SetSampleRate(int srate)
        {
            VerifyInstanceInitialized();

            soundtouch_setSampleRate(m_handle, (uint)srate);
        }

        public void SetChannels(int numChannels)
        {
            VerifyInstanceInitialized();

            soundtouch_setChannels(m_handle, (uint)numChannels);
        }

        public void SetTempoChange(float newTempo)
        {
            VerifyInstanceInitialized();

            soundtouch_setTempoChange(m_handle, newTempo);
        }

        public void SetPitchSemiTones(float newPitch)
        {
            VerifyInstanceInitialized();

            soundtouch_setPitchSemiTones(m_handle, newPitch);
        }

        public void SetRateChange(float newRate)
        {
            VerifyInstanceInitialized();

            soundtouch_setRateChange(m_handle, newRate);
        }

        public void SetSetting(SoundTouchSettings settingId, int value)
        {
            VerifyInstanceInitialized();

            soundtouch_setSetting(m_handle, (int)settingId, value);
        }

        public void SetTempo(float newTempo)
        {
            VerifyInstanceInitialized();

            soundtouch_setTempo(m_handle, newTempo);
        }

        public void SetPitch(float newPitch)
        {
            VerifyInstanceInitialized();

            soundtouch_setPitch(m_handle, newPitch);
        }

        public void SetRate(float newRate)
        {
            VerifyInstanceInitialized();

            soundtouch_setRate(m_handle, newRate);
        }



        public enum SoundTouchSettings
        {
            /// <summary>
            /// Available setting IDs for the 'setSetting' and 'get_setting' functions.
            /// Enable/disable anti-alias filter in pitch transposer (0 = disable)
            /// </summary>
            SETTING_USE_AA_FILTER = 0,

            /// <summary>
            /// Pitch transposer anti-alias filter length (8 .. 128 taps, default = 32)
            /// </summary>
            SETTING_AA_FILTER_LENGTH = 1,

            /// <summary>
            /// Enable/disable quick seeking algorithm in tempo changer routine
            /// (enabling quick seeking lowers CPU utilization but causes a minor sound
            ///  quality compromising)
            /// </summary>
            SETTING_USE_QUICKSEEK = 2,

            /// <summary>
            /// Time-stretch algorithm single processing sequence length in milliseconds. This determines 
            /// to how long sequences the original sound is chopped in the time-stretch algorithm. 
            /// See "STTypes.h" or README for more information.
            /// </summary>
            SETTING_SEQUENCE_MS = 3,

            /// <summary>
            /// Time-stretch algorithm seeking window length in milliseconds for algorithm that finds the 
            /// best possible overlapping location. This determines from how wide window the algorithm 
            /// may look for an optimal joining location when mixing the sound sequences back together. 
            /// See "STTypes.h" or README for more information.
            /// </summary>
            SETTING_SEEKWINDOW_MS = 4,

            /// <summary>
            /// Time-stretch algorithm overlap length in milliseconds. When the chopped sound sequences 
            /// are mixed back together, to form a continuous sound stream, this parameter defines over 
            /// how long period the two consecutive sequences are let to overlap each other. 
            /// See "STTypes.h" or README for more information.
            /// </summary>
            SETTING_OVERLAP_MS = 5
        };
        #endregion


        #region SoundSharp Native API - DLL Imports

        public const string soundTouchDLL = "../../../references/SoundTouch.dll";

        /// Create a new instance of SoundTouch processor.
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr soundtouch_createInstance();

        /// Destroys a SoundTouch processor instance.
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern void soundtouch_destroyInstance(IntPtr h);

        /// Get SoundTouch library version string
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        private static extern String soundtouch_getVersionString();

        /// Get SoundTouch library version Id
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern uint soundtouch_getVersionId();

        /// Sets new rate control value. Normal rate = 1.0, smaller values
        /// represent slower rate, larger faster rates.
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern void soundtouch_setRate(IntPtr h, float newRate);

        /// Sets new tempo control value. Normal tempo = 1.0, smaller values
        /// represent slower tempo, larger faster tempo.
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern void soundtouch_setTempo(IntPtr h, float newTempo);

        /// Sets new rate control value as a difference in percents compared
        /// to the original rate (-50 .. +100 %);
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern void soundtouch_setRateChange(IntPtr h, float newRate);

        /// Sets new tempo control value as a difference in percents compared
        /// to the original tempo (-50 .. +100 %);
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern void soundtouch_setTempoChange(IntPtr h, float newTempo);

        /// Sets new pitch control value. Original pitch = 1.0, smaller values
        /// represent lower pitches, larger values higher pitch.
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern void soundtouch_setPitch(IntPtr h, float newPitch);

        /// Sets pitch change in octaves compared to the original pitch  
        /// (-1.00 .. +1.00);
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern void soundtouch_setPitchOctaves(IntPtr h, float newPitch);

        /// Sets pitch change in semi-tones compared to the original pitch
        /// (-12 .. +12);
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern void soundtouch_setPitchSemiTones(IntPtr h, float newPitch);


        /// Sets the number of channels, 1 = mono, 2 = stereo
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern void soundtouch_setChannels(IntPtr h, uint numChannels);

        /// Sets sample rate.
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern void soundtouch_setSampleRate(IntPtr h, uint srate);

        /// Flushes the last samples from the processing pipeline to the output.
        /// Clears also the internal processing buffers.
        //
        /// Note: This function is meant for extracting the last samples of a sound
        /// stream. This function may introduce additional blank samples in the end
        /// of the sound stream, and thus it's not recommended to call this function
        /// in the middle of a sound stream.
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern void soundtouch_flush(IntPtr h);

        /// Adds 'numSamples' pcs of samples from the 'samples' memory position into
        /// the input of the object. Notice that sample rate _has_to_ be set before
        /// calling this function, otherwise throws a runtime_error exception.
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern void soundtouch_putSamples(IntPtr h,
           [MarshalAs(UnmanagedType.LPArray)] short[] samples,       ///< Pointer to sample buffer.
           uint numSamples      ///< Number of samples in buffer. Notice
            ///< that in case of stereo-sound a single sample
            ///< contains data for both channels.
           );

        /// Clears all the samples in the object's output and internal processing
        /// buffers.
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern void soundtouch_clear(IntPtr h);

        /// Changes a setting controlling the processing system behaviour. See the
        /// 'SETTING_...' defines for available setting ID's.
        /// 
        /// \return 'TRUE' if the setting was succesfully changed
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern bool soundtouch_setSetting(IntPtr h,
                   int settingId,   ///< Setting ID number. see SETTING_... defines.
                   int value        ///< New setting value.
                   );

        /// Reads a setting controlling the processing system behaviour. See the
        /// 'SETTING_...' defines for available setting ID's.
        ///
        /// \return the setting value.
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern int soundtouch_getSetting(IntPtr h,
                             int settingId    ///< Setting ID number, see SETTING_... defines.
                   );


        /// Returns number of samples currently unprocessed.
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern uint soundtouch_numUnprocessedSamples(IntPtr h);

        /// Adjusts book-keeping so that given number of samples are removed from beginning of the 
        /// sample buffer without copying them anywhere. 
        ///
        /// Used to reduce the number of samples in the buffer when accessing the sample buffer directly
        /// with 'ptrBegin' function.
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern uint soundtouch_receiveSamples(IntPtr h,
               [MarshalAs(UnmanagedType.LPArray)] short[] outBuffer,           ///< Buffer where to copy output samples.
               uint maxSamples     ///< How many samples to receive at max.
               );

        /// Returns number of samples currently available.
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern uint soundtouch_numSamples(IntPtr h);

        /// Returns nonzero if there aren't any samples available for outputting.
        [DllImport(soundTouchDLL, CallingConvention = CallingConvention.StdCall)]
        private static extern uint soundtouch_isEmpty(IntPtr h);

        #endregion
    }
}
