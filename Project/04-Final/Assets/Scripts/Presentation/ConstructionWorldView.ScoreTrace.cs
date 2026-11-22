using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionWorldView
    {
        public Transform ScoreTraceActor(ScoreTraceActor reference)
        {
            if (reference.Kind == ScoreTraceActorKind.TrafficVictim)
                return transientTrafficVictim != null && transientTrafficVictim.activeInHierarchy
                    ? transientTrafficVictim.transform : null;
            if (string.IsNullOrEmpty(reference.HomeId)) return null;
            if (reference.Kind == ScoreTraceActorKind.Resident)
                return residents.TryGetValue(reference.HomeId, out var person) && person != null && person.gameObject.activeInHierarchy
                    ? person.transform : null;
            IEnumerable<Transform> candidates = reference.Kind switch
            {
                ScoreTraceActorKind.Squirrel => squirrels.Where(v => v != null).Select(v => v.transform),
                ScoreTraceActorKind.Pigeon => pigeons.Where(v => v != null).Select(v => v.transform),
                ScoreTraceActorKind.Hedgehog => hedgehogs.Where(v => v != null).Select(v => v.transform),
                _ => Enumerable.Empty<Transform>()
            };
            // Missing/departed historical actors are not replaced or revived.
            return candidates.Where(actor => actor.gameObject.activeInHierarchy &&
                    animalHomeTiles.TryGetValue(actor, out var home) && home == reference.HomeId)
                .Skip(reference.Ordinal).FirstOrDefault();
        }
    }
}
