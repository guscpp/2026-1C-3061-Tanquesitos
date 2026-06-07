using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP.Cameras;

/// <summary>
///     Camara en tercera persona que sigue al tanque desde atras y arriba.
/// </summary>
public class TankFollowCamera
{
    //configuracion de la camara
    public float Distance { get; set; } = GameConfig.Camera.DefaultDistance;        //1800f
    public float HeightOffset { get; set; } = GameConfig.Camera.HeightOffset;       //1250f
    public float LookAtHeight { get; set; } = GameConfig.Camera.LookAtHeight;       //8f
    public float Smoothness { get; set; } = GameConfig.Camera.Smoothness;           //8f

    //configuracion del zoom
    public float MinDistance { get; set; } = GameConfig.Camera.MinDistance;         //20f
    public float MaxDistance { get; set; } = GameConfig.Camera.MaxDistance;         //4500f
    public float ZoomSensitivity { get; set; } = GameConfig.Camera.ZoomSensitivity; //250f

    //para calcular el 3d del audio 3d
    public Vector3 ListenerPosition => _currentPosition;
    public Vector3 ListenerForward => Vector3.Normalize(_lookAt - _currentPosition);

    private Vector3 _currentPosition;
    private Vector3 _targetPosition;
    private Vector3 _lookAt;
    private int _lastScrollValue;

    public Matrix View { get; private set; }
    public Matrix Projection { get; private set; }

    public TankFollowCamera(float aspectRatio, Vector3 initialTankPos)
    {
        _currentPosition = initialTankPos + Vector3.Backward * Distance + Vector3.Up * HeightOffset;
        _targetPosition = _currentPosition;
        _lookAt = initialTankPos + Vector3.Up * LookAtHeight;

        UpdateProjection(aspectRatio);
        UpdateView();
    }

    public void UpdateProjection(float aspectRatio)
    {
        Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 0.5f, 50000f);
    }

    public void Update(GameTime gameTime, Vector3 tankPosition, float tankRotationY)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        //calcular direccion atras relativa al tanque
        Vector3 tankForward = Vector3.TransformNormal(Vector3.Forward, Matrix.CreateRotationY(tankRotationY));
        Vector3 tankBackward = -tankForward;

        //calcular posicion objetivo de la camara
        _targetPosition = tankPosition + tankBackward * Distance + Vector3.Up * HeightOffset;

        //punto al que mira la camara
        _lookAt = tankPosition + Vector3.Up * LookAtHeight;

        //zoom in/out con ruedita del mouse
        var mouseState = Mouse.GetState();
        int scrollDelta = mouseState.ScrollWheelValue - _lastScrollValue;
        if (scrollDelta != 0)
        {
            Distance -= scrollDelta / 120f * ZoomSensitivity;
            Distance = MathHelper.Clamp(Distance, MinDistance, MaxDistance);
        }
        _lastScrollValue = mouseState.ScrollWheelValue;

        //suavizare movimiento
        _currentPosition = Vector3.Lerp(_currentPosition, _targetPosition, dt * Smoothness);

        UpdateView();
    }

    private void UpdateView()
    {
        View = Matrix.CreateLookAt(_currentPosition, _lookAt, Vector3.Up);
    }
}