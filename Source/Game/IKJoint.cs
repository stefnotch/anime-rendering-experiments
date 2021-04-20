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

        private Actor _jointTip;
        private IKJoint[] _childJoints;

        public IKJoint FirstChild => _childJoints?.Length > 0 ? _childJoints[0] : null;
        public Actor JointTip => _jointTip;

        public override void OnEnable()
        {
            var childJoints = new List<IKJoint>();
            for (int i = 0; i < Actor.ChildrenCount; i++)
            {
                if (Actor.GetChild(i).TryGetScript<IKJoint>(out var childJoint))
                {
                    childJoints.Add(childJoint);
                }
            }
            _childJoints = childJoints.ToArray();

            _jointTip = _childJoints.Length > 0 ? _childJoints[0].Actor : (Actor.ChildrenCount > 0 ? Actor.GetChild(0) : null);

            HingeAxis.Normalize();
            ConstraintAxis.Normalize();
        }

        public override void OnDisable()
        {
            _jointTip = null;
            _childJoints = null;
        }

        public override void OnDebugDrawSelected()
        {
            if (IsHinge)
            {
                Vector3 currentHingeAxis = HingeAxis * Transform.Orientation;

                DebugDraw.DrawLine(Actor.Position - currentHingeAxis * 10f, Actor.Position + currentHingeAxis * 10f, Color.Green);
            }
        }

        public void Evaluate(ref Vector3 target)
        {
            // TODO: Make smoooother

            if (!_jointTip) return;
            for (int i = 0; i < _childJoints.Length; i++)
            {
                _childJoints[i].Evaluate(ref target);
            }

            var transform = Transform;

            Vector3 directionToTip = _jointTip.Transform.Translation - transform.Translation;
            Vector3 directionToTarget = target - transform.Translation;

            directionToTip.Normalize();
            directionToTarget.Normalize();
            Quaternion jointRotation = InverseKinematics.FromVectors(ref directionToTip, ref directionToTarget);
            transform.Orientation = jointRotation * transform.Orientation;

            if (IsHinge)
            {
                Vector3 currentHingeAxis = HingeAxis * transform.Orientation;
                Vector3 targetHingeAxis = HingeAxis * Actor.Parent.Orientation;

                Quaternion hingeRotation = InverseKinematics.FromVectors(ref currentHingeAxis, ref targetHingeAxis);
                transform.Orientation = hingeRotation * transform.Orientation;
            }

            if (ClampToCone)
            {
                Vector3 currentConstraintAxis = ConstraintAxis * transform.Orientation;
                Vector3 parentConstraintAxis = ConstraintAxis * Actor.Parent.Orientation;
                Vector3 constrainedConstraintAxis = InverseKinematics.ConstrainToCone(ref currentConstraintAxis, ref parentConstraintAxis, MaxAngle * Mathf.DegreesToRadians);
                Quaternion constraintRotation = InverseKinematics.FromVectors(ref currentConstraintAxis, ref constrainedConstraintAxis);

                transform.Orientation = constraintRotation * transform.Orientation;
            }

            Transform = transform;
        }
    }
}
