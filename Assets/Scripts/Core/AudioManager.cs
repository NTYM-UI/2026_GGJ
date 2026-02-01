using UnityEngine;

namespace Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Clips")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip optionClickSound;
        [SerializeField] private AudioClip popupOpenSound;
        [SerializeField] private AudioClip messageSound;
        [SerializeField] private AudioClip notificationSound;
        [SerializeField] private AudioClip backgroundMusic;
        
        [Header("Settings")]
        [SerializeField] private float sfxVolume = 1.0f;
        [SerializeField] private float musicVolume = 1.0f;
        
        [Range(0f, 1f)]
        [SerializeField] private float messageVolumeScale = 0.5f;

        private AudioSource sfxSource;
        private AudioSource musicSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioSource();
                PlayMusic(backgroundMusic);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAudioSource()
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        public void PlayButtonSound()
        {
            if (buttonClickSound != null)
            {
                PlaySFX(buttonClickSound);
            }
            else
            {
                Debug.LogWarning("[AudioManager] Button click sound is not assigned!");
            }
        }

        public void PlayOptionSound()
        {
            if (optionClickSound != null)
            {
                PlaySFX(optionClickSound);
            }
            else
            {
                // Fallback to normal button sound if option sound is not assigned
                if (buttonClickSound != null)
                {
                    PlaySFX(buttonClickSound);
                }
            }
        }

        public void PlayPopupSound()
        {
            if (popupOpenSound != null)
            {
                PlaySFX(popupOpenSound);
            }
        }

        public void PlayMessageSound()
        {
            if (messageSound != null)
            {
                PlaySFX(messageSound, messageVolumeScale);
            }
        }

        public void PlayNotificationSound()
        {
            if (notificationSound != null)
            {
                PlaySFX(notificationSound);
            }
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
        {
            if (clip == null) return;
            if (sfxSource == null) InitializeAudioSource();
            
            sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
        }

        public void SetVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null) return;
            if (musicSource == null) InitializeAudioSource();

            if (musicSource.clip == clip && musicSource.isPlaying) return;

            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
        }
    }
}
