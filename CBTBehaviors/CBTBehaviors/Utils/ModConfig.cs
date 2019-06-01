

namespace CBTBehaviors {
    
    public class ModStats {
        public const string TurnsOverheated = "CBTB_TurnsOverheated";
        public const string CanShootAfterSpring = "CanShootAfterSprinting";
        public const string MeleeHitPushBackPhases = "MeleeHitPushBackPhases";
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

        // Piloting
        public float PilotStabilityCheck = 0.30f;

        // Movement
        public int ToHitSelfJumped = 2;

        public void LogConfig() {
            Mod.Log.Info("=== MOD CONFIG BEGIN ===");
            Mod.Log.Info($"  DEBUG: {this.Debug} Trace: {this.Trace}");
            Mod.Log.Info("=== MOD CONFIG END ===");
        }
    }
}
