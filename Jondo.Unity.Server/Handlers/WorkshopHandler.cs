using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Jondo.Unity.Protocol;
using Jondo.Unity.Server.Managers;
using Jondo.Unity.Server.Network;

namespace Jondo.Unity.Server.Handlers
{
    /// <summary>Ouverture et fermeture de l'interface de fabrication d'un atelier.</summary>
    public static class WorkshopHandler
    {
        public static bool IsOpen => SessionContext.State.OpenWorkshopSkillId != 0;

        public static async Task OpenAsync(NetworkStream stream, int elementId, int skillId)
        {
            long mapId = SessionContext.State.MapId;
            if (!Workshops.TryGet(mapId, elementId, out var station) || station.SkillId != skillId)
            {
                Console.WriteLine($"[Ateliers] Poste inconnu: carte {mapId}, élément {elementId}, " +
                                  $"compétence {skillId}.");
                return;
            }

            if (!CraftHandler.TryResolve(skillId, out _, out var job, out var recipes,
                                         out string error))
            {
                Console.WriteLine($"[Ateliers] {error}");
                return;
            }

            SessionContext.State.OpenWorkshopSkillId = skillId;
            SessionContext.State.SelectedWorkshopRecipeResultId = 0;
            SessionContext.State.SelectedWorkshopIngredients.Clear();

            // Mesuré à chaque ouverture: iwn, éventuellement iwi pour les trois postes qui le
            // font réellement, inventaire complet, hlm vide, puis kgq { f1: compétence }.
            await Jondo.Protocol.NetworkMessage.WriteFrameAsync(stream,
                ConnectionProtocol.Push(Op.Iwn, ConnectionProtocol.BuildElementInUse(
                    elementId, skillId, SessionContext.State.CharacterId)));

            if (station.EndsUseImmediately)
            {
                await Jondo.Protocol.NetworkMessage.WriteFrameAsync(stream,
                    ConnectionProtocol.Push(Op.Iwi,
                        ConnectionProtocol.BuildInteractiveUseEnded(elementId, skillId)));
            }

            await Jondo.Protocol.NetworkMessage.WriteFrameAsync(stream,
                ConnectionProtocol.Push(Op.Ivx, ConnectionProtocol.BuildWorkshopInventory()));
            await Jondo.Protocol.NetworkMessage.WriteFrameAsync(stream,
                ConnectionProtocol.Push(Op.Hlm));
            await Jondo.Protocol.NetworkMessage.WriteFrameAsync(stream,
                ConnectionProtocol.Push(Op.Kgq, ConnectionProtocol.BuildWorkshopOpened(skillId)));

            Console.WriteLine($"[Ateliers] Métier {job.Id}, compétence {skillId}: " +
                              $"{recipes.Count} recette(s) disponibles.");
        }

        public static async Task CloseAsync(NetworkStream stream)
        {
            ClearSelection();

            // La fermeture mesurée est khd { f3:11 }, suivie du même inventaire et du hlm vide.
            await Jondo.Protocol.NetworkMessage.WriteFrameAsync(stream,
                ConnectionProtocol.Push(Op.Khd, ConnectionProtocol.BuildShopClosed()));
            await Jondo.Protocol.NetworkMessage.WriteFrameAsync(stream,
                ConnectionProtocol.Push(Op.Ivx, ConnectionProtocol.BuildWorkshopInventory()));
            await Jondo.Protocol.NetworkMessage.WriteFrameAsync(stream,
                ConnectionProtocol.Push(Op.Hlm));
        }

        /// <summary>
        /// Traite <c>kew</c>, envoyé quand le joueur choisit une recette dans l'atelier.
        /// La capture disponible contient <c>{ f2: resultId }</c>. Le champ 1 existe dans le
        /// descripteur 3.6.10.10 mais n'a jamais été observé; on le lit sans lui inventer de rôle.
        /// </summary>
        public static bool TrySelectRecipe(byte[] frame, out List<byte[]> addedPayloads)
        {
            addedPayloads = new List<byte[]>();
            ClearRecipeSelection();

            int skillId = SessionContext.State.OpenWorkshopSkillId;
            if (skillId == 0)
            {
                Console.WriteLine("[Ateliers] Sélection kew ignorée: aucun atelier ouvert.");
                return false;
            }

            if (!TryReadRecipeSelection(frame, out int auxiliaryValue, out int resultId,
                                        out string error))
            {
                Console.WriteLine($"[Ateliers] Sélection kew invalide: {error}");
                return false;
            }

            if (!CraftHandler.TryResolveRecipe(skillId, resultId, out var recipe, out error))
            {
                Console.WriteLine($"[Ateliers] Sélection kew refusée: {error}");
                return false;
            }

            if (!TrySelectIngredients(recipe.Ingredients, out var ingredients, out error))
            {
                Console.WriteLine($"[Ateliers] Sélection kew refusée: {error}");
                return false;
            }

            SessionContext.State.SelectedWorkshopRecipeResultId = resultId;
            foreach (var ingredient in ingredients)
                SessionContext.State.SelectedWorkshopIngredients[ingredient.Item.Uid] =
                    ingredient.Quantity;

            foreach (var ingredient in ingredients)
                addedPayloads.Add(ConnectionProtocol.BuildWorkshopIngredientAdded(
                    ingredient.Item, ingredient.Quantity));
            string auxiliary = auxiliaryValue == 0 ? "absent/0" : auxiliaryValue.ToString();
            Console.WriteLine($"[Ateliers] Recette {resultId} sélectionnée pour la compétence " +
                              $"{skillId}: {recipe.Ingredients.Count} ingrédient(s), " +
                              $"{ingredients.Count} pile(s) envoyée(s) par kex, f1={auxiliary}.");
            return true;
        }

        /// <summary>
        /// Choisit des piles de la bolsa sans les consommer. Une recette doit être entièrement
        /// disponible; les objets équipés ne sont jamais déplacés automatiquement vers l'atelier.
        /// </summary>
        public static bool TrySelectIngredients(IReadOnlyList<RecipeIngredient> requirements,
            out List<(Equipment.Item Item, int Quantity)> selected, out string error)
        {
            selected = new List<(Equipment.Item Item, int Quantity)>();
            error = "";

            var bag = Equipment.All
                .Where(item => item.Position == Equipment.Bag && item.Quantity > 0)
                .OrderBy(item => item.Uid)
                .ToList();

            foreach (var requirement in requirements)
            {
                int left = requirement.Quantity;
                foreach (var item in bag)
                {
                    if (left == 0) break;
                    if (item.Template != requirement.ItemId) continue;

                    int quantity = Math.Min(left, item.Quantity);
                    selected.Add((item, quantity));
                    left -= quantity;
                }

                if (left != 0)
                {
                    selected.Clear();
                    error = $"il manque {left} unité(s) de l'objet {requirement.ItemId}";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Traite le <c>kcr</c> envoyé lorsqu'une pile est placée manuellement dans l'atelier.
        /// Le même opcode sert aux coffres; l'atelier ouvert décide donc quel handler le reçoit.
        /// </summary>
        public static bool TryMoveIngredient(byte[] frame, out byte[] addedPayload)
        {
            addedPayload = Array.Empty<byte>();
            if (!IsOpen) return false;

            byte[]? body = ConnectionProtocol.ReadPayload(frame, Op.Kcr);
            if (body == null) return true;

            long uid = 0;
            int requested = 0;
            foreach (var field in ProtoMessage.Parse(body).Fields)
            {
                if (field.WireType != 0) continue;
                if (field.FieldNumber == 1)
                    requested = field.VarIntValue <= 0 || field.VarIntValue > int.MaxValue
                        ? 0 : (int)field.VarIntValue;
                else if (field.FieldNumber == 2) uid = field.VarIntValue;
            }

            var item = Equipment.ByUid(uid);
            if (item == null || item.Position != Equipment.Bag || item.Quantity <= 0)
            {
                Console.WriteLine($"[Ateliers] Mouvement kcr ignoré: pile {uid} absente du sac.");
                return true;
            }

            int quantity = requested == 0 ? item.Quantity : Math.Min(requested, item.Quantity);
            SessionContext.State.SelectedWorkshopIngredients[uid] = quantity;
            addedPayload = ConnectionProtocol.BuildWorkshopIngredientAdded(item, quantity);
            Console.WriteLine($"[Ateliers] Pile {uid} x{quantity} ajoutée manuellement par kex.");
            return true;
        }

        /// <summary>
        /// Traite la validation de fabrication 3.6.10.10 (<c>lmr</c>). La capture 3.6.11.15
        /// envoie son équivalent <c>kcs</c> sous la forme f2=true, f3=2. Le troisième champ est
        /// une étape du dialogue, pas une quantité: une validation produit donc un seul objet.
        /// </summary>
        public static async Task<bool> TryCraftAsync(NetworkStream stream, byte[] frame)
        {
            if (!IsOpen) return false;

            byte[]? body = ConnectionProtocol.ReadPayload(frame, Op.Lmr);
            if (body == null) return false;

            bool ready = false;
            int step = 0;
            foreach (var field in ProtoMessage.Parse(body).Fields)
            {
                if (field.WireType != 0) continue;
                if (field.FieldNumber == 2) ready = field.VarIntValue != 0;
                else if (field.FieldNumber == 3 && field.VarIntValue <= int.MaxValue)
                    step = (int)field.VarIntValue;
            }

            if (!ready)
            {
                Console.WriteLine($"[Ateliers] Validation lmr ignorée: prêt=false, étape {step}.");
                return true;
            }

            int skillId = SessionContext.State.OpenWorkshopSkillId;
            int resultId = SessionContext.State.SelectedWorkshopRecipeResultId;
            if (!CraftHandler.TryResolveRecipe(skillId, resultId, out var recipe, out string error))
            {
                Console.WriteLine($"[Ateliers] Fabrication refusée: {error}");
                return true;
            }

            // On refait le choix au moment de fabriquer. Les quantités peuvent avoir changé
            // depuis le clic sur la recette, et le serveur ne doit jamais se fier à la barre UI.
            if (!TrySelectIngredients(recipe.Ingredients, out var ingredients, out error))
            {
                Console.WriteLine($"[Ateliers] Fabrication {resultId} refusée: {error}.");
                return true;
            }

            long characterId = SessionContext.State.CharacterId;
            foreach (var ingredient in ingredients)
            {
                int before = ingredient.Item.Quantity;
                if (!DatabaseManager.DestroyCharacterItem(characterId, ingredient.Item.Uid,
                                                          ingredient.Quantity))
                {
                    Console.WriteLine($"[Ateliers] Fabrication {resultId} interrompue: impossible " +
                                      $"de consommer la pile {ingredient.Item.Uid}.");
                    return true;
                }

                Equipment.Remove(ingredient.Item.Uid, ingredient.Quantity);
                int remaining = before - ingredient.Quantity;
                await Jondo.Protocol.NetworkMessage.WriteFrameAsync(stream,
                    remaining > 0
                        ? ConnectionProtocol.Push(Op.Ivj,
                            ConnectionProtocol.BuildItemQuantity(ingredient.Item.Uid, remaining))
                        : ConnectionProtocol.Push(Op.Ium,
                            ConnectionProtocol.BuildItemGone(ingredient.Item.Uid)));
            }

            var existing = Equipment.All.FirstOrDefault(item =>
                item.Template == resultId && item.Position == Equipment.Bag);
            var stored = DatabaseManager.AddItemToInventory(characterId, resultId, 1);
            Equipment.Item crafted;
            if (existing != null && existing.Uid == stored.Uid)
            {
                existing.Quantity = stored.Quantity;
                crafted = existing;
                await Jondo.Protocol.NetworkMessage.WriteFrameAsync(stream,
                    ConnectionProtocol.Push(Op.Ivj,
                        ConnectionProtocol.BuildItemQuantity(crafted.Uid, crafted.Quantity)));
            }
            else
            {
                crafted = Equipment.Add(stored.Uid, resultId, stored.Quantity,
                                        Equipment.Bag, stored.RawEffects);
                await Jondo.Protocol.NetworkMessage.WriteFrameAsync(stream,
                    ConnectionProtocol.Push(Op.Itd, ConnectionProtocol.BuildItemArrived(3,
                        new HavenBagStore.StoredItem
                        {
                            Uid = crafted.Uid,
                            Gid = crafted.Template,
                            Quantity = crafted.Quantity,
                            Effects = stored.RawEffects,
                        })));
            }

            await Jondo.Protocol.NetworkMessage.WriteFrameAsync(stream,
                ConnectionProtocol.Push(Op.Kdr,
                    ConnectionProtocol.BuildWorkshopCraftSucceeded(resultId)));

            const int craftExperience = 20; // mesuré sur Le Plussain, recette de niveau 1
            SessionContext.State.AddJobExperience(recipe.JobId, craftExperience,
                                                   out long total, out int level);
            DatabaseManager.SaveJobExperience(characterId, recipe.JobId, total);
            await Jondo.Protocol.NetworkMessage.WriteFrameAsync(stream,
                ConnectionProtocol.Push(Op.Irq, ConnectionProtocol.BuildJobExperience(
                    recipe.JobId, JobExperience.Next(level), level,
                    JobExperience.Floor(level), total)));
            await Jondo.Protocol.NetworkMessage.WriteFrameAsync(stream,
                ConnectionProtocol.Push(Op.Iun, ConnectionProtocol.BuildPods(
                    0, 1000 + 5L * SessionContext.State.StatStrength)));

            SessionContext.State.SelectedWorkshopIngredients.Clear();
            Console.WriteLine($"[Ateliers] Recette {resultId} fabriquée (étape lmr {step}), " +
                              $"objet {crafted.Uid}, +{craftExperience} XP métier.");
            return true;
        }

        /// <summary>Décode la forme exacte du proto <c>kew</c>: deux int32, résultat en f2.</summary>
        public static bool TryReadRecipeSelection(byte[] frame, out int auxiliaryValue,
                                                  out int resultId, out string error)
        {
            auxiliaryValue = 0;
            resultId = 0;
            error = "";

            byte[]? body = ConnectionProtocol.ReadPayload(frame, Op.Kew);
            if (body == null)
            {
                error = "enveloppe kew absente";
                return false;
            }

            foreach (var field in ProtoMessage.Parse(body).Fields)
            {
                if ((field.FieldNumber == 1 || field.FieldNumber == 2) && field.WireType != 0)
                {
                    error = $"le champ {field.FieldNumber} n'est pas un varint";
                    return false;
                }
                if (field.WireType != 0) continue;
                if (field.VarIntValue < 0 || field.VarIntValue > int.MaxValue)
                {
                    error = $"le champ {field.FieldNumber} dépasse int32";
                    return false;
                }

                if (field.FieldNumber == 1) auxiliaryValue = (int)field.VarIntValue;
                else if (field.FieldNumber == 2) resultId = (int)field.VarIntValue;
            }

            if (resultId <= 0)
            {
                error = "identifiant de résultat f2 absent";
                return false;
            }
            return true;
        }

        public static void Forget() => ClearSelection();

        private static void ClearSelection()
        {
            SessionContext.State.OpenWorkshopSkillId = 0;
            ClearRecipeSelection();
        }

        private static void ClearRecipeSelection()
        {
            SessionContext.State.SelectedWorkshopRecipeResultId = 0;
            SessionContext.State.SelectedWorkshopIngredients.Clear();
        }
    }
}
