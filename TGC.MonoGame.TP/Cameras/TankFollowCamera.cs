using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP.Cameras;

/// <summary>
///     Camara en tercera persona que sigue al tanque desde atras y arriba.
/// </summary>
public class TankFollowCamera
{
    //configuracion de la camara
    public float Distance { get; set; } = 1800f;
    public float HeightOffset { get; set; } = 1250f;
    public float LookAtHeight { get; set; } = 8f;
    public float Smoothness { get; set; } = 8f;

    //configuracion del zoom
    public float MinDistance { get; set; } = 20f;
    public float MaxDistance { get; set; } = 4500f;
    public float ZoomSensitivity { get; set; } = 40f;

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
        Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 0.5f, 10000f);
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