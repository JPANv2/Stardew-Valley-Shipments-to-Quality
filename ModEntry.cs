using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using StardewModdingAPI;
using StardewValley;

namespace ShipmentsToQuality
{
    public class ModEntry : StardewModdingAPI.Mod
    {
        public static IModHelper __helper;
        public static IMonitor monitor;
        public static ShipConfig config;
        public override void Entry(IModHelper helper)
        {
            __helper = helper;
            monitor = Monitor;
            config = helper.ReadConfig<ShipConfig>() ?? new ShipConfig();
            // HarmonyInstance.DEBUG = true;
            var harmony = HarmonyInstance.Create(ModManifest.UniqueID);
            harmony.PatchAll();
        }

        

        [HarmonyPatch(typeof(Crop), nameof(Crop.harvest))]
        public static class CropHarvestPatchTranspiler
        {

            public static StardewValley.Object changeObjectQuality(StardewValley.Crop crop, StardewValley.Object obj)
            {
                if (Game1.player.basicShipped.ContainsKey(obj.ParentSheetIndex))
                {
                    float total = Game1.player.basicShipped[obj.parentSheetIndex];
                    if (config.increaseShipmentsByAverageYield && (crop.minHarvest > 1 || crop.maxHarvest > 1))
                    {
                            total = total / ((crop.maxHarvest + crop.minHarvest) / 2f);
                    }
                    if(config.silverStarShipments >= 0)
                    {
                        if(total >= config.silverStarShipments && obj.Quality < 1)
                        {
                            obj.Quality = 1;
                        }
                    }
                    if (config.goldStarShipments >= 0)
                    {
                        if (total >= config.goldStarShipments && obj.Quality < 2)
                        {
                            obj.Quality = 2;
                        }
                    }
                    if (config.iridiumStarShipments >= 0)
                    {
                        if (total >= config.iridiumStarShipments && obj.Quality < 3)
                        {
                            obj.Quality = 4;
                        }
                    }
                    if(config.shouldApplyPriceIncreases && total > config.priceIncreaseShipments && config.priceIncreasePerLog > 0)
                    {
                        obj.Price = (int)Math.Round(obj.Price * (1f + (Math.Log((total * 1f - config.priceIncreaseShipments) / config.priceIncreaseShipments) * config.priceIncreasePerLog)));
                    }
                }
                return obj;
            }
            
            public static IEnumerable<CodeInstruction> Transpiler(ILGenerator gen, MethodBase original, IEnumerable<CodeInstruction> insns)
            {
                MethodInfo info = typeof(CropHarvestPatchTranspiler).GetMethod("changeObjectQuality",BindingFlags.Static | BindingFlags.Public);
                var newInsns = new List<CodeInstruction>();
                foreach (var insn in insns)
                {
                    if (insn.opcode == OpCodes.Stloc_S && (insn.operand as LocalBuilder).LocalIndex == 12)
                    {
                        monitor.Log("Found Harvested Item Storage. Changing...");
                        newInsns.Add(insn);
                        CodeInstruction cc = new CodeInstruction(OpCodes.Ldarg_0);
                        newInsns.Add(cc);
                        cc = new CodeInstruction(OpCodes.Ldloc_S, insn.operand);
                        newInsns.Add(cc);
                        cc = new CodeInstruction(OpCodes.Call, info);
                        newInsns.Add(cc);
                        cc = new CodeInstruction(OpCodes.Stloc_S, insn.operand);
                        newInsns.Add(cc);
                    }
                    else
                    {
                        newInsns.Add(insn);
                    }
                }
                return newInsns;
            }
        }
    }

    public class ShipConfig
    {
        public bool increaseShipmentsByAverageYield = false;
        public int silverStarShipments = 250;
        public int goldStarShipments = 500;
        public int iridiumStarShipments = 1000;
        public bool shouldApplyPriceIncreases = true;
        public int priceIncreaseShipments = 2000;
        public double priceIncreasePerLog = 0.25f;
    }
}
