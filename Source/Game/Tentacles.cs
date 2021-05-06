using System;
using System.Collections.Generic;
using FlaxEngine;

namespace Game
{
    public class Tentacles : Script
    {
        public Model SplineModel;
        public float TentacleLength = 140f;
        public float TipGroundRadius = 15f;

        /// <summary>
        /// How many seconds does it take to move a tentacle from one position to the next
        /// </summary>
        public float TentacleMoveTime = 1f;

        private readonly List<Spline> _splines = new List<Spline>();
        private readonly List<IKJoint> _rootJoints = new List<IKJoint>();
        private readonly List<Actor> _jointTips = new List<Actor>();
        private readonly List<Vector3> _targetDirections = new List<Vector3>();
        private readonly List<TargetPosition> _targetPositions = new List<TargetPosition>();
        private readonly List<Tentacle> _tentacles = new List<Tentacle>();

        private class Tentacle
        {
            private readonly Tentacles _tentacles;

            /// <summary>
            /// The tentacle spline
            /// </summary>
            public readonly Spline Spline;

            /// <summary>
            /// The root joint of the IK chain
            /// </summary>
            public readonly IKJoint RootJoint;

            /// <summary>
            /// The actor at the end of the IK chain
            /// </summary>
            public readonly Actor JointTip;

            /// <summary>
            /// The previous target position
            /// </summary>
            public Vector3 LastTargetPosition;

            /// <summary>
            /// The current target position
            /// </summary>
            public Vector3 TargetPosition;

            /// <summary>
            /// If the tentacle is not <see cref="OnGround"/>, then this
            /// tells you when the tencle started moving
            /// </summary>
            public float MovementStartTime;

            /// <summary>
            /// If the tentacle is on the ground
            /// </summary>
            public bool OnGround;

            /// <summary>
            /// The tentacle target direction, used for raycasting possible tentacle spots
            /// </summary>
            public readonly Vector3 TargetDirection;

            public Tentacle(Tentacles tentaclesScript, Spline spline, IKJoint rootJoint, Actor jointTip, Vector3 position, Vector3 targetDirection)
            {
                _tentacles = tentaclesScript;

                // TODO: Maybe create the IK Joints based on the spline?
                Spline = spline;
                RootJoint = rootJoint;
                JointTip = jointTip;
                LastTargetPosition = TargetPosition = position;
                MovementStartTime = Time.GameTime;
                OnGround = false;
                TargetDirection = targetDirection;
            }

            public void SetNewTarget(Vector3 position)
            {
                LastTargetPosition = TargetPosition;
                TargetPosition = position;
                if (OnGround)
                {
                    MovementStartTime = Time.GameTime;
                    OnGround = false;
                }
                else
                {
                    // TODO: We're setting a new target while the tencle is moving.
                    // We could simply set the movementstarttime, but that'd break effects like "raise tencle according to sine wave during movement"
                }
            }

            // TODO: After movement, update the OnGround flag depending on how far away from the ground the tencle is

            private bool ShouldMove()
            {
                bool targetTooFar = OnGround && (TargetPosition - RootJoint.Actor.Position).Length > _tentacles.TentacleLength * 0.9f;
                bool groundedTooFar = OnGround && (TargetPosition - JointTip.Position).Length > _tentacles.TipGroundRadius;

                return targetTooFar || groundedTooFar;
            }

            public void UpdateInverseKinematics()
            {
                // TODO: Replace TargetPosition with the calculated target position
                Vector3 target = TargetPosition;
                for (int j = 0; j < 1; j++) // Ik iterations
                {
                    RootJoint.Evaluate(JointTip, ref target);
                }
            }

            public void UpdateSpline()
            {
                // Stabilize tentacles that are on the ground
                Vector3 splineOffset = Vector3.Zero;
                if (OnGround)
                {
                    // TODO: Replace TargetPosition with the calculated target position
                    splineOffset = TargetPosition - JointTip.Position;
                }

                var ikActor = JointTip;
                int splinePointCount = Spline.SplinePointsCount;
                for (int i = splinePointCount - 1; i >= 0 && ikActor != null; i--)
                {
                    Spline.SetSplinePoint(i, ikActor.Position + splineOffset / splinePointCount, false);
                    ikActor = ikActor.Parent;
                }

                Spline.UpdateSpline(); // TODO: Check out how slow this is
                Spline.SetTangentsSmooth(); // TODO: Check out how slow this is
            }
        }

        private class TargetPosition
        {
            public Vector3 LastPosition;
            public Vector3 Position;
            public float StartTime;

            public TargetPosition(Vector3 position, float gameTime)
            {
                LastPosition = Position = position;
                StartTime = gameTime;
            }

            public void SetNewPosition(Vector3 position, float gameTime)
            {
                LastPosition = Position;
                Position = position;
                StartTime = gameTime;
            }

            public Vector3 GetLerpedPosition(float gameTime, float moveTime)
            {
                float t = Mathf.Saturate((gameTime - StartTime) / moveTime);
                Vector3.Lerp(ref LastPosition, ref Position, Mathf.InterpEaseOut(0, 1, t, 3), out Vector3 result);
                // slightly raise legs during movement
                // TODO: Fix this
                //result += Vector3.Up * Mathf.Sin(t * Mathf.Pi) * 50f;
                return result;
            }
        }

        public override void OnEnable()
        {
            //Int2 size = new Int2(2, 2);
            Int2 size = new Int2(8, 8);
            int numberOfSegments = 3;
            float segmentLength = TentacleLength / (numberOfSegments - 1);
            for (int i = 0; i < size.X; i++)
            {
                bool isEven = i % 2 == 0;
                for (int j = isEven ? 0 : -1; j < size.Y; j++)
                {
                    Vector2 position = new Vector2(i - size.X / 2f, j + (isEven ? 0 : 0.5f) - size.Y / 2f);

                    if (position.Length < size.X / 2f) // Good enough for now
                    {
                        float yOffset = 5 * position.Length;

                        var spline = Actor.AddChild<Spline>();
                        spline.StaticFlags = StaticFlags.None;
                        //   spline.HideFlags = HideFlags.FullyHidden;
                        spline.LocalPosition = new Vector3(position.X * 10f, yOffset, position.Y * 10f);
                        spline.LocalOrientation = Quaternion.RotationX(Mathf.PiOverTwo);
                        _splines.Add(spline);

                        for (int k = 0; k < 3; k++)
                        {
                            spline.AddSplineLocalPoint(
                                new Transform(
                                        new Vector3(position.X * k * 2.0f, position.Y * k, segmentLength * k),
                                        Quaternion.Identity,
                                        new Vector3(1)
                                    ), false);
                        }
                        spline.SetTangentsSmooth();

                        var splineModel = spline.AddChild<SplineModel>();
                        spline.StaticFlags = StaticFlags.None;
                        splineModel.PreTransform = new Transform(
                             new Vector3(0, 0, 0),
                             Quaternion.RotationX(Mathf.PiOverTwo),
                             new Vector3(0.04f, 1f, 0.04f)
                         );
                        splineModel.Model = SplineModel;

                        spline.UpdateSpline();

                        var ikJoint = Actor.AddChild<EmptyActor>().AddScript<IKJoint>();
                        ikJoint.Actor.StaticFlags = StaticFlags.None;
                        ikJoint.Actor.LocalPosition = new Vector3(position.X * 10f, yOffset, position.Y * 10f);
                        ikJoint.Enabled = false;
                        ikJoint.ClampToCone = true;
                        ikJoint.ConstraintAxis = Vector3.Down; // TODO: Move a bit to the outside
                        ikJoint.MaxAngle = 60;

                        var secondIkJoint = ikJoint.Actor.AddChild<EmptyActor>().AddScript<IKJoint>();
                        secondIkJoint.Actor.StaticFlags = StaticFlags.None;
                        secondIkJoint.Actor.LocalPosition = new Vector3(position.X * 2.0f, -segmentLength, position.Y);
                        secondIkJoint.Enabled = false;
                        secondIkJoint.ClampToCone = true;
                        secondIkJoint.ConstraintAxis = Vector3.Down; // TODO: Move a bit to the outside
                        secondIkJoint.MaxAngle = 60;

                        var ikTip = secondIkJoint.Actor.AddChild<EmptyActor>();
                        ikTip.StaticFlags = StaticFlags.None;
                        ikTip.LocalPosition = new Vector3(position.X * 2.0f, -segmentLength, position.Y);

                        _targetPositions.Add(new TargetPosition(ikTip.Position, Time.GameTime));

                        // TODO: Improve this (the enable dance)
                        Scripting.InvokeOnUpdate(() =>
                        {
                            secondIkJoint.Enabled = true;
                            ikJoint.Enabled = true;
                        });

                        _rootJoints.Add(ikJoint);
                        _jointTips.Add(ikTip);

                        _targetDirections.Add(new Vector3(position.X * 2f, -15f, position.Y * 2f).Normalized);
                    }
                }
            }
        }

        public override void OnDisable()
        {
            _splines.Clear();
            _rootJoints.Clear();
            _jointTips.Clear();
            _targetDirections.Clear();
            _targetPositions.Clear();
            _tentacles.Clear();
            Actor.DestroyChildren();
        }

        public override void OnFixedUpdate()
        {
            // TODO: There is no order guarantee, so the actor might move before/after this gets called https://docs.flaxengine.com/manual/scripting/events.html#order

            float time = Time.GameTime;
            for (int i = 0; i < _rootJoints.Count; i++)
            {
                Vector3 target = _targetPositions[i].GetLerpedPosition(time, TentacleMoveTime);
                for (int j = 0; j < 1; j++)
                {
                    _rootJoints[i].Evaluate(_jointTips[i], ref target);
                }
            }

            for (int i = 0; i < _rootJoints.Count; i++)
            {
                _splines[i].SetSplinePoint(0, _rootJoints[i].Actor.Position, false);
                _splines[i].SetSplinePoint(1, _rootJoints[i].ChildJoint?.Actor?.Position ?? Vector3.Zero, false);
                _splines[i].SetSplinePoint(2, StabilizeTentacleTip(_targetPositions[i], _jointTips[i].Position, time), false);
                _splines[i].UpdateSpline(); // TODO: Check out how slow this is
                _splines[i].SetTangentsSmooth(); // TODO: Check out how slow this is
            }
        }

        /// <summary>
        /// Visually stabilizes the tip of a tentacle
        /// </summary>
        private Vector3 StabilizeTentacleTip(TargetPosition targetPosition, Vector3 jointTipPosition, float time)
        {
            Vector3 target = targetPosition.GetLerpedPosition(time, TentacleMoveTime);

            if (Vector3.Distance(ref target, ref jointTipPosition) < 15f)
            {
                return target;
            }

            return jointTipPosition;
        }

        public override void OnUpdate()
        {
            float time = Time.GameTime;
            for (int i = 0; i < _targetPositions.Count; i++)
            {
                Vector3 jointTipPosition = _jointTips[i].Position;


                // TODO: Hmmm, would a sphere check work? (check if anything is in the bounding sphere of the tencle, move point there)
                // TODO: Handle walls (later)

                // TODO: Prevent tencles from just being sad & floaty 
                // TODO: Prevent tencles from moving if nearby tencles are moving
                if (Physics.RayCast(_rootJoints[i].Actor.Position, _targetDirections[i], out var hit, TentacleLength, layerMask: ~(1U << 1)))
                {
                    if (Vector3.Distance(ref jointTipPosition, ref hit.Point) > TipGroundRadius)
                    {
                        _targetPositions[i].SetNewPosition(hit.Point, time);
                    }
                }
                else
                {
                    // TODO: Handle the case where the raycast didn't hit anything (slowly lower the tencle?)
                }

                // TODO: Detect if a leg is on the ground
                // TODO: Calculate the leg velocity. If it's in the air and the velocity is too low, try giving the leg a closer target

                // TODO: Use raycasts every other frame
                //       and in the frames where we aren't using raycasts, just move the target positions that aren't on the ground by the actor position
            }


            // Update the splines
            for (int i = 0; i < _rootJoints.Count; i++)
            {
                _splines[i].SetSplinePoint(0, _rootJoints[i].Actor.Position, false);
                _splines[i].SetSplinePoint(1, _rootJoints[i].ChildJoint?.Actor?.Position ?? Vector3.Zero, false);
                _splines[i].SetSplinePoint(2, _jointTips[i].Position, false);
                _splines[i].UpdateSpline(); // TODO: Check out how slow this is
                _splines[i].SetTangentsSmooth(); // TODO: Check out how slow this is
            }
        }

        public override void OnDebugDrawSelected()
        {
            for (int i = 0; i < _rootJoints.Count; i++)
            {
                DebugDraw.DrawLine(_rootJoints[i].Actor.Position, _rootJoints[i].Actor.Position + _targetDirections[i] * 25f, Color.Red);
            }
        }
    }
}
