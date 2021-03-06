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
            // Content.DeleteAsset()
            // TODO: Which file path do I have?

            var vectorGraphicsControl = layout.Custom<VectorGraphicsControl>();
            //vectorGraphicsControl.CustomControl.SetAnchorPreset(FlaxEngine.GUI.AnchorPresets.StretchAll, false);
            vectorGraphicsControl.CustomControl.Height = Mathf.Max(512, (Values[0] as VectorGraphics)?.Size.Y * 1.5f ?? 512);
            vectorGraphicsControl.CustomControl.VectorGraphics = Values[0] as VectorGraphics;
        }
    }
}
#endif
