using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    public enum SymbiosisSound
    {
        CaseOpen,
        CaseClose,
        UiSelect,
        UiCancel,
        BuildPreview,
        BuildConfirm,
        NextDay,
        RestartConfirm
    }

    // One soundtrack survives the additive Main -> Construction scene change.
    // The results overlay has its own audio and is deliberately not routed here.
    public sealed class SymbiosisAudioDirector : MonoBehaviour
    {
        private const string ResourcePath = "Audio/Symbiosis49/";
        private const float MusicLevel = 0.27f;
        private const float FadeSeconds = 1.1f;
        private const string MusicVolumeKey = "symbiosis49.musicVolume";
        private const string EffectsVolumeKey = "symbiosis49.effectsVolume";
        private static SymbiosisAudioDirector instance;

        private readonly Dictionary<SymbiosisSound, AudioClip> sounds = new();
        private AudioSource music;
        private AudioSource effects;
        private GameRuntimeController runtime;
        private bool transitionDucking;

        public static float MusicVolume => Mathf.Clamp01(
            PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
        public static float EffectsVolume => Mathf.Clamp01(
            PlayerPrefs.GetFloat(EffectsVolumeKey, 1f));

        public static void SetMusicVolume(float value)
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }

        public static void SetEffectsVolume(float value)
        {
            PlayerPrefs.SetFloat(EffectsVolumeKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            if (instance != null) instance.effects.volume = EffectsVolume;
        }

        public static void Ensure()
        {
            if (instance != null) return;
            var root = new GameObject("SYMBIOSIS:49 Audio");
            root.AddComponent<SymbiosisAudioDirector>();
        }

        public static void Play(SymbiosisSound sound)
        {
            Ensure();
            if (instance == null) return;
            if (!instance.sounds.TryGetValue(sound, out var clip) || clip == null)
            {
                clip = Resources.Load<AudioClip>(ResourcePath + sound);
                instance.sounds[sound] = clip;
            }
            if (clip == null) return;
            instance.effects.PlayOneShot(clip, EffectLevel(sound));
        }

        public static void SetTransitionDucking(bool enabled)
        {
            Ensure();
            if (instance != null) instance.transitionDucking = enabled;
        }

        private static float EffectLevel(SymbiosisSound sound) => sound switch
        {
            SymbiosisSound.CaseOpen => 0.62f,
            SymbiosisSound.CaseClose => 0.58f,
            SymbiosisSound.UiSelect => 0.32f,
            SymbiosisSound.UiCancel => 0.36f,
            SymbiosisSound.BuildPreview => 0.34f,
            SymbiosisSound.BuildConfirm => 0.60f,
            SymbiosisSound.NextDay => 0.55f,
            SymbiosisSound.RestartConfirm => 0.52f,
            _ => 0.4f
        };

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.spatialBlend = 0f;
            music.volume = 0f;
            music.clip = Resources.Load<AudioClip>(ResourcePath + "MenuLoop");
            if (music.clip != null) music.Play();

            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.spatialBlend = 0f;
            effects.volume = EffectsVolume;
            foreach (SymbiosisSound sound in System.Enum.GetValues(
                         typeof(SymbiosisSound)))
                sounds[sound] = Resources.Load<AudioClip>(ResourcePath + sound);
        }

        private void Update()
        {
            if (music.clip == null)
            {
                // Artists may import the WAVs during an editor Play session.
                music.clip = Resources.Load<AudioClip>(ResourcePath + "MenuLoop");
                if (music.clip == null) return;
                music.Play();
            }
            var scene = SceneManager.GetActiveScene().name;
            // Keep the same playhead through a brief single-scene load too.
            var enabled = transitionDucking || scene == "ConstructionPrototype";
            if (!enabled && scene == "Main")
            {
                if (runtime == null)
                    runtime = FindFirstObjectByType<GameRuntimeController>();
                enabled = runtime != null && runtime.AtDesktop;
            }
            var target = enabled
                ? MusicLevel * MusicVolume *
                    (transitionDucking ? 0.62f : 1f) : 0f;
            if (enabled && !music.isPlaying) music.UnPause();
            music.volume = Mathf.MoveTowards(music.volume, target,
                MusicLevel * Time.unscaledDeltaTime / FadeSeconds);
            if (!enabled && music.volume <= 0f && music.isPlaying)
                music.Pause();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
