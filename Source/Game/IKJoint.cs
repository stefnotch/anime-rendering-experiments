using System;
using System.Collections.Generic;
using FlaxEngine;

namespace Game
{
    public class IKJoint : Script
    {
        [EditorOrder(0)]
        public bool IsHinge;

        [EditorOrder(10)]
        [VisibleIf(nameof(IsHinge))]
        public Vector3 HingeAxis;


        [EditorOrder(20)]
        public bool ClampToCone;

        [EditorOrder(30)]
        [VisibleIf(nameof(ClampToCone))]
        public Vector3 ConstraintAxis;

        [EditorOrder(40)]
        [VisibleIf(nameof(ClampToCone))]
        public float MaxAngle = 180; // TODO: Custom degree editor?

        private IKJoint _childJoint;

        public IKJoint ChildJoint => _childJoint;

        public override void OnEnable()
        {
            // TODO: Child should register & unregister itself
            // TODO: Update the docs to reflect on that

            if (Actor.Parent.TryGetScript<IKJoint>(out var parentJoint))
            {
                parentJoint._childJoint = this;
            }

            HingeAxis.Normalize();
            ConstraintAxis.Normalize();
        }

        public override void OnDisable()
        {
            _childJoint = null;
        }

        public override void OnDebugDrawSelected()
        {
            if (IsHinge)
            {
                Vector3 currentHingeAxis = HingeAxis * Transform.Orientation;

                DebugDraw.DrawLine(Actor.Position - currentHingeAxis * 10f, Actor.Position + currentHingeAxis * 10f, Color.Green);
            }
        }

        public void Evaluate(Actor jointTip, ref Vector3 target)
        {
            if (!Enabled) return;

            _childJoint?.Evaluate(jointTip, ref target);

            Vector3 directionToTip = jointTip.Transform.Translation - Transform.Translation;
            Vector3 directionToTarget = target - Transform.Translation;

            directionToTip.Normalize();
            directionToTarget.Normalize();
            Quaternion jointRotation = InverseKinematics.FromVectors(ref directionToTip, ref directionToTarget);
            Quaternion orientation = jointRotation * Transform.Orientation;

            // TODO: Lerp

            if (IsHinge)
            {
                Vector3 currentHingeAxis = HingeAxis * orientation;
                Vector3 targetHingeAxis = HingeAxis * Actor.Parent.Orientation;

                Quaternion hingeRotation = InverseKinematics.FromVectors(ref currentHingeAxis, ref targetHingeAxis);
                orientation = hingeRotation * orientation;
            }

            if (ClampToCone)
            {
                Vector3 currentConstraintAxis = ConstraintAxis * orientation;
                Vector3 parentConstraintAxis = ConstraintAxis * Actor.Parent.Orientation;
                Vector3 constrainedConstraintAxis = InverseKinematics.ConstrainToConeUsingSlerp(ref currentConstraintAxis, ref parentConstraintAxis, MaxAngle * Mathf.DegreesToRadians);
                Quaternion constraintRotation = InverseKinematics.FromVectors(ref currentConstraintAxis, ref constrainedConstraintAxis);

                orientation = constraintRotation * orientation;
            }

            Actor.Orientation = orientation;
        }
    }
}
