using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

#if TOOLS
using Godot;
#endif

/// <summary>
/// Actor class for managing skeletal hierarchies in Godot.
/// Provides functionality for skeleton extraction, bone management, and posture tracking.
/// </summary>
[GlobalClass]
public partial class Actor : Node3D {

	#region Inspector Properties
	/// <summary>Inspector flag for skeleton visualization (legacy - kept for compatibility)</summary>
	[Export] public bool InspectSkeleton = false;
	
	/// <summary>Drawing flags (legacy - kept for compatibility)</summary>
	[Export] public bool DrawRoot = false;
	[Export] public bool DrawSkeleton = true;
	[Export] public bool DrawVelocities = false;
	[Export] public bool DrawTransforms = false;
	
	/// <summary>Visual properties (legacy - kept for compatibility)</summary>
	[Export] public float BoneSize = 0.025f;
	[Export] public Color BoneColor = Colors.Black;
	[Export] public Color JointColor = Colors.Yellow;
	#endregion

	#region Core Data
	/// <summary>Array containing all bones in the skeleton hierarchy</summary>
	public Bone[] Bones = new Bone[0];
	#endregion

	#region Initialization
	/// <summary>
	/// Called when the node is ready. Automatically extracts skeleton in editor mode.
	/// </summary>
	public override void _Ready() {
		if (Engine.IsEditorHint()) {
			ExtractSkeleton();
		}
	}
	#endregion

	#region Node Access Methods
	/// <summary>
	/// Gets the root node of this actor (itself).
	/// </summary>
	/// <returns>The root Node3D</returns>
	public Node3D GetRoot() {
		return this;
	}

	/// <summary>
	/// Recursively finds a Node3D by name within this actor's hierarchy.
	/// </summary>
	/// <param name="name">Name of the node to find</param>
	/// <returns>The found Node3D or null if not found</returns>
	public Node3D FindTransform(string name) {
		Node3D element = null;
		System.Action<Node3D> recursion = null;
		recursion = new System.Action<Node3D>((node) => {
			if(node.Name == name) {
				element = node;
				return;
			}
			// Iterate through all children, filtering for Node3D types
			foreach(Node child in node.GetChildren()) {
				if(child is Node3D node3D) {
					recursion(node3D);
				}
			}
		});
		recursion(GetRoot());
		return element;
	}
	#endregion

	#region Bone Search Methods
	/// <summary>
	/// Finds a bone by its associated Node3D transform.
	/// </summary>
	/// <param name="transform">The Node3D transform to search for</param>
	/// <returns>The corresponding bone or null if not found</returns>
	public Bone FindBone(Node3D transform) {
		return System.Array.Find(Bones, x => x.Transform == transform);
	}

	/// <summary>
	/// Finds a bone by its exact name.
	/// </summary>
	/// <param name="name">Exact name of the bone to find</param>
	/// <returns>The bone with matching name or null if not found</returns>
	public Bone FindBone(string name) {
		return System.Array.Find(Bones, x => x.GetName() == name);
	}

	/// <summary>
	/// Finds a bone whose name contains the specified string.
	/// </summary>
	/// <param name="name">Partial name to search for</param>
	/// <returns>The first bone containing the name or null if not found</returns>
	public Bone FindBoneContains(string name) {
		return System.Array.Find(Bones, x => x.GetName().Contains(name));
	}
	#endregion

	#region Skeleton Extraction
	/// <summary>
	/// Extracts the complete skeleton hierarchy starting from the root node.
	/// Recursively processes all Node3D children to build the bone structure.
	/// </summary>
	public void ExtractSkeleton() {
		ArrayExtensions.Clear(ref Bones);
		System.Action<Node3D, Bone> recursion = null;
		recursion = new System.Action<Node3D, Bone>((node, parent) => {
			// Create a new bone for this node
			Bone bone = new Bone(this, node, Bones.Length);
			ArrayExtensions.Add(ref Bones, bone);
			
			// Establish parent-child relationships
			if(parent != null) {
				bone.Parent = parent.Index;
				ArrayExtensions.Add(ref parent.Childs, bone.Index);
			}
			parent = bone;
			
			// Recursively process all Node3D children
			foreach(Node child in node.GetChildren()) {
				if(child is Node3D node3D) {
					recursion(node3D, parent);
				}
			}
		});
		recursion(GetRoot(), null);
	}

	/// <summary>
	/// Extracts skeleton from a specific set of Node3D transforms.
	/// Only processes nodes that are included in the provided array.
	/// </summary>
	/// <param name="bones">Array of Node3D transforms to include in the skeleton</param>
	public void ExtractSkeleton(Node3D[] bones) {
		ArrayExtensions.Clear(ref Bones);
		System.Action<Node3D, Bone> recursion = null;
		recursion = new System.Action<Node3D, Bone>((node, parent) => {
			// Only process nodes that are in the specified bones array
			if(System.Array.Find(bones, x => x == node) != null) {
				Bone bone = new Bone(this, node, Bones.Length);
				ArrayExtensions.Add(ref Bones, bone);
				
				// Establish parent-child relationships
				if(parent != null) {
					bone.Parent = parent.Index;
					ArrayExtensions.Add(ref parent.Childs, bone.Index);
				}
				parent = bone;
			}
			
			// Continue recursion through all children
			foreach(Node child in node.GetChildren()) {
				if(child is Node3D node3D) {
					recursion(node3D, parent);
				}
			}
		});
		recursion(GetRoot(), null);
	}
	#endregion

	#region Posture and Motion Data
	/// <summary>
	/// Gets the current world-space transformation matrices for all bones.
	/// Useful for animation systems and pose analysis.
	/// </summary>
	/// <returns>Array of Transform3D representing each bone's world transform</returns>
	public Transform3D[] GetPosture() {
		Transform3D[] posture = new Transform3D[Bones.Length];
		for(int i=0; i<posture.Length; i++) {
			posture[i] = Bones[i].Transform.GlobalTransform;
		}
		return posture;
	}

	/// <summary>
	/// Gets the current velocity vectors for all bones.
	/// Used for motion analysis and animation blending.
	/// </summary>
	/// <returns>Array of Vector3 representing each bone's velocity</returns>
	public Vector3[] GetVelocities() {
		Vector3[] velocities = new Vector3[Bones.Length];
		for(int i=0; i<velocities.Length; i++) {
			velocities[i] = Bones[i].Velocity;
		}
		return velocities;
	}
	#endregion

	#region Godot Lifecycle
	/// <summary>
	/// Called every frame. Currently unused but kept for potential future debug functionality.
	/// </summary>
	/// <param name="delta">Time since last frame</param>
	public override void _Process(double delta) {
		// Drawing functionality removed - no debug rendering needed
	}
	#endregion

	#region Nested Bone Class
	/// <summary>
	/// Represents a single bone in the skeletal hierarchy.
	/// Contains transform reference, velocity data, and hierarchical relationships.
	/// </summary>
	[System.Serializable]
	public class Bone {
		#region Properties
		/// <summary>Reference to the parent Actor</summary>
		public Actor Actor;
		
		/// <summary>The Node3D transform this bone represents</summary>
		public Node3D Transform;
		
		/// <summary>Current velocity of this bone</summary>
		public Vector3 Velocity;
		
		/// <summary>Index of this bone in the Actor's Bones array</summary>
		public int Index;
		
		/// <summary>Index of the parent bone (-1 if root)</summary>
		public int Parent;
		
		/// <summary>Array of child bone indices</summary>
		public int[] Childs;
		#endregion

		#region Constructor
		/// <summary>
		/// Creates a new bone instance.
		/// </summary>
		/// <param name="actor">Reference to the parent Actor</param>
		/// <param name="transform">The Node3D this bone represents</param>
		/// <param name="index">Index in the bone array</param>
		public Bone(Actor actor, Node3D transform, int index) {
			Actor = actor;
			Transform = transform;
			Velocity = Vector3.Zero;
			Index = index;
			Parent = -1;
			Childs = new int[0];
		}
		#endregion

		#region Accessor Methods
		/// <summary>
		/// Gets the name of this bone (from its Node3D).
		/// </summary>
		/// <returns>The bone's name</returns>
		public string GetName() {
			return Transform.Name;
		}

		/// <summary>
		/// Gets the parent bone of this bone.
		/// </summary>
		/// <returns>Parent bone or null if this is the root</returns>
		public Bone GetParent() {
			return Parent == -1 ? null : Actor.Bones[Parent];
		}

		/// <summary>
		/// Gets a child bone by index.
		/// </summary>
		/// <param name="index">Index of the child to retrieve</param>
		/// <returns>Child bone or null if index is out of range</returns>
		public Bone GetChild(int index) {
			return index >= Childs.Length ? null : Actor.Bones[Childs[index]];
		}

		/// <summary>
		/// Calculates the distance from this bone to its parent.
		/// Returns 0 for root bones.
		/// </summary>
		/// <returns>Distance to parent bone</returns>
		public float GetLength() {
			if(GetParent() == null) {
				return 0f;
			} else {
				return GetParent().Transform.GlobalPosition.DistanceTo(Transform.GlobalPosition);
			}
		}
		#endregion
	}
	#endregion

	#region Editor Support (Legacy)
	#if TOOLS
	// Note: Godot's editor plugins work differently than Unity's custom editors
	// You'll need to create a separate EditorPlugin for the inspector functionality
	// This is just a placeholder structure showing how the data would be organized
	
	/*
	[Tool]
	public partial class ActorEditorPlugin : EditorPlugin {
		// Editor functionality would go here
		// Godot uses a different system for custom inspectors
	}
	*/
	#endif
	#endregion
}