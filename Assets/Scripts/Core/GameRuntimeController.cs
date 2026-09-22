using System;
using UnityEngine;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Core
{
    public enum GameMode
    {
        Sandbox,
        Research
    }

    public enum InterfaceLanguage
    {
        Chinese,
        English
    }

    public sealed class GameRuntimeController : MonoBehaviour
    {
        public const int RequiredPhysicalModuleCount = 35;

        private const string LanguageKey = "symbiosis49.language";
        private const string VolumeKey = "symbiosis49.masterVolume";
        private const string MuteKey = "symbiosis49.muted";

        private readonly SimulationClockModel clock = new();
        private BoardCameraController boardCamera;
        private bool activeRun;
        private bool pauseMenuOpen;
        private bool settingsOpen;
        private bool atDesktop;
        private bool layoutEditing;
        private bool resultsOpen;
        private bool cameraCalibrationOpen;
        private bool onboardingOpen;
        private int recognisedPhysicalModuleCount;
        private CameraRecognitionFeedbackState cameraRecognitionState;
        private float cameraRecognitionProgress;
        private bool muted;
        private float masterVolume = 0.8f;
        private int speedMultiplier = 1;

        public event Action StateChanged;
        public event Action SaveRequested;
        public event Action RestartRequested;
        public event Action CameraRecalibrationRequested;
        public event Action<RunResultsData> RunEnded;

        public SimulationClockModel Clock => clock;
        public GameMode Mode { get; private set; } = GameMode.Sandbox;
        public InterfaceLanguage Language { get; private set; } = InterfaceLanguage.Chinese;
        public bool PauseMenuOpen => pauseMenuOpen;
        public bool SettingsOpen => settingsOpen;
        public bool AtDesktop => atDesktop;
        public bool LayoutEditing => layoutEditing;
        public bool ResultsOpen => resultsOpen;
        public RunResultsData CurrentResults { get; private set; }
        public bool HasEndedRun => CurrentResults != null;
        public bool HasActiveRun => activeRun;
        public bool HasResumableRun => activeRun && CurrentResults == null;
        public bool IsPaused => pauseMenuOpen || atDesktop || layoutEditing || resultsOpen ||
                                cameraCalibrationOpen || onboardingOpen || CameraRecognitionBlocksSimulation ||
                                SpeedMultiplier == 0;
        public int SpeedMultiplier => Mode == GameMode.Research ? 1 : speedMultiplier;
        public float MasterVolume => masterVolume;
        public bool Muted => muted;
        public bool CameraCalibrationOpen => cameraCalibrationOpen;
        public bool OnboardingOpen => onboardingOpen;
        public bool OnboardingInteractionAllowed => onboardingOpen && !pauseMenuOpen && !atDesktop &&
                                                    !layoutEditing && !resultsOpen && !cameraCalibrationOpen &&
                                                    !CameraRecognitionBlocksSimulation;
        public int RecognisedPhysicalModuleCount => recognisedPhysicalModuleCount;
        public bool CameraCalibrationReady =>
            recognisedPhysicalModuleCount >= RequiredPhysicalModuleCount;
        public CameraRecognitionFeedbackState CameraRecognitionState => cameraRecognitionState;
        public float CameraRecognitionProgress => cameraRecognitionProgress;
        public bool CameraRecognitionBlocksSimulation =>
            cameraRecognitionState == CameraRecognitionFeedbackState.Scanning ||
            cameraRecognitionState == CameraRecognitionFeedbackState.InvalidPlacement ||
            cameraRecognitionState == CameraRecognitionFeedbackState.Stabilising;
        public int BestSurvivalDays { get; private set; }

        public void Initialize(BoardCameraController cameraController)
        {
            boardCamera = cameraController;
            Language = (InterfaceLanguage)Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, 0), 0, 1);
            masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.8f));
            muted = PlayerPrefs.GetInt(MuteKey, 0) != 0;
            atDesktop = true;
            if (Application.isPlaying)
            {
                boardCamera?.SetMenuViewImmediate();
            }
            ApplyAudioSettings();
            ApplyTimeScale();
            StateChanged?.Invoke();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (resultsOpen || cameraCalibrationOpen)
                {
                    return;
                }
                if (layoutEditing)
                {
                    return;
                }

                if (atDesktop)
                {
                    return;
                }

                if (!pauseMenuOpen && boardCamera != null && boardCamera.ReturnToOverviewIfNeeded())
                {
                    return;
                }

                TogglePauseMenu();
            }

            if (!pauseMenuOpen && !atDesktop && !layoutEditing && !resultsOpen &&
                !cameraCalibrationOpen && !onboardingOpen && !CameraRecognitionBlocksSimulation)
            {
                clock.Advance(Time.unscaledDeltaTime, SpeedMultiplier);
            }

            StateChanged?.Invoke();
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                Time.timeScale = 1f;
            }
        }

        public void SetMode(GameMode mode)
        {
            Mode = NormalizeModeForBuild(mode);
            if (Mode == GameMode.Research)
            {
                speedMultiplier = 1;
            }

            ApplyTimeScale();
            StateChanged?.Invoke();
        }

        public void RestoreSession(double elapsedSeconds, int multiplier, GameMode mode)
        {
            clock.Restore(elapsedSeconds);
            Mode = NormalizeModeForBuild(mode);
            activeRun = true;
            speedMultiplier = Mode == GameMode.Research
                ? 1
                : multiplier switch
                {
                    <= 0 => 0,
                    2 => 2,
                    >= 4 => 4,
                    _ => 1
                };
            ApplyTimeScale();
            StateChanged?.Invoke();
        }

        public void SetSpeed(int multiplier)
        {
            if (Mode == GameMode.Research)
            {
                multiplier = 1;
            }

            speedMultiplier = multiplier switch
            {
                <= 0 => 0,
                2 => 2,
                >= 4 => 4,
                _ => 1
            };
            ApplyTimeScale();
            StateChanged?.Invoke();
        }

        public void TogglePauseMenu()
        {
            if (resultsOpen)
            {
                return;
            }
            pauseMenuOpen = !pauseMenuOpen;
            if (!pauseMenuOpen)
            {
                settingsOpen = false;
            }

            ApplyTimeScale();
            StateChanged?.Invoke();
        }

        public void ContinueGame()
        {
            pauseMenuOpen = false;
            settingsOpen = false;
            ApplyTimeScale();
            StateChanged?.Invoke();
        }

        public void ReturnToDesktop()
        {
            atDesktop = true;
            onboardingOpen = false;
            pauseMenuOpen = false;
            settingsOpen = false;
            cameraRecognitionState = CameraRecognitionFeedbackState.Hidden;
            cameraRecognitionProgress = 0f;
            ApplyTimeScale();
            StateChanged?.Invoke();
            SaveRequested?.Invoke();
        }

        public void EndRun(RunResultsData results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            CurrentResults = results;
            activeRun = false;
            resultsOpen = true;
            pauseMenuOpen = false;
            settingsOpen = false;
            layoutEditing = false;
            onboardingOpen = false;
            cameraRecognitionState = CameraRecognitionFeedbackState.Hidden;
            cameraRecognitionProgress = 0f;
            ApplyTimeScale();
            RunEnded?.Invoke(CurrentResults);
            StateChanged?.Invoke();
            SaveRequested?.Invoke();
        }

        public void RestartRun()
        {
            clock.Reset();
            CurrentResults = null;
            activeRun = true;
            resultsOpen = false;
            atDesktop = false;
            pauseMenuOpen = false;
            settingsOpen = false;
            layoutEditing = false;
            cameraCalibrationOpen = false;
            onboardingOpen = false;
            recognisedPhysicalModuleCount = 0;
            cameraRecognitionState = CameraRecognitionFeedbackState.Hidden;
            cameraRecognitionProgress = 0f;
            speedMultiplier = 1;
            boardCamera?.ReturnToOverviewIfNeeded();
            ApplyTimeScale();
            RestartRequested?.Invoke();
            StateChanged?.Invoke();
            SaveRequested?.Invoke();
        }

        public void ReturnToMainMenuFromResults()
        {
            resultsOpen = false;
            atDesktop = true;
            pauseMenuOpen = false;
            settingsOpen = false;
            cameraRecognitionState = CameraRecognitionFeedbackState.Hidden;
            cameraRecognitionProgress = 0f;
            ApplyTimeScale();
            StateChanged?.Invoke();
            SaveRequested?.Invoke();
        }

        public void ResumeFromDesktop()
        {
            if (HasEndedRun || !activeRun)
            {
                StartNewRun(Mode);
                return;
            }

            atDesktop = false;
            pauseMenuOpen = false;
            settingsOpen = false;
            ApplyTimeScale();
            StateChanged?.Invoke();
        }

        public void StartNewRun(GameMode mode)
        {
            Mode = NormalizeModeForBuild(mode);
            RestartRun();
            if (BuildVariantSettings.UsesCameraRecognition)
            {
                RequestCameraRecalibration();
            }
        }

        private static GameMode NormalizeModeForBuild(GameMode requestedMode)
        {
            return BuildVariantSettings.SupportsResearch
                ? requestedMode
                : GameMode.Sandbox;
        }

        public void SetLayoutEditing(bool value)
        {
            layoutEditing = value;
            ApplyTimeScale();
            StateChanged?.Invoke();
        }

        public void SetOnboardingOpen(bool value)
        {
            onboardingOpen = value;
            ApplyTimeScale();
            StateChanged?.Invoke();
        }

        public void OpenSettings()
        {
            pauseMenuOpen = true;
            settingsOpen = true;
            ApplyTimeScale();
            StateChanged?.Invoke();
        }

        public void CloseSettings()
        {
            settingsOpen = false;
            if (atDesktop)
            {
                pauseMenuOpen = false;
            }
            StateChanged?.Invoke();
        }

        public void ExitApplication()
        {
            SaveRequested?.Invoke();
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
#else
            Application.Quit();
#endif
        }

        public void ToggleLanguage()
        {
            Language = Language == InterfaceLanguage.Chinese
                ? InterfaceLanguage.English
                : InterfaceLanguage.Chinese;
            PlayerPrefs.SetInt(LanguageKey, (int)Language);
            PlayerPrefs.Save();
            StateChanged?.Invoke();
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey, masterVolume);
            PlayerPrefs.Save();
            ApplyAudioSettings();
            StateChanged?.Invoke();
        }

        public void ToggleMute()
        {
            muted = !muted;
            PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyAudioSettings();
            StateChanged?.Invoke();
        }

        public void RequestCameraRecalibration()
        {
            if (!BuildVariantSettings.UsesCameraRecognition)
            {
                return;
            }

            cameraCalibrationOpen = true;
            recognisedPhysicalModuleCount = 0;
            cameraRecognitionState = CameraRecognitionFeedbackState.Hidden;
            cameraRecognitionProgress = 0f;
            pauseMenuOpen = false;
            settingsOpen = false;
            ApplyTimeScale();
            CameraRecalibrationRequested?.Invoke();
            StateChanged?.Invoke();
        }

        public void ReportRecognisedPhysicalModules(int count)
        {
            if (!BuildVariantSettings.UsesCameraRecognition || !cameraCalibrationOpen)
            {
                return;
            }

            recognisedPhysicalModuleCount = Mathf.Clamp(
                count,
                0,
                RequiredPhysicalModuleCount);
            StateChanged?.Invoke();
        }

        public void CompleteCameraCalibration()
        {
            if (!cameraCalibrationOpen || !CameraCalibrationReady)
            {
                return;
            }

            cameraCalibrationOpen = false;
            recognisedPhysicalModuleCount = 0;
            pauseMenuOpen = false;
            settingsOpen = false;
            ApplyTimeScale();
            StateChanged?.Invoke();
        }

        public void BeginCameraRecognition()
        {
            if (!BuildVariantSettings.UsesCameraRecognition || !activeRun ||
                cameraCalibrationOpen)
            {
                return;
            }

            cameraRecognitionState = CameraRecognitionFeedbackState.Scanning;
            cameraRecognitionProgress = 0f;
            ApplyTimeScale();
            StateChanged?.Invoke();
        }

        public void ReportCameraRecognitionStability(float progress)
        {
            if (!BuildVariantSettings.UsesCameraRecognition || !activeRun ||
                cameraCalibrationOpen ||
                cameraRecognitionState == CameraRecognitionFeedbackState.Hidden)
            {
                return;
            }

            cameraRecognitionState = CameraRecognitionFeedbackState.Stabilising;
            cameraRecognitionProgress = Mathf.Clamp01(progress);
            ApplyTimeScale();
            StateChanged?.Invoke();
        }

        public void ReportCameraRecognitionInvalidPlacement()
        {
            if (!BuildVariantSettings.UsesCameraRecognition || !activeRun ||
                cameraCalibrationOpen ||
                cameraRecognitionState == CameraRecognitionFeedbackState.Hidden)
            {
                return;
            }

            cameraRecognitionState = CameraRecognitionFeedbackState.InvalidPlacement;
            cameraRecognitionProgress = 0f;
            ApplyTimeScale();
            StateChanged?.Invoke();
        }

        public void ConfirmCameraRecognisedLayout()
        {
            if (!BuildVariantSettings.UsesCameraRecognition ||
                cameraRecognitionState != CameraRecognitionFeedbackState.Stabilising ||
                cameraRecognitionProgress < 1f)
            {
                return;
            }

            cameraRecognitionState = CameraRecognitionFeedbackState.Confirmed;
            cameraRecognitionProgress = 1f;
            ApplyTimeScale();
            StateChanged?.Invoke();
        }

        public void ClearCameraRecognitionFeedback()
        {
            if (!BuildVariantSettings.UsesCameraRecognition)
            {
                return;
            }

            cameraRecognitionState = CameraRecognitionFeedbackState.Hidden;
            cameraRecognitionProgress = 0f;
            ApplyTimeScale();
            StateChanged?.Invoke();
        }

        public void SetBestSurvivalDays(int days)
        {
            BestSurvivalDays = Mathf.Max(0, days);
            StateChanged?.Invoke();
        }

        private void ApplyAudioSettings()
        {
            AudioListener.volume = muted ? 0f : masterVolume;
        }

        private void ApplyTimeScale()
        {
            Time.timeScale = pauseMenuOpen || atDesktop || layoutEditing || resultsOpen ||
                             cameraCalibrationOpen || onboardingOpen || CameraRecognitionBlocksSimulation
                ? 0f
                : Mathf.Max(0f, SpeedMultiplier);
        }
    }
}
