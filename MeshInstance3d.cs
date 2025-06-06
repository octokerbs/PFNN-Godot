using Godot;

public partial class CubeMover : MeshInstance3D
{
	public override void _Process(double delta)
	{
		Translate(Vector3.Up * (float)delta);
	}
}
