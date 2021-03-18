using FlaxEngine;
using FlaxEngine.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class VectorGraphicsControl : Control
    {
        private struct SelectedPoint
        {
            public VectorGraphics.LineSegment Segment;
            public bool IsEndSelected;
        }

        private Vector2 _mousePosition;
        private bool _isDraggingPoints;
        private readonly List<SelectedPoint> _selectedPoints = new List<SelectedPoint>();
        private Matrix3x3 _cachedTransform;
        private Matrix3x3 _cachedInverseTransform;

        public VectorGraphics VectorGraphics { get; set; }

        public override void OnMouseMove(Vector2 location)
        {
            base.OnMouseMove(location);

            _mousePosition = location;

            if (_isDraggingPoints)
            {
                Matrix3x3.Transform2D(ref location, ref _cachedInverseTransform, out var mousePos);

                for (int i = 0; i < _selectedPoints.Count; i++)
                {
                    if (_selectedPoints[i].IsEndSelected)
                    {
                        // TODO: Notify parent/whatever of a change
                        _selectedPoints[i].Segment.End = mousePos;
                    }
                    else
                    {
                        _selectedPoints[i].Segment.Start = mousePos;
                    }
                }
            }
        }

        public override bool OnMouseUp(Vector2 location, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                _isDraggingPoints = false;
            }
            return base.OnMouseUp(location, button);
        }

        public override bool OnMouseDown(Vector2 location, MouseButton button)
        {
            if (base.OnMouseDown(location, button)) return true;

            if (button == MouseButton.Left)
            {
                _isDraggingPoints = true;

                Matrix3x3.Transform2D(ref location, ref _cachedInverseTransform, out var mousePos);

                _selectedPoints.Clear();

                if (VectorGraphics?.LineSegments != null)
                {
                    for (int i = 0; i < VectorGraphics.LineSegments.Count; i++)
                    {
                        var segment = VectorGraphics.LineSegments[i];

                        if (Vector2.Distance(ref segment.Start, ref mousePos) < 5f)
                        {
                            _selectedPoints.Add(new SelectedPoint() { Segment = segment, IsEndSelected = false });
                        }
                        if (Vector2.Distance(ref segment.End, ref mousePos) < 5f)
                        {
                            _selectedPoints.Add(new SelectedPoint() { Segment = segment, IsEndSelected = true });
                        }
                    }
                }
            }
            return true;
        }

        private void UpdateTransforms()
        {
            _cachedTransform = Matrix3x3.Translation2D(new Vector2(15f));
            Matrix3x3.Invert(ref _cachedTransform, out _cachedInverseTransform);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            UpdateTransforms(); // Could be done more efficiently, eh, whatevs
        }

        public override void Draw()
        {
            var style = Style.Current;

            base.Draw();
            // Outline
            Render2D.DrawRectangle(new Rectangle(new Vector2(10f), Size - 20f), style.BorderSelected);

            Render2D.PushTransform(ref _cachedTransform);
            Render2D.PushClip(new Rectangle(new Vector2(-1), Size - 28f));

            Matrix3x3.Transform2D(ref _mousePosition, ref _cachedInverseTransform, out var mousePos);

            if (VectorGraphics?.LineSegments != null)
            {
                Render2D.DrawRectangle(new Rectangle(Vector2.Zero, (Vector2)VectorGraphics.Size), Color.White);

                for (int i = 0; i < VectorGraphics.LineSegments.Count; i++)
                {
                    var segment = VectorGraphics.LineSegments[i];

                    Render2D.DrawLine(segment.Start, segment.End, Color.LightGray);
                    Vector2 leftSide = Vector2.Perpendicular(segment.End - segment.Start);
                    leftSide.Normalize();
                    leftSide *= 2f;
                    Render2D.DrawRectangle(new Rectangle(Vector2.Lerp(segment.Start, segment.End, 0.5f) + leftSide, Vector2.Zero).MakeExpanded(2f), segment.LeftColor);
                    Render2D.DrawRectangle(new Rectangle(Vector2.Lerp(segment.Start, segment.End, 0.5f) - leftSide, Vector2.Zero).MakeExpanded(2f), segment.RightColor);
                    if (IsMouseOver)
                    {
                        if (Vector2.Distance(ref segment.Start, ref mousePos) < 5f)
                        {
                            Render2D.FillRectangle(new Rectangle(segment.Start, 0, 0).MakeExpanded(2), Color.White);
                        }
                        if (Vector2.Distance(ref segment.End, ref mousePos) < 5f)
                        {
                            Render2D.FillRectangle(new Rectangle(segment.End, 0, 0).MakeExpanded(2), Color.White);
                        }
                    }
                }
            }

            Render2D.PopClip();
            Render2D.PopTransform();
        }
    }
}
