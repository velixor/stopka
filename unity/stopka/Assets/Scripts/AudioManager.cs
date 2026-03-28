using UnityEngine;

namespace Stopka
{
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Clips")]
        [SerializeField] private AudioClip placeSound;
        [SerializeField] private AudioClip sliceSound;
        [SerializeField] private AudioClip perfectSound;
        [SerializeField] private AudioClip gameOverSound;
        [SerializeField] private AudioClip backgroundMusic;

        [Header("Settings")]
        [SerializeField] private float basePitch = 1f;
        [SerializeField] private float pitchIncreasePerCombo = 0.05f;
        [SerializeField] private float maxPitch = 1.5f;
        [SerializeField] private float musicVolume = 0.3f;

        private AudioSource sfxSource;
        private AudioSource musicSource;
        private bool isMuted;

        private void Awake()
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = musicVolume;

            isMuted = PlayerPrefs.GetInt("SoundEnabled", 1) == 0;
            ApplyMuteState();
        }

        public void SetMuted(bool muted)
        {
            isMuted = muted;
            PlayerPrefs.SetInt("SoundEnabled", muted ? 0 : 1);
            PlayerPrefs.Save();
            ApplyMuteState();
        }

        public bool IsMuted => isMuted;

        private void ApplyMuteState()
        {
            sfxSource.mute = isMuted;
            musicSource.mute = isMuted;
        }

        public void StartMusic()
        {
            if (backgroundMusic != null && !musicSource.isPlaying)
            {
                musicSource.clip = backgroundMusic;
                musicSource.Play();
            }
        }

        public void StopMusic()
        {
            musicSource.Stop();
        }

        public void PlayPlace(int comboCount)
        {
            if (comboCount > 0 && perfectSound != null)
            {
                sfxSource.pitch = Mathf.Min(basePitch + comboCount * pitchIncreasePerCombo, maxPitch);
                sfxSource.PlayOneShot(perfectSound);
            }
            else if (placeSound != null)
            {
                sfxSource.pitch = basePitch;
                sfxSource.PlayOneShot(placeSound);
            }
        }

        public void PlaySlice()
        {
            if (sliceSound != null)
            {
                sfxSource.pitch = basePitch;
                sfxSource.PlayOneShot(sliceSound);
            }
        }

        public void PlayGameOver()
        {
            if (gameOverSound != null)
            {
                sfxSource.pitch = basePitch;
                sfxSource.PlayOneShot(gameOverSound);
            }
        }
    }
}
