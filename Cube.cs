using Godot;

public partial class Cube : MeshInstance3D
{
	public override void _Process(double delta)
	{
		Translate(Vector3.Up * (float)delta);
	}
}
