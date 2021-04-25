using System;
using System.Collections.Generic;
using FlaxEngine;

namespace Game
{
    public class Tentacles : Script
    {
        public Model SplineModel;
        public float ReachRadius;

        /// <summary>
        /// How many seconds does it take to move a tentacle from one position to the next
        /// </summary>
        private static readonly float TentacleMoveSpeed = 0.25f;
        private List<Spline> _splines = new List<Spline>();
        private List<IKJoint> _rootJoints = new List<IKJoint>();
        private List<Actor> _jointTips = new List<Actor>();
        private List<Vector3> _targetDirections = new List<Vector3>();
        private List<TargetPosition> _targetPositions = new List<TargetPosition>();

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

            public Vector3 GetLerpedPosition(float gameTime)
            {
                Vector3.Lerp(ref LastPosition, ref Position, Mathf.Saturate((gameTime - StartTime) / TentacleMoveSpeed), out Vector3 result);
                return result;
            }
        }

        public override void OnEnable()
        {
            //Int2 size = new Int2(2, 2);
            Int2 size = new Int2(8, 8);
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
                            spline.AddSplineLocalPoint(new Vector3(position.X * k * 2.0f, position.Y * k, 60f * k), false);
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
                        ikJoint.MaxAngle = 45;

                        var secondIkJoint = ikJoint.Actor.AddChild<EmptyActor>().AddScript<IKJoint>();
                        secondIkJoint.Actor.StaticFlags = StaticFlags.None;
                        secondIkJoint.Actor.LocalPosition = new Vector3(position.X * 2.0f, -60f, position.Y);
                        secondIkJoint.Enabled = false;
                        secondIkJoint.ClampToCone = true;
                        secondIkJoint.ConstraintAxis = Vector3.Down; // TODO: Move a bit to the outside
                        secondIkJoint.MaxAngle = 45;

                        var ikTip = secondIkJoint.Actor.AddChild<EmptyActor>();
                        ikTip.StaticFlags = StaticFlags.None;
                        ikTip.LocalPosition = new Vector3(position.X * 2.0f, -60f, position.Y);

                        _targetPositions.Add(new TargetPosition(ikTip.Position, Time.GameTime));

                        // TODO: Improve this (the enable dance)
                        Scripting.InvokeOnUpdate(() =>
                        {
                            secondIkJoint.Enabled = true;
                            ikJoint.Enabled = true;
                        });

                        _rootJoints.Add(ikJoint);
                        _jointTips.Add(ikTip);

                        _targetDirections.Add(new Vector3(position.X * 2f, -20f, position.Y * 2f).Normalized);
                    }
                }
            }
        }

        public override void OnDisable()
        {
            _splines.Clear();
            _rootJoints.Clear();
            Actor.DestroyChildren();
        }

        public override void OnFixedUpdate()
        {
            float time = Time.GameTime;
            for (int i = 0; i < _rootJoints.Count; i++)
            {
                Vector3 target = _targetPositions[i].GetLerpedPosition(time);

                _rootJoints[i].Evaluate(_jointTips[i], ref target);
            }
        }

        public override void OnUpdate()
        {
            float time = Time.GameTime;
            Vector3 actorPosition = Actor.Position;
            for (int i = 0; i < _targetPositions.Count; i++)
            {
                Vector3 target = _targetPositions[i].Position;

                if (Vector3.Distance(ref target, ref actorPosition) > ReachRadius)
                {
                    // TODO: Make the direction and starting position a bit more random/dependant on stuff
                    if (Physics.RayCast(_rootJoints[i].Actor.Position, _targetDirections[i], out var hit, 400f, layerMask: ~(1U << 1)))
                    {
                        _targetPositions[i].SetNewPosition(hit.Point, time);
                    }
                }
            }


            // Update the splines
            for (int i = 0; i < _rootJoints.Count; i++)
            {
                _splines[i].SetSplinePoint(0, _rootJoints[i].Actor.Position, false);
                _splines[i].SetSplinePoint(1, _rootJoints[i].ChildJoint?.Actor?.Position ?? Vector3.Zero, false);
                _splines[i].SetSplinePoint(2, _jointTips[i].Position, false);

                _splines[i].UpdateSpline();
            }
        }

        public override void OnDebugDrawSelected()
        {
            DebugDraw.DrawWireSphere(new BoundingSphere(Actor.Position, ReachRadius), Color.AliceBlue);
            for (int i = 0; i < _rootJoints.Count; i++)
            {
                DebugDraw.DrawLine(_rootJoints[i].Actor.Position, _rootJoints[i].Actor.Position + _targetDirections[i] * 25f, Color.Red);
            }
        }
    }
}
