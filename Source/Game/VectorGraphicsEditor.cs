#if FLAX_EDITOR
using FlaxEditor.CustomEditors;
using FlaxEditor.CustomEditors.Editors;
using FlaxEngine;

namespace Game
{
    [CustomEditor(typeof(VectorGraphics))]
    public class VectorGraphicsEditor : GenericEditor
    {
        public override void Initialize(LayoutElementsContainer layout)
        {
            layout.Label("My Custom Editor", TextAlignment.Center);
            var group = layout.Group("Inner group");

            base.Initialize(group);

            layout.Space(20);
            var button = layout.Button("Generate Textures", Color.Green);
            button.Button.Clicked += () => Values.ForEach(v => (v as VectorGraphics)?.GenerateTexture());

            // TODO: Which file path do I have?

            // TODO: Add editor for the line segments!
            var vectorGraphicsControl = layout.Custom<VectorGraphicsControl>();
            vectorGraphicsControl.CustomControl.SetAnchorPreset(FlaxEngine.GUI.AnchorPresets.HorizontalStretchBottom, false);
            vectorGraphicsControl.CustomControl.Height = Mathf.Max(128, (Values[0] as VectorGraphics)?.Size.Y * 1.5f ?? 128);
            vectorGraphicsControl.CustomControl.VectorGraphics = Values[0] as VectorGraphics;
        }
    }
}
#endif
