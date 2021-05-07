using System;
using System.Collections.Generic;
using FlaxEngine;

namespace Game
{
    public class Tentacles : Script
    {
        public Model SplineModel;
        public float TentacleLength = 145f;
        public float TipGroundRadius = 15f;

        /// <summary>
        /// How many seconds does it take to move a tentacle from one position to the next
        /// </summary>
        public float TentacleMoveTime = 1f;

        private readonly List<Tentacle> _tentacles = new List<Tentacle>();
        private Vector3 _previousPosition;

        private struct Tween
        {
            public Vector3 Start;
            public Vector3 End;
            public float StartTime;
            public float Duration;
        }

        private class Tentacle
        {
            private readonly Tentacles _tentacles;
            private readonly List<Tween> _tweens = new List<Tween>();

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

            // TODO: Calculate the leg velocity. If it's in the air and the velocity is too low, try giving the leg a closer target

            public Tentacle(Tentacles tentaclesScript, Spline spline, IKJoint rootJoint, Actor jointTip, Vector3 position, Vector3 targetDirection)
            {
                _tentacles = tentaclesScript;

                // TODO: Maybe create the IK Joints based on the spline?
                Spline = spline;
                RootJoint = rootJoint;
                JointTip = jointTip;
                TargetPosition = position;
                MovementStartTime = Time.GameTime;
                OnGround = false;
                TargetDirection = targetDirection;
            }

            public void SetNewTarget(Vector3 position)
            {
                _tweens.Add(new Tween()
                {
                    Start = JointTip.Position,
                    End = position,
                    StartTime = Time.GameTime,
                    Duration = _tentacles.TentacleMoveTime
                });

                TargetPosition = position;
                if (OnGround)
                {
                    OnGround = false;
                }
                else
                {
                    // TODO: We're setting a new target while the tencle is moving.
                    // We could simply set the movementstarttime, but that'd break effects like "raise tencle according to sine wave during movement"
                }
            }

            public bool ShouldMove()
            {
                bool targetTooFar = OnGround && (TargetPosition - RootJoint.Actor.Position).Length > _tentacles.TentacleLength;
                bool groundedTooFar = OnGround && (TargetPosition - JointTip.Position).Length > _tentacles.TipGroundRadius;

                return targetTooFar || groundedTooFar;
            }

            public void UpdateInverseKinematics()
            {
                Vector3 target = GetLerpedPosition(Time.GameTime);
                for (int j = 0; j < 1; j++) // Ik iterations
                {
                    RootJoint.Evaluate(JointTip, ref target);
                }

                // After movement, update the OnGround flag depending on how far away from the ground the tencle is
                if (!OnGround)
                {
                    OnGround = (TargetPosition - JointTip.Position).Length <= _tentacles.TipGroundRadius;
                }
            }

            public void UpdateSpline()
            {
                // Stabilize tentacles that are on the ground
                Vector3 splineOffset = Vector3.Zero;
                if (OnGround)
                {
                    // TODO: Replace TargetPosition with the calculated target position
                    splineOffset = (TargetPosition - JointTip.Position) / (float)(Spline.SplinePointsCount - 1);
                }

                var ikActor = JointTip;
                for (int i = Spline.SplinePointsCount - 1; i >= 0 && ikActor != null; i--)
                {
                    Spline.SetSplinePoint(i, ikActor.Position + splineOffset, false);
                    ikActor = ikActor.Parent;
                }
                Spline.SetSplinePoint(0, RootJoint.Actor.Position, false);

                Spline.UpdateSpline(); // TODO: Check out how slow this is
                Spline.SetTangentsSmooth(); // TODO: Check out how slow this is
            }

            // TODO: Thoroughly think through the lerping
            private Vector3 GetLerpedPosition(float time)
            {
                if (_tweens.Count == 0) return TargetPosition;

                bool firstIteration = true;
                Vector3 result = Vector3.Zero;
                for (int i = 0; i < _tweens.Count; i++)
                {
                    float t = Mathf.Saturate((time - _tweens[i].StartTime) / _tweens[i].Duration);
                    var start = _tweens[i].Start;
                    var end = _tweens[i].End;
                    Vector3.Lerp(ref start, ref end, Mathf.InterpEaseOut(0, 1, t, 3), out Vector3 tweenResult);

                    // slightly raise legs during movement
                    //tweenResult += Vector3.Up * Mathf.Sin(t * Mathf.Pi) * 50f;

                    if (firstIteration)
                    {
                        firstIteration = false;
                        result = tweenResult;
                    }
                    else
                    {
                        float inbetweenDuration = _tweens[i].Duration * 0.1f;
                        float inbetweenT = Mathf.Saturate((time - _tweens[i].StartTime) / inbetweenDuration);
                        Vector3.Lerp(ref result, ref tweenResult, inbetweenT, out result);
                    }
                }

                for (int i = _tweens.Count - 1; i >= 0; i--)
                {
                    if (_tweens[i].StartTime + _tweens[i].Duration < time)
                    {
                        //_tweens.RemoveAt(i);
                    }
                }

                return result;
            }
        }

        public override void OnEnable()
        {
            _previousPosition = Actor.Position;

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

                    if (position.Length < size.X / 2f)
                    {
                        float yOffset = 5 * position.Length;

                        var spline = Actor.AddChild<Spline>();
                        spline.StaticFlags = StaticFlags.None;
                        spline.HideFlags = HideFlags.FullyHidden;
                        spline.LocalPosition = new Vector3(position.X * 10f, yOffset, position.Y * 10f);
                        spline.LocalOrientation = Quaternion.RotationX(Mathf.PiOverTwo);

                        for (int k = 0; k < numberOfSegments; k++)
                        {
                            spline.AddSplineLocalPoint(
                                new Transform(
                                        new Vector3(position.X * k * 2.0f, position.Y * k, segmentLength * k),
                                        Quaternion.Identity,
                                        new Vector3(1 - (k / (float)numberOfSegments) * 0.5f)
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
                        ikJoint.Enabled = false;
                        //ikJoint.Actor.HideFlags = HideFlags.FullyHidden;
                        ikJoint.Actor.StaticFlags = StaticFlags.None;
                        ikJoint.Actor.LocalPosition = new Vector3(position.X * 10f, yOffset, position.Y * 10f);
                        ikJoint.ClampToCone = true;
                        ikJoint.ConstraintAxis = Vector3.Down; // TODO: Move a bit to the outside
                        ikJoint.MaxAngle = 100;

                        var rootIkJoint = ikJoint;

                        Scripting.InvokeOnUpdate(() => rootIkJoint.Enabled = true);

                        for (int k = 1; k < numberOfSegments - 1; k++)
                        {
                            var childIkJoint = ikJoint.Actor.AddChild<EmptyActor>().AddScript<IKJoint>();
                            childIkJoint.Enabled = false;
                            childIkJoint.Actor.StaticFlags = StaticFlags.None;
                            childIkJoint.Actor.LocalPosition = new Vector3(position.X * 2.0f, -segmentLength, position.Y);
                            childIkJoint.ClampToCone = true;
                            childIkJoint.ConstraintAxis = Vector3.Down; // TODO: Move a bit to the outside
                            childIkJoint.MaxAngle = 100;
                            ikJoint = childIkJoint;

                            Scripting.InvokeOnUpdate(() => childIkJoint.Enabled = true);
                        }

                        var ikTip = ikJoint.Actor.AddChild<EmptyActor>();
                        ikTip.StaticFlags = StaticFlags.None;
                        ikTip.LocalPosition = new Vector3(position.X * 2.0f, -segmentLength, position.Y);

                        var targetDirection = new Vector3(position.X * 2f, -15f, position.Y * 2f).Normalized;

                        _tentacles.Add(new Tentacle(this, spline, rootIkJoint, ikTip, ikTip.Position, targetDirection));
                    }
                }
            }
        }

        public override void OnDisable()
        {
            _tentacles.Clear();
            Actor.DestroyChildren();
        }

        // There is no order guarantee, so the actor might move before/after this gets called https://docs.flaxengine.com/manual/scripting/events.html#order
        // Thus, we have to do this
        public void CustomFixedUpdate()
        {
            Vector3 positionDelta = Actor.Position - _previousPosition;
            _previousPosition = Actor.Position;

            for (int i = 0; i < _tentacles.Count; i++)
            {
                _tentacles[i].UpdateInverseKinematics();
            }

            for (int i = 0; i < _tentacles.Count; i++)
            {
                // Move target while we are in the air
                if (!_tentacles[i].OnGround)
                {
                    _tentacles[i].SetNewTarget(_tentacles[i].TargetPosition + positionDelta);
                    // TODO: Use raycasts every other frame
                    //       and in the frames where we aren't using raycasts, just move the target positions that aren't on the ground by the actor position
                }
                // If the tentacle needs to be moved
                else if (_tentacles[i].ShouldMove()) // TODO: ShouldMove could also do a raycast to find potential targets
                {
                    var targetDirection = Actor.Transform.TransformDirection(_tentacles[i].TargetDirection);
                    if (Physics.RayCast(_tentacles[i].RootJoint.Actor.Position, targetDirection, out var hit, TentacleLength, layerMask: ~(1U << 1)))
                    {
                        _tentacles[i].SetNewTarget(hit.Point);
                    }
                    else
                    {
                        // TODO: Handle the case where the raycast didn't hit anything (slowly lower the tencle?)
                    }
                }

                // TODO: Handle walls (later)
                // TODO: Prevent tencles from just being sad & floaty 
                // TODO: Prevent tencles from moving if nearby tencles are moving
            }

            // Required to prevent the tentacle tips from being slidy and delayed
            for (int i = 0; i < _tentacles.Count; i++)
            {
                _tentacles[i].UpdateSpline();
            }
        }

        public override void OnUpdate()
        {
            for (int i = 0; i < _tentacles.Count; i++)
            {
                _tentacles[i].UpdateSpline();
            }
        }

        public override void OnDebugDraw()
        {
            for (int i = 0; i < _tentacles.Count; i++)
            {
                DebugDraw.DrawWireSphere(new BoundingSphere(_tentacles[i].TargetPosition, 5f), Color.Red);
            }
        }
    }
}
