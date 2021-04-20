using FlaxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class ElTesterino : Script
    {
        [EditorOrder(0)]
        public Vector3 InputVec;

        [EditorOrder(10)]
        public Vector3 ConeVec;

        [EditorOrder(20)]
        public float alpha;

        public override void OnDebugDraw()
        {
            float d = Mathf.Cos(alpha * Mathf.DegreesToRadians);

            Vector3 inputVec = InputVec.Normalized;
            Vector3 coneVec = ConeVec.Normalized;

            // Aw, I want pushtransform for debugdraw
            // Aw, I want automatic intersection finding for debugdraw


            DebugDraw.DrawWireSphere(new BoundingSphere(Vector3.Zero, 1f), Color.White);
            DebugDraw.DrawSphere(new BoundingSphere(Vector3.Zero, 0.05f), Color.White);
            DebugDraw.DrawLine(Vector3.Zero, inputVec, Color.Blue);
            DebugDraw.DrawLine(Vector3.Zero, coneVec, Color.Green);
            DebugDraw.DrawSphere(new BoundingSphere(coneVec * d, 0.05f), Color.LightGreen);
            for (int i = 0; i < 10; i++)
            {
                DebugDraw.DrawCircle(coneVec * d, coneVec, i / 10f, Color.LightGreen);
            }

            float dotProduct = Vector3.Dot(inputVec, coneVec);

            if (dotProduct < d)
            {
                // nothing to do
            }
            else
            {
                // clamp inputvec
            }

            Vector3 clampedTo90Deg = (inputVec - coneVec * dotProduct);
            //clampedTo90Deg.Normalize();

            DebugDraw.DrawLine(inputVec, clampedTo90Deg, Color.Purple);
            DebugDraw.DrawLine(clampedTo90Deg, (clampedTo90Deg + coneVec * d).Normalized, Color.PeachPuff);

            // return normalized result
        }
    }
}
