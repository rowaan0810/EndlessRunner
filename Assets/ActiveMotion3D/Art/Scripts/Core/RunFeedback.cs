using System.Globalization;
using ActiveMotion3D.EndlessRunner.Obstacles;

namespace ActiveMotion3D.EndlessRunner.Core
{
    internal sealed class RunFeedback
    {
        public int LaneClears;
        public int JumpObstacleClears;
        public int DuckObstacleClears;
        public int PoseClears;
        public int PunchHits;
        public int PunchMisses;
        public int EarlyPunchMisses;
        public int ShieldHits;
        public float LastJump = float.NegativeInfinity;
        public float LastDuck = float.NegativeInfinity;
        public float LastLaneChange = float.NegativeInfinity;
        public string Failure { get; private set; } = "The run ended. The cause is unknown.";
        public string Correction { get; private set; } = "Focus on the next obstacle and read its cue before moving.";

        public void ClearObstacle(ObstacleType type)
        {
            switch (type)
            {
                case ObstacleType.BarrierFull:
                case ObstacleType.MenacingRoadSigns:
                    LaneClears++;
                    break;
                case ObstacleType.BarrierLow:
                case ObstacleType.SheerHeartAttack:
                    JumpObstacleClears++;
                    break;
                case ObstacleType.BarrierHigh:
                    DuckObstacleClears++;
                    break;
            }
        }

        public void EndAtObstacle(ObstacleType type, int lane, float time, bool grounded, bool ducking)
        {
            string laneName = lane == 0 ? "left" : lane == 2 ? "right" : "center";
            switch (type)
            {
                case ObstacleType.MenacingRoadSigns:
                case ObstacleType.BarrierFull:
                    string obstacle = type == ObstacleType.MenacingRoadSigns ? "Menacing Road Sign" : "full barrier";
                    Failure = $"You collided with a {obstacle} in the {laneName} lane.";
                    if (time - LastLaneChange <= 0.45f)
                        Failure += " You began changing lanes shortly before impact.";
                    Correction = "Start moving toward an open lane sooner.";
                    break;
                case ObstacleType.SheerHeartAttack:
                case ObstacleType.BarrierLow:
                    string hazard = type == ObstacleType.SheerHeartAttack ? "Sheer Heart Attack" : "a low barrier";
                    Failure = $"You collided with {hazard} in the {laneName} lane.";
                    bool landedBeforeImpact = grounded && time - LastJump <= 1.4f;
                    if (landedBeforeImpact) Failure += " You jumped and were already back on the ground.";
                    Correction = landedBeforeImpact ? "Time the jump later so you are airborne at the obstacle."
                        : "Time the jump so you reach clearance before impact.";
                    if (type == ObstacleType.SheerHeartAttack)
                        Correction += " An open adjacent lane is also an option.";
                    break;
                case ObstacleType.BarrierHigh:
                    Failure = $"You collided with a high barrier in the {laneName} lane.";
                    if (!ducking && time - LastDuck <= 1.4f) Failure += " Your crouch had already ended.";
                    Correction = "Time the crouch to stay low as the barrier passes.";
                    break;
            }
        }

        public void EndAtPose(string pose, bool keyboard, string key, bool reliable, float matchAge, float grace)
        {
            Failure = $"You hit a Human Tetris wall requiring {pose}.";
            if (!keyboard && !reliable)
            {
                Failure += " Tracking was unreliable at the wall, so your pose could not be confirmed.";
                Correction = "Keep shoulders and arms visible to the camera while matching the silhouette.";
                return;
            }

            if (matchAge > grace && matchAge < float.PositiveInfinity)
                Failure += $" The last detected match expired (match age {Number(matchAge)} seconds, grace {Number(grace)} seconds).";
            else
                Failure += " The required match was not registered in time.";
            Correction = keyboard ? $"Hold the {key} key for that wall until you pass through."
                : "Match the silhouette and hold the pose until you pass through.";
        }

        public string Describe(GameManager game, string mode)
        {
            return $"Controls: {mode}.\nMeasured result: {Overview(game, mode)}\n" +
                $"Final encounter: {Failure}\nNext action: {Correction}";
        }

        private string Overview(GameManager game, string mode)
        {
            if (EarlyPunchMisses >= 2)
            {
                string input = mode == "Keyboard" ? "a matching key press" : "a matching arm raise";
                return $"You hit {PunchHits} of {PunchHits + PunchMisses} punch targets, and {EarlyPunchMisses} misses followed {input} before the target lit up.";
            }

            int clears = LaneClears + JumpObstacleClears + DuckObstacleClears + PoseClears;
            if (clears > 0) return $"You cleared {clears} {(clears == 1 ? "obstacle" : "obstacles")} over {game.DistanceScore:F0} meters.";
            if (PunchHits > 0) return $"You hit {PunchHits} of {PunchHits + PunchMisses} punch targets.";
            if (ShieldHits == 1) return "Your shield absorbed one collision before the crash.";
            if (ShieldHits > 1) return $"Your shield absorbed {ShieldHits} collisions before the crash.";
            return $"You ran {game.DistanceScore:F0} meters and collected {game.CoinScore} coins.";
        }

        private static string Number(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
