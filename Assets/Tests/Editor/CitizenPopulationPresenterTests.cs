using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.People;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class CitizenPopulationPresenterTests
    {
        [Test]
        public void VisiblePeopleFollowResidentArrivalDepartureAndHomeChange()
        {
            var root = new GameObject("Resident Visual Test");
            Material material = null;
            try
            {
                material = new Material(Shader.Find("Sprites/Default"));
                var homes = new Dictionary<string, Transform>();
                foreach (var room in RoomLayoutData.All)
                {
                    if (room.Type != RoomType.Residence)
                    {
                        continue;
                    }
                    var home = new GameObject(room.Id);
                    home.transform.SetParent(root.transform, false);
                    home.transform.position = new Vector3(room.Column * 3.1f, 0f, room.Row * 3.1f);
                    homes[room.Id] = home.transform;
                }

                var residents = new List<ResidentState>
                {
                    new() { id = "R001", residenceId = "residence-a" },
                    new() { id = "R002", residenceId = "residence-b" },
                    new() { id = "R003", residenceId = "residence-c" },
                    new() { id = "R004", residenceId = "residence-d" }
                };
                var presenterObject = new GameObject("Presenter");
                presenterObject.transform.SetParent(root.transform, false);
                var presenter = presenterObject.AddComponent<CitizenPopulationPresenter>();
                presenter.Initialize(() => residents, roomId => homes[roomId], material, HideFlags.None);
                Assert.That(presenter.VisibleResidentCount, Is.EqualTo(4));
                Assert.That(presenter.GetComponentsInChildren<CitizenDemoAgent>().Length, Is.EqualTo(4));

                residents.Add(new ResidentState { id = "R005", residenceId = "residence-e" });
                presenter.Synchronize();
                Assert.That(presenter.VisibleResidentCount, Is.EqualTo(5));

                residents[0].residenceId = "residence-f";
                presenter.Synchronize();
                Assert.That(presenter.PrimaryAgent.SpawnPosition.x,
                    Is.EqualTo(homes["residence-f"].position.x + 0.48f).Within(0.001f));

                residents.RemoveAt(4);
                presenter.Synchronize();
                Assert.That(presenter.VisibleResidentCount, Is.EqualTo(4));
                Assert.That(presenter.GetComponentsInChildren<CitizenDemoAgent>().Length, Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (material != null)
                {
                    Object.DestroyImmediate(material);
                }
            }
        }
    }
}
