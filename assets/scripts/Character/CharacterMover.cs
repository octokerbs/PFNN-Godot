using Godot;

public partial class CharacterMover : CharacterBody3D
{
	[Export] public float Speed = 3.0f;
	[Export] public float Gravity = 9.8f;

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
			velocity.Y -= Gravity * (float)delta;
		else
			velocity.Y = 0;


		Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = new Vector3(input.X, 0, input.Y);

		if (direction != Vector3.Zero)
		{
			direction = GlobalTransform.Basis * direction;
			direction.Y = 0;
			direction = direction.Normalized();
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
