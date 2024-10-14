using System;
using System.Numerics;

using ManagedBass;
using ManagedBass.DirectX8;

namespace ProjectThunderIV.Classes
{
    internal class SoundStream
    {
        #region Variables
        public int Handle;
        private int reverbEffectHandle;
        private int compressorEffectHandle;

        public float VolumeBoost;
        public float TargetVolume;

        public Vector3 Position;

        public bool KeepAlive;
        #endregion

        #region Constructor
        public SoundStream(int handle, float volumeBoost, bool keepAlive)
        {
            Handle = handle;
            VolumeBoost = volumeBoost;
            KeepAlive = keepAlive;
        }
        public SoundStream(int handle, float volumeBoost)
        {
            Handle = handle;
            VolumeBoost = volumeBoost;
            KeepAlive = false;
        }
        #endregion

        #region Methods
        public void CalculateTargetVolume(uint gameSfxVolume, bool playerIsInInterior)
        {
            float v = ((gameSfxVolume + VolumeBoost) * ModSettings.GlobalSoundMultiplier) / 10.0f;

            if (playerIsInInterior)
                v = v - ModSettings.LowerVolumeByCertainAmountWhenInInterior;

            if (ModSettings.VolumeIsAllowedToGoAboveOne)
                TargetVolume = Math.Max(0.0f, v);
            else
                TargetVolume = Math.Max(0.0f, Math.Min(1.0f, v));
        }
        public void Set3DPosition(Vector3 pos)
        {
            Position = pos;
            Bass.ChannelSet3DPosition(Handle, new Vector3D(pos.X, pos.Y, pos.Z), null, null);
            Bass.Apply3D();
        }

        // Reverb
        public void AddReverbEffect(DXReverbParameters parameters)
        {
            reverbEffectHandle = Bass.ChannelSetFX(Handle, EffectType.DXReverb, 1);
            ApplyReverbParameters(parameters);
        }
        public void RemoveReverbEffect()
        {
            if (reverbEffectHandle != 0)
            {
                Bass.ChannelRemoveFX(Handle, reverbEffectHandle);
                reverbEffectHandle = 0;
            }
        }
        public void ApplyReverbParameters(DXReverbParameters parameters)
        {
            if (reverbEffectHandle == 0)
                return;

            Bass.FXSetParameters(reverbEffectHandle, parameters);
        }

        // Compressor
        public void AddCompressorEffect(DXCompressorParameters parameters)
        {
            compressorEffectHandle = Bass.ChannelSetFX(Handle, EffectType.DXCompressor, 2);
            ApplyCompressorParameters(parameters);
        }
        public void RemoveCompressorEffect()
        {
            if (compressorEffectHandle != 0)
            {
                Bass.ChannelRemoveFX(Handle, compressorEffectHandle);
                compressorEffectHandle = 0;
            }
        }
        public void ApplyCompressorParameters(DXCompressorParameters parameters)
        {
            if (compressorEffectHandle == 0)
                return;

            Bass.FXSetParameters(compressorEffectHandle, parameters);
        }
        #endregion

        #region Functions
        public bool Free()
        {
            return Bass.StreamFree(Handle);
        }

        public bool Play(bool restart = false)
        {
            return Bass.ChannelPlay(Handle, restart);
        }
        public bool Pause()
        {
            return Bass.ChannelPause(Handle);
        }
        public bool Stop()
        {
            return Bass.ChannelStop(Handle);
        }

        public bool ApplyVolume()
        {
            return Bass.ChannelSetAttribute(Handle, ChannelAttribute.Volume, TargetVolume);
        }

        public PlaybackState GetState()
        {
            return Bass.ChannelIsActive(Handle);
        }
        #endregion
    }
}
