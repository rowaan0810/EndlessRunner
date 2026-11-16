using System;
using UnityEngine;

namespace ActiveMotion3D.EndlessRunner.UI
{
    internal sealed class StandCoachPose
    {
        private sealed class Limb
        {
            public readonly Transform Upper, Lower, End;
            private readonly Quaternion upperRest, lowerRest, endRest;
            public float Reach => Vector3.Distance(Upper.position, Lower.position) + Vector3.Distance(Lower.position, End.position);

            public Limb(Animator animator, HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones end)
            {
                Upper = animator.GetBoneTransform(upper);
                Lower = animator.GetBoneTransform(lower);
                End = animator.GetBoneTransform(end);
                upperRest = Upper.localRotation;
                lowerRest = Lower.localRotation;
                endRest = End.localRotation;
            }

            public void Aim(Vector3 target, Vector3 bend)
            {
                Upper.localRotation = upperRest;
                Lower.localRotation = lowerRest;
                End.localRotation = endRest;

                Vector3 start = Upper.position;
                float a = Vector3.Distance(start, Lower.position);
                float b = Vector3.Distance(Lower.position, End.position);
                Vector3 direction = (target - start).normalized;
                float reach = Mathf.Clamp(Vector3.Distance(start, target), Mathf.Abs(a - b) + 0.001f, a + b - 0.001f);
                float along = (a * a + reach * reach - b * b) / (2f * reach);
                float offset = Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
                Vector3 elbow = start + direction * along + Vector3.ProjectOnPlane(bend, direction).normalized * offset;
                Upper.rotation = Quaternion.FromToRotation(Lower.position - start, elbow - start) * Upper.rotation;
                Lower.rotation = Quaternion.FromToRotation(End.position - Lower.position,
                    start + direction * reach - Lower.position) * Lower.rotation;
            }
        }

        private readonly struct Hand
        {
            private readonly Transform wrist, index, middle, little;
            private readonly float side;

            public Hand(Animator animator, bool right)
            {
                wrist = animator.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
                index = animator.GetBoneTransform(right ? HumanBodyBones.RightIndexProximal : HumanBodyBones.LeftIndexProximal);
                middle = animator.GetBoneTransform(right ? HumanBodyBones.RightMiddleProximal : HumanBodyBones.LeftMiddleProximal);
                little = animator.GetBoneTransform(right ? HumanBodyBones.RightLittleProximal : HumanBodyBones.LeftLittleProximal);
                side = right ? 1f : -1f;
            }

            public void Aim(Vector3 fingers, Vector3 palm)
            {
                if (index == null || middle == null || little == null) return;
                Vector3 current = middle.position - wrist.position;
                Vector3 normal = Vector3.Cross(current, index.position - little.position) * side;
                if (normal.sqrMagnitude < 0.0000001f) return;
                wrist.rotation = Quaternion.LookRotation(fingers, palm) *
                    Quaternion.Inverse(Quaternion.LookRotation(current, normal)) * wrist.rotation;
            }
        }

        private readonly bool killerQueen;
        private readonly Transform pelvis, chest, head;
        private readonly Vector3 pelvisRest;
        private readonly Quaternion chestRest, headRest;
        private readonly float shoulders;
        private float blend;
        private float spokenTime;

        public static void PrepareRig(Animator animator, bool killerQueen)
        {
            if (!killerQueen)
            {
                // This FBX was exported in a running pose. The humanoid avatar
                // provides an upright starting point for the torso and head.
                using var handler = new HumanPoseHandler(animator.avatar, animator.transform);
                var neutral = new HumanPose();
                handler.GetHumanPose(ref neutral);
                neutral.bodyPosition = Vector3.up;
                neutral.bodyRotation = Quaternion.identity;
                Array.Clear(neutral.muscles, 0, neutral.muscles.Length);
                handler.SetHumanPose(ref neutral);
            }
            animator.enabled = false;
        }

        public StandCoachPose(Animator animator, bool killerQueen)
        {
            this.killerQueen = killerQueen;
            var leftArm = new Limb(animator, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand);
            var rightArm = new Limb(animator, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);
            var leftLeg = new Limb(animator, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);
            var rightLeg = new Limb(animator, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);
            var leftHand = new Hand(animator, false);
            var rightHand = new Hand(animator, true);
            pelvis = animator.GetBoneTransform(HumanBodyBones.Hips);
            chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            head = animator.GetBoneTransform(HumanBodyBones.Head);
            shoulders = Vector3.Distance(leftArm.Upper.position, rightArm.Upper.position);
            Vector3 shoulderCenter = (leftArm.Upper.position + rightArm.Upper.position) * 0.5f;
            Vector3 hips = pelvis.position;

            if (killerQueen)
            {
                leftArm.Aim(shoulderCenter + InShoulderWidths(0.13f, -0.34f, 0.37f), new Vector3(-0.72f, -1f, 0.17f));
                rightArm.Aim(shoulderCenter + InShoulderWidths(-0.10f, -0.41f, 0.44f), new Vector3(0.72f, -1f, 0.17f));
                leftHand.Aim(new Vector3(1f, 0.15f, -0.45f), Vector3.back);
                rightHand.Aim(new Vector3(-1f, 0.12f, -0.45f), Vector3.back);
                float ankleY = (leftLeg.End.position.y + rightLeg.End.position.y) * 0.5f;
                Vector3 ankles = new Vector3(hips.x, ankleY, hips.z);
                pelvis.position += InShoulderWidths(0.02f, -0.045f, 0f);
                PlantFoot(leftLeg, ankles + InShoulderWidths(-0.44f, 0f, -0.15f), -8f);
                PlantFoot(rightLeg, ankles + InShoulderWidths(0.46f, 0f, -0.19f), 10f);
            }
            else
            {
                leftArm.Aim(hips + InShoulderWidths(-0.57f, 0.52f, 0.04f), new Vector3(-1f, -0.26f, -0.58f));
                rightArm.Aim(hips + InShoulderWidths(0.56f, 0.55f, 0.01f), new Vector3(1f, -0.19f, -0.48f));
                leftHand.Aim(new Vector3(0.12f, -0.72f, 0.67f), new Vector3(0.88f, 0f, -0.48f));
                rightHand.Aim(new Vector3(-0.12f, -0.72f, 0.67f), new Vector3(-0.88f, 0f, -0.48f));
                float ankleHeight = rightLeg.Upper.position.y - Mathf.Min(leftLeg.Reach, rightLeg.Reach) * 0.97f;
                Vector3 ankles = new Vector3(hips.x, ankleHeight, hips.z);
                PlantFoot(leftLeg, ankles + InShoulderWidths(-0.34f, 0f, 0.08f), -6f);
                PlantFoot(rightLeg, ankles + InShoulderWidths(0.35f, 0f, -0.08f), 7f);
            }

            leftFoot = leftLeg.End;
            rightFoot = rightLeg.End;
            leftAnchor = leftFoot.position;
            rightAnchor = rightFoot.position;
            leftSole = leftFoot.rotation;
            rightSole = rightFoot.rotation;
            this.leftLeg = leftLeg;
            this.rightLeg = rightLeg;
            pelvisRest = pelvis.localPosition;
            chestRest = chest.rotation;
            headRest = head.rotation;
        }

        private readonly Limb leftLeg, rightLeg;
        private readonly Transform leftFoot, rightFoot;
        private readonly Vector3 leftAnchor, rightAnchor;
        private readonly Quaternion leftSole, rightSole;

        public bool Update(bool speaking, float sentenceProgress)
        {
            if (!speaking && blend == 0f) return false;
            float delta = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (speaking) spokenTime += delta;
            blend = Mathf.MoveTowards(blend, speaking ? 1f : 0f, delta * 2.5f);
            float weightShift = Mathf.Sin(spokenTime * (killerQueen ? 0.9f : 1.18f)) * blend;
            float nod = Mathf.Sin(sentenceProgress * Mathf.PI) * blend;
            float chin = killerQueen ? -2.8f * nod : 3.4f * nod;
            pelvis.localPosition = pelvisRest;
            pelvis.position += Vector3.right * (0.028f * shoulders * weightShift);
            leftLeg.Aim(leftAnchor, Vector3.forward);
            rightLeg.Aim(rightAnchor, Vector3.forward);
            leftFoot.rotation = leftSole;
            rightFoot.rotation = rightSole;
            chest.rotation = Quaternion.Euler(0f, 2.0f * weightShift, -1.35f * weightShift) * chestRest;
            head.rotation = Quaternion.Euler(chin, 3.3f * weightShift, 1.2f * weightShift) * headRest;
            return true;
        }

        private Vector3 InShoulderWidths(float x, float y, float z) => new Vector3(x, y, z) * shoulders;

        private static void PlantFoot(Limb leg, Vector3 target, float turnout)
        {
            Quaternion neutral = leg.End.rotation;
            leg.Aim(target, Vector3.forward);
            leg.End.rotation = Quaternion.AngleAxis(turnout, Vector3.up) * neutral;
        }
    }
}
