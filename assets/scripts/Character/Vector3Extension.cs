using Godot;

/// <summary>
/// Extension methods for Vector3 to handle coordinate transformations and mirroring operations.
/// Provides functionality for converting between different coordinate spaces and reflection operations.
/// </summary>
public static class Vector3Extensions {
    
    /// <summary>
    /// Transforms a position from world space to the local space of the given transform.
    /// </summary>
    /// <param name="position">The world space position to transform</param>
    /// <param name="from">The transform to convert from (world to local)</param>
    /// <returns>Position in the local space of the 'from' transform</returns>
    public static Vector3 GetRelativePositionFrom(this Vector3 position, Transform3D from) {
        return from * position;
    }
    
    /// <summary>
    /// Transforms a position from the local space of the given transform to world space.
    /// </summary>
    /// <param name="position">The local space position to transform</param>
    /// <param name="to">The transform to convert to (local to world)</param>
    /// <returns>Position in world space</returns>
    public static Vector3 GetRelativePositionTo(this Vector3 position, Transform3D to) {
        return to.AffineInverse() * position;
    }
    
    /// <summary>
    /// Transforms a direction vector from world space to the local space of the given transform.
    /// Does not apply translation, only rotation and scale.
    /// </summary>
    /// <param name="direction">The world space direction to transform</param>
    /// <param name="from">The transform to convert from (world to local)</param>
    /// <returns>Direction in the local space of the 'from' transform</returns>
    public static Vector3 GetRelativeDirectionFrom(this Vector3 direction, Transform3D from) {
        return from.Basis * direction;
    }
    
    /// <summary>
    /// Transforms a direction vector from the local space of the given transform to world space.
    /// Does not apply translation, only rotation and scale.
    /// </summary>
    /// <param name="direction">The local space direction to transform</param>
    /// <param name="to">The transform to convert to (local to world)</param>
    /// <returns>Direction in world space</returns>
    public static Vector3 GetRelativeDirectionTo(this Vector3 direction, Transform3D to) {
        return to.Basis.Inverse() * direction;
    }
    
    /// <summary>
    /// Creates a mirrored version of the vector along the specified axis.
    /// Flips the component corresponding to the given axis direction.
    /// </summary>
    /// <param name="vector">The vector to mirror</param>
    /// <param name="axis">The axis to mirror along (Right, Up, or Forward)</param>
    /// <returns>The mirrored vector</returns>
    public static Vector3 GetMirror(this Vector3 vector, Vector3 axis) {
        // Create a copy to avoid modifying the original (Vector3 is a struct)
        Vector3 result = vector;
        
        // Check against Godot's standard axis vectors
        if(axis == Vector3.Right) {
            result.X *= -1f;
        }
        if(axis == Vector3.Up) {
            result.Y *= -1f;
        }
        if(axis == Vector3.Forward) {
            result.Z *= -1f;
        }
        
        return result;
    }
}