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
        private string alternateCorrection = "Look farther down the track so you can react before the next obstacle is close.";

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
                    Failure = $"You hit a {obstacle} in the {laneName} lane.";
                    if (time - LastLaneChange <= 0.45f)
                        Failure += " You began changing lanes shortly before impact.";
                    Correction = "Start moving toward an open lane sooner.";
                    alternateCorrection = "Pick your escape lane as soon as you see a blocker ahead.";
                    break;
                case ObstacleType.SheerHeartAttack:
                case ObstacleType.BarrierLow:
                    string hazard = type == ObstacleType.SheerHeartAttack ? "Sheer Heart Attack" : "a low barrier";
                    Failure = $"You hit {hazard} in the {laneName} lane.";
                    bool landedBeforeImpact = grounded && time - LastJump <= 1.4f;
                    if (landedBeforeImpact) Failure += " You jumped and were already back on the ground.";
                    Correction = landedBeforeImpact ? "Jump later so you stay airborne over the obstacle."
                        : "Jump in time to clear the obstacle.";
                    alternateCorrection = type == ObstacleType.SheerHeartAttack
                        ? "Move to an open lane before Sheer Heart Attack reaches you."
                        : "If the next lane is clear, change lanes before you reach the low barrier.";
                    break;
                case ObstacleType.BarrierHigh:
                    Failure = $"You hit a high barrier in the {laneName} lane.";
                    if (!ducking && time - LastDuck <= 1.4f) Failure += " Your crouch had already ended.";
                    Correction = "Time the crouch to stay low as the barrier passes.";
                    alternateCorrection = "If the next lane is open, move out of the barrier's lane early.";
                    break;
            }
        }

        public void EndAtPose(string pose, bool keyboard, string key, bool reliable, float matchAge, float grace)
        {
            Failure = $"You hit a Human Tetris wall requiring {pose}.";
            if (!keyboard && !reliable)
            {
                Failure += " The camera couldn't confirm your pose.";
                Correction = "Keep shoulders and arms visible to the camera while matching the silhouette.";
                alternateCorrection = "Step back until the preview shows both arms, then try the pose again.";
                return;
            }

            if (matchAge > grace && matchAge < float.PositiveInfinity)
                Failure += " Your last matching pose expired too soon.";
            else
                Failure += " The required match was not registered in time.";
            Correction = keyboard ? $"Hold the {key} key for that wall until you pass through."
                : "Match the silhouette and hold the pose until you pass through.";
            alternateCorrection = keyboard ? "Read the key on the wall as soon as it appears so you have time to react."
                : "Start matching the silhouette while the wall is still approaching.";
        }

        internal readonly struct Report
        {
            public readonly string Mode, Overview, Diagnosis;
            public readonly string[] Tips;

            public Report(string mode, string overview, string diagnosis, string[] tips)
            {
                Mode = mode;
                Overview = overview;
                Diagnosis = diagnosis;
                Tips = tips;
            }
        }

        public Report Describe(GameManager game, string mode)
        {
            string[] tips = EarlyPunchMisses >= 2
                ? new[] { Correction, alternateCorrection, "Wait for the punch target to light up before you strike." }
                : new[] { Correction, alternateCorrection };
            return new Report(mode, Overview(game), Failure, tips);
        }

        private string Overview(GameManager game)
        {
            if (EarlyPunchMisses >= 2)
            {
                return $"You hit {PunchHits} of {PunchHits + PunchMisses} punch targets and struck too early {EarlyPunchMisses} times.";
            }

            int clears = LaneClears + JumpObstacleClears + DuckObstacleClears + PoseClears;
            if (clears > 0) return $"You cleared {clears} {(clears == 1 ? "obstacle" : "obstacles")} over {game.DistanceScore:F0} meters.";
            if (PunchHits > 0) return $"You hit {PunchHits} of {PunchHits + PunchMisses} punch targets.";
            if (ShieldHits == 1) return "Your shield absorbed one collision before the crash.";
            if (ShieldHits > 1) return $"Your shield absorbed {ShieldHits} collisions before the crash.";
            return $"You ran {game.DistanceScore:F0} meters and collected {game.CoinScore} coins.";
        }

    }
}
