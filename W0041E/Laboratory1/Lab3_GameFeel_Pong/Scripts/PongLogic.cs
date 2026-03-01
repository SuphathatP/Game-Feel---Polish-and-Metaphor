using Godot;
using System;

public partial class PongLogic : Node
{
    [Export] public Node3D polish;
    bool isPolishOn = true;

    [ExportGroup("Node and Instance")]
    [Export] public Node3D leftPaddleHD;
    [Export] public Node3D rightPaddleHD;
    [Export] public MeshInstance3D leftPaddleOld;
    [Export] public MeshInstance3D rightPaddleOld;
    [Export] public MeshInstance3D ballMesh;
    [Export] public Node3D coconut;
    [Export] public Camera3D camera;

    [ExportGroup("Original")]
    [Export] public Node3D original;
    bool isOriginalOn = false;

    [Export] public Node3D leftPaddle;

    [Export] public Node3D rightPaddle;

    [Export] public Node3D ball;

    [Export] public Vector2 tableSize;

    private Vector3 ballVelocity = Vector3.Zero;

    [ExportGroup("Speed")]
    [Export] private float ballSpeed = 5.0f; 

    [Export] private float paddleSpeed = 10.0f; 

    [Export] public float paddleLerpSpeed = 20;
    [Export] public float blendAnimSpeed = 0.25f;


    private Random random = new Random();

    public float leftStickMagnitude = 0;
    public float rightStickMagnitude = 0;
    public Vector2 leftStickInput = Vector2.Zero;
    public Vector2 rightStickInput = Vector2.Zero;

	public int sideCheck = 0;

    private float leftPaddleVerticalVelocity = 0;
    private float rightPaddleVerticalVelocity = 0;

    // Animantions
    private AnimationPlayer cameraHDAnim;
    private AnimationPlayer boatLeftAnim;
    private AnimationPlayer boatRightAnim;

    // Audios
    private AudioStreamPlayer3D leftBoatHitAudio;
    private AudioStreamPlayer3D rightBoatHitAudio;
    private AudioStreamPlayer3D environmentAudio;
    private AudioStreamPlayer3D backgroundAudio;
    private AudioStreamPlayer3D leftBoatMovingAudio;
    private AudioStreamPlayer3D rightBoatMovingAudio;

    public override void _Ready()
    {
        InitMatch();

        // Animations
        cameraHDAnim = camera.GetNode<AnimationPlayer>("AnimationPlayer");

        boatLeftAnim = leftPaddle.GetNode<AnimationPlayer>("LeftPaddleHD/BoatLeft/AnimationPlayer");
        boatRightAnim = rightPaddle.GetNode<AnimationPlayer>("RightPaddleHD/BoatRight/AnimationPlayer");

        boatLeftAnim.AnimationFinished += OnLeftBoatAnimFinished;
        boatRightAnim.AnimationFinished += OnRightBoatAnimFinished;

        // Audios
        leftBoatHitAudio = leftPaddle.GetNode<AudioStreamPlayer3D>("LeftPaddleHD/BoatLeft/BoatHitAudio");
        rightBoatHitAudio = rightPaddle.GetNode<AudioStreamPlayer3D>("RightPaddleHD/BoatRight/BoatHitAudio");
        
        environmentAudio = polish.GetNode<AudioStreamPlayer3D>("EnvironmentAudio");
        backgroundAudio = polish.GetNode<AudioStreamPlayer3D>("BackGroundAudio");

        leftBoatMovingAudio = leftPaddle.GetNode<AudioStreamPlayer3D>("LeftPaddleHD/BoatLeft/BoatMovingAudio");
        rightBoatMovingAudio = rightPaddle.GetNode<AudioStreamPlayer3D>("RightPaddleHD/BoatRight/BoatMovingAudio");
    }

    public override void _Process(double delta)
    {
        PollInput((float)delta);
        PaddleMovement((float)delta);
        BallMovement((float)delta);
        CheckPaddleCollision();
        CheckForScore();
        SetPolishGFXTransform();
        TogglePolish();
    }

    // Ball movement with speed adjustments
    public void BallMovement(float delta)
    {
        ball.Translate(ballVelocity * delta);
        bool outOfBoundsTop = ball.Position.Z > tableSize.Y / 2.0f;
        bool outOfBoundsBottom = ball.Position.Z < -tableSize.Y / 2.0f;
        if (outOfBoundsTop && ballVelocity.Z > 0.0f || outOfBoundsBottom && ballVelocity.Z < 0.0f)
        {
            ballVelocity.Z *= -1;
        }


    }

    public void SetPolishGFXTransform()
    {
        coconut.RotationDegrees = ballVelocity.X > 0 ? new Vector3(0, 0, 0) : new Vector3(0, 180, 0);
    }




    // Paddle movement
    public void PaddleMovement(float delta)
    {
        Vector3 leftPaddlePosition = leftPaddle.Position;
        leftPaddlePosition.Z += leftStickInput.Y * paddleSpeed * leftStickMagnitude * delta;
        leftPaddlePosition.Z = Mathf.Clamp(leftPaddlePosition.Z, (-tableSize.Y + leftPaddle.Scale.Z) / 2, (tableSize.Y - leftPaddle.Scale.Z) / 2);
        leftPaddleVerticalVelocity = (leftPaddlePosition - leftPaddle.Position).Length();
        leftPaddle.Position = leftPaddlePosition;

        Vector3 rightPaddlePosition = rightPaddle.Position;
        rightPaddlePosition.Z += rightStickInput.Y * paddleSpeed * rightStickMagnitude * delta;
        rightPaddlePosition.Z = Mathf.Clamp(rightPaddlePosition.Z, (-tableSize.Y + rightPaddle.Scale.Z) / 2, (tableSize.Y - rightPaddle.Scale.Z) / 2);
        rightPaddleVerticalVelocity = (rightPaddlePosition - rightPaddle.Position).Length();
        rightPaddle.Position = rightPaddlePosition;

        // LEFT PADDLE SOUND
        if (leftPaddleVerticalVelocity > 0.001f && isPolishOn)
        {
            if (!leftBoatMovingAudio.Playing)
                leftBoatMovingAudio.Play();
            
            leftBoatMovingAudio.VolumeDb = Mathf.Lerp(-12, 0, leftPaddleVerticalVelocity * 5f);
            leftBoatMovingAudio.PitchScale = 1f + leftPaddleVerticalVelocity * 0.5f;
        }
        else
        {
            if (leftBoatMovingAudio.Playing)
            {
                var tween = GetTree().CreateTween();
                tween.TweenProperty(leftBoatMovingAudio, "volume_db", -40, 0.15f);
                tween.TweenCallback(Callable.From(() => leftBoatMovingAudio.Stop()));
            }
        }

        // RIGHT PADDLE SOUND
        if (rightPaddleVerticalVelocity > 0.001f && isPolishOn)
        {
            if (!rightBoatMovingAudio.Playing)
                rightBoatMovingAudio.Play();

            rightBoatMovingAudio.VolumeDb = Mathf.Lerp(-12, 0, rightPaddleVerticalVelocity * 5f);
            rightBoatMovingAudio.PitchScale = 1f + rightPaddleVerticalVelocity * 0.5f;
        }
        else
        {
            if (rightBoatMovingAudio.Playing)
            {
                var tween = GetTree().CreateTween();
                tween.TweenProperty(rightBoatMovingAudio, "volume_db", -40, 0.15f);
                tween.TweenCallback(Callable.From(() => rightBoatMovingAudio.Stop()));
            }
        } 
    }

    // Initialize match and set ball starting velocity
    public void InitMatch()
    {
        ball.GlobalPosition = Vector3.Zero;
        float angle = Mathf.DegToRad(random.Next(-45, 45));
        int horizontalDirection = random.Next(0, 2) == 0 ? 1 : -1;
        float velocityX = horizontalDirection * Mathf.Cos(angle);
        float velocityZ = Mathf.Sin(angle);
        ballVelocity = new Vector3(velocityX, 0, velocityZ) * ballSpeed;
    }

    // Restart match
    public void LooseMatch()
    {
        InitMatch();
    }

    // Handle joystick input for paddles (Same joystick)
    public void PollInput(float delta)
    {
        float leftX = Input.GetJoyAxis(0, JoyAxis.LeftX);
        float leftY = Input.GetJoyAxis(0, JoyAxis.LeftY);
        leftStickMagnitude = new Vector2(leftX, leftY).Length();
        leftStickInput = new Vector2(leftX, leftY);
        if (leftStickMagnitude < 0.04f) // Fuzzy joystick setting..
        {
            leftStickInput = Vector2.Zero;
        }

        float rightX = Input.GetJoyAxis(0, JoyAxis.RightX);
        float rightY = Input.GetJoyAxis(0, JoyAxis.RightY);
        rightStickMagnitude = new Vector2(rightX, rightY).Length();
        rightStickInput = new Vector2(rightX, rightY);

        if (rightStickMagnitude < 0.04f) // Fuzzy joystick setting..
        {
            rightStickInput = Vector2.Zero;
        }
    }

 // Check paddle collision with the ball
private void CheckPaddleCollision()
{
    Node3D targetPaddle = ballVelocity.X < 0 ? leftPaddle : rightPaddle;
    float paddleHalfSizeZ = targetPaddle.Scale.Z / 2.0f;
    float paddleCenterZ = targetPaddle.GlobalPosition.Z;
    float paddleMinZ = paddleCenterZ - paddleHalfSizeZ;
    float paddleMaxZ = paddleCenterZ + paddleHalfSizeZ;

    if (Mathf.Abs(ball.GlobalPosition.X - targetPaddle.GlobalPosition.X) < targetPaddle.Scale.X / 2.0f)
    {
        if (ball.GlobalPosition.Z >= paddleMinZ && ball.GlobalPosition.Z <= paddleMaxZ)
        {
            ballVelocity.X *= -1;

            if (targetPaddle == leftPaddle)
            {
                boatLeftAnim?.Play("boat_hit_anim");
                
                if (isPolishOn)
                {
                    cameraHDAnim?.Play("camera_shake_anim");
                    leftBoatHitAudio?.Play();
                }
                
            }
            else if (targetPaddle == rightPaddle)
            {
                boatRightAnim?.Play("boat_hit_anim");
                
                if (isPolishOn)
                {
                    cameraHDAnim?.Play("camera_shake_anim");
                    rightBoatHitAudio?.Play();
                }
            }

            float distanceFromCenter = ball.GlobalPosition.Z - paddleCenterZ;
            float maxAngle = 75.0f;  
            float angle = Mathf.DegToRad(maxAngle * (distanceFromCenter / paddleHalfSizeZ));

            ballVelocity.Z = Mathf.Sin(angle) * ballSpeed;
            ballVelocity = ballVelocity.Normalized() * ballSpeed;

			if(leftPaddleVerticalVelocity > 0.07f && targetPaddle == leftPaddle)
            {
				ballVelocity = ballVelocity * 2.0f;
			}
            if(rightPaddleVerticalVelocity > 0.07f && targetPaddle == rightPaddle)
            {
                ballVelocity = ballVelocity * 2.0f;
            }

            if (ball.GlobalPosition.X < targetPaddle.GlobalPosition.X)
            {
                ball.GlobalPosition = new Vector3(targetPaddle.GlobalPosition.X - targetPaddle.Scale.X / 2, ball.GlobalPosition.Y, ball.GlobalPosition.Z);
            }
            else
            {
                ball.GlobalPosition = new Vector3(targetPaddle.GlobalPosition.X + targetPaddle.Scale.X / 2, ball.GlobalPosition.Y, ball.GlobalPosition.Z);
            }
        }
    }
}

    // Check if the ball goes out of bounds for scoring
    private void CheckForScore()
    {
        float padding = 2f;
        if (ball.GlobalPosition.X < -tableSize.X / 2 - padding || ball.GlobalPosition.X > tableSize.X / 2 + padding)
        {
            LooseMatch();
        }
    }


    private void TogglePolish()
    {
        if (Input.IsActionJustPressed("polish"))
        {
            // Polish Check
            isPolishOn = !isPolishOn;
            polish.Visible = isPolishOn;
            leftPaddleHD.Visible = isPolishOn;
            rightPaddleHD.Visible = isPolishOn;
            coconut.Visible = isPolishOn;

            // Original Check
            isOriginalOn = !isOriginalOn;
            original.Visible = isOriginalOn;
            leftPaddleOld.Visible = isOriginalOn;
            rightPaddleOld.Visible = isOriginalOn;
            ballMesh.Visible = isOriginalOn;

            // Background audios
            if (isPolishOn)
            {
                if (!environmentAudio.Playing)
                    environmentAudio.Play();

                if (!backgroundAudio.Playing)
                    backgroundAudio.Play();
                
                var tween = GetTree().CreateTween();
                tween.TweenProperty(environmentAudio, "volume_db", -20, 0.5f);

                var tween2 = GetTree().CreateTween();
                tween2.TweenProperty(backgroundAudio, "volume_db", 0, 0.5f);
            }
            else
            {
                var tween = GetTree().CreateTween();
                tween.TweenProperty(environmentAudio, "volume_db", -40, 0.5f);
                tween.TweenCallback(Callable.From(() => environmentAudio.Stop()));

                var tween2 = GetTree().CreateTween();
                tween2.TweenProperty(backgroundAudio, "volume_db", -40, 0.5f);
                tween.TweenCallback(Callable.From(() => backgroundAudio.Stop()));
            }
        }
    }

    private void OnLeftBoatAnimFinished(StringName animName)
    {
        if (animName == "boat_hit_anim")
        {
            boatLeftAnim.Play("boat_animation", customBlend: blendAnimSpeed);
        }    
    }

    private void OnRightBoatAnimFinished(StringName animName)
    {
        if (animName == "boat_hit_anim")
        {
            boatRightAnim.Play("boat_animation", customBlend: blendAnimSpeed);
        }   
    }

}
