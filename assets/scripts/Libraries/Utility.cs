using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public sealed class Gaussian
{
    private bool _hasDeviate;
    private double _storedDeviate;
    private readonly Random _random;

    public Gaussian(Random random = null)
    {
        _random = random ?? new Random();
    }

    /// <summary>
    /// Obtains normally (Gaussian) distributed random numbers, using the Box-Muller
    /// transformation.  This transformation takes two uniformly distributed deviates
    /// within the unit circle, and transforms them into two independently
    /// distributed normal deviates.
    /// </summary>
    /// <param name="mu">The mean of the distribution.  Default is zero.</param>
    /// <param name="sigma">The standard deviation of the distribution.  Default is one.</param>
    /// <returns></returns>
    public double NextGaussian(double mu = 0, double sigma = 1)
    {
        if (sigma <= 0)
            throw new ArgumentOutOfRangeException("sigma", "Must be greater than zero.");

        if (_hasDeviate)
        {
            _hasDeviate = false;
            return _storedDeviate * sigma + mu;
        }

        double v1, v2, rSquared;
        do
        {
            // two random values between -1.0 and 1.0
            v1 = 2 * _random.NextDouble() - 1;
            v2 = 2 * _random.NextDouble() - 1;
            rSquared = v1 * v1 + v2 * v2;
            // ensure within the unit circle
        } while (rSquared >= 1 || rSquared == 0);

        // calculate polar tranformation for each deviate
        var polar = Math.Sqrt(-2 * Math.Log(rSquared) / rSquared);
        // store first deviate
        _storedDeviate = v2 * polar;
        _hasDeviate = true;
        // return second deviate
        return v1 * polar * sigma + mu;
    }
}

public static class Utility
{
    public static Random RNG;

    public static Random GetRNG()
    {
        if (RNG == null)
        {
            RNG = new Random();
        }
        return RNG;
    }

    public static Quaternion QuaternionEuler(float roll, float pitch, float yaw)
    {
        roll = Mathf.DegToRad(roll) / 2f;
        pitch = Mathf.DegToRad(pitch) / 2f;
        yaw = Mathf.DegToRad(yaw) / 2f;

        Vector3 Z = Vector3.Forward;
        Vector3 X = Vector3.Right;
        Vector3 Y = Vector3.Up;

        float sin, cos;

        sin = (float)Math.Sin(roll);
        cos = (float)Math.Cos(roll);
        Quaternion q1 = new Quaternion(0f, 0f, Z.Z * sin, cos);
        sin = (float)Math.Sin(pitch);
        cos = (float)Math.Cos(pitch);
        Quaternion q2 = new Quaternion(X.X * sin, 0f, 0f, cos);
        sin = (float)Math.Sin(yaw);
        cos = (float)Math.Cos(yaw);
        Quaternion q3 = new Quaternion(0f, Y.Y * sin, 0f, cos);

        return MultiplyQuaternions(MultiplyQuaternions(q1, q2), q3);
    }

    public static Quaternion MultiplyQuaternions(Quaternion q1, Quaternion q2)
    {
        float x = q1.X * q2.W + q1.Y * q2.Z - q1.Z * q2.Y + q1.W * q2.X;
        float y = -q1.X * q2.Z + q1.Y * q2.W + q1.Z * q2.X + q1.W * q2.Y;
        float z = q1.X * q2.Y - q1.Y * q2.X + q1.Z * q2.W + q1.W * q2.Z;
        float w = -q1.X * q2.X - q1.Y * q2.Y - q1.Z * q2.Z + q1.W * q2.W;
        return new Quaternion(x, y, z, w);
    }

    public static void Screenshot(string name, int x, int y, int width, int height)
    {
        var viewport = GetTree().CurrentScene.GetViewport();
        var img = viewport.GetTexture().GetImage();

        // Create a new image with the specified region
        var croppedImg = Godot.Image.CreateEmpty(width, height, false, Image.Format.Rg8);  //Image.Create(width, height, false, Image.Format.Rgb8);
        croppedImg.BlitRect(img, new Rect2I(x, y, width, height), Vector2I.Zero);

        croppedImg.SavePng(name + ".png");
    }

    private static SceneTree GetTree()
    {
        return Engine.GetMainLoop() as SceneTree;
    }

    public static void SetFPS(int fps)
    {
        Engine.MaxFps = fps;
    }

    public static DateTime GetTimestamp()
    {
        return DateTime.Now;
    }

    public static double GetElapsedTime(DateTime timestamp)
    {
        return (DateTime.Now - timestamp).Duration().TotalSeconds;
    }

    public static float Exponential01(float value)
    {
        float basis = 2f;
        return (Mathf.Pow(basis, Mathf.Clamp(value, 0f, 1f)) - 1f) / (basis - 1f);
    }

    public static float Normalise(float value, float valueMin, float valueMax, float resultMin, float resultMax)
    {
        if (valueMax - valueMin != 0f)
        {
            return (value - valueMin) / (valueMax - valueMin) * (resultMax - resultMin) + resultMin;
        }
        else
        {
            return value;
        }
    }

    public static double Normalise(double value, double valueMin, double valueMax, double resultMin, double resultMax)
    {
        if (valueMax - valueMin != 0)
        {
            return (value - valueMin) / (valueMax - valueMin) * (resultMax - resultMin) + resultMin;
        }
        else
        {
            return value;
        }
    }

    //0 = Amplitude, 1 = Frequency, 2 = Shift, 3 = Offset, 4 = Slope, 5 = Time
    public static float LinSin(float a, float f, float s, float o, float m, float t)
    {
        return a * Mathf.Sin(f * (t - s) * 2f * Mathf.Pi) + o + m * t;
    }

    public static float LinSin1(float a, float f, float s, float o, float m, float t)
    {
        return a * f * Mathf.Cos(f * (t - s) * 2f * Mathf.Pi) + m;
    }

    public static float LinSin2(float a, float f, float s, float o, float m, float t)
    {
        return a * f * f * -Mathf.Sin(f * (t - s) * 2f * Mathf.Pi);
    }

    public static float LinSin3(float a, float f, float s, float o, float m, float t)
    {
        return a * f * f * f * -Mathf.Cos(f * (t - s) * 2f * Mathf.Pi);
    }

    public static float Gaussian(float mean, float std, float x)
    {
        return 1f / (std * Mathf.Sqrt(2f * Mathf.Pi)) * Mathf.Exp(-0.5f * (x * x) / (std * std));
    }

    public static float Sigmoid(float x)
    {
        return 1f / (1f + Mathf.Exp(-x));
    }

    public static float TanH(float x)
    {
        float positive = Mathf.Exp(x);
        float negative = Mathf.Exp(-x);
        return (positive - negative) / (positive + negative);
    }

    public static float Interpolate(float from, float to, float amount)
    {
        amount = Mathf.Clamp(amount, 0f, 1f);
        return (1f - amount) * from + amount * to;
    }

    public static double Interpolate(double from, double to, float amount)
    {
        amount = Mathf.Clamp(amount, 0f, 1f);
        return (1f - amount) * from + amount * to;
    }

    public static Vector2 Interpolate(Vector2 from, Vector2 to, float amount)
    {
        amount = Mathf.Clamp(amount, 0f, 1f);
        return (1f - amount) * from + amount * to;
    }

    public static Vector3 Interpolate(Vector3 from, Vector3 to, float amount)
    {
        amount = Mathf.Clamp(amount, 0f, 1f);
        return (1f - amount) * from + amount * to;
    }

    public static Quaternion Interpolate(Quaternion from, Quaternion to, float amount)
    {
        amount = Mathf.Clamp(amount, 0f, 1f);
        return from.Slerp(to, amount);
    }

    public static Transform3D Interpolate(Transform3D from, Transform3D to, float amount)
    {
        return from.InterpolateWith(to, amount);
    }

    public static float[] Interpolate(float[] from, float[] to, float amount)
    {
        if (from.Length != to.Length)
        {
            GD.Print("Interpolation not possible.");
            return from;
        }
        float[] result = new float[from.Length];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = Interpolate(from[i], to[i], amount);
        }
        return result;
    }

    public static float GetSignedAngle(Vector3 A, Vector3 B, Vector3 axis)
    {
        return Mathf.RadToDeg(Mathf.Atan2(axis.Dot(A.Cross(B)), A.Dot(B)));
    }

    public static Vector3 RotateAround(Vector3 vector, Vector3 pivot, Vector3 axis, float angle)
    {
        return new Basis(axis, angle) * (vector - pivot) + vector;
    }

    public static Vector3 ProjectCollision(Vector3 start, Vector3 end, uint mask)
    {
        var spaceState = GetTree().CurrentScene.GetViewport().GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(start, end, mask);
        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            return result["position"].AsVector3();
        }
        return end;
    }

    public static Vector3 ProjectGround(Vector3 position, uint mask)
    {
        position.Y = GetHeight(position, mask);
        return position;
    }

    public static float GetHeight(Vector3 origin, uint mask)
    {
        return 0;
    }

    public static float GetSlope(Vector3 origin, uint mask)
    {
        var spaceState = GetTree().CurrentScene.GetViewport().GetWorld3D().DirectSpaceState;

        var upQuery = PhysicsRayQueryParameters3D.Create(origin + Vector3.Down, origin + Vector3.Up * 1000, mask);
        var downQuery = PhysicsRayQueryParameters3D.Create(origin + Vector3.Up, origin + Vector3.Down * 1000, mask);

        var upResults = spaceState.IntersectRay(upQuery);
        var downResults = spaceState.IntersectRay(downQuery);

        if (upResults.Count == 0 && downResults.Count == 0)
        {
            return 0f;
        }

        Vector3 normal = Vector3.Up;
        float height = float.MinValue;

        if (downResults.Count > 0)
        {
            var point = downResults["position"].AsVector3();
            var hitNormal = downResults["normal"].AsVector3();
            if (point.Y > height)
            {
                height = point.Y;
                normal = hitNormal;
            }
        }

        if (upResults.Count > 0)
        {
            var point = upResults["position"].AsVector3();
            var hitNormal = upResults["normal"].AsVector3();
            if (point.Y > height)
            {
                height = point.Y;
                normal = hitNormal;
            }
        }

        return normal.AngleTo(Vector3.Up) / (Mathf.Pi / 2f);
    }

    public static Vector3 GetNormal(Vector3 position, Vector3 point, CollisionObject3D collider, float radius, uint mask)
    {
        var spaceState = GetTree().CurrentScene.GetViewport().GetWorld3D().DirectSpaceState;

        if (position == point)
        {
            List<Godot.Collections.Dictionary> hits = new List<Godot.Collections.Dictionary>();
            Transform3D transform = collider.Transform;

            Vector3 x = transform.Basis.X;
            Vector3 y = transform.Basis.Y;
            Vector3 z = transform.Basis.Z;

            var queries = new[]
            {
                PhysicsRayQueryParameters3D.Create(point + radius * x, point - radius * x, mask),
                PhysicsRayQueryParameters3D.Create(point - radius * x, point + radius * x, mask),
                PhysicsRayQueryParameters3D.Create(point + radius * y, point - radius * y, mask),
                PhysicsRayQueryParameters3D.Create(point - radius * y, point + radius * y, mask),
                PhysicsRayQueryParameters3D.Create(point + radius * z, point - radius * z, mask),
                PhysicsRayQueryParameters3D.Create(point - radius * z, point + radius * z, mask)
            };

            foreach (var query in queries)
            {
                var result = spaceState.IntersectRay(query);
                if (result.Count > 0)
                {
                    hits.Add(result);
                }
            }

            if (hits.Count > 0)
            {
                var closest = hits[0];
                var closestPoint = closest["position"].AsVector3();

                for (int k = 1; k < hits.Count; k++)
                {
                    var hitPoint = hits[k]["position"].AsVector3();
                    if (point.DistanceTo(hitPoint) < point.DistanceTo(closestPoint))
                    {
                        closest = hits[k];
                        closestPoint = hitPoint;
                    }
                }
                return closest["normal"].AsVector3();
            }
            else
            {
                GD.Print("Could not compute normal for collider " + collider.Name + ".");
                return Vector3.Zero;
            }
        }
        else
        {
            var query = PhysicsRayQueryParameters3D.Create(position, point, mask);
            var result = spaceState.IntersectRay(query);

            if (result.Count > 0)
            {
                return result["normal"].AsVector3();
            }
            else
            {
                GD.Print("Could not compute normal for collider " + collider.Name + ".");
                return Vector3.Zero;
            }
        }
    }

    public static Vector3 GetNormal(Vector3 origin, uint mask)
    {
        var spaceState = GetTree().CurrentScene.GetViewport().GetWorld3D().DirectSpaceState;

        var upQuery = PhysicsRayQueryParameters3D.Create(origin + Vector3.Down, origin + Vector3.Up * 1000, mask);
        var downQuery = PhysicsRayQueryParameters3D.Create(origin + Vector3.Up, origin + Vector3.Down * 1000, mask);

        var upResults = spaceState.IntersectRay(upQuery);
        var downResults = spaceState.IntersectRay(downQuery);

        if (upResults.Count == 0 && downResults.Count == 0)
        {
            return Vector3.Up;
        }

        Vector3 normal = Vector3.Up;
        float height = float.MinValue;

        if (downResults.Count > 0)
        {
            var point = downResults["position"].AsVector3();
            var hitNormal = downResults["normal"].AsVector3();
            if (point.Y > height)
            {
                height = point.Y;
                normal = hitNormal;
            }
        }

        if (upResults.Count > 0)
        {
            var point = upResults["position"].AsVector3();
            var hitNormal = upResults["normal"].AsVector3();
            if (point.Y > height)
            {
                height = point.Y;
                normal = hitNormal;
            }
        }

        return normal;
    }

    public static void Destroy(GodotObject obj)
    {
        if (obj != null && GodotObject.IsInstanceValid(obj))
        {
            if (obj is Node node)
            {
                node.QueueFree();
            }
            else
            {
                obj.Dispose();
            }
        }
    }

    // GUI functions removed as Godot uses different UI system
    // Use Control nodes and their methods instead

    public static Rect2 GetGUIRect(float x, float y, float width, float height)
    {
        var screenSize = DisplayServer.ScreenGetSize();
        return new Rect2(x * screenSize.X, y * screenSize.Y, width * screenSize.X, height * screenSize.Y);
    }

    public static int ReadInt(string value)
    {
        value = FilterValueField(value);
        return ParseInt(value);
    }

    public static float ReadFloat(string value)
    {
        value = FilterValueField(value);
        return ParseFloat(value);
    }

    public static float[] ReadArray(string value)
    {
        value = FilterValueField(value);
        if (value.StartsWith(" "))
        {
            value = value.Substring(1);
        }
        if (value.EndsWith(" "))
        {
            value = value.Substring(0, value.Length - 1);
        }
        string[] values = value.Split(' ');
        float[] array = new float[values.Length];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = ParseFloat(values[i]);
        }
        return array;
    }

    public static string FilterValueField(string value)
    {
        while (value.Contains("  "))
        {
            value = value.Replace("  ", " ");
        }
        while (value.Contains("< "))
        {
            value = value.Replace("< ", "<");
        }
        while (value.Contains(" >"))
        {
            value = value.Replace(" >", ">");
        }
        while (value.Contains(" ."))
        {
            value = value.Replace(" .", " 0.");
        }
        while (value.Contains(". "))
        {
            value = value.Replace(". ", ".0");
        }
        while (value.Contains("<."))
        {
            value = value.Replace("<.", "<0.");
        }
        return value;
    }

    public static int ParseInt(string value)
    {
        int parsed = 0;
        if (int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }
        else
        {
            return 0;
        }
    }

    public static float ParseFloat(string value)
    {
        float parsed = 0f;
        if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }
        else
        {
            return 0f;
        }
    }

    public static void Normalise(ref float[] values)
    {
        float sum = 0f;
        for (int i = 0; i < values.Length; i++)
        {
            sum += Mathf.Abs(values[i]);
        }
        if (sum != 0f)
        {
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = Mathf.Abs(values[i]) / sum;
            }
        }
    }

    public static void SoftMax(ref float[] values)
    {
        float frac = 0f;
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = Mathf.Exp(values[i]);
            frac += values[i];
        }
        for (int i = 0; i < values.Length; i++)
        {
            values[i] /= frac;
        }
    }

    public static Quaternion QuaternionAverage(Quaternion[] quaternions)
    {
        Vector3 forward = Vector3.Zero;
        Vector3 upwards = Vector3.Zero;
        for (int i = 0; i < quaternions.Length; i++)
        {
            forward += quaternions[i] * Vector3.Forward;
            upwards += quaternions[i] * Vector3.Up;
        }
        forward /= quaternions.Length;
        upwards /= quaternions.Length;
        return Quaternion.FromEuler(forward.Normalized().Cross(upwards.Normalized()));
    }

    public static float GaussianValue(float mean, float sigma)
    {
        if (mean == 0f && sigma == 0f)
        {
            return 0f;
        }

        double x1 = 1 - GetRNG().NextDouble();
        double x2 = 1 - GetRNG().NextDouble();
        double y1 = Math.Sqrt(-2.0 * Math.Log(x1)) * Math.Cos(2.0 * Math.PI * x2);
        return (float)(y1 * sigma + mean);
    }

    public static Vector3 GaussianVector3(float mean, float sigma)
    {
        return new Vector3(GaussianValue(mean, sigma), GaussianValue(mean, sigma), GaussianValue(mean, sigma));
    }

    public static float PhaseUpdate(float from, float to)
    {
        return ((to - from + 1f) % 1f + 1f) % 1f; // Godot equivalent of Mathf.Repeat
    }

    public static Vector2 PhaseVector(float phase)
    {
        phase *= 2f * Mathf.Pi;
        return new Vector2(Mathf.Sin(phase), Mathf.Cos(phase));
    }

    public static float PhaseAverage(float[] values)
    {
        float[] x = new float[values.Length];
        float[] y = new float[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            Vector2 v = PhaseVector(values[i]);
            x[i] = v.X;
            y[i] = v.Y;
        }
        var vec = new Vector2(FilterGaussian(x), FilterGaussian(y)).Normalized();
        return (((-Vector2.Up.AngleTo(vec) / (2f * Mathf.Pi)) % 1f) + 1f) % 1f;
    }

    public static float FilterGaussian(float[] values)
    {
        if (values.Length == 0)
        {
            return 0f;
        }
        float window = ((float)values.Length - 1f) / 2f;
        float sum = 0f;
        float value = 0f;
        for (int i = 0; i < values.Length; i++)
        {
            float weight = Mathf.Exp(-Mathf.Pow((float)i - window, 2f) / Mathf.Pow(0.5f * window, 2f));
            value += weight * values[i];
            sum += weight;
        }
        return value / sum;
    }
}