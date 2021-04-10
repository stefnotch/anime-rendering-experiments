using System;
using System.Collections.Generic;
using FlaxEngine;

namespace Game
{
    public class Tentacles : Script
    {
        public Model SplineModel;

        private List<Spline> _splines = new List<Spline>();

        public override void OnEnable()
        {
            Int2 size = new Int2(8, 8);
            for (int i = 0; i < size.X; i++)
            {
                bool isEven = i % 2 == 0;
                for (int j = isEven ? 0 : -1; j < size.Y; j++)
                {
                    Vector2 position = new Vector2(i - size.X / 2f, j + (isEven ? 0 : 0.5f) - size.Y / 2f);

                    if (position.Length < size.X / 2f) // Good enough for now
                    {
                        var spline = Actor.AddChild<Spline>();
                        spline.RemoveStaticFlags(StaticFlags.Transform);
                        //spline.HideFlags = HideFlags.FullyHidden;
                        spline.LocalPosition = new Vector3(position.X * 10f, 0f, position.Y * 10f);
                        _splines.Add(spline);

                        for (int k = 0; k < 4; k++)
                        {
                            spline.AddSplineLocalPoint(new Vector3(0f, -40f * k, 0f));
                        }
                        spline.SetTangentsSmooth();

                        var splineModel = spline.AddChild<SplineModel>();
                        spline.RemoveStaticFlags(StaticFlags.Transform);
                        splineModel.PreTransform = new Transform(
                             new Vector3(0, 0, 300f),
                             Quaternion.RotationX(Mathf.PiOverTwo),
                             new Vector3(0.05f, 1f, 0.05f)
                         );
                        splineModel.Model = SplineModel;
                    }
                }
            }
        }

        public override void OnDisable()
        {
            for (int i = 0; i < _splines.Count; i++)
            {
                Destroy(_splines[i]);
            }
            _splines.Clear();
        }

        public override void OnUpdate()
        {
            // Here you can add code that needs to be called every frame
        }
    }
}
