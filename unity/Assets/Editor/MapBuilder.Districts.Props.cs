using UnityEngine;
using ColonQuest.World;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// The standing things of the north and east districts: the real estate agency, the car agency, the new
    /// neighborhood, the farm with its coffee and cattle, the trees at the edges, and the people and places to talk to.
    /// </summary>
    public static partial class MapBuilder
    {
        private static readonly string[] DistrictGreens = { "#3FA66A", "#4CB36F", "#3B9660", "#2F8A58" };

        /// <summary>True over the river and its banks, where nothing is planted.</summary>
        private static bool OverRiver(float x)
        {
            return x > MapLayout.RiverXMin - 1.6f && x < MapLayout.RiverXMax + 1.6f;
        }

        /// <summary>True over the waterfall pool at the foot of the north cliff.</summary>
        private static bool OverPool(float x, float y)
        {
            return x > 15.5f && x < 25.5f && y > PoolY - 3f;
        }

        // Where the player talks to the agencies and the farm: one step in front of each door.
        private static readonly Vector2 RealEstateSpot = new Vector2(-12f, 31f);
        private static readonly Vector2 CarDealerSpot = new Vector2(9f, 31f);
        private static readonly Vector2 FarmSpot = new Vector2(54.5f, 4.9f);

        // The new public buildings: where their doors are. The police station is at the east end of the avenue and
        // the hospital between the car agency and the river, on the same row. Both are ten units wide.
        private const float PoliceX = 56f;
        private const float HospitalX = 22f;
        private const float HospitalY = NorthSidewalkY;

        // ---------------------------------------------------------------- north district

        private static void BuildNorthDistrictProps(Transform parent, Transform shadows, System.Random random)
        {
            float row = NorthSidewalkY;

            // The new neighborhood, Los Almendros. The first two houses are listed by the agency.
            House(parent, shadows, "Casa_Almendros_1", -42f, -36f, row, row + 5.5f, "#E07A5F", "#F7E8D0");
            House(parent, shadows, "Casa_Almendros_2", -34f, -28f, row, row + 5.5f, "#4D9DE0", "#FBE3C4");
            House(parent, shadows, "Casa_Almendros_3", -26f, -20f, row, row + 5.5f, "#8E6BB8", "#F4EBD0");
            YardSign(parent, shadows, -36.6f, 31.7f, "SE VENDE", "#E8505B");
            YardSign(parent, shadows, -28.6f, 31.7f, "SE ALQUILA", "#1D4F8C");

            Building(parent, shadows, new BuildingSpec
            {
                Name = "Inmobiliaria", Sign = "INMOBILIARIA", Roof = "#2A7F8C", Wall = "#F2E3C8",
                XMin = -17f, XMax = -7f, YBottom = row, YTop = row + 7.2f, DoorX = RealEstateSpot.x, FacadeHeight = 3.3f, Entrance = Entrance.Door,
                AwningA = "#2A7F8C", AwningB = "#F8F5E8"
            });

            var agency = Building(parent, shadows, new BuildingSpec
            {
                Name = "AgenciaDeCarros", Sign = "AUTOS DON CHEPE", Roof = "#C0504D", Wall = "#E8EEF9",
                XMin = 2f, XMax = 16f, YBottom = row, YTop = row + 7f, DoorX = CarDealerSpot.x, FacadeHeight = 3.4f, Entrance = Entrance.Garage
            });
            Banner(agency, "TODO A CUOTAS", 0f, 3.4f + 1.7f, "#E8505B");

            // The police station at the east end of the avenue and the hospital, two floors high, by the east streets.
            Building(parent, shadows, new BuildingSpec
            {
                Name = "Comisaria", Sign = "POLICIA", Roof = "#2F4B8C", Wall = "#DCE3F2",
                XMin = PoliceX - 5f, XMax = PoliceX + 5f, YBottom = row, YTop = row + 6.6f, DoorX = PoliceX, FacadeHeight = 3.2f, Entrance = Entrance.Door,
                AwningA = "#2F4B8C", AwningB = "#F8F5E8"
            });
            var hospital = Building(parent, shadows, new BuildingSpec
            {
                Name = "Hospital", Sign = "HOSPITAL", Roof = "#9FB4CC", Wall = "#F5F8FB",
                XMin = HospitalX - 5f, XMax = HospitalX + 5f, YBottom = HospitalY, YTop = HospitalY + 8.4f, DoorX = HospitalX, FacadeHeight = 5.6f, Entrance = Entrance.Door
            });
            Banner(hospital, "URGENCIAS", 0f, 5.6f + 1.4f, "#E8505B");

            // Two cars on show in front of the agency, on either side of the place where the player talks.
            Car(parent, shadows, 4f, 32f, "#E8505B");
            Car(parent, shadows, 13.5f, 32f, "#4DA3FF");

            // East of the river.
            House(parent, shadows, "Casa_Almendros_4", 35f, 41f, row, row + 5.5f, "#E1BC29", "#FBE3C4");
            House(parent, shadows, "Casa_Almendros_5", 43f, 49f, row, row + 5.5f, "#3FA66A", "#F7E8D0");

            foreach (float x in new[] { -40f, -30f, -20f, -1f, 19f, 24f, 34f, 44f, 54f, 62f })
            {
                Lamp(parent, x, 30.6f);
            }

            Bench(parent, shadows, -4.5f, 31.4f);
            Bench(parent, shadows, 21f, 31.4f);

            Signpost(parent, new Vector2(24.4f, 7f), "NORTE");

            // Trees between the houses and behind the whole row.
            for (float x = -43f; x < 63f; x += 5.2f)
            {
                float jx = x + ((float)random.NextDouble() - 0.5f) * 1.8f;
                float jy = row + 8.2f + (float)random.NextDouble() * 1.4f;
                if (OverRiver(jx))
                {
                    continue;
                }

                Tree(parent, shadows, jx, jy, 1.0f + (float)random.NextDouble() * 0.3f, DistrictGreens[random.Next(DistrictGreens.Length)], random.Next(6) == 0);
            }

            foreach (float x in new[] { -18.8f, -6.2f, 1.2f, 17.2f, -2f })
            {
                Bush(parent, shadows, x, row + 0.3f, 1f);
            }

            // The coffee field in the north-east, between the avenue and the old farm.
            for (float y = 18.5f; y < 24f; y += 2.3f)
            {
                for (float x = 45.9f; x < 62f; x += 1.8f)
                {
                    CoffeeBush(parent, shadows, x, y);
                }
            }

            // Woods on the south side of the avenue, east of the old forest.
            for (float y = 15.5f; y < 22.5f; y += 3f)
            {
                for (float x = 44f; x < 64f; x += 5.5f)
                {
                    if (y > 18f && x < 63f && random.Next(3) > 0)
                    {
                        continue;
                    }

                    Tree(parent, shadows, x + (float)random.NextDouble() * 1.5f, y + (float)random.NextDouble(), 1.1f, DistrictGreens[random.Next(DistrictGreens.Length)], false);
                }
            }

            foreach (var spot in new[] { new Vector2(59.5f, 16.5f), new Vector2(52f, 36.5f), new Vector2(58f, 35.5f), new Vector2(-14f, 21f), new Vector2(33.2f, 40f) })
            {
                Rock(parent, shadows, spot.x, spot.y, 1f);
            }
        }

        // ---------------------------------------------------------------- east district

        private static void BuildEastDistrictProps(Transform parent, Transform shadows, System.Random random)
        {
            Building(parent, shadows, new BuildingSpec
            {
                Name = "FincaSanIsidro", Sign = "FINCA SAN ISIDRO", Roof = "#2F6B4A", Wall = "#EFD9A8",
                XMin = 48.5f, XMax = 60.5f, YBottom = 6f, YTop = 12.8f, DoorX = FarmSpot.x, FacadeHeight = 3.0f, Entrance = Entrance.Door,
                AwningA = "#2F6B4A", AwningB = "#F8F5E8"
            });
            Silo(parent, shadows, 62.3f, 9f);

            // The pasture between the two streets, fenced like the old one, with its cattle.
            var min = PastureArea.min;
            var max = PastureArea.max;
            FenceRun(parent, new Vector2(min.x, min.y), new Vector2(max.x, min.y));
            FenceRun(parent, new Vector2(min.x, max.y), new Vector2(max.x, max.y));
            FenceRun(parent, new Vector2(min.x, min.y), new Vector2(min.x, max.y));
            FenceRun(parent, new Vector2(max.x, min.y), new Vector2(max.x, max.y));
            var cattle = Group(parent, "Cattle");
            Cow(cattle, new Vector2(49f, -5f), new Vector2(53f, -4.4f), 0.5f);
            Cow(cattle, new Vector2(60f, -6.2f), new Vector2(55f, -6f), 0.4f);
            Cow(cattle, new Vector2(52f, -3.9f), new Vector2(58f, -3.9f), 0.45f);
            Cow(cattle, new Vector2(57f, -5.2f), new Vector2(61f, -4.2f), 0.35f);

            foreach (float x in new[] { 36f, 42f, 50f, 58f })
            {
                Lamp(parent, x, 4.5f);
            }

            foreach (float x in new[] { 38f, 46f, 54f, 62f })
            {
                Lamp(parent, x, -9.3f);
            }

            Bench(parent, shadows, 46f, -0.9f);

            // Trees around the farm, behind it, and in the corner of the streets.
            foreach (var spot in new[] { new Vector2(46.5f, -1.4f), new Vector2(63.4f, -1.4f), new Vector2(63.4f, -9.2f), new Vector2(46.6f, -9.2f), new Vector2(46.5f, 13.6f), new Vector2(63f, 13.8f), new Vector2(54f, 14.8f) })
            {
                Tree(parent, shadows, spot.x, spot.y, 1.15f, DistrictGreens[random.Next(DistrictGreens.Length)], random.Next(4) == 0);
            }

            // Palms and umbrellas on the beach.
            foreach (var spot in new[] { new Vector2(47f, -20.6f), new Vector2(53f, -22.2f), new Vector2(59f, -21f), new Vector2(64f, -23.4f) })
            {
                Palm(parent, shadows, spot.x, spot.y, 1.1f);
            }

            Umbrella(parent, shadows, 50f, -22.6f, "#E8505B");
            Umbrella(parent, shadows, 57f, -23f, "#4DA3FF");
            foreach (float x in new[] { 44f, 52f, 60f, 64f })
            {
                Lamp(parent, x, -17.2f);
            }
        }

        /// <summary>Trees along the foot of the east cliff and past the north wall, where the player cannot go but can see.</summary>
        private static void BuildDistrictEdges(Transform parent, Transform shadows, System.Random random)
        {
            for (float x = -44f; x < MapLayout.EastCliffX; x += 3.4f)
            {
                float jx = x + ((float)random.NextDouble() - 0.5f) * 1.4f;
                float jy = MapLayout.PlayNorthY + 1.6f + (float)random.NextDouble() * 3.4f;
                if (OverRiver(jx) || OverPool(jx, jy))
                {
                    continue;
                }

                Tree(parent, shadows, jx, jy, 1.1f + (float)random.NextDouble() * 0.35f, DistrictGreens[random.Next(DistrictGreens.Length)], random.Next(7) == 0);
            }

            for (float y = -25f; y < MapLayout.PlayNorthY; y += 3.3f)
            {
                float jy = y + ((float)random.NextDouble() - 0.5f) * 1.2f;
                bool onStreet = (jy >= -16f && jy <= -8f) || (jy >= -2f && jy <= 6f) || (jy >= 20f && jy <= 29f);
                float baseX = onStreet ? 67.2f : MapLayout.PlayEastX + 1.4f;
                float jx = baseX + (float)random.NextDouble() * 1.2f;
                Tree(parent, shadows, jx, jy, 1.05f + (float)random.NextDouble() * 0.3f, DistrictGreens[random.Next(DistrictGreens.Length)], false);
            }

            foreach (var spot in new[] { new Vector2(65.8f, 20f), new Vector2(66.5f, 9.5f), new Vector2(65.5f, -6f), new Vector2(-8f, 44.5f), new Vector2(34f, 45.5f) })
            {
                Rock(parent, shadows, spot.x, spot.y, 1.1f);
            }
        }

        /// <summary>
        /// Takes away the rocks, trees and bushes the scenery put where the police station and the hospital now stand.
        /// It runs last, once every prop of the town is down.
        /// </summary>
        private static void ClearNewBuildingFootprints(Transform map)
        {
            var areas = new[]
            {
                new Rect(PoliceX - 5.5f, NorthSidewalkY - 0.6f, 11f, 8.6f),
                new Rect(HospitalX - 5.5f, NorthSidewalkY - 0.6f, 11f, 10.4f)
            };

            foreach (var item in map.GetComponentsInChildren<Transform>(true))
            {
                if (item == null || !(item.name == "Rock" || item.name == "Tree" || item.name == "Bush"))
                {
                    continue;
                }

                foreach (var area in areas)
                {
                    if (area.Contains(item.position))
                    {
                        Object.DestroyImmediate(item.gameObject);
                        break;
                    }
                }
            }
        }

        // ---------------------------------------------------------------- places and people

        private static void BuildMarketStations(Transform parent)
        {
            // The shore of the beach: a swim once a month, if the swimsuit was bought.
            BuildStation(parent, "Station_Beach", StationType.Beach, "Darse un chapuzón en el mar", new Vector2(-12f, -25.6f));
            // The real estate agency, the farm, the car agency, the police station and the hospital are entered by their doors.
            Doorway(parent, new Vector2(RealEstateSpot.x, 33.2f), SceneNames.Inmobiliaria, true);
            BuildSpawnPoint(parent, SpawnIds.TownFromInmobiliaria, new Vector2(RealEstateSpot.x, 31.6f));
            Doorway(parent, new Vector2(FarmSpot.x, 6.2f), SceneNames.Finca, true);
            BuildSpawnPoint(parent, SpawnIds.TownFromFinca, FarmSpot);
            Doorway(parent, new Vector2(CarDealerSpot.x, 33.2f), SceneNames.Agencia, true);
            Doorway(parent, new Vector2(PoliceX, 33.2f), SceneNames.Carcel);
            Doorway(parent, new Vector2(HospitalX, HospitalY + 0.2f), SceneNames.Hospital);

            // Every house that is not on the market can be entered: they all lead to the same neighbor's interior, and the
            // door the player came in by is the one they come out of. The four houses with a sign are only for sale or rent.
            BuildHouseDoor(parent, "Casa_Vecina_A", -40.25f, 6f);
            BuildHouseDoor(parent, "Casa_Almendros_3", -23f, 33f);
            BuildHouseDoor(parent, "Casa_Almendros_4", 38f, 33f);
            BuildHouseDoor(parent, "Casa_Almendros_5", 46f, 33f);
            BuildSpawnPoint(parent, SpawnIds.TownFromAgencia, new Vector2(CarDealerSpot.x, 31.6f));
            BuildSpawnPoint(parent, SpawnIds.TownFromComisaria, new Vector2(PoliceX, 31.6f));
            BuildSpawnPoint(parent, SpawnIds.TownFromHospital, new Vector2(HospitalX, 31.6f));
            BuildSpawnPoint(parent, SpawnIds.TownFromVecino, new Vector2(-32.5f, -9.2f));

            // The yellow house on the south row rents out rooms: the player's own door.
            Doorway(parent, new Vector2(-32.5f, -7.8f), SceneNames.Cuarto);
            BuildSpawnPoint(parent, SpawnIds.TownFromCuarto, new Vector2(-32.5f, -9.2f));
            string layer = currentLayer;
            currentLayer = LayerForeground;
            WallPlate(parent, "CUARTOS", new Vector2(-32.5f, -5.85f), 1.5f, "#F8F5E8", GamePalette.Outline, 0.03f);
            currentLayer = layer;

            // A house with a sign opens just that house. The first two signs are in the original town.
            BuildListingStation(parent, "casa_vecina_b", "Ver la casa en venta", new Vector2(-38.2f, -9.5f));
            BuildListingStation(parent, "casa_vecina_d", "Ver la casa en alquiler", new Vector2(-5.7f, -9.5f));
            BuildListingStation(parent, "casa_almendros_1", "Ver la casa en venta", new Vector2(-36.6f, 30.9f));
            BuildListingStation(parent, "casa_almendros_2", "Ver la casa en alquiler", new Vector2(-28.6f, 30.9f));

            // Listed houses have walkthrough doors that lead into their house interior.
            BuildListingHouseDoor(parent, "casa_vecina_b", -40.25f, -9.1f);
            BuildListingHouseDoor(parent, "casa_vecina_d", -8.5f, -9.1f);
            BuildListingHouseDoor(parent, "casa_almendros_1", -39f, 31.8f);
            BuildListingHouseDoor(parent, "casa_almendros_2", -31f, 31.8f);
        }

        private static void BuildListingHouseDoor(Transform parent, string listingId, float x, float yBottom)
        {
            string spawn = "Town_From_" + listingId;
            var go = new GameObject("Door_" + listingId);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector2(x, yBottom + 0.2f);
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.size = new Vector2(1.3f, 1.0f);
            trigger.isTrigger = true;
            var door = go.AddComponent<Door>();
            door.ConfigureHouse(SceneNames.Casa, SpawnIds.FromTown, Vector2.up, spawn);
            door.MarkListing(listingId);
            BuildSpawnPoint(parent, spawn, new Vector2(x, yBottom - 1.2f));
        }

        /// <summary>The door of a house that is not for sale: it leads inside, and back out to the spot in front of this very door.</summary>
        private static void BuildHouseDoor(Transform parent, string house, float x, float yBottom)
        {
            string spawn = "Town_From_" + house;
            var go = new GameObject("Door_" + house);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector2(x, yBottom + 0.2f);
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.size = new Vector2(1.3f, 1.0f);
            trigger.isTrigger = true;
            go.AddComponent<Door>().ConfigureHouse(SceneNames.Vecino, SpawnIds.FromTown, Vector2.up, spawn);
            BuildSpawnPoint(parent, spawn, new Vector2(x, yBottom - 1.2f));
        }

        /// <summary>A zone in front of a house sign. The listing it opens is named in the station's first line.</summary>
        private static void BuildListingStation(Transform parent, string listingId, string prompt, Vector2 center)
        {
            var go = new GameObject("Station_Listing_" + listingId);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;

            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(2.6f, 2.2f);

            go.AddComponent<Station>().Configure(StationType.RealEstate, prompt, new[] { listingId });
        }

        private static void BuildMarketPeople(Transform parent)
        {
            // The agent and Don Beto now wait inside their own places.

            BuildPedestrian(parent, "Peaton_J", new Vector2(-44f, 31.6f), new Vector2(-21f, 31.6f), new Look { Skin = "#FF8FB1", Extra = Accessory.None }, 1.0f);
            BuildPedestrian(parent, "Peaton_K", new Vector2(36f, 31.6f), new Vector2(62f, 31.6f), new Look { Skin = "#55E08F", Extra = Accessory.Cap, ExtraColor = "#4DA3FF" }, 1.1f);
            BuildPedestrian(parent, "Peaton_L", new Vector2(64f, -9f), new Vector2(46f, -9f), new Look { Skin = "#FFD23F", Extra = Accessory.None }, 1.2f);
        }
    }
}
