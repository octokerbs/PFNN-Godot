using Godot;
using System;
using System.Collections.Generic;
using DeepLearning;

namespace SIGGRAPH_2018
{
    /// <summary>
    /// Neural network-based character animation controller for Godot.
    /// Uses a Phase-Functioned Neural Network (PFNN) to generate realistic character movement
    /// based on user input and predicted trajectory paths.
    /// 
    /// This is a translation from Unity's SIGGRAPH 2018 "Mode-Adaptive Neural Networks" implementation.
    /// </summary>
    public partial class BioAnimation : Skeleton3D
    {
        #region Inspector Properties
        [Export] public bool Inspect = false;
        [Export] public bool ShowTrajectory = true;
        [Export] public bool ShowVelocities = true;

        /// <summary>Rate at which the character responds to new input (higher = more responsive)</summary>
        [Export] public float TargetGain = 0.25f;

        /// <summary>Rate at which the character stops moving when no input is given</summary>
        [Export] public float TargetDecay = 0.05f;

        /// <summary>Enable/disable trajectory prediction and control</summary>
        [Export] public bool TrajectoryControl = true;

        /// <summary>How strongly the neural network predictions correct the trajectory (0-1)</summary>
        [Export] public float TrajectoryCorrection = 1f;

        /// <summary>Neural network input dimension for trajectory data</summary>
        [Export] public int TrajectoryDimIn = 6;

        /// <summary>Neural network output dimension for trajectory data</summary>
        [Export] public int TrajectoryDimOut = 6;

        /// <summary>Neural network input dimension for joint/bone data</summary>
        [Export] public int JointDimIn = 12;

        /// <summary>Neural network output dimension for joint/bone data</summary>
        [Export] public int JointDimOut = 12;
        #endregion

        #region Core Components
        /// <summary>Handles user input and style blending</summary>
        public Controller Controller;

        /// <summary>Character rig with bone hierarchy</summary>
        private Actor Actor;    // Hay que cambiarlo por skeleton

        private Skeleton3D Skeleton;

        /// <summary>Phase-Functioned Neural Network for motion prediction</summary>
        private PFNN NN;

        /// <summary>Trajectory path prediction and management</summary>
        private Trajectory Trajectory;
        #endregion

        #region Movement Targets
        /// <summary>Current target direction the character should face</summary>
        private Vector3 TargetDirection;

        /// <summary>Current target velocity the character should move with</summary>
        private Vector3 TargetVelocity;
        #endregion

        #region Character State Arrays
        /// <summary>Current world positions of all character bones</summary>
        private Vector3[] Positions = new Vector3[0];

        /// <summary>Current forward directions of all character bones</summary>
        private Vector3[] Forwards = new Vector3[0];

        /// <summary>Current up directions of all character bones</summary>
        private Vector3[] Ups = new Vector3[0];

        /// <summary>Current velocities of all character bones</summary>
        private Vector3[] Velocities = new Vector3[0];
        #endregion

        #region Trajectory Constants
        /// <summary>Target framerate for consistent physics calculations</summary>
        private const int Framerate = 60;

        /// <summary>Total number of trajectory points (past + current + future)</summary>
        private const int Points = 111;

        /// <summary>Number of trajectory points sampled for neural network input</summary>
        private const int PointSamples = 12;

        /// <summary>Number of past trajectory points stored</summary>
        private const int PastPoints = 60;

        /// <summary>Number of future trajectory points predicted</summary>
        private const int FuturePoints = 50;

        /// <summary>Index of the current root position in trajectory array</summary>
        private const int RootPointIndex = 60;

        /// <summary>Density of trajectory points (frames between major points)</summary>
        private const int PointDensity = 10;
        #endregion

        /// <summary>
        /// Initialize the animation system components and character state.
        /// Called when the node enters the scene tree.
        /// </summary>
        public override void _Ready()
        {
            // Initialize controller if not already set
            if (Controller == null)
                Controller = new Controller();

            // Get required components - try as child nodes first, then as attached components
            // Actor = GetNode<Actor>("Actor");
            Skeleton = GetNode<Skeleton3D>("Character/Skeleton3D");
            NN = GetNode<PFNN>("PFNN");

            // Initialize target direction based on current transform (flattened to ground plane)
            TargetDirection = new Vector3(GlobalTransform.Basis.Z.X, 0f, GlobalTransform.Basis.Z.Z);
            TargetVelocity = Vector3.Zero;

            // Allocate arrays for bone state tracking
            Positions = new Vector3[Actor.Bones.Length];
            Forwards = new Vector3[Actor.Bones.Length];
            Ups = new Vector3[Actor.Bones.Length];
            Velocities = new Vector3[Actor.Bones.Length];

            // Create trajectory predictor with initial position and direction
            Trajectory = new Trajectory(Points, Controller.GetNames(), GlobalPosition, TargetDirection);

            // Set default style if styles are available (usually idle/rest pose)
            if (Controller.Styles.Length > 0)
            {
                for (int i = 0; i < Trajectory.Points.Length; i++)
                {
                    Trajectory.Points[i].Styles[0] = 1f; // Set first style to 100%
                }
            }

            // Initialize bone state arrays with current bone transforms
            GD.Print("Initializing bone states...");
            for (int i = 0; i < Actor.Bones.Length; i++)
            {
                Positions[i] = Actor.Bones[i].Transform.Position;
                GD.Print($"Bone: {Actor.Bones[i].GetName()} | Index: {i}");

                // Note: Godot uses -Z as forward direction, Y as up
                Forwards[i] = -Actor.Bones[i].Transform.Basis.Z;
                Ups[i] = Actor.Bones[i].Transform.Basis.Y;
                Velocities[i] = Vector3.Zero; // Start with no velocity
            }
            GD.Print("Bone initialization complete.");

            // Load neural network parameters
            if (NN.Parameters == null)
            {
                GD.PrintErr("No neural network parameters found! Animation will not work.");
                return;
            }
            NN.LoadParameters();
        }

        /// <summary>
        /// Main animation loop - called every frame.
        /// Handles trajectory prediction and neural network-based animation generation.
        /// </summary>
        /// <param name="delta">Time elapsed since last frame</param>
        // public override void _Process(double delta)
        // {
        //     // Ensure consistent 60 FPS for stable neural network predictions
        //     Engine.MaxFps = 60;

        //     // Exit early if neural network isn't ready
        //     if (NN.Parameters == null)  // Se inicializa en null => No podemos pedir los parameters de null
        //         return;

        //     // Update trajectory prediction based on user input
        //     if (TrajectoryControl)
        //         PredictTrajectory();

        //     // Generate new animation pose using neural network
        //     if (NN.Parameters != null)
        //         Animate();

        //     // Update character's root position to match trajectory
        //     GlobalPosition = Trajectory.Points[RootPointIndex].GetPosition();
        // }

        public override void _Process(double delta)
        {
            // Ensure consistent 60 FPS for stable neural network predictions
            Engine.MaxFps = 60;

            // Exit early if neural network isn't ready or doesn't exist
            if (NN == null || NN.Parameters == null)
                return;

            // Update trajectory prediction based on user input
            if (TrajectoryControl)
                PredictTrajectory();

            // Generate new animation pose using neural network
            Animate();

            // Update character's root position to match trajectory
            GlobalPosition = Trajectory.Points[RootPointIndex].GetPosition();
        }

        /// <summary>
        /// Predicts future trajectory based on user input and current character state.
        /// This creates a smooth path that the character will follow, blending user intent
        /// with natural movement patterns.
        /// </summary>
        private void PredictTrajectory()
        {
            // Calculate movement bias based on current animation style
            float bias = PoolBias();

            // Get user input for turning and movement
            float turn = Controller.QueryTurn();
            Vector3 move = Controller.QueryMove();
            bool control = turn != 0f || move != Vector3.Zero; // Is user actively controlling?

            // Smooth transition of target direction and velocity
            // Use different rates for active control vs. natural decay
            TargetDirection = TargetDirection.Lerp(
                new Basis(Vector3.Up, Mathf.DegToRad(turn * 60f)) * Trajectory.Points[RootPointIndex].GetDirection(),
                control ? TargetGain : TargetDecay
            );

            // Convert target direction to world space movement
            Vector3 lookDirection = new Basis(Vector3.Up, Mathf.Atan2(TargetDirection.X, TargetDirection.Z)) * move;
            TargetVelocity = TargetVelocity.Lerp(
                bias * lookDirection.Normalized(),
                control ? TargetGain : TargetDecay
            );

            // Update trajectory correction strength based on input intensity
            TrajectoryCorrection = Utility.Interpolate(
                TrajectoryCorrection,
                Mathf.Max(move.Normalized().Length(), Mathf.Abs(turn)),
                control ? TargetGain : TargetDecay
            );

            // === FUTURE TRAJECTORY PREDICTION ===
            // Create smooth trajectory curve for future movement
            Vector3[] trajectory_positions_blend = new Vector3[Trajectory.Points.Length];
            trajectory_positions_blend[RootPointIndex] = Trajectory.Points[RootPointIndex].GetTransformation().Origin;

            for (int i = RootPointIndex + 1; i < Trajectory.Points.Length; i++)
            {
                // Different bias curves for position, direction, and velocity
                // These create natural acceleration/deceleration curves
                float bias_pos = 0.75f;  // Position prediction curve
                float bias_dir = 1.25f;  // Direction prediction curve  
                float bias_vel = 1.50f;  // Velocity prediction curve

                float weight = (float)(i - RootPointIndex) / (float)FuturePoints; // 0 to 1
                float scale_pos = 1.0f - Mathf.Pow(1.0f - weight, bias_pos);
                float scale_dir = 1.0f - Mathf.Pow(1.0f - weight, bias_dir);
                float scale_vel = 1.0f - Mathf.Pow(1.0f - weight, bias_vel);

                float scale = 1f / (Trajectory.Points.Length - (RootPointIndex + 1f));

                // Blend current trajectory with target velocity
                trajectory_positions_blend[i] = trajectory_positions_blend[i - 1] +
                    (Trajectory.Points[i].GetPosition() - Trajectory.Points[i - 1].GetPosition()).Lerp(
                        scale * TargetVelocity,
                        scale_pos
                    );

                // Update direction and velocity with smooth blending
                Trajectory.Points[i].SetDirection(
                    Trajectory.Points[i].GetDirection().Lerp(TargetDirection, scale_dir)
                );
                Trajectory.Points[i].SetVelocity(
                    Trajectory.Points[i].GetVelocity().Lerp(TargetVelocity, scale_vel)
                );
            }

            // Apply blended positions to trajectory
            for (int i = RootPointIndex + 1; i < Trajectory.Points.Length; i++)
            {
                Trajectory.Points[i].SetPosition(trajectory_positions_blend[i]);
            }

            // === STYLE BLENDING ===
            // Update animation style based on movement speed and user input
            float[] style = Controller.GetStyle();

            // Auto-adjust walk/run style based on velocity when not jumping
            if (style[2] == 0f) // If not jumping
            {
                style[1] = Mathf.Max(style[1],
                    Mathf.Clamp(Trajectory.Points[RootPointIndex].GetVelocity().Length(), 0f, 1f));
            }

            // Propagate style changes through future trajectory points
            for (int i = RootPointIndex; i < Trajectory.Points.Length; i++)
            {
                float weight = (float)(i - RootPointIndex) / (float)FuturePoints;
                for (int j = 0; j < Trajectory.Points[i].Styles.Length; j++)
                {
                    Trajectory.Points[i].Styles[j] = Utility.Interpolate(
                        Trajectory.Points[i].Styles[j],
                        style[j],
                        Utility.Normalise(weight, 0f, 1f, Controller.Styles[j].Transition, 1f)
                    );
                }
                Utility.Normalise(ref Trajectory.Points[i].Styles); // Keep styles normalized

                // Update movement speed
                Trajectory.Points[i].SetSpeed(
                    Utility.Interpolate(
                        Trajectory.Points[i].GetSpeed(),
                        TargetVelocity.Length(),
                        control ? TargetGain : TargetDecay
                    )
                );
            }
        }

        private void Animate()
        {
            // Calculate Root
            Transform3D currentRoot = Trajectory.Points[RootPointIndex].GetTransformation();
            currentRoot.Origin = new Vector3(currentRoot.Origin.X, 0f, currentRoot.Origin.Z); // For flat terrain

            int start = 0;
            // Input Trajectory Positions / Directions / Velocities / Styles
            for (int i = 0; i < PointSamples; i++)
            {
                Vector3 pos = GetSample(i).GetPosition().GetRelativePositionTo(currentRoot);
                Vector3 dir = GetSample(i).GetDirection().GetRelativeDirectionTo(currentRoot);
                Vector3 vel = GetSample(i).GetVelocity().GetRelativeDirectionTo(currentRoot);
                float speed = GetSample(i).GetSpeed();

                NN.SetInput(start + i * TrajectoryDimIn + 0, pos.X);
                NN.SetInput(start + i * TrajectoryDimIn + 1, pos.Z);
                NN.SetInput(start + i * TrajectoryDimIn + 2, dir.X);
                NN.SetInput(start + i * TrajectoryDimIn + 3, dir.Z);
                NN.SetInput(start + i * TrajectoryDimIn + 4, vel.X);
                NN.SetInput(start + i * TrajectoryDimIn + 5, vel.Z);
                NN.SetInput(start + i * TrajectoryDimIn + 6, speed);

                for (int j = 0; j < Controller.Styles.Length; j++)
                {
                    NN.SetInput(start + i * TrajectoryDimIn + (TrajectoryDimIn - Controller.Styles.Length) + j, GetSample(i).Styles[j]);
                }
            }
            start += TrajectoryDimIn * PointSamples;

            Transform3D previousRoot = Trajectory.Points[RootPointIndex - 1].GetTransformation();
            previousRoot.Origin = new Vector3(previousRoot.Origin.X, 0f, previousRoot.Origin.Z); // For flat terrain

            // Input Previous Bone Positions / Velocities
            for (int i = 0; i < Actor.Bones.Length; i++)
            {
                Vector3 pos = Positions[i].GetRelativePositionTo(previousRoot);
                Vector3 forward = Forwards[i].GetRelativeDirectionTo(previousRoot);
                Vector3 up = Ups[i].GetRelativeDirectionTo(previousRoot);
                Vector3 vel = Velocities[i].GetRelativeDirectionTo(previousRoot);

                NN.SetInput(start + i * JointDimIn + 0, pos.X);
                NN.SetInput(start + i * JointDimIn + 1, pos.Y);
                NN.SetInput(start + i * JointDimIn + 2, pos.Z);
                NN.SetInput(start + i * JointDimIn + 3, forward.X);
                NN.SetInput(start + i * JointDimIn + 4, forward.Y);
                NN.SetInput(start + i * JointDimIn + 5, forward.Z);
                NN.SetInput(start + i * JointDimIn + 6, up.X);
                NN.SetInput(start + i * JointDimIn + 7, up.Y);
                NN.SetInput(start + i * JointDimIn + 8, up.Z);
                NN.SetInput(start + i * JointDimIn + 9, vel.X);
                NN.SetInput(start + i * JointDimIn + 10, vel.Y);
                NN.SetInput(start + i * JointDimIn + 11, vel.Z);
            }
            start += JointDimIn * Actor.Bones.Length;

            // Predict
            float rest = Mathf.Pow(1.0f - Trajectory.Points[RootPointIndex].Styles[0], 0.25f);
            ((PFNN)NN).SetDamping(1f - (rest * 0.9f + 0.1f));
            NN.Predict();

            // Update Past Trajectory
            for (int i = 0; i < RootPointIndex; i++)
            {
                Trajectory.Points[i].SetPosition(Trajectory.Points[i + 1].GetPosition());
                Trajectory.Points[i].SetDirection(Trajectory.Points[i + 1].GetDirection());
                Trajectory.Points[i].SetVelocity(Trajectory.Points[i + 1].GetVelocity());
                Trajectory.Points[i].SetSpeed(Trajectory.Points[i + 1].GetSpeed());
                for (int j = 0; j < Trajectory.Points[i].Styles.Length; j++)
                {
                    Trajectory.Points[i].Styles[j] = Trajectory.Points[i + 1].Styles[j];
                }
            }

            // Update Root
            Vector3 translationalOffset = Vector3.Zero;
            float rotationalOffset = 0f;
            Vector3 rootMotion = new Vector3(
                NN.GetOutput(TrajectoryDimOut * 6 + JointDimOut * Actor.Bones.Length + 0),
                NN.GetOutput(TrajectoryDimOut * 6 + JointDimOut * Actor.Bones.Length + 1),
                NN.GetOutput(TrajectoryDimOut * 6 + JointDimOut * Actor.Bones.Length + 2)
            );
            rootMotion /= Framerate;
            translationalOffset = rest * new Vector3(rootMotion.X, 0f, rootMotion.Z);
            rotationalOffset = rest * rootMotion.Y;

            Trajectory.Points[RootPointIndex].SetPosition(translationalOffset.GetRelativePositionFrom(currentRoot));
            Trajectory.Points[RootPointIndex].SetDirection(
                new Basis(Vector3.Up, rotationalOffset) * Trajectory.Points[RootPointIndex].GetDirection()
            );
            Trajectory.Points[RootPointIndex].SetVelocity(translationalOffset.GetRelativeDirectionFrom(currentRoot) * Framerate);

            Transform3D nextRoot = Trajectory.Points[RootPointIndex].GetTransformation();
            nextRoot.Origin = new Vector3(nextRoot.Origin.X, 0f, nextRoot.Origin.Z); // For flat terrain

            // Update Future Trajectory
            for (int i = RootPointIndex + 1; i < Trajectory.Points.Length; i++)
            {
                Trajectory.Points[i].SetPosition(
                    Trajectory.Points[i].GetPosition() + rest * translationalOffset.GetRelativeDirectionFrom(nextRoot)
                );
                Trajectory.Points[i].SetDirection(
                    new Basis(Vector3.Up, rotationalOffset) * Trajectory.Points[i].GetDirection()
                );
                Trajectory.Points[i].SetVelocity(
                    Trajectory.Points[i].GetVelocity() + translationalOffset.GetRelativeDirectionFrom(nextRoot) * Framerate
                );
            }

            start = 0;
            for (int i = RootPointIndex + 1; i < Trajectory.Points.Length; i++)
            {
                int index = i;
                int prevSampleIndex = GetPreviousSample(index).GetIndex() / PointDensity;
                int nextSampleIndex = GetNextSample(index).GetIndex() / PointDensity;
                float factor = (float)(i % PointDensity) / PointDensity;

                Vector3 prevPos = new Vector3(
                    NN.GetOutput(start + (prevSampleIndex - 6) * TrajectoryDimOut + 0),
                    0f,
                    NN.GetOutput(start + (prevSampleIndex - 6) * TrajectoryDimOut + 1)
                ).GetRelativePositionFrom(nextRoot);

                Vector3 prevDir = new Vector3(
                    NN.GetOutput(start + (prevSampleIndex - 6) * TrajectoryDimOut + 2),
                    0f,
                    NN.GetOutput(start + (prevSampleIndex - 6) * TrajectoryDimOut + 3)
                ).Normalized().GetRelativeDirectionFrom(nextRoot);

                Vector3 prevVel = new Vector3(
                    NN.GetOutput(start + (prevSampleIndex - 6) * TrajectoryDimOut + 4),
                    0f,
                    NN.GetOutput(start + (prevSampleIndex - 6) * TrajectoryDimOut + 5)
                ).GetRelativeDirectionFrom(nextRoot);

                Vector3 nextPos = new Vector3(
                    NN.GetOutput(start + (nextSampleIndex - 6) * TrajectoryDimOut + 0),
                    0f,
                    NN.GetOutput(start + (nextSampleIndex - 6) * TrajectoryDimOut + 1)
                ).GetRelativePositionFrom(nextRoot);

                Vector3 nextDir = new Vector3(
                    NN.GetOutput(start + (nextSampleIndex - 6) * TrajectoryDimOut + 2),
                    0f,
                    NN.GetOutput(start + (nextSampleIndex - 6) * TrajectoryDimOut + 3)
                ).Normalized().GetRelativeDirectionFrom(nextRoot);

                Vector3 nextVel = new Vector3(
                    NN.GetOutput(start + (nextSampleIndex - 6) * TrajectoryDimOut + 4),
                    0f,
                    NN.GetOutput(start + (nextSampleIndex - 6) * TrajectoryDimOut + 5)
                ).GetRelativeDirectionFrom(nextRoot);

                Vector3 pos = (1f - factor) * prevPos + factor * nextPos;
                Vector3 dir = ((1f - factor) * prevDir + factor * nextDir).Normalized();
                Vector3 vel = (1f - factor) * prevVel + factor * nextVel;

                pos = (Trajectory.Points[i].GetPosition() + vel / Framerate).Lerp(pos, 0.5f);

                Trajectory.Points[i].SetPosition(
                    Utility.Interpolate(
                        Trajectory.Points[i].GetPosition(),
                        pos,
                        TrajectoryCorrection
                    )
                );
                Trajectory.Points[i].SetDirection(
                    Utility.Interpolate(
                        Trajectory.Points[i].GetDirection(),
                        dir,
                        TrajectoryCorrection
                    )
                );
                Trajectory.Points[i].SetVelocity(
                    Utility.Interpolate(
                        Trajectory.Points[i].GetVelocity(),
                        vel,
                        TrajectoryCorrection
                    )
                );
            }
            start += TrajectoryDimOut * 6;

            // Compute Posture
            for (int i = 0; i < Actor.Bones.Length; i++)
            {
                Vector3 position = new Vector3(
                    NN.GetOutput(start + i * JointDimOut + 0),
                    NN.GetOutput(start + i * JointDimOut + 1),
                    NN.GetOutput(start + i * JointDimOut + 2)
                ).GetRelativePositionFrom(currentRoot);

                Vector3 forward = new Vector3(
                    NN.GetOutput(start + i * JointDimOut + 3),
                    NN.GetOutput(start + i * JointDimOut + 4),
                    NN.GetOutput(start + i * JointDimOut + 5)
                ).Normalized().GetRelativeDirectionFrom(currentRoot);

                Vector3 up = new Vector3(
                    NN.GetOutput(start + i * JointDimOut + 6),
                    NN.GetOutput(start + i * JointDimOut + 7),
                    NN.GetOutput(start + i * JointDimOut + 8)
                ).Normalized().GetRelativeDirectionFrom(currentRoot);

                Vector3 velocity = new Vector3(
                    NN.GetOutput(start + i * JointDimOut + 9),
                    NN.GetOutput(start + i * JointDimOut + 10),
                    NN.GetOutput(start + i * JointDimOut + 11)
                ).GetRelativeDirectionFrom(currentRoot);

                Positions[i] = (Positions[i] + velocity / Framerate).Lerp(position, 0.5f);
                Forwards[i] = forward;
                Ups[i] = up;
                Velocities[i] = velocity;
            }
            start += JointDimOut * Actor.Bones.Length;

            // Assign Posture
            GlobalPosition = nextRoot.Origin;
            GlobalTransform = new Transform3D(nextRoot.Basis, nextRoot.Origin);

            for (int i = 0; i < Actor.Bones.Length; i++)
            {
                Actor.Bones[i].Transform.Position = Positions[i];
                Actor.Bones[i].Transform.Basis = Basis.LookingAt(-Forwards[i], Ups[i]); // Godot uses -Z as forward
            }
        }

        private float PoolBias()
        {
            float[] styles = Trajectory.Points[RootPointIndex].Styles;
            float bias = 0f;
            for (int i = 0; i < styles.Length; i++)
            {
                float _bias = Controller.Styles[i].Bias;
                float max = 0f;
                for (int j = 0; j < Controller.Styles[i].Multipliers.Length; j++)
                {
                    if (Input.IsKeyPressed(Controller.Styles[i].Multipliers[j].Key))
                    {
                        max = Mathf.Max(max, Controller.Styles[i].Bias * Controller.Styles[i].Multipliers[j].Value);
                    }
                }
                for (int j = 0; j < Controller.Styles[i].Multipliers.Length; j++)
                {
                    if (Input.IsKeyPressed(Controller.Styles[i].Multipliers[j].Key))
                    {
                        _bias = Mathf.Min(max, _bias * Controller.Styles[i].Multipliers[j].Value);
                    }
                }
                bias += styles[i] * _bias;
            }
            return bias;
        }

        private Trajectory.Point GetSample(int index)
        {
            return Trajectory.Points[Mathf.Clamp(index * 10, 0, Trajectory.Points.Length - 1)];
        }

        private Trajectory.Point GetPreviousSample(int index)
        {
            return GetSample(index / 10);
        }

        private Trajectory.Point GetNextSample(int index)
        {
            if (index % 10 == 0)
            {
                return GetSample(index / 10);
            }
            else
            {
                return GetSample(index / 10 + 1);
            }
        }

        // Rendering methods would need to be adapted to Godot's rendering system
        // This would typically be done in _Draw() for 2D or custom mesh rendering for 3D
        // For debugging/visualization, you might want to use DebugDraw or custom mesh instances

        // Extension methods would need to be implemented separately for Transform operations
        // like GetRelativePositionTo, GetRelativeDirectionTo, etc.
    }
}

// Note: You'll need to implement these extension methods for Transform3D and Vector3
// to handle the relative position/direction calculations used throughout the code