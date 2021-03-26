using FlaxEngine;
using FlaxEngine.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class VectorGraphicsControl : ContainerControl
    {
        private struct SelectedPoint
        {
            public VectorGraphics.LineSegment Segment;
            public bool IsEndSelected;
        }

        private Vector2 _mousePosition;
        private bool _isDraggingPoints;
        private bool _isDraggingSurface;
        private readonly List<SelectedPoint> _selectedPoints = new List<SelectedPoint>();
        private Vector2 _translation = new Vector2(15f, 35f);
        private float _zoom = 1f;
        private Matrix3x3 _cachedTransform;
        private Matrix3x3 _cachedInverseTransform;
        private bool _showColors = true;
        private Color[] _layerColors = new Color[] { Color.Red, Color.Green, Color.Blue, Color.Yellow };

        public VectorGraphicsControl() : base()
        {
            var toggleButton = new Button(15, 15, 50, 16) { Text = "Colors" };
            toggleButton.Clicked += () =>
            {
                _showColors = !_showColors;
                toggleButton.Text = _showColors ? "Colors" : "Layers";
            };

            AddChild(toggleButton);
        }

        public VectorGraphics VectorGraphics { get; set; }

        public override bool OnMouseWheel(Vector2 location, float delta)
        {
            // Don't call the base
            _mousePosition = location;
            Matrix3x3.Transform2D(ref location, ref _cachedInverseTransform, out var mousePos);
            _zoom += delta * 0.1f;
            // TODO: Zoom in on the mouse position
            UpdateTransforms();
            return true;
        }

        public override void OnMouseMove(Vector2 location)
        {
            base.OnMouseMove(location);
            var oldPosition = _mousePosition;
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
            else if (_isDraggingSurface)
            {
                Matrix3x3.Transform2D(ref oldPosition, ref _cachedInverseTransform, out var oldMousePos);
                Matrix3x3.Transform2D(ref location, ref _cachedInverseTransform, out var mousePos);

                _translation += mousePos - oldMousePos;
                UpdateTransforms();
            }
        }

        public override bool OnMouseUp(Vector2 location, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                _isDraggingPoints = false;
                _isDraggingSurface = false;
                EndMouseCapture();
            }
            return base.OnMouseUp(location, button);
        }

        public override bool OnMouseDown(Vector2 location, MouseButton button)
        {
            if (base.OnMouseDown(location, button)) return true;
            _mousePosition = location;

            if (button == MouseButton.Left)
            {
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

                if (_selectedPoints.Count > 0)
                {
                    _isDraggingPoints = true;
                    StartMouseCapture();
                }
                else
                {
                    _isDraggingSurface = true;
                    StartMouseCapture();
                }
            }
            return true;
        }

        private void UpdateTransforms()
        {
            _cachedTransform = Matrix3x3.Scaling(_zoom, _zoom, 1) * Matrix3x3.Translation2D(_translation);
            Matrix3x3.Invert(ref _cachedTransform, out _cachedInverseTransform);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            UpdateTransforms(); // Could be done more efficiently, eh, whatevs
        }

        public override void DrawSelf()
        {
            base.DrawSelf();
            var style = Style.Current;

            // Outline
            Render2D.DrawRectangle(new Rectangle(new Vector2(10f), Size - 20f), style.BorderSelected);
            Render2D.PushClip(new Rectangle(new Vector2(10f), Size - 20f));

            Render2D.PushTransform(ref _cachedTransform);

            Matrix3x3.Transform2D(ref _mousePosition, ref _cachedInverseTransform, out var mousePos);

            if (VectorGraphics?.LineSegments != null)
            {
                Render2D.DrawRectangle(new Rectangle(Vector2.Zero, (Vector2)VectorGraphics.Size), Color.White);

                for (int i = 0; i < VectorGraphics.LineSegments.Count; i++)
                {
                    var segment = VectorGraphics.LineSegments[i];

                    if (segment == null) continue;


                    Render2D.DrawLine(segment.Start, segment.End, Color.LightGray);
                    Vector2 leftSide = Vector2.Perpendicular(segment.End - segment.Start);
                    leftSide.Normalize();
                    leftSide *= 2f;
                    var leftColor = _showColors ? segment.LeftColor : (segment.LeftFaceLayer == VectorGraphics.InvalidLayer ? Color.Black : _layerColors[segment.LeftFaceLayer]);
                    var rightColor = _showColors ? segment.RightColor : (segment.RightFaceLayer == VectorGraphics.InvalidLayer ? Color.Black : _layerColors[segment.RightFaceLayer]);
                    Render2D.DrawRectangle(new Rectangle(Vector2.Lerp(segment.Start, segment.End, 0.5f) + leftSide, Vector2.Zero).MakeExpanded(2f), leftColor);
                    Render2D.DrawRectangle(new Rectangle(Vector2.Lerp(segment.Start, segment.End, 0.5f) - leftSide, Vector2.Zero).MakeExpanded(2f), rightColor);
                    if (IsMouseOver)
                    {
                        if (Vector2.Distance(ref segment.Start, ref mousePos) < 5f / _zoom)
                        {
                            Render2D.FillRectangle(new Rectangle(segment.Start, 0, 0).MakeExpanded(2), Color.White);
                        }
                        if (Vector2.Distance(ref segment.End, ref mousePos) < 5f / _zoom)
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
