using UnityEngine;

namespace MadMantra.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Sources")]
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Clips")]
        [SerializeField] private AudioClip ambientHum;
        [SerializeField] private AudioClip lightFlicker;
        [SerializeField] private AudioClip footstep;
        [SerializeField] private AudioClip buttonClick;
        [SerializeField] private AudioClip puzzleSolved;
        [SerializeField] private AudioClip puzzleFail;
        [SerializeField] private AudioClip keyPickup;
        [SerializeField] private AudioClip doorLocked;
        [SerializeField] private AudioClip doorUnlock;
        [SerializeField] private AudioClip doorOpen;
        [SerializeField] private AudioClip typewriterBlip;

        private void Awake()
        {
            Instance = this;
            if (ambientSource != null && ambientHum != null)
            {
                ambientSource.clip = ambientHum;
                ambientSource.loop = true;
                ambientSource.volume = 0.25f;
                ambientSource.Play();
            }
        }

        private void Play(AudioClip clip, float volume = 1f, float pitch = 1f, float pitchJitter = 0f)
        {
            if (sfxSource == null || clip == null) return;
            sfxSource.pitch = pitch + Random.Range(-pitchJitter, pitchJitter);
            sfxSource.PlayOneShot(clip, volume);
        }

        public void PlayLightFlicker() => Play(lightFlicker, 0.25f, 1f, 0.1f);
        public void PlayFootstep() => Play(footstep, 0.3f, 0.85f, 0.08f);
        public void PlayButtonClick() => Play(buttonClick, 0.7f, 1f, 0.05f);
        public void PlayPuzzleSolved() => Play(puzzleSolved, 0.75f);
        public void PlayPuzzleFail() => Play(puzzleFail, 0.7f);
        public void PlayKeyPickup() => Play(keyPickup, 0.75f);
        public void PlayDoorLocked() => Play(doorLocked, 0.65f, 0.9f);
        public void PlayDoorUnlock() => Play(doorUnlock, 0.75f, 1.1f);
        public void PlayDoorOpen() => Play(doorOpen, 0.65f, 1f, 0.05f);
        public void PlayTypewriterBlip() => Play(typewriterBlip, 0.25f, 1f, 0.05f);
    }
}
