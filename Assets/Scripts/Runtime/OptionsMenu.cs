using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Runtime
{
    public class OptionsMenu : MonoBehaviour
    {
        [SerializeField] private AudioMixer _mixer;

        [Header("Volume")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private Slider _voicesVolumeSlider;
        
        [Header("Accessibility")]
        [SerializeField] private Toggle _flashingEffectsToggle;
        
        private void OnEnable()
        {
            _masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1);
            _musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1);
            _sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1);
            _voicesVolumeSlider.value = PlayerPrefs.GetFloat("VoicesVolume", 1);
            
            _flashingEffectsToggle.isOn = PlayerPrefs.GetInt("FlashingEffects", 1) == 1;
        }

        public void SetMasterVolume(float value) => SetVolume("MasterVolume", value);
        public void SetMusicVolume(float value) => SetVolume("MusicVolume", value);
        public void SetSFXVolume(float value) => SetVolume("SFXVolume", value);
        public void SetVoicesVolume(float value) => SetVolume("VoicesVolume", value);

        private void SetVolume(string sliderName, float value)
        {
            if (!_mixer)
                return;
            
            if (Mathf.Approximately(value, 0))
                value = 0.0001f;
            
            _mixer.SetFloat(sliderName, Mathf.Log10(value) * 20);
            PlayerPrefs.SetFloat(sliderName, value);
        }
        
        public void SetFlashingEffects(bool value) => PlayerPrefs.SetInt("FlashingEffects", value ? 1 : 0);
    }
}