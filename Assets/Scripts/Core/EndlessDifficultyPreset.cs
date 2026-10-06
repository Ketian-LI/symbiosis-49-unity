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
                    template = EndlessDifficulty.Gentle, startingCommunity = 75,
                    commuteTargetPercent = 75, wildlifeFloor = 8, wildlifeGraceDays = 3
                },
                EndlessDifficulty.Demanding => new EndlessDifficultySettings
                {
                    template = EndlessDifficulty.Demanding, startingCommunity = 45,
                    commuteTargetPercent = 100, wildlifeFloor = 12, wildlifeGraceDays = 2
                },
                _ => new EndlessDifficultySettings()
            };
        }

        public EndlessDifficultySettings Normalized() => new EndlessDifficultySettings
        {
            template = EndlessDifficultyRules.Normalize(template),
            startingCommunity = Math.Clamp(startingCommunity, 20, 100),
            commuteTargetPercent = Math.Clamp(commuteTargetPercent, 50, 100),
            wildlifeFloor = Math.Clamp(wildlifeFloor, 3, 18),
            wildlifeGraceDays = Math.Clamp(wildlifeGraceDays, 1, 5)
        };

        public bool IsCustom
        {
            get
            {
                var baseline = Preset(EndlessDifficultyRules.Normalize(template));
                return startingCommunity != baseline.startingCommunity ||
                       commuteTargetPercent != baseline.commuteTargetPercent ||
                       wildlifeFloor != baseline.wildlifeFloor ||
                       wildlifeGraceDays != baseline.wildlifeGraceDays;
            }
        }
    }

    // Community changes retain the existing +4 / 0 / -4 / -8 ladder.
    // The two lower bands follow the chosen commute target, so one visible
    // target never leaves unseen preset-specific thresholds behind.
    public readonly struct EndlessDifficultyRules
    {
        private EndlessDifficultyRules(EndlessDifficultySettings settings)
        {
            Settings = settings.Normalized();
        }

        public EndlessDifficultySettings Settings { get; }
        public EndlessDifficulty Difficulty => Settings.template;
        public int StartingCommunity => Settings.startingCommunity;
        public int WildlifeFloor => Settings.wildlifeFloor;
        public int WildlifeGraceDays => Settings.wildlifeGraceDays;
        public double GainShare => Settings.commuteTargetPercent / 100d;
        public double StableShare => Math.Max(0d, GainShare - 0.15d);
        public double MinorLossShare => Math.Max(0d, GainShare - 0.40d);

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

        public int CommunityChangeFor(int completedRoutes, int residents)
        {
            if (residents <= 0) return -8;
            var share = Math.Max(0, completedRoutes) / (double)residents;
            if (share >= GainShare) return 4;
            if (share >= StableShare) return 0;
            if (share >= MinorLossShare) return -4;
            return -8;
        }
    }
}
