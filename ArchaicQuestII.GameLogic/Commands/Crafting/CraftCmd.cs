using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArchaicQuestII.GameLogic.Account;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Character.Equipment;
using ArchaicQuestII.GameLogic.Character.Status;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.Crafting;
using ArchaicQuestII.GameLogic.Item;
using ArchaicQuestII.GameLogic.Utilities;
using ArchaicQuestII.GameLogic.World.Room;
using Newtonsoft.Json;

namespace ArchaicQuestII.GameLogic.Commands.Crafting
{
    public class CraftCmd : ICommand
    {
        public CraftCmd(ICore core)
        {
            Aliases = new[] {"craft"};
            Description = "Craft items, type craft list to see which items you can craft.";
            Usages = new[] {"Type: craft list - to view craft-able items. \n\r craft <item> - to craft the item e.g. craft sword"};
            Title = "Crafting";
            DeniedStatus = new[]
            {
                CharacterStatus.Status.Busy,
                CharacterStatus.Status.Dead,
                CharacterStatus.Status.Fighting,
                CharacterStatus.Status.Ghost,
                CharacterStatus.Status.Fleeing,
                CharacterStatus.Status.Incapacitated,
                CharacterStatus.Status.Sleeping,
                CharacterStatus.Status.Stunned,
                CharacterStatus.Status.Resting
            };
            UserRole = UserRole.Player;
            Core = core;
        }
        
        public string[] Aliases { get; }
        public string Description { get; }
        public string[] Usages { get; }
        public string Title { get; }
        public CharacterStatus.Status[] DeniedStatus { get; }
        public UserRole UserRole { get; }
        public ICore Core { get; }

        public void Execute(Player player, Room room, string[] input)
        {
            var target = string.Join(" ", input.Skip(1));

            if (!string.IsNullOrEmpty(target) && target.Equals("list"))
            {
                ListCrafts(player, true);
                return;
            }
            
            if(string.IsNullOrEmpty(target))
            {
                ListCrafts(player, false);
                return;
            }
            
            CraftItem(player, room, target);
            
        }
        
        private void CraftItem(Player player, Room room, string item)
        {
            var craftingRecipes = ReturnValidRecipes(player);

            if (room.Items.FirstOrDefault(x => x.ItemType == Item.Item.ItemTypes.Crafting) == null && player.Inventory.FirstOrDefault(x => x.ItemType == Item.Item.ItemTypes.Crafting) == null)
            {
                Core.Writer.WriteLine("<p>To begin crafting you require the correct tools such as a crafting bench.</p>", player.ConnectionId);
                return;
            }

            var recipe =
                craftingRecipes.FirstOrDefault(x =>
                    x.Title.StartsWith(item, StringComparison.CurrentCultureIgnoreCase));

            if (recipe == null)
            {
                Core.Writer.WriteLine("<p>You can't craft that.</p>", player.ConnectionId);
                return;
            }
            Core.Writer.WriteLine($"<p>You begin crafting {Helpers.AddArticle(recipe.Title).ToLower()}.</p>", player.ConnectionId);

            var success = Helpers.SkillSuccessCheck(player, "crafting");
            
            // use up materials
            foreach (var material in recipe.CraftingMaterials)
            {
                var craftItem = player.Inventory.FirstOrDefault(x => x.Name.Equals(material.Material, StringComparison.CurrentCultureIgnoreCase));

                if (craftItem == null)
                {
                    Core.Writer.WriteLine("<p>You appear to be missing required items.</p>", player.ConnectionId);
                    return;
                }

                var limit = 1;
                for (var i = player.Inventory.Count - 1; i >= 0; i--)
                {
                    if (player.Inventory[i].Name == craftItem.Name && limit <= material.Quantity)
                    {
                        limit++;
                        player.Weight -= craftItem.Weight;
                        player.Inventory.RemoveAt(i);
                    }
                }
            }

            if (success)
            {

                if (recipe.CreatedItemDropsInRoom)
                {
                    room.Items.Add(JsonConvert.DeserializeObject<Item.Item>(
                        JsonConvert.SerializeObject(recipe.CreatedItem)));
                    Core.Writer.WriteLine($"<p>You continue working on {Helpers.AddArticle(recipe.Title).ToLower()}.</p>",
                        player.ConnectionId, 2000);

                    Core.Writer.WriteLine($"<p class='improve'>You have successfully created {Helpers.AddArticle(recipe.Title).ToLower()}.</p>",
                        player.ConnectionId, 4000);
                }
                else
                {
                    player.Inventory.Add(JsonConvert.DeserializeObject<Item.Item>(
                        JsonConvert.SerializeObject(recipe.CreatedItem)));
                    player.Weight += recipe.CreatedItem.Weight;
                    Core.Writer.WriteLine("<p>You slave over the crafting bench working away.</p>",
                        player.ConnectionId, 2000);
                    Core.Writer.WriteLine($"<p class='improve'>You have crafted successfully {Helpers.AddArticle(recipe.Title).ToLower()}.</p>",
                        player.ConnectionId, 4000);
                }

                Core.UpdateClient.UpdateScore(player);
                Core.UpdateClient.UpdateInventory(player);

            }
            else
            {
                Core.UpdateClient.UpdateScore(player);
                Core.UpdateClient.UpdateInventory(player);

                if (recipe.CreatedItemDropsInRoom)
                {


                    Core.Writer.WriteLine($"<p>You have failed in making {recipe.Title}.</p>",
                        player.ConnectionId, 2000);
                }
                else
                {


                    Core.Writer.WriteLine($"<p>You have failed to craft {recipe.Title}. It looks nothing like!</p>",
                        player.ConnectionId, 2000);
                }

                Core.Writer.WriteLine(Helpers.SkillLearnMistakes(player, "Crafting", Core.Gain, 2000), player.ConnectionId, 2120);
            }
        }

         private void ListCrafts(Player player, bool showAllCrafts)
        {

            
            var materials = player.Inventory.Where(x => x.ItemType == Item.Item.ItemTypes.Material).ToList();

          /*  if (materials.Count == 0)
            {
                Core.Writer.WriteLine("<p>You don't have any materials to craft a thing.</p>", player.ConnectionId);
                return;
            }
*/
            // Lets find what you can craft
            var craftingRecipes = Core.Cache.GetCraftingRecipes();

            if (craftingRecipes == null)
            {
                Core.Writer.WriteLine("<p>No crafting recipes have been set up.</p>", player.ConnectionId);
                return;
            }

            var craftingList = showAllCrafts ? craftingRecipes : ReturnValidRecipes(player);

            if (craftingList.Count == 0)
            {
                Core.Writer.WriteLine("<p>No crafting recipes found with the current materials you have.</p>", player.ConnectionId);
                return;
            }


            var sb = new StringBuilder();
            sb.Append("<p>You can craft the following items:</p>");
            sb.Append("<table class='simple'>");
            sb.Append($"<tr><td>Name</td><td>Materials</td></tr>");
            foreach (var craft in craftingList.Distinct())
            {
                var materialsRequired = "";
                foreach (var material in craft.CraftingMaterials)
                {
                    materialsRequired += $"{material.Material} x{material.Quantity}, ";
                }

                sb.Append($"<tr><td>{craft.Title}</td><td>{materialsRequired}</td></tr>");
            }

            sb.Append($"</table>");

            Core.Writer.WriteLine(sb.ToString(), player.ConnectionId);

        }

         private List<CraftingRecipes> ReturnValidRecipes(Player player)
         {
             var craftingRecipes = Core.Cache.GetCraftingRecipes();
             var materials = player.Inventory.Where(x => x.ItemType == Item.Item.ItemTypes.Material).GroupBy(y => y.Name)
                 .Select(z => z.First());
             var craftingList = new List<CraftingRecipes>();
             foreach (var material in materials)
             {
                 var quantity = player.Inventory.Where(x => x.ItemType == Item.Item.ItemTypes.Material && x.Name.Equals(material.Name, StringComparison.CurrentCultureIgnoreCase)).ToList();
                 var canCraft = craftingRecipes.Where(x =>
                     x.CraftingMaterials.Any(y => y.Material.Equals(material.Name, StringComparison.CurrentCultureIgnoreCase) && y.Quantity <= quantity.Count));

                 craftingList.AddRange(canCraft);
             }

             return craftingList;
         }
       
    }
}