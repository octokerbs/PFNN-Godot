using Godot;
using System.Collections.Generic;

/// <summary>
/// Represents a trajectory composed of multiple points in 3D space.
/// Used for character movement prediction and path planning.
/// </summary>
public partial class Trajectory : Node
{

	/// <summary>
	/// Whether this trajectory should be inspected/debugged
	/// </summary>
	public bool Inspect = false;

	/// <summary>
	/// Array of trajectory points containing position, rotation, and style data
	/// </summary>
	public Point[] Points = new Point[0];

	/// <summary>
	/// Names of the different movement styles this trajectory can represent
	/// </summary>
	public string[] Styles = new string[0];

	/// <summary>
	/// Width used for ground sampling on left/right sides of trajectory points
	/// </summary>
	private static float Width = 0.5f;

	/// <summary>
	/// Default constructor required by Godot
	/// </summary>
	public Trajectory()
	{
		// Default constructor for Godot
	}

	/// <summary>
	/// Initializes trajectory with specified size and styles, all points at identity transform
	/// </summary>
	/// <param name="size">Number of points in the trajectory</param>
	/// <param name="styles">Array of style names</param>
	public Trajectory(int size, string[] styles)
	{
		Inspect = false;
		Points = new Point[size];
		Styles = styles;
		for (int i = 0; i < Points.Length; i++)
		{
			Points[i] = new Point(i, styles.Length);
			Points[i].SetTransformation(Transform3D.Identity);
		}
	}

	/// <summary>
	/// Initializes trajectory with all points starting from the same position and direction
	/// </summary>
	/// <param name="size">Number of points in the trajectory</param>
	/// <param name="styles">Array of style names</param>
	/// <param name="seedPosition">Starting position for all points</param>
	/// <param name="seedDirection">Starting direction for all points</param>
	public Trajectory(int size, string[] styles, Vector3 seedPosition, Vector3 seedDirection)
	{
		Inspect = false;
		Points = new Point[size];
		Styles = styles;
		for (int i = 0; i < Points.Length; i++)
		{
			Points[i] = new Point(i, styles.Length);
			Transform3D transform = new Transform3D();
			transform.Origin = seedPosition;
			transform.Basis = Basis.LookingAt(seedDirection, Vector3.Up);
			Points[i].SetTransformation(transform);
		}
	}

	/// <summary>
	/// Initializes trajectory with specific positions and directions for each point
	/// </summary>
	/// <param name="size">Number of points in the trajectory</param>
	/// <param name="styles">Array of style names</param>
	/// <param name="positions">Position for each point</param>
	/// <param name="directions">Direction for each point</param>
	public Trajectory(int size, string[] styles, Vector3[] positions, Vector3[] directions)
	{
		Inspect = false;
		Points = new Point[size];
		Styles = styles;
		for (int i = 0; i < Points.Length; i++)
		{
			Points[i] = new Point(i, styles.Length);
			Transform3D transform = new Transform3D();
			transform.Origin = positions[i];
			transform.Basis = Basis.LookingAt(directions[i], Vector3.Up);
			Points[i].SetTransformation(transform);
		}
	}

	/// <summary>
	/// Gets the first point in the trajectory
	/// </summary>
	/// <returns>First trajectory point</returns>
	public Point GetFirst()
	{
		return Points[0];
	}

	/// <summary>
	/// Gets the last point in the trajectory
	/// </summary>
	/// <returns>Last trajectory point</returns>
	public Point GetLast()
	{
		return Points[Points.Length - 1];
	}

	/// <summary>
	/// Calculates the total length of the trajectory by summing distances between consecutive points
	/// </summary>
	/// <returns>Total trajectory length</returns>
	public float GetLength()
	{
		float length = 0f;
		for (int i = 1; i < Points.Length; i++)
		{
			length += Points[i - 1].GetPosition().DistanceTo(Points[i].GetPosition());
		}
		return length;
	}

	/// <summary>
	/// Calculates trajectory length within a specific range with step intervals
	/// </summary>
	/// <param name="start">Starting index</param>
	/// <param name="end">Ending index</param>
	/// <param name="step">Step size between points</param>
	/// <returns>Length of trajectory segment</returns>
	public float GetLength(int start, int end, int step)
	{
		float length = 0f;
		for (int i = 0; i < end - step; i += step)
		{
			length += Points[i + step].GetPosition().DistanceTo(Points[i].GetPosition());
		}
		return length;
	}

	/// <summary>
	/// Calculates the curvature of the trajectory based on angle changes between segments
	/// </summary>
	/// <param name="start">Starting index</param>
	/// <param name="end">Ending index</param>
	/// <param name="step">Step size between points</param>
	/// <returns>Normalized curvature value (0-1)</returns>
	public float GetCurvature(int start, int end, int step)
	{
		float curvature = 0f;
		for (int i = step; i < end - step; i += step)
		{
			Vector3 v1 = Points[i].GetPosition() - Points[i - step].GetPosition();
			Vector3 v2 = Points[i + step].GetPosition() - Points[i].GetPosition();
			curvature += v1.SignedAngleTo(v2, Vector3.Up);
		}
		curvature = Mathf.Abs(curvature);
		curvature = Mathf.Clamp(curvature / 180f, 0f, 1f);
		return curvature;
	}

	/// <summary>
	/// Applies post-processing to all points in the trajectory (ground projection, slope calculation, etc.)
	/// </summary>
	public void Postprocess()
	{
		for (int i = 0; i < Points.Length; i++)
		{
			Points[i].Postprocess();
		}
	}

	/// <summary>
	/// Represents a single point in a trajectory with position, rotation, velocity, and style data
	/// </summary>
	public class Point
	{
		// Core transform data
		private int Index;                    // Index of this point in the trajectory
		private Transform3D Transformation;  // 3D transformation (position + rotation)
		private Vector3 Velocity;           // Current velocity vector
		private float Speed;                 // Speed magnitude

		// Ground sampling data
		private Vector3 LeftSample;          // Left side ground sample position
		private Vector3 RightSample;         // Right side ground sample position
		private float Slope;                 // Ground slope at this point

		// Animation/style data
		public float Phase;                  // Animation phase value
		public float[] Signals = new float[0];      // Binary style signals
		public float[] Styles = new float[0];       // Continuous style weights
		public float[] StyleUpdate = new float[0];  // Style update values

		/// <summary>
		/// Creates a new trajectory point
		/// </summary>
		/// <param name="index">Index of this point in the trajectory</param>
		/// <param name="styles">Number of style categories</param>
		public Point(int index, int styles)
		{
			Index = index;
			Transformation = Transform3D.Identity;
			Velocity = Vector3.Zero;
			LeftSample = Vector3.Zero;
			RightSample = Vector3.Zero;
			Slope = 0f;
			Signals = new float[styles];
			Styles = new float[styles];
			StyleUpdate = new float[styles];
		}

		// Index getter/setter
		public void SetIndex(int index)
		{
			Index = index;
		}

		public int GetIndex()
		{
			return Index;
		}

		// Transform getter/setter
		public void SetTransformation(Transform3D transform)
		{
			Transformation = transform;
		}

		public Transform3D GetTransformation()
		{
			return Transformation;
		}

		// Position getter/setter
		public void SetPosition(Vector3 position)
		{
			Transformation = new Transform3D(Transformation.Basis, position);
		}

		public Vector3 GetPosition()
		{
			return Transformation.Origin;
		}

		// Rotation getter/setter
		public void SetRotation(Quaternion rotation)
		{
			Transformation = new Transform3D(new Basis(rotation), Transformation.Origin);
		}

		public Quaternion GetRotation()
		{
			return Transformation.Basis.GetRotationQuaternion();
		}

		// Direction getter/setter (forward vector)
		public void SetDirection(Vector3 direction)
		{
			if (direction == Vector3.Zero)
			{
				direction = Vector3.Forward;
			}
			Basis basis = Basis.LookingAt(direction, Vector3.Up);
			SetRotation(basis.GetRotationQuaternion());
		}

		public Vector3 GetDirection()
		{
			return -Transformation.Basis.Z; // Forward vector in Godot
		}

		// Velocity getter/setter
		public void SetVelocity(Vector3 velocity)
		{
			Velocity = velocity;
		}

		public Vector3 GetVelocity()
		{
			return Velocity;
		}

		// Speed getter/setter
		public void SetSpeed(float speed)
		{
			Speed = speed;
		}

		public float GetSpeed()
		{
			return Speed;
		}

		// Phase getter/setter
		public void SetPhase(float value)
		{
			Phase = value;
		}

		public float GetPhase()
		{
			return Phase;
		}

		// Ground sampling getters/setters
		public void SetLeftsample(Vector3 position)
		{
			LeftSample = position;
		}

		public Vector3 GetLeftSample()
		{
			return LeftSample;
		}

		public void SetRightSample(Vector3 position)
		{
			RightSample = position;
		}

		public Vector3 GetRightSample()
		{
			return RightSample;
		}

		// Slope getter/setter
		public void SetSlope(float slope)
		{
			Slope = slope;
		}

		public float GetSlope()
		{
			return Slope;
		}

		/// <summary>
		/// Post-processes this point by projecting to ground, calculating slope, and sampling left/right positions
		/// Requires Utility class with GetHeight() and GetSlope() methods
		/// </summary>
		public void Postprocess()
		{
			uint mask = 1u << 0; // Assuming "Ground" layer is layer 0, adjust as needed
			Vector3 position = Transformation.Origin;
			Vector3 direction = GetDirection();

			// Project position to ground
			position.Y = Utility.GetHeight(Transformation.Origin, mask);
			SetPosition(position);

			// Calculate ground slope at this position
			Slope = Utility.GetSlope(position, mask);

			// Calculate left and right sample positions for ground contact
			Basis rotationBasis = new Basis(Vector3.Up, Mathf.DegToRad(90f));
			Vector3 ortho = rotationBasis * direction;
			RightSample = position + Trajectory.Width * ortho.Normalized();
			RightSample.Y = Utility.GetHeight(RightSample, mask);
			LeftSample = position - Trajectory.Width * ortho.Normalized();
			LeftSample.Y = Utility.GetHeight(LeftSample, mask);
		}
	}

	// Debug visualization method - currently empty since UltiDraw was removed
	// You can implement your own drawing logic here if needed
	public void Draw(int step = 1)
	{
		for (int i = 0; i < Points.Length; i += step)
		{
			Vector3 pos = Points[i].GetPosition();
			Vector3 dir = Points[i].GetDirection();

			// Draw point as sphere
			DebugDraw3D.DrawSphere(pos, 0.1f, Colors.Red);

			// Draw direction arrow
			DebugDraw3D.DrawArrow(pos, pos + dir * 0.5f, Colors.Blue, 0.05f);

			// Draw line to next point
			if (i + step < Points.Length)
			{
				DebugDraw3D.DrawLine(pos, Points[i + step].GetPosition(), Colors.Green);
			}

			// Draw left/right samples
			DebugDraw3D.DrawSphere(Points[i].GetLeftSample(), 0.05f, Colors.Yellow);
			DebugDraw3D.DrawSphere(Points[i].GetRightSample(), 0.05f, Colors.Cyan);
		}
	}

}