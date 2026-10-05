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

    public enum DailyActionKind
    {
        None,
        Rearrange,
        Feed,
        Transform
    }

    [DefaultExecutionOrder(-100)]
    public sealed class GameRuntimeController : MonoBehaviour
    {
        public const int RequiredPhysicalModuleCount = 49;
        public const int DaySkipSpeed = 72;
        public const double DaySkipDurationSeconds = 5d;

        private const string LanguageKey = "symbiosis49.language";
        private const string VolumeKey = "symbiosis49.masterVolume";
        private const string MuteKey = "symbiosis49.muted";

        private readonly SimulationClockModel clock = new();
        private BoardCameraController boardCamera;
        private RoomLayoutEditorController layoutEditor;
        private PlayerFeedingController playerFeeding;
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
        private int daySkipSpeed;
        private bool daySkipActive;
        private double daySkipTargetSeconds;
        private float lastSimulationDeltaTime;

        public event Action StateChanged;
        public event Action SaveRequested;
        public event Action RestartRequested;
        public event Action CameraRecalibrationRequested;
        public event Action<RunResultsData> RunEnded;

        public SimulationClockModel Clock => clock;
        public GameMode Mode { get; private set; } = GameMode.Sandbox;
        public InterfaceLanguage Language { get; private set; } = InterfaceLanguage.English;
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
        public int SelectedSpeedMultiplier => Mode == GameMode.Research ? 1 : speedMultiplier;
        public int SpeedMultiplier => Mode == GameMode.Research ? 1 : daySkipActive ? daySkipSpeed : speedMultiplier;
        public bool IsDaySkipping => daySkipActive;
        // Today's successful action, not merely entering a tool or inspecting a plan.
        // Transform is reserved for the later room-type conversion feature.
        public DailyActionKind TodayAction =>
            layoutEditor != null && layoutEditor.LastConfirmedMovementDay == clock.DayNumber
                ? DailyActionKind.Rearrange
                : playerFeeding != null && playerFeeding.LastManualFeedingDay == clock.DayNumber
                    ? DailyActionKind.Feed
                    : DailyActionKind.None;
        public bool CanTakeDailyAction => TodayAction == DailyActionKind.None;
        public bool HasDailyAction => layoutEditor == null || !CanTakeDailyAction;
        // Keep the old API for existing integrations; it now means any daily action.
        public bool HasDailySpatialDecision => HasDailyAction;
        public double DaySkipTargetSeconds => daySkipActive ? daySkipTargetSeconds : 0d;
        public float MasterVolume => masterVolume;
        public bool Muted => muted;
        public bool CameraCalibrationOpen => cameraCalibrationOpen;
        public bool OnboardingOpen => onboardingOpen;
        public bool OnboardingInteractionAllowed => onboardingOpen && !pauseMenuOpen && !atDesktop &&
                                                    !layoutEditing && !resultsOpen && !cameraCalibrationOpen &&
                                                    !CameraRecognitionBlocksSimulation;
        // The tutorial freezes the clock but lets actors demonstrate ordinary
        // motion. During play, use the actual clock advance: Time.deltaTime is
        // capped by Unity's Maximum Allowed Timestep during a 72x day skip.
        public float ActorPresentationDeltaTime => activeRun && SelectedSpeedMultiplier > 0 &&
            OnboardingInteractionAllowed && Time.timeScale <= 0f
            ? Time.unscaledDeltaTime
            : activeRun && !IsPaused ? lastSimulationDeltaTime : 0f;
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
            // English is the first-launch default; an explicit player choice still wins.
            Language = (InterfaceLanguage)Mathf.Clamp(
                PlayerPrefs.GetInt(LanguageKey, (int)InterfaceLanguage.English), 0, 1);
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

            AdvanceSimulation(Time.unscaledDeltaTime);

            StateChanged?.Invoke();
        }

        public void AdvanceSimulation(double unscaledDeltaSeconds)
        {
            lastSimulationDeltaTime = 0f;
            if (!activeRun || IsPaused || unscaledDeltaSeconds <= 0d)
            {
                return;
            }

            var simulatedSeconds = unscaledDeltaSeconds * SpeedMultiplier;
            if (daySkipActive)
            {
                simulatedSeconds = Math.Min(simulatedSeconds,
                    Math.Max(0d, daySkipTargetSeconds - clock.TotalSeconds));
            }
            if (Mode == GameMode.Sandbox && !HasDailyAction)
            {
                // Hold just before dawn so ordinary 1x/2x/4x play cannot
                // bypass the same daily decision required by the skip button.
                var nextDawn = (Math.Floor(clock.TotalSeconds / SimulationClockModel.CycleSeconds) + 1d) *
                               SimulationClockModel.CycleSeconds;
                simulatedSeconds = Math.Min(simulatedSeconds,
                    Math.Max(0d, nextDawn - clock.TotalSeconds - 0.0001d));
            }
            var before = clock.TotalSeconds;
            clock.Advance(simulatedSeconds, 1f);
            lastSimulationDeltaTime = activeRun
                ? (float)Math.Max(0d, clock.TotalSeconds - before)
                : 0f;
            if (daySkipActive && clock.TotalSeconds >= daySkipTargetSeconds - 0.0001d)
            {
                daySkipActive = false;
                ApplyTimeScale();
            }
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
            daySkipActive = false;
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
            daySkipActive = false;
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
            daySkipActive = false;
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

        public bool TrySkipToNextDay()
        {
            if (!activeRun || Mode != GameMode.Sandbox || IsPaused || daySkipActive ||
                !HasDailyAction)
            {
                return false;
            }

            daySkipTargetSeconds =
                (Math.Floor(clock.TotalSeconds / SimulationClockModel.CycleSeconds) + 1d) *
                SimulationClockModel.CycleSeconds;
            // Advance through the remaining day in at most five unpaused real
            // seconds, keeping the normal clock-driven daily events active.
            daySkipSpeed = Mathf.Clamp(
                (int)Math.Ceiling((daySkipTargetSeconds - clock.TotalSeconds) /
                                  DaySkipDurationSeconds),
                1,
                DaySkipSpeed);
            daySkipActive = true;
            ApplyTimeScale();
            StateChanged?.Invoke();
            return true;
        }

        public void BindLayoutEditor(RoomLayoutEditorController editor)
        {
            layoutEditor = editor;
            StateChanged?.Invoke();
        }

        public void BindPlayerFeeding(PlayerFeedingController controller)
        {
            playerFeeding = controller;
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
            daySkipActive = false;
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
            daySkipActive = false;
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
            daySkipActive = false;
            lastSimulationDeltaTime = 0f;
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
