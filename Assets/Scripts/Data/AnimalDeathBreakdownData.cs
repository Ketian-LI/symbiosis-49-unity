using System;
using System.Collections.Generic;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Data
{
    [Serializable]
    public sealed class AnimalDeathBreakdownData
    {
        public WildlifeSpecies species;
        public int starvation;
        public int traffic;
        public int predation;
        public int other;
        public int unrecorded;

        public int Total => Math.Max(0, starvation) + Math.Max(0, traffic) +
                            Math.Max(0, predation) + Math.Max(0, other) + Math.Max(0, unrecorded);

        public int Count(AnimalDeathCause cause)
        {
            return cause switch
            {
                AnimalDeathCause.Starvation => Math.Max(0, starvation),
                AnimalDeathCause.Traffic => Math.Max(0, traffic),
                AnimalDeathCause.Predation => Math.Max(0, predation),
                AnimalDeathCause.Unrecorded => Math.Max(0, unrecorded),
                _ => Math.Max(0, other)
            };
        }

        public AnimalDeathBreakdownData Clone()
        {
            return new AnimalDeathBreakdownData
            {
                species = species,
                starvation = Math.Max(0, starvation),
                traffic = Math.Max(0, traffic),
                predation = Math.Max(0, predation),
                other = Math.Max(0, other),
                unrecorded = Math.Max(0, unrecorded)
            };
        }

        public string LocalizedLine(bool chinese)
        {
            var name = SpeciesName(species, chinese);
            if (Total == 0)
            {
                return chinese ? $"{name}：无死亡" : $"{name}: no deaths";
            }

            var details = new List<string>();
            AddDetail(details, AnimalDeathCause.Starvation, starvation, chinese);
            AddDetail(details, AnimalDeathCause.Traffic, traffic, chinese);
            AddDetail(details, AnimalDeathCause.Predation, predation, chinese);
            AddDetail(details, AnimalDeathCause.Other, other, chinese);
            AddDetail(details, AnimalDeathCause.Unrecorded, unrecorded, chinese);
            return chinese
                ? $"{name} {Total}：{string.Join("、", details)}"
                : $"{name} {Total}: {string.Join(", ", details)}";
        }

        public static string SpeciesName(WildlifeSpecies value, bool chinese)
        {
            return value switch
            {
                WildlifeSpecies.Pigeon => chinese ? "鸽子" : "Pigeon",
                WildlifeSpecies.Squirrel => chinese ? "松鼠" : "Squirrel",
                WildlifeSpecies.Hedgehog => chinese ? "刺猬" : "Hedgehog",
                WildlifeSpecies.Fox => chinese ? "狐狸" : "Fox",
                _ => chinese ? "动物" : "Animal"
            };
        }

        public static string CauseName(AnimalDeathCause cause, bool chinese)
        {
            return cause switch
            {
                AnimalDeathCause.Starvation => chinese ? "饥饿" : "starvation",
                AnimalDeathCause.Traffic => chinese ? "交通事故" : "traffic",
                AnimalDeathCause.Predation => chinese ? "狐狸捕食" : "fox predation",
                AnimalDeathCause.Unrecorded => chinese ? "原因未记录" : "cause not recorded",
                _ => chinese ? "其他" : "other"
            };
        }

        private static void AddDetail(List<string> details, AnimalDeathCause cause, int count, bool chinese)
        {
            if (count > 0)
            {
                details.Add($"{CauseName(cause, chinese)} {count}");
            }
        }
    }
}
