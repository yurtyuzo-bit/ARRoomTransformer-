using System;
using System.Collections;
using UnityEngine;

namespace ARRoomTransformer
{
    /// <summary>
    /// Ortam sesleri ve efekt sesleri yöneticisi.
    /// Backrooms ambiyansı, UI sesleri ve yerleştirme efektleri sağlar.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Ses Kaynakları")]
        [SerializeField] private AudioSource ambianceSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;

        [Header("Ortam Sesleri")]
        [SerializeField] private AudioClip ambianceBackrooms;
        [SerializeField] private AudioClip ambianceDarkCorridor;
        [SerializeField] private AudioClip ambianceHospital;

        [Header("Efekt Sesleri")]
        [SerializeField] private AudioClip sfxPlaceAsset;
        [SerializeField] private AudioClip sfxDeleteAsset;
        [SerializeField] private AudioClip sfxCornerMark;
        [SerializeField] private AudioClip sfxScanComplete;
        [SerializeField] private AudioClip sfxRecordStart;
        [SerializeField] private AudioClip sfxRecordStop;
        [SerializeField] private AudioClip sfxCountdownTick;
        [SerializeField] private AudioClip sfxScreenshot;

        [Header("UI Sesleri")]
        [SerializeField] private AudioClip uiButtonClick;
        [SerializeField] private AudioClip uiPanelOpen;
        [SerializeField] private AudioClip uiPanelClose;
        [SerializeField] private AudioClip uiError;

        [Header("Ses Ayarları")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float ambianceVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 0.6f;
        [SerializeField] private float ambianceFadeDuration = 2f;

        private Coroutine _fadeCoroutine;
        private string _currentAmbianceTheme;

        // Singleton
        private static AudioManager _instance;
        public static AudioManager Instance => _instance;

        /// <summary>Ana ses seviyesi.</summary>
        public float MasterVolume
        {
            get => masterVolume;
            set
            {
                masterVolume = Mathf.Clamp01(value);
                UpdateVolumes();
            }
        }

        /// <summary>Ortam ses seviyesi.</summary>
        public float AmbianceVolume
        {
            get => ambianceVolume;
            set
            {
                ambianceVolume = Mathf.Clamp01(value);
                UpdateVolumes();
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            EnsureAudioSources();
        }

        private void EnsureAudioSources()
        {
            if (ambianceSource == null)
            {
                var go = new GameObject("AmbianceSource");
                go.transform.SetParent(transform);
                ambianceSource = go.AddComponent<AudioSource>();
                ambianceSource.loop = true;
                ambianceSource.playOnAwake = false;
                ambianceSource.spatialBlend = 0f; // 2D ses
            }

            if (sfxSource == null)
            {
                var go = new GameObject("SFXSource");
                go.transform.SetParent(transform);
                sfxSource = go.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }

            if (uiSource == null)
            {
                var go = new GameObject("UISource");
                go.transform.SetParent(transform);
                uiSource = go.AddComponent<AudioSource>();
                uiSource.loop = false;
                uiSource.playOnAwake = false;
                uiSource.spatialBlend = 0f;
            }

            UpdateVolumes();
        }

        private void UpdateVolumes()
        {
            if (ambianceSource) ambianceSource.volume = ambianceVolume * masterVolume;
            if (sfxSource) sfxSource.volume = sfxVolume * masterVolume;
            if (uiSource) uiSource.volume = uiVolume * masterVolume;
        }

        // ================================================================
        // Ortam Sesleri
        // ================================================================

        /// <summary>Temaya göre ortam sesini başlatır (fade-in ile).</summary>
        public void PlayAmbiance(string themeName)
        {
            AudioClip clip = GetAmbianceClip(themeName);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] '{themeName}' için ambiyans sesi bulunamadı.");
                return;
            }

            _currentAmbianceTheme = themeName;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(CrossfadeAmbiance(clip));
        }

        /// <summary>Ortam sesini durdurur (fade-out ile).</summary>
        public void StopAmbiance()
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeOutAmbiance());
        }

        private IEnumerator CrossfadeAmbiance(AudioClip newClip)
        {
            float targetVolume = ambianceVolume * masterVolume;

            // Fade out
            if (ambianceSource.isPlaying)
            {
                yield return FadeVolume(ambianceSource, 0f, ambianceFadeDuration * 0.5f);
            }

            // Yeni clip
            ambianceSource.clip = newClip;
            ambianceSource.volume = 0f;
            ambianceSource.Play();

            // Fade in
            yield return FadeVolume(ambianceSource, targetVolume, ambianceFadeDuration * 0.5f);
        }

        private IEnumerator FadeOutAmbiance()
        {
            yield return FadeVolume(ambianceSource, 0f, ambianceFadeDuration);
            ambianceSource.Stop();
        }

        private IEnumerator FadeVolume(AudioSource source, float targetVolume, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            source.volume = targetVolume;
        }

        private AudioClip GetAmbianceClip(string themeName)
        {
            return themeName switch
            {
                Constants.THEME_BACKROOMS => ambianceBackrooms,
                Constants.THEME_DARK_CORRIDOR => ambianceDarkCorridor,
                Constants.THEME_HOSPITAL => ambianceHospital,
                _ => ambianceBackrooms
            };
        }

        // ================================================================
        // Efekt Sesleri
        // ================================================================

        /// <summary>Asset yerleştirme sesi çalar.</summary>
        public void PlayPlaceAsset() => PlaySFX(sfxPlaceAsset);

        /// <summary>Asset silme sesi çalar.</summary>
        public void PlayDeleteAsset() => PlaySFX(sfxDeleteAsset);

        /// <summary>Köşe işaretleme sesi çalar.</summary>
        public void PlayCornerMark() => PlaySFX(sfxCornerMark);

        /// <summary>Tarama tamamlandı sesi çalar.</summary>
        public void PlayScanComplete() => PlaySFX(sfxScanComplete);

        /// <summary>Kayıt başlama sesi çalar.</summary>
        public void PlayRecordStart() => PlaySFX(sfxRecordStart);

        /// <summary>Kayıt durdurma sesi çalar.</summary>
        public void PlayRecordStop() => PlaySFX(sfxRecordStop);

        /// <summary>Geri sayım sesi çalar.</summary>
        public void PlayCountdownTick() => PlaySFX(sfxCountdownTick);

        /// <summary>Ekran görüntüsü sesi çalar.</summary>
        public void PlayScreenshot() => PlaySFX(sfxScreenshot);

        private void PlaySFX(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
            }
        }

        // ================================================================
        // UI Sesleri
        // ================================================================

        /// <summary>Buton tıklama sesi.</summary>
        public void PlayButtonClick() => PlayUI(uiButtonClick);

        /// <summary>Panel açılma sesi.</summary>
        public void PlayPanelOpen() => PlayUI(uiPanelOpen);

        /// <summary>Panel kapanma sesi.</summary>
        public void PlayPanelClose() => PlayUI(uiPanelClose);

        /// <summary>Hata sesi.</summary>
        public void PlayError() => PlayUI(uiError);

        private void PlayUI(AudioClip clip)
        {
            if (clip != null && uiSource != null)
            {
                uiSource.PlayOneShot(clip, uiVolume * masterVolume);
            }
        }

        // ================================================================
        // Genel
        // ================================================================

        /// <summary>Tüm sesleri susturur/açar.</summary>
        public void SetMute(bool mute)
        {
            if (ambianceSource) ambianceSource.mute = mute;
            if (sfxSource) sfxSource.mute = mute;
            if (uiSource) uiSource.mute = mute;
        }
    }
}
