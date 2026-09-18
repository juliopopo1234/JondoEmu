using System;
using System.Collections.Generic;

namespace Jondo.Unity.Server.Managers
{
    /// <summary>Un poste de fabrication mesuré dans un atelier d'Incarnam.</summary>
    public readonly record struct WorkshopStation(
        long MapId, int ElementId, int Type, int SkillId, bool EndsUseImmediately = false);

    /// <summary>
    /// Les postes des neuf ateliers d'Incarnam, relevés sur le <c>jss</c> officiel de chaque
    /// intérieur. Le client fournit cellule et graphique dans ses données de carte; le serveur
    /// doit encore fournir le triplet élément/type/compétence pour rendre le poste cliquable.
    /// </summary>
    public static class Workshops
    {
        private static readonly WorkshopStation[] Stations =
        {
            // Alchimiste — JobsIncarnam/AlchimisteIncarnam.pcapng, jss frame 19.
            new(153355270, 489066, 90, 23),
            new(153355270, 489068, 90, 23),
            new(153355270, 489069, 90, 23),

            // Bricoleur — AtelierBricoIncarnam.pcapng, jss frame 48.
            new(153354248, 490182, 13, 171),
            new(153354248, 490183, 13, 171),

            // Bûcheron / sculpteur — AtelierBucheronSculpteurIncarnam.pcapng, jss frame 10.
            new(153355266, 489533, 13, 15),
            new(153355266, 489534, 2, 101, EndsUseImmediately: true),
            new(153355266, 489536, 13, 15),

            // Bijoutier — BijoutierIncarnam.pcapng, jss frame 20.
            new(153355272, 489548, 12, 12),
            new(153355272, 489549, 12, 12),
            new(153355272, 489550, 12, 12),
            new(153355272, 489551, 12, 12),

            // Chasseur — ChasseursIncarnam.pcapng, jss frame 17.
            new(153355268, 489360, 97, 134),
            new(153355268, 489361, 97, 134),
            new(153355268, 489362, 97, 134),

            // Façonneur — FaconneursIncarnam.pcapng, jss frame 20.
            new(153354240, 489674, 97, 156),
            new(153354240, 489676, 97, 156),
            new(153354240, 490230, 138, 201),

            // Forgeron — ForgeronIncarnam.pcapng, jss frame 18.
            new(153355264, 489176, 27, 32),
            new(153355264, 489177, 57, 20),
            new(153355264, 489178, 41, 48),
            new(153355264, 489345, 27, 32),

            // Paysan — PaysanIncarnam.pcapng, jss frame 15. Les deux moulins sont libérés par
            // un iwi juste après le iwn dans les deux utilisations mesurées.
            new(153354242, 489524, 22, 27),
            new(153354242, 489525, 22, 27),
            new(153354242, 489526, 12, 47, EndsUseImmediately: true),
            new(153354242, 489527, 12, 47, EndsUseImmediately: true),

            // Tailleur — TailleurIncarnam.pcapng, jss frame 16.
            new(153354244, 489569, 86, 63),
            new(153354244, 489570, 11, 13),
            new(153354244, 489571, 86, 63),
            new(153354244, 489572, 86, 63),
        };

        private static readonly Dictionary<(long MapId, int ElementId), WorkshopStation> ByElement =
            BuildIndex();

        public static IReadOnlyList<WorkshopStation> All => Stations;
        public static int Count => Stations.Length;

        public static bool TryGet(long mapId, int elementId, out WorkshopStation station)
            => ByElement.TryGetValue((mapId, elementId), out station);

        /// <summary>Contrôle au démarrage que les captures concordent encore avec les données 3.6.</summary>
        public static void Initialize()
        {
            int valid = 0;
            foreach (var station in Stations)
            {
                var element = Interactives.ByElementId(station.MapId, station.ElementId);
                if (element.Id == 0)
                {
                    Console.WriteLine($"[Ateliers] Élément {station.ElementId} absent de la carte " +
                                      $"{station.MapId}; poste ignoré.");
                    continue;
                }

                if (!SkillManager.TryGet(station.SkillId, out _) ||
                    RecipeManager.ForSkill(station.SkillId).Count == 0)
                {
                    Console.WriteLine($"[Ateliers] Compétence de fabrication {station.SkillId} " +
                                      $"inconnue pour {station.MapId}/{station.ElementId}.");
                    continue;
                }
                valid++;
            }
            Console.WriteLine($"[Ateliers] {valid}/{Stations.Length} postes d'Incarnam validés.");
        }

        private static Dictionary<(long, int), WorkshopStation> BuildIndex()
        {
            var result = new Dictionary<(long, int), WorkshopStation>();
            foreach (var station in Stations) result.Add((station.MapId, station.ElementId), station);
            return result;
        }
    }
}
