using System;
using System.Collections.Generic;
using System.IO;
using FlaxEngine;

namespace Game
{
    public class VectorGraphics
    {
        public class LineSegment
        {
            public Vector2 Start;
            public Vector2 End; // Possibly refactor this into a "next" pointer

            // Pawsibly refactor those into "faces"
            // But for now, keep them super general so that we can try out funky, impossible things :3
            public Color LeftColor;
            public byte LeftFaceLayer;
            public Color RightColor;
            public byte RightFaceLayer;
        }

        public static readonly int NumberOfLayers = 4;

        [Serialize]
        public Int2 Size { get; set; } = new Int2(64, 64);

        [Serialize]
        public float FadeStrength { get; set; } = 1;

        [Serialize]
        public List<LineSegment> LineSegments { get; set; } = new List<LineSegment>();

        public unsafe void GenerateTexture()
        {
            var texture = Content.CreateVirtualAsset<Texture>();
            try
            {
                TextureBase.InitData initData;
                initData.Width = Size.X;
                initData.Height = Size.Y;
                initData.ArraySize = 1;
                initData.Format = PixelFormat.R8G8B8A8_UNorm;

                byte[] data = new byte[initData.Width * initData.Height * PixelFormatExtensions.SizeInBytes(initData.Format)];
                float[] distances = new float[NumberOfLayers];
                Color[] colors = new Color[NumberOfLayers];

                fixed (byte* dataPtr = data)
                {
                    var colorsPtr = (Color32*)dataPtr;
                    for (int y = 0; y < initData.Height; y++)
                    {
                        for (int x = 0; x < initData.Width; x++)
                        {
                            Vector2 point = new Vector2(x, y);
                            CalculateDistancesAtPoint(ref point, distances, colors);

                            colorsPtr[(y * initData.Width) + x] = new Color32(
                                ClampToByte(distances[0] * FadeStrength),
                                ClampToByte(distances[1] * FadeStrength),
                                ClampToByte(distances[2] * FadeStrength),
                                ClampToByte(distances[3] * FadeStrength)
                            );
                            // TODO: Colors
                        }
                    }
                }

                initData.Mips = new[]
                {
                    new TextureBase.InitData.MipData
                    {
                        Data = data,
                        RowPitch = data.Length / initData.Height,
                        SlicePitch = data.Length
                    }
                };
                texture.Init(ref initData);
                texture.WaitForLoaded();
                texture.Save(Path.Combine(Globals.ProjectContentFolder, "MyTexture.flax")); // TODO: Delete/move previous texture?
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            FlaxEngine.Object.Destroy(texture);
        }

        private byte ClampToByte(float value)
        {
            return (byte)Mathf.Floor(Mathf.Clamp(value, 0, byte.MaxValue));
        }

        /// <summary>
        /// Which side of the line is the point on?
        /// </summary>
        public float PointLineSide(ref Vector2 point, ref Vector2 start, ref Vector2 end)
        {
            return (end.X - start.X) * (point.Y - start.Y) - (end.Y - start.Y) * (point.X - start.X);
        }

        /// <summary>
        /// Calculate stuff at a given point
        /// </summary>
        /// <param name="point">The point</param>
        /// <param name="distances">The distances</param>
        /// <param name="colors">The colors</param>
        public void CalculateDistancesAtPoint(ref Vector2 point, float[] distances, Color[] colors)
        {
            float minDistance = float.PositiveInfinity;
            byte minLayer = 0;
            Color minColor = Color.Black;

            for (int i = 0; i < distances.Length; i++)
            {
                distances[i] = float.PositiveInfinity;
            }

            // Find the closest distance for every layer
            for (int i = 0; i < LineSegments.Count; i++)
            {
                var segment = LineSegments[i];
                CollisionsHelper.ClosestPointPointLine(ref point, ref segment.Start, ref segment.End, out Vector2 closestPoint);
                float distance = Vector2.Distance(ref point, ref closestPoint);

                bool isRight = PointLineSide(ref point, ref segment.Start, ref segment.End) >= 0;

                byte layer = isRight ? segment.LeftFaceLayer : segment.RightFaceLayer;

                if (distance < distances[layer])
                {
                    distances[layer] = distance;
                    colors[layer] = isRight ? segment.LeftColor : segment.RightColor; // TODO: Only use a single color texture

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        minLayer = isRight ? segment.RightFaceLayer : segment.LeftFaceLayer;
                        minColor = isRight ? segment.RightColor : segment.LeftColor;
                    }
                }
            }

            // I'm inside the min-distance layer. Color me fullbright
            if (minDistance < float.PositiveInfinity)
            {
                distances[minLayer] = 0; // This will be black, because we're working with an 'inverted' texture
                colors[minLayer] = minColor;
            }

            // Original algorithm idea:
            // 1. Find closest line segment
            // 2. Compute distance to it
            // 3. Take the color & layer on the *other* side
            // 3.5 I'm inside the line. Color this layer properly (fullbright)
            // 4. Take the color & layer on *this* side
            // 4.5 I'm away from the line. Color that layer according to the distance
            // 5. Find the distances to the other layers
        }


        /*
         * Point in Polygon
         * Taken from https://wrf.ecse.rpi.edu/Research/Short_Notes/pnpoly.html
         * Copyright (c) 1970-2003, Wm. Randolph Franklin
         *
         * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files 
         * (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, 
         * publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
         * subject to the following conditions:
         *
         * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimers.
         * Redistributions in binary form must reproduce the above copyright notice in the documentation and/or other materials provided with the distribution.
         * The name of W. Randolph Franklin may not be used to endorse or promote products derived from this Software without specific prior written permission. 
         *
         * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
         * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
         * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
         * IN THE SOFTWARE. 
         */
        private bool PointInPolygon(Vector2[] vertices, Vector2 point)
        {
            bool c = false;
            for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
            {
                if (
                    ((vertices[i].Y > point.Y) != (vertices[j].Y > point.Y)) &&
                      (point.X < ((vertices[j].X - vertices[i].X) * (point.Y - vertices[i].Y) / (vertices[j].Y - vertices[i].Y)) + vertices[i].X)
                   )
                {
                    c = !c;
                }
            }
            return c;
        }

        /*private bool PointInPolygon(List<LineSegment> segments, Vector2 point)
        {
            uint layers = 0;
            bool c = false;
            for (int i = 0; i < segments.Count; i++)
            {
                Vector2 start = segments[i].Start;
                Vector2 end = segments[i].End;
                if (
                    ((end.Y > point.Y) != (start.Y > point.Y)) &&
                      (point.X < ((start.X - end.X) * (point.Y - end.Y) / (start.Y - end.Y)) + end.X)
                   )
                {
                    segments[i].
                    c = !c;
                }
            }
            return c;
        }*/
    }
}
