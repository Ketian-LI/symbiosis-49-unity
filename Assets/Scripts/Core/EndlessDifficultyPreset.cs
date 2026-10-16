using System;

namespace UrbanWildlifeRooms.Core
{
    public enum EndlessDifficulty
    {
        Gentle,
        Standard,
        Demanding
    }

    // Saved with each Sandbox run. Presets are editable templates, not hidden rules.
    [Serializable]
    public sealed class EndlessDifficultySettings
    {
        public EndlessDifficulty template = EndlessDifficulty.Standard;
        // Legacy save fields: read for compatibility, but no longer tune a
        // hidden community-health loss condition.
        public int startingCommunity = 60;
        public int commuteTargetPercent = 90;
        public int wildlifeFloor = 10;
        public int wildlifeGraceDays = 2;

        public static EndlessDifficultySettings Preset(EndlessDifficulty difficulty)
        {
            return difficulty switch
            {
                EndlessDifficulty.Gentle => new EndlessDifficultySettings
                {
                    template = EndlessDifficulty.Gentle,
                    wildlifeFloor = 8, wildlifeGraceDays = 3
                },
                EndlessDifficulty.Demanding => new EndlessDifficultySettings
                {
                    template = EndlessDifficulty.Demanding,
                    wildlifeFloor = 12, wildlifeGraceDays = 2
                },
                _ => new EndlessDifficultySettings()
            };
        }

        public EndlessDifficultySettings Normalized() => new EndlessDifficultySettings
        {
            template = EndlessDifficultyRules.Normalize(template),
            startingCommunity = startingCommunity,
            commuteTargetPercent = commuteTargetPercent,
            wildlifeFloor = Math.Clamp(wildlifeFloor, 3, 18),
            wildlifeGraceDays = Math.Clamp(wildlifeGraceDays, 1, 5)
        };

        public bool IsCustom
        {
            get
            {
                var baseline = Preset(EndlessDifficultyRules.Normalize(template));
                return wildlifeFloor != baseline.wildlifeFloor ||
                       wildlifeGraceDays != baseline.wildlifeGraceDays;
            }
        }
    }

    // Only wildlife collapse tuning remains in the Endless difficulty menu.
    public readonly struct EndlessDifficultyRules
    {
        private EndlessDifficultyRules(EndlessDifficultySettings settings)
        {
            Settings = settings.Normalized();
        }

        public EndlessDifficultySettings Settings { get; }
        public EndlessDifficulty Difficulty => Settings.template;
        public int WildlifeFloor => Settings.wildlifeFloor;
        public int WildlifeGraceDays => Settings.wildlifeGraceDays;

        public string Label(bool chinese)
        {
            if (Settings.IsCustom) return chinese ? "自定义" : "Custom";
            return Difficulty switch
            {
                EndlessDifficulty.Gentle => chinese ? "舒缓" : "Gentle",
                EndlessDifficulty.Demanding => chinese ? "挑战" : "Demanding",
                _ => chinese ? "标准" : "Standard"
            };
        }

        public static EndlessDifficulty Normalize(EndlessDifficulty difficulty) =>
            difficulty is EndlessDifficulty.Gentle or EndlessDifficulty.Standard or
                EndlessDifficulty.Demanding
                ? difficulty : EndlessDifficulty.Standard;

        public static EndlessDifficultyRules For(EndlessDifficulty difficulty) =>
            new(EndlessDifficultySettings.Preset(Normalize(difficulty)));

        public static EndlessDifficultyRules For(EndlessDifficultySettings settings) =>
            new(settings ?? EndlessDifficultySettings.Preset(EndlessDifficulty.Standard));

    }
}
