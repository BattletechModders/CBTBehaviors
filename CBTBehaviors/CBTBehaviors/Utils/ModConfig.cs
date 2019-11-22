

namespace CBTBehaviors {
    
    public class ModStats {
        public const string TurnsOverheated = "TurnsOverheated";
        public const string CanShootAfterSprinting = "CanShootAfterSprinting";
        public const string MeleeHitPushBackPhases = "MeleeHitPushBackPhases";
    }
    public class BoundedModifier {
        public int Bound = 0;
        public int Modifier = 0;
    }

    public class ModConfig {

        public bool Debug = false;
        public bool Trace = false;

        // Heat
        public float[] ShutdownPercentages = new float[] { 0.083f, 0.278f, 0.583f, 0.833f };
        public float[] AmmoExplosionPercentages = new float[] { 0f, 0.083f, 0.278f, 0.583f };
        public int[] HeatToHitModifiers = new int[] { 1, 2, 3, 4 };
        public bool UseGuts = false;
        public int GutsDivisor = 40;
        public float[] OverheatedMovePenalty = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };

        // https://github.com/Bohica/BattletechCombatMachine/wiki/HEAT
        public class HeatOptions {
            // 5:-1, 10:-2, 15:-3, 20:-4, 25:-5, 31:-6, 37:-7, 43:-8, 49:-9
            public BoundedModifier[] Movement = new BoundedModifier[] { };
            // 8:-1, 13:-2, 17:-3, 24:-4, 33:-5, 41:-6, 48:-7
            public BoundedModifier[] Firing = new BoundedModifier[] { };
            // 14:4, 18:6, 22:8, 26:10, 30:12, 34:14, 38:16, 42:18, 46:20, 50:-1
            public BoundedModifier[] Shutdown= new BoundedModifier[] { };
            // 
            public BoundedModifier[] Explosion = new BoundedModifier[] { };

            public BoundedModifier[] PilotInjury = new BoundedModifier[] { };

            public BoundedModifier[] SystemFailures = new BoundedModifier[] { };
        }
        public HeatOptions Heat = new HeatOptions();

        // Piloting
        public float PilotStabilityCheck = 0.30f;
        public bool ShowAllStabilityRolls = false;

        // Movement
        public int ToHitSelfJumped = 2;

        public void LogConfig() {
            Mod.Log.Info("=== MOD CONFIG BEGIN ===");
            Mod.Log.Info($"  DEBUG: {this.Debug} Trace: {this.Trace}");
            Mod.Log.Info("=== MOD CONFIG END ===");
        }
    }
}
