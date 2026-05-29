using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace CleanerControlApp.Utilities
{
    public class ModuleSettings
    {
        public List<MS_DryingTanks>? MS_DryingTanks { get; set; }

        [JsonIgnore]
        public List<MS_DryingTanks>? DryingTanks
        {
            get => MS_DryingTanks;
            set => MS_DryingTanks = value;
        }

        // Keep a property that matches the JSON key 'MS_Sink'
        public MS_Sink? MS_Sink { get; set; }

        [JsonIgnore]
        public MS_Sink? Sink
        {
            get => MS_Sink;
            set => MS_Sink = value;
        }

        public MS_HeatingTank? MS_HeatingTank { get; set; }

        [JsonIgnore]
        public MS_HeatingTank? HeatingTank
        {
            get => MS_HeatingTank;
            set => MS_HeatingTank = value;
        }

        public MS_SoakingTank? MS_SoakingTank { get; set; }

        [JsonIgnore]
        public MS_SoakingTank? SoakingTank
        {
            get => MS_SoakingTank;
            set => MS_SoakingTank = value;
        }

        public MS_Shuttle? MS_Shuttle { get; set; }

        [JsonIgnore]
        public MS_Shuttle? Shuttle
        {
            get => MS_Shuttle;
            set => MS_Shuttle = value;
        }

        public List<MS_Motor>? MS_Motors { get; set; }
        [JsonIgnore]
        public List<MS_Motor>? Motors
        {
            get => MS_Motors;
            set => MS_Motors = value;
        }

        public MS_System? MS_System { get; set; }
        public MS_System System
        {
            get => MS_System;
            set => MS_System = value;
        }

        // Recipe metadata
        // The currently selected recipe name for this ModuleSettings (used later to load/save per-file)
        public string? RecipeName { get; set; }

        // Export a Recipe capturing only the specified linked parameters
        public Recipe ExportRecipe(string name)
        {
            var recipe = new Recipe
            {
                Name = name,
                DryingTanks = MS_DryingTanks?.Select(dt => DryingTankRecipe.From(dt)).ToList(),
                Sink = MS_Sink != null ? SinkRecipe.From(MS_Sink) : null,
                HeatingTank = MS_HeatingTank != null ? HeatingTankRecipe.From(MS_HeatingTank) : null,
                SoakingTank = MS_SoakingTank != null ? SoakingTankRecipe.From(MS_SoakingTank) : null,
                System = MS_System != null ? SystemRecipe.From(MS_System) : null
            };

            return recipe;
        }

        // Apply a recipe to the current ModuleSettings (only the linked fields will be written)
        public void ApplyRecipe(Recipe recipe)
        {
            if (recipe == null) return;

            // Drying tanks: map recipe entries by index
            if (recipe.DryingTanks != null)
            {
                if (MS_DryingTanks == null) MS_DryingTanks = new List<MS_DryingTanks>();

                for (int i =0; i < recipe.DryingTanks.Count; i++)
                {
                    var r = recipe.DryingTanks[i];
                    if (i >= MS_DryingTanks.Count)
                    {
                        MS_DryingTanks.Add(new MS_DryingTanks());
                    }

                    var target = MS_DryingTanks[i];
                    target.SV_Low = r.SV_Low;
                    target.SV_High = r.SV_High;
                    target.ActTime_Second = r.ActTime_Second;
                }
            }

            // Sink
            if (recipe.Sink != null)
            {
                if (MS_Sink == null) MS_Sink = new MS_Sink();
                recipe.Sink.ApplyTo(MS_Sink);
            }

            // Heating tank
            if (recipe.HeatingTank != null)
            {
                if (MS_HeatingTank == null) MS_HeatingTank = new MS_HeatingTank();
                recipe.HeatingTank.ApplyTo(MS_HeatingTank);
            }

            // Soaking tank
            if (recipe.SoakingTank != null)
            {
                if (MS_SoakingTank == null) MS_SoakingTank = new MS_SoakingTank();
                recipe.SoakingTank.ApplyTo(MS_SoakingTank);
            }

            // System
            if (recipe.System != null)
            {
                if (MS_System == null) MS_System = new MS_System();
                recipe.System.ApplyTo(MS_System);
            }
        }

    }

    public class MS_DryingTanks
    {
        public int SV_Low { get; set; }
        public int SV_High { get; set; }
        public int ActTime_Second { get; set; }

    }
    public class MS_Sink
    {
        public int SV_Low { get; set; }
        public int SV_High { get; set; }
        public int ActTime_Second { get; set; }

        // Property names aligned with JSON keys in appsettings.json
        public int MotorPosition_01 { get; set; }
        public int MotorPosition_02 { get; set; }
        public int MotorPosition_03 { get; set; }
        public int MotorVelocity_01 { get; set; }
        public int MotorVelocity_02 { get; set; }
        public int AirKnifeRetryCount { get; set; }
        public int ShakingDelayTime_ms { get; set; }

    }

    public class MS_HeatingTank
    {
        public int SV_Low { get; set; }
        public int SV_High { get; set; }

        public float INV_High { get; set; }
        public float INV_Low { get; set; }
        public float INV_Zero { get; set; }
        public int Water_H_CheckDelay_Second { get; set; }
        public int Water_L_CheckDelay_Second { get; set; }
    }

    public class MS_SoakingTank
    {
        public int ActTime_Second { get; set; }

        // Property names aligned with JSON keys in appsettings.json
        public int MotorPosition_01 { get; set; }
        public int MotorPosition_02 { get; set; }
        public int MotorPosition_03 { get; set; }
        public int MotorVelocity_01 { get; set; }
        public int MotorVelocity_02 { get; set; }
        public int AirKnifeRetryCount { get; set; }
        public float UltrasonicSetCurrent { get; set; }

        public int ShakingDelayTime_ms { get; set; }
    }

    public class MS_Shuttle
    {
        public int Shuttle_ZAxis_StableTime_Second { get; set; }
        public int Shuttle_Procedure_ClamperActDelayTime_ms { get; set; }
        public int Shuttle_Procedure_MoveEndDelayTime_ms { get; set; }
    }

    public class MS_Motor
    { 
        public List<int>? Positions { get; set; }
        public List<int>? Velocities { get; set; }
    }

    public class MS_System
    { 
        public int SinkModulePass { get; set; }
        public int SoakingTankModulePass { get; set; }
        public int DryingTank1ModulePass { get; set; }
        public int DryingTank2ModulePass { get; set; }

        public int WriteMotionParameterAfterInitialization { get; set; }
    }

    // --- Recipe related types ---
    public class Recipe
    {
        public string? Name { get; set; }

        // Drying tanks is a list (one recipe per tank)
        public List<DryingTankRecipe>? DryingTanks { get; set; }

        public SinkRecipe? Sink { get; set; }

        public HeatingTankRecipe? HeatingTank { get; set; }

        public SoakingTankRecipe? SoakingTank { get; set; }

        public SystemRecipe? System { get; set; }
    }

    public class DryingTankRecipe
    {
        public int SV_Low { get; set; }
        public int SV_High { get; set; }
        public int ActTime_Second { get; set; }

        public static DryingTankRecipe From(MS_DryingTanks src)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            return new DryingTankRecipe
            {
                SV_Low = src.SV_Low,
                SV_High = src.SV_High,
                ActTime_Second = src.ActTime_Second
            };
        }

        public void ApplyTo(MS_DryingTanks target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            target.SV_Low = SV_Low;
            target.SV_High = SV_High;
            target.ActTime_Second = ActTime_Second;
        }
    }

    public class SinkRecipe    
    {
        public int SV_Low { get; set; }
        public int SV_High { get; set; }
        public int ActTime_Second { get; set; }
        public int AirKnifeRetryCount { get; set; }

        public int ShakingDelayTime_ms { get; set; }

        public static SinkRecipe From(MS_Sink src)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            return new SinkRecipe
            {
                SV_Low = src.SV_Low,
                SV_High = src.SV_High,
                ActTime_Second = src.ActTime_Second,
                AirKnifeRetryCount = src.AirKnifeRetryCount,
                ShakingDelayTime_ms = src.ShakingDelayTime_ms
            };
        }

        public void ApplyTo(MS_Sink target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            target.SV_Low = SV_Low;
            target.SV_High = SV_High;
            target.ActTime_Second = ActTime_Second;
            target.AirKnifeRetryCount = AirKnifeRetryCount;
            target.ShakingDelayTime_ms = ShakingDelayTime_ms;
        }
    }

    public class HeatingTankRecipe
    {
        public int SV_Low { get; set; }
        public int SV_High { get; set; }
        public float INV_High { get; set; }
        public float INV_Low { get; set; }
        public float INV_Zero { get; set; }

        public static HeatingTankRecipe From(MS_HeatingTank src)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            return new HeatingTankRecipe
            {
                SV_Low = src.SV_Low,
                SV_High = src.SV_High,
                INV_High = src.INV_High,
                INV_Low = src.INV_Low,
                INV_Zero = src.INV_Zero
            };
        }

        public void ApplyTo(MS_HeatingTank target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            target.SV_Low = SV_Low;
            target.SV_High = SV_High;
            target.INV_High = INV_High;
            target.INV_Low = INV_Low;
            target.INV_Zero = INV_Zero;
        }
    }

    public class SoakingTankRecipe
    {
        public int ActTime_Second { get; set; }
        public int AirKnifeRetryCount { get; set; }
        public float UltrasonicSetCurrent { get; set; }

        public int ShakingDelayTime_ms { get; set; }

        public static SoakingTankRecipe From(MS_SoakingTank src)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            return new SoakingTankRecipe
            {
                ActTime_Second = src.ActTime_Second,
                AirKnifeRetryCount = src.AirKnifeRetryCount,
                UltrasonicSetCurrent = src.UltrasonicSetCurrent,
                ShakingDelayTime_ms = src.ShakingDelayTime_ms
            };
        }

        public void ApplyTo(MS_SoakingTank target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            target.ActTime_Second = ActTime_Second;
            target.AirKnifeRetryCount = AirKnifeRetryCount;
            target.UltrasonicSetCurrent = UltrasonicSetCurrent;
            target.ShakingDelayTime_ms = ShakingDelayTime_ms;
        }
    }

    public class SystemRecipe
    {
        public int SinkModulePass { get; set; }
        public int SoakingTankModulePass { get; set; }
        public int DryingTank1ModulePass { get; set; }
        public int DryingTank2ModulePass { get; set; }

        public static SystemRecipe From(MS_System src)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            return new SystemRecipe
            {
                SinkModulePass = src.SinkModulePass,
                SoakingTankModulePass = src.SoakingTankModulePass,
                DryingTank1ModulePass = src.DryingTank1ModulePass,
                DryingTank2ModulePass = src.DryingTank2ModulePass
            };
        }

        public void ApplyTo(MS_System target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            target.SinkModulePass = SinkModulePass;
            target.SoakingTankModulePass = SoakingTankModulePass;
            target.DryingTank1ModulePass = DryingTank1ModulePass;
            target.DryingTank2ModulePass = DryingTank2ModulePass;
        }
    }

}
