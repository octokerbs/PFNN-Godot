using Godot;

public partial class CharacterMover : CharacterBody3D
{
    [Export] public float Speed = 3.0f;

    private Skeleton3D skeleton;

    public override void _Ready()
    {
        skeleton = GetNode<Skeleton3D>("Character/Skeleton3D");

        int boneCount = skeleton.GetBoneCount();
        GD.Print("Total bones: " + boneCount);

        for (int i = 0; i < boneCount; i++)
        {
            string boneName = skeleton.GetBoneName(i);
            GD.Print($"Bone {i}: {boneName}");
        }

        // Example: rotate a known bone by name
        int spineIndex = skeleton.FindBone("mixamorig_Spine");
        if (spineIndex != -1)
        {
            var pose = skeleton.GetBonePose(spineIndex);
            pose.Basis = pose.Basis.Rotated(Vector3.Right, Mathf.DegToRad(100));
            skeleton.SetBonePose(spineIndex, pose);
        }
        else
        {
            GD.Print("Spine bone not found.");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

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
