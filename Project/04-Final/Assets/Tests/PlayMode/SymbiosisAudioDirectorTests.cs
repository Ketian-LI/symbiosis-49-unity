using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UrbanWildlifeRooms.Presentation;
using Object = UnityEngine.Object;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class SymbiosisAudioDirectorTests
    {
        private const string ResourcePath = "Audio/Symbiosis49/";

        [Test]
        public void OriginalMusicAndEveryEffectCanBeLoaded()
        {
            var music = Resources.Load<AudioClip>(ResourcePath + "MenuLoop");
            Assert.That(music, Is.Not.Null, "The original menu loop must be imported as an AudioClip.");
            Assert.That(music.samples, Is.GreaterThan(0));

            foreach (SymbiosisSound sound in Enum.GetValues(typeof(SymbiosisSound)))
            {
                var clip = Resources.Load<AudioClip>(ResourcePath + sound);
                Assert.That(clip, Is.Not.Null, $"Missing sound effect: {sound}.");
                Assert.That(clip.samples, Is.GreaterThan(0), $"Empty sound effect: {sound}.");
            }
        }

        [UnityTest]
        public IEnumerator EnsureIsIdempotentAndPlaybackSourcesAreConfigured()
        {
            var existedBefore = Object.FindObjectsByType<SymbiosisAudioDirector>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0;
            SymbiosisAudioDirector director = null;

            try
            {
                SymbiosisAudioDirector.Ensure();
                var directors = Object.FindObjectsByType<SymbiosisAudioDirector>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.That(directors, Has.Length.EqualTo(1));
                director = directors[0];

                SymbiosisAudioDirector.Ensure();
                Assert.That(Object.FindObjectsByType<SymbiosisAudioDirector>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None),
                    Has.Length.EqualTo(1), "Repeated Ensure must not create another audio director.");

                var sources = director.GetComponents<AudioSource>();
                Assert.That(sources, Has.Length.EqualTo(2));
                var music = sources.Single(source => source.loop);
                var effects = sources.Single(source => !source.loop);
                Assert.That(music.playOnAwake, Is.False);
                Assert.That(music.spatialBlend, Is.Zero.Within(0.001f));
                Assert.That(music.clip, Is.Not.Null);
                Assert.That(effects.playOnAwake, Is.False);
                Assert.That(effects.spatialBlend, Is.Zero.Within(0.001f));

                foreach (SymbiosisSound sound in Enum.GetValues(typeof(SymbiosisSound)))
                    Assert.DoesNotThrow(() => SymbiosisAudioDirector.Play(sound),
                        $"Playing {sound} must not throw.");
            }
            finally
            {
                // Other PlayMode tests or a live scene may own an existing singleton.
                if (!existedBefore && director != null)
                    Object.Destroy(director.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator MusicKeepsPlayingAndFadesWithUnscaledTimeDuringTransition()
        {
            var existedBefore = Object.FindFirstObjectByType<SymbiosisAudioDirector>() != null;
            var originalTimeScale = Time.timeScale;
            SymbiosisAudioDirector.Ensure();
            var director = Object.FindFirstObjectByType<SymbiosisAudioDirector>();
            var music = director.GetComponents<AudioSource>().Single(source => source.loop);

            try
            {
                Time.timeScale = 0f;
                SymbiosisAudioDirector.SetTransitionDucking(true);
                yield return new WaitForSecondsRealtime(0.15f);
                Assert.That(director.gameObject.scene.name,
                    Is.EqualTo("DontDestroyOnLoad"));
                Assert.That(music.isPlaying, Is.True);
                Assert.That(music.volume, Is.GreaterThan(0f),
                    "The case animation must still fade music while gameplay time is paused.");
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                SymbiosisAudioDirector.SetTransitionDucking(false);
                if (!existedBefore && director != null)
                    Object.Destroy(director.gameObject);
            }

            yield return null;
        }
    }
}
