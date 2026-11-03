using System.Collections.Generic;
using UnityEngine;

namespace ColonQuest.World
{
    public enum PlaceKind
    {
        Home,
        Work,
        Money,
        Shop,
        Service,
        Danger
    }

    /// <summary>A place worth finding on the map: the object of the town that marks it, and what the map calls it.</summary>
    public readonly struct TownPlace
    {
        public readonly string ObjectName;
        public readonly string Name;
        public readonly PlaceKind Kind;

        /// <summary>True when the name goes over the mark, because a neighbour's name takes the space under it.</summary>
        public readonly bool NameAbove;

        public TownPlace(string objectName, string name, PlaceKind kind, bool nameAbove = false)
        {
            ObjectName = objectName;
            Name = name;
            Kind = kind;
            NameAbove = nameAbove;
        }
    }

    /// <summary>The places on the town map, and the sums that put the world on a picture of it. Kept apart from the screen so it can be tested.</summary>
    public static class TownPlaces
    {
        public static readonly IReadOnlyList<TownPlace> All = new List<TownPlace>
        {
            new TownPlace("Door_" + SceneNames.Casa, "Casa de mamá", PlaceKind.Home, true),
            new TownPlace("Door_" + SceneNames.Cuarto, "Tu cuarto", PlaceKind.Home),
            new TownPlace("Door_" + SceneNames.Soda, "Soda El Muelle", PlaceKind.Work),
            new TownPlace("Door_" + SceneNames.Banco, "Banco", PlaceKind.Money),
            new TownPlace("Door_" + SceneNames.Cooperativa, "Cooperativa", PlaceKind.Money),
            new TownPlace("Door_" + SceneNames.Finca, "Finca", PlaceKind.Money),
            new TownPlace("Door_" + SceneNames.Tienda, "Tienda", PlaceKind.Shop, true),
            new TownPlace("Door_" + SceneNames.Agencia, "Agencia de carros", PlaceKind.Shop),
            new TownPlace("Door_" + SceneNames.Inmobiliaria, "Inmobiliaria", PlaceKind.Shop),
            new TownPlace("Station_Grocery", "Pulpería", PlaceKind.Shop),
            new TownPlace("Station_Garage", "Taller de Don Rafa", PlaceKind.Service),
            new TownPlace("Door_" + SceneNames.Hospital, "Hospital", PlaceKind.Service),
            new TownPlace("Station_Courses", "Cursos", PlaceKind.Service, true),
            new TownPlace("Station_Beach", "Playa", PlaceKind.Service),
            new TownPlace("Station_Lender", "Prestamos Rapidos", PlaceKind.Danger),
            new TownPlace("Station_Pyramid", "Inversiones Seguras", PlaceKind.Danger)
        };

        /// <summary>What each kind means, for the legend of the full map.</summary>
        public static string KindName(PlaceKind kind)
        {
            switch (kind)
            {
                case PlaceKind.Home: return "Dónde vivís";
                case PlaceKind.Work: return "Tu trabajo";
                case PlaceKind.Money: return "Tu plata";
                case PlaceKind.Shop: return "Comprar";
                case PlaceKind.Service: return "Servicios";
                default: return "Ojo: cuidado aquí";
            }
        }

        /// <summary>Where a point of the world falls on the picture of the town, from 0 to 1 on each side.</summary>
        public static Vector2 ToMap(Vector2 world, Rect bounds)
        {
            return new Vector2((world.x - bounds.xMin) / bounds.width, (world.y - bounds.yMin) / bounds.height);
        }

        /// <summary>
        /// The part of the picture a square minimap shows around a point, <paramref name="units"/> of the world across. Near
        /// an edge it stops at the edge and the point moves off its center, so the minimap never shows outside the picture.
        /// </summary>
        public static Rect View(Vector2 world, Rect bounds, float units)
        {
            var size = new Vector2(Mathf.Min(1f, units / bounds.width), Mathf.Min(1f, units / bounds.height));
            var center = ToMap(world, bounds);
            float x = Mathf.Clamp(center.x - size.x * 0.5f, 0f, 1f - size.x);
            float y = Mathf.Clamp(center.y - size.y * 0.5f, 0f, 1f - size.y);
            return new Rect(x, y, size.x, size.y);
        }
    }
}
