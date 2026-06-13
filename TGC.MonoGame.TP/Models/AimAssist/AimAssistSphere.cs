using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Models.AimAssist;

public class AimAssistSphere
{
    private static Model _model;

    private static Effect _effect;

    private Matrix _world;

    public AimAssistSphere(Vector3 position)
    {
        _world = Matrix.CreateScale(0.005f) * Matrix.CreateTranslation(position);
    }

    public static void LoadContent(Model model, Effect effect)
    {
        _model = model;
        _effect = effect;
    }

    public void Draw(Matrix view, Matrix projection)
    {
        _effect.Parameters["World"]?.SetValue(_world);

        _effect.Parameters["View"]?.SetValue(view);

        _effect.Parameters["Projection"]?.SetValue(projection);

        _effect.Parameters["DiffuseColor"]?.SetValue(Color.LightGray.ToVector3());

        Matrix[] transforms = new Matrix[_model.Bones.Count];

        _model.CopyAbsoluteBoneTransformsTo(transforms);

        foreach (var mesh in _model.Meshes)
        {
            Matrix localWorld = transforms[mesh.ParentBone.Index] * _world;

            _effect.Parameters["World"]?.SetValue(localWorld);

            mesh.Draw();
        }
    }
}