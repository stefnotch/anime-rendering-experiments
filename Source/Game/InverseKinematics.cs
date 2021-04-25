using FlaxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class InverseKinematics
    {
        // Terrible name, suggest better one plez:
        public static Quaternion FromVectors(ref Vector3 from, ref Vector3 to)
        {
            // See https://web.archive.org/web/20210224054602/lolengine.net/blog/2014/02/24/quaternion-from-two-vectors-final
            float normUV = Mathf.Sqrt(from.LengthSquared * to.LengthSquared);
            Vector3.Dot(ref from, ref to, out var realPart);
            realPart += normUV;

            Vector3 normal;

            if (realPart < Mathf.Epsilon * normUV)
            {
                realPart = 0.0f;
                normal = Mathf.Abs(from.X) > Mathf.Abs(from.Z) ?
                    new Vector3(-from.Y, from.X, 0.0f) :
                    new Vector3(0.0f, -from.Z, from.Y);
            }
            else
            {
                Vector3.Cross(ref from, ref to, out normal);
            }

            var rotationQuaternion = new Quaternion(normal, realPart);
            rotationQuaternion.Normalize();
            return rotationQuaternion;
        }

        public static Quaternion FromUnitVectors(ref Vector3 from, ref Vector3 to)
        {
            // See https://web.archive.org/web/20210224054602/lolengine.net/blog/2014/02/24/quaternion-from-two-vectors-final
            Vector3.Cross(ref from, ref to, out var normal);
            Vector3.Dot(ref from, ref to, out var realPart);
            var rotationQuaternion = new Quaternion(normal, realPart);
            rotationQuaternion.W += rotationQuaternion.Length;
            rotationQuaternion.Normalize();
            return rotationQuaternion;
        }

        // Constrains a vector to a cone around some other vector
        // coneDirection should be normalized
        public static Vector3 ConstrainToCone(ref Vector3 direction, ref Vector3 coneDirection, float maxAngle)
        {
            if (maxAngle <= 0) return coneDirection * direction.Length;
            if (maxAngle >= Mathf.Pi) return direction;

            Vector3 unitDirection = direction.Normalized; // normalizing can sometimes be avoided (just pass in a normalized direction)

            float cutoffDotProduct = Mathf.Cos(maxAngle); // could be precomputed
            float coneRadius = Mathf.Sin(maxAngle); // could be precomputed

            // Oooooo, it gets stuck at that point. So that's why he intellicatly rotated it instead...
            float vectorDotProduct = Vector3.Dot(ref unitDirection, ref coneDirection);
            if (vectorDotProduct >= cutoffDotProduct) return direction;

            // Note: The case with the direction vector being the exact opposite of the coneDirection might need extra handling
            Vector3 clampedToPie = direction - coneDirection * vectorDotProduct;
            clampedToPie.Normalize();
            clampedToPie /= coneRadius;

            return clampedToPie + coneDirection * cutoffDotProduct + coneDirection * 0.01f; // (coneDirection * 0.01f) stupid hack to make it non-stuck-y
        }

        public static Vector3 ConstrainToConeUsingSlerp(ref Vector3 unitDirection, ref Vector3 coneDirection, float maxAngle)
        {
            if (maxAngle <= 0) return coneDirection;
            if (maxAngle >= Mathf.Pi) return unitDirection;

            // doesn't work very well if the two vectors have a ~180 degree angle, but that shouldn't happen anyways

            float cutoffDotProduct = Mathf.Cos(maxAngle); // could be precomputed
            float coneRadius = Mathf.Sin(maxAngle); // could be precomputed

            float dotProduct = Vector3.Dot(ref unitDirection, ref coneDirection);

            float angleDelta = Mathf.Acos(Mathf.Clamp(dotProduct, -1, 1)) - maxAngle;
            if (angleDelta <= 0) return unitDirection;

            // https://keithmaggio.wordpress.com/2011/02/15/math-magician-lerp-slerp-and-nlerp/
            // https://observablehq.com/@spattana/slerp-spherical-linear-interpolation?ui=next

            // Gets a vector that is perpendicular to the unitDirection
            // (It takes the end vector and removes the part that is parallel to the unitDirection)
            // https://i.stack.imgur.com/tm3Tx.png
            Vector3 perpendicularVector = coneDirection - unitDirection * dotProduct;
            perpendicularVector.Normalize();

            // The "- maxAngle" part guarantees that the dot products aren't the same
            //Debug.Log("cos " + Mathf.Cos(angleDelta) + "     " + dotProduct + "      " + (1 - dotProduct));
            //Debug.Log("sin " + Mathf.Sin(angleDelta) + "     " + Vector3.Dot(ref perpendicularVector, ref coneDirection));

            return (unitDirection * Mathf.Cos(angleDelta)) + (perpendicularVector * Mathf.Sin(angleDelta));
        }

        public struct IKJoint
        {
            public Transform LocalTransform;
            public Transform Transform;
        }

        public class IKJointsList
        {
            public Spline Spline;
            public IKJoint[] Joints;
            public IKJointsList(Spline spline)
            {
                Spline = spline;
                Joints = new IKJoint[spline.SplinePointsCount];
            }

            public void Update(ref Vector3 target)
            {
                IKJoint finalJoint = Joints[Joints.Length - 1];
                finalJoint.LocalTransform.Translation = target;
                // TODO: Set a decent rotation as well?
                Joints[Joints.Length - 1] = finalJoint;

                for (int i = Joints.Length - 2; i >= 0; i--)
                {
                    IKJoint joint = Joints[i];
                    IKJoint targetJoint = Joints[i + 1];

                    Vector3 directionToTip = targetJoint.LocalTransform.Translation - joint.LocalTransform.Translation;
                    Vector3 directionToTarget = target - joint.LocalTransform.Translation;

                    Quaternion rotation = FromVectors(ref directionToTip, ref directionToTarget);
                    joint.LocalTransform.Orientation *= rotation; // TODO: Is this the correct order?

                    // TODO: Constraints and stuff
                }

                Transform accumulatedTransform = Joints[0].LocalTransform;
                for (int i = 1; i < Joints.Length; i++)
                {
                    IKJoint joint = Joints[i];
                    joint.Transform = joint.LocalTransform.LocalToWorld(accumulatedTransform); // I hope that's in the correct order
                    Joints[i] = joint;
                }

                // And now we gotta update the spline
            }
        }

        /* public static void UpdateSpline(Spline spline, ref Vector3 target)
         {
             var pointsCount = spline.SplinePointsCount;
             for (int i = pointsCount - 1; i >= 1; i--)
             {


                 for (int j = i; j < pointsCount; j++)
                 {
                     spline.SetSplineTransform(
                         j,
                         new Transform(
                         spline.GetSplinePoint(j),
                         rotation
                         ),
                         false
                     );
                     //spline.SetSplineLocalTransform
                 }
             }

             spline.UpdateSpline();
         }*/
    }
}
