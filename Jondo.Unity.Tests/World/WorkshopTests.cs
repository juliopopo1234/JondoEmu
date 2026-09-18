using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jondo.Unity.Launcher;
using Jondo.Unity.Protocol;
using Jondo.Unity.Server;
using Jondo.Unity.Server.Handlers;
using Jondo.Unity.Server.Managers;
using Jondo.Unity.Server.Network;
using Xunit;

namespace Jondo.Unity.Tests.World
{
    [Collection("MapManager")]
    public class WorkshopTests
    {
        private static readonly IReadOnlyDictionary<long, int> ExpectedPerMap =
            new Dictionary<long, int>
            {
                [153354240] = 3, // façonneur
                [153354242] = 4, // paysan
                [153354244] = 4, // tailleur
                [153354248] = 2, // bricoleur
                [153355264] = 4, // forgeron
                [153355266] = 3, // bûcheron / sculpteur
                [153355268] = 3, // chasseur
                [153355270] = 3, // alchimiste
                [153355272] = 4, // bijoutier
            };

        [Fact]
        public void The_nine_captured_maps_expose_all_thirty_stations()
        {
            Assert.Equal(30, Workshops.Count);
            var actual = Workshops.All.GroupBy(x => x.MapId)
                .ToDictionary(x => x.Key, x => x.Count());
            Assert.Equal(ExpectedPerMap, actual);
        }

        [Fact]
        public void Every_station_is_registered_with_its_measured_type_and_skill()
        {
            if (!File.Exists(Paths.InteractiveElementsJson)) return;

            Interactives.Initialize();
            InteractiveRegistry.Initialize();

            foreach (var station in Workshops.All)
            {
                var element = Interactives.ByElementId(station.MapId, station.ElementId);
                Assert.Equal(station.ElementId, element.Id);

                var registered = Assert.Single(InteractiveRegistry.OnMap(station.MapId),
                    x => x.Element.Id == station.ElementId);
                Assert.Equal(station.Type, registered.Type);

                var action = Assert.Single(registered.Actions,
                    x => x.Kind == InteractiveActionKind.Workshop);
                Assert.Equal(station.SkillId, action.SkillId);
                Assert.Equal(Interactives.SkillInstanceOf(station.ElementId),
                             action.SkillInstanceId);
            }
        }

        [Fact]
        public void Workshop_open_message_carries_only_the_selected_skill()
        {
            byte[] payload = ConnectionProtocol.BuildWorkshopOpened(201);
            var field = Assert.Single(ProtoMessage.Parse(payload).Fields);
            Assert.Equal(1, field.FieldNumber);
            Assert.Equal(0, field.WireType);
            Assert.Equal(201, field.VarIntValue);
        }

        [Fact]
        public void Workshop_inventory_carries_the_captured_context()
        {
            var context = Assert.Single(ProtoMessage.Parse(
                ConnectionProtocol.BuildWorkshopInventory()).Fields,
                x => x.FieldNumber == 1);
            Assert.Equal(0, context.WireType);
            Assert.Equal(30, context.VarIntValue);
        }

        [Fact]
        public void Kew_carries_the_selected_recipe_result_in_field_two()
        {
            byte[] frame = ConnectionProtocol.Push(Op.Kew, new byte[] { 0x10, 0xd9, 0x42 });

            Assert.True(WorkshopHandler.TryReadRecipeSelection(
                frame, out int auxiliary, out int resultId, out string error), error);
            Assert.Equal(0, auxiliary);
            Assert.Equal(8537, resultId);
        }

        [Fact]
        public void Kew_reads_the_optional_first_field_without_assigning_it_a_meaning()
        {
            byte[] frame = ConnectionProtocol.Push(Op.Kew,
                Pb.New().Var(1, 7).Var(2, 8537).Build());

            Assert.True(WorkshopHandler.TryReadRecipeSelection(
                frame, out int auxiliary, out int resultId, out string error), error);
            Assert.Equal(7, auxiliary);
            Assert.Equal(8537, resultId);
        }

        [Theory]
        [InlineData(new byte[0])]
        [InlineData(new byte[] { 0x08, 0x01 })]
        public void Kew_without_a_positive_result_is_rejected(byte[] body)
        {
            byte[] frame = ConnectionProtocol.Push(Op.Kew, body);

            Assert.False(WorkshopHandler.TryReadRecipeSelection(
                frame, out _, out _, out string error));
            Assert.Contains("f2", error);
        }

        [Fact]
        public void Kew_is_rejected_when_no_workshop_is_open()
        {
            var session = GameSession.SinSocket();
            using var scope = SessionContext.Push(session);
            byte[] frame = ConnectionProtocol.Push(Op.Kew, new byte[] { 0x10, 0xd9, 0x42 });

            Assert.False(WorkshopHandler.TrySelectRecipe(frame, out _));
            Assert.Equal(0, session.State.SelectedWorkshopRecipeResultId);
        }

        [Fact]
        public void Ingredient_selection_splits_quantities_across_bag_stacks_and_ignores_equipment()
        {
            var session = GameSession.SinSocket();
            using var scope = SessionContext.Push(session);
            Equipment.Add(9, 16518, 99, 0, null); // équipé: ne doit jamais partir à l'atelier
            Equipment.Add(10, 16518, 1, Equipment.Bag, null);
            Equipment.Add(11, 16518, 3, Equipment.Bag, null);
            Equipment.Add(12, 289, 2, Equipment.Bag, null);

            Assert.True(WorkshopHandler.TrySelectIngredients(new[]
            {
                new RecipeIngredient(16518, 2),
                new RecipeIngredient(289, 2),
            }, out var selected, out string error), error);

            Assert.Equal(new[] { (10L, 1), (11L, 1), (12L, 2) },
                selected.Select(x => (x.Item.Uid, x.Quantity)).ToArray());
        }

        [Fact]
        public void Kfb_contains_one_exchange_object_in_field_one_and_a_zero_float_in_field_three()
        {
            var first = new Equipment.Item
                { Uid = 10, Template = 16518, Position = Equipment.Bag, Quantity = 4 };

            byte[] payload = ConnectionProtocol.BuildWorkshopIngredientAdded(first, 2);

            var entries = ProtoMessage.Parse(payload).Fields;
            var exchangeObject = Assert.Single(entries, x => x.FieldNumber == 1);
            Assert.Equal(1, exchangeObject.FieldNumber);
            var coefficient = Assert.Single(entries, x => x.FieldNumber == 3);
            Assert.Equal(5, coefficient.WireType);
            Assert.Equal(0u, coefficient.Fixed32Value);
            var lec = ProtoMessage.Parse(exchangeObject.BytesValue).Fields;
            var bodyField = Assert.Single(lec, x => x.FieldNumber == 5);
            var body = ProtoMessage.Parse(bodyField.BytesValue).Fields;
            Assert.Equal(16518, Assert.Single(body, x => x.FieldNumber == 1).VarIntValue);
            Assert.Equal(2, Assert.Single(body, x => x.FieldNumber == 3).VarIntValue);
            Assert.Equal(10, Assert.Single(body, x => x.FieldNumber == 4).VarIntValue);
        }

        [Fact]
        public void Kcr_in_an_open_workshop_is_answered_with_the_selected_bag_stack()
        {
            var session = GameSession.SinSocket();
            using var scope = SessionContext.Push(session);
            session.State.OpenWorkshopSkillId = 12;
            Equipment.Add(44, 289, 6, Equipment.Bag, null);
            byte[] frame = ConnectionProtocol.Push(Op.Kcr,
                Pb.New().Var(1, 2).Var(2, 44).Build());

            Assert.True(WorkshopHandler.TryMoveIngredient(frame, out byte[] added));
            Assert.NotEmpty(added);
            Assert.Equal(2, session.State.SelectedWorkshopIngredients[44]);

            var exchangeObject = Assert.Single(ProtoMessage.Parse(added).Fields,
                x => x.FieldNumber == 1);
            var lec = ProtoMessage.Parse(exchangeObject.BytesValue).Fields;
            var body = ProtoMessage.Parse(Assert.Single(
                lec, x => x.FieldNumber == 5).BytesValue).Fields;
            Assert.Equal(44, Assert.Single(body, x => x.FieldNumber == 4).VarIntValue);
            Assert.Equal(2, Assert.Single(body, x => x.FieldNumber == 3).VarIntValue);
        }

        [Fact]
        public void Kep_carries_ready_in_field_one_and_the_dialogue_step_in_field_two()
        {
            byte[] frame = ConnectionProtocol.Push(Op.Kep,
                new byte[] { 0x08, 0x01, 0x10, 0x02 });

            Assert.True(WorkshopHandler.TryReadCraftRequest(
                frame, out bool ready, out int step, out string error), error);
            Assert.True(ready);
            Assert.Equal(2, step);
        }

        [Fact]
        public void Successful_craft_result_carries_the_recipe_result_and_success_enum()
        {
            var fields = ProtoMessage.Parse(
                ConnectionProtocol.BuildWorkshopCraftSucceeded(8537)).Fields;

            Assert.Equal(8537, Assert.Single(fields, x => x.FieldNumber == 1).VarIntValue);
            Assert.Equal(2, Assert.Single(fields, x => x.FieldNumber == 3).VarIntValue);
            Assert.DoesNotContain(fields, x => x.FieldNumber == 2);
        }

        [Fact]
        public void Le_Plussain_rolls_strength_and_agility_between_four_and_six()
        {
            if (!File.Exists(Paths.WorldDb)) return;

            Assert.True(DatabaseManager.TryRollItemTemplateEffects(8537, out string effects));
            var rolled = Equipment.ParseEffects(effects).ToDictionary(effect => effect.Effect);

            Assert.Equal(2, rolled.Count);
            Assert.InRange(rolled[118].Value, 4, 6); // force
            Assert.InRange(rolled[119].Value, 4, 6); // agilité
            Assert.All(rolled.Values, effect =>
            {
                Assert.Equal(0, effect.DiceNum);
                Assert.Equal(0, effect.DiceSide);
            });
        }

        [Fact]
        public void Forget_clears_both_the_workshop_and_its_selected_recipe()
        {
            var session = GameSession.SinSocket();
            using var scope = SessionContext.Push(session);
            session.State.OpenWorkshopSkillId = 12;
            session.State.SelectedWorkshopRecipeResultId = 8537;

            WorkshopHandler.Forget();

            Assert.Equal(0, session.State.OpenWorkshopSkillId);
            Assert.Equal(0, session.State.SelectedWorkshopRecipeResultId);
            Assert.Empty(session.State.SelectedWorkshopIngredients);
        }

        [Fact]
        public void Only_the_three_measured_stations_end_the_use_immediately()
        {
            var ended = Workshops.All.Where(x => x.EndsUseImmediately)
                .Select(x => (x.MapId, x.ElementId, x.SkillId))
                .OrderBy(x => x.ElementId)
                .ToArray();

            Assert.Equal(new[]
            {
                (153354242L, 489526, 47),
                (153354242L, 489527, 47),
                (153355266L, 489534, 101),
            }, ended);
        }
    }
}
