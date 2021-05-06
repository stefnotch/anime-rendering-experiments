using FlaxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    // Some references:
    // https://zalo.github.io/blog/inverse-kinematics/
    // https://github.com/zalo/MathUtilities/blob/master/Assets/IK/CCDIK/CCDIKJoint.cs#L10
    // TODO: prismatic joints?
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
    }
}
