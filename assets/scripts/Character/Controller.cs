using Godot;


[System.Serializable]
public partial class Controller : Resource
{
	[Export] public bool Inspect = false;

	[Export] public Key Forward = Key.W;
	[Export] public Key Back = Key.S;
	[Export] public Key Left = Key.A;
	[Export] public Key Right = Key.D;
	[Export] public Key TurnLeft = Key.Q;
	[Export] public Key TurnRight = Key.E;

	[Export] public Style[] Styles = { new Style(), new Style(), new Style() };

	public float[] GetStyle()
	{
		float[] style = new float[Styles.Length];
		for (int i = 0; i < Styles.Length; i++)
		{
			style[i] = Styles[i].Query() ? 1f : 0f;
		}
		return style;
	}

	public string[] GetNames()
	{
		string[] names = new string[Styles.Length];
		for (int i = 0; i < names.Length; i++)
		{
			names[i] = Styles[i].Name;
		}
		return names;
	}

	public Vector3 QueryMove()
	{
		Vector3 move = Vector3.Zero;
		if (Input.IsKeyPressed(Forward))
		{
			move.Z -= 1f;
		}
		if (Input.IsKeyPressed(Back))
		{
			move.Z += 1f;
		}
		if (Input.IsKeyPressed(Left))
		{
			move.X -= 1f;
		}
		if (Input.IsKeyPressed(Right))
		{
			move.X += 1f;
		}
		return move;
	}

	public float QueryTurn()
	{
		float turn = 0f;
		if (Input.IsKeyPressed(TurnLeft))
		{
			turn -= 1f;
		}
		if (Input.IsKeyPressed(TurnRight))
		{
			turn += 1f;
		}
		return turn;
	}

	public void SetStyleCount(int count)
	{
		GD.Print("Set style count called with number: ", count);
		count = Mathf.Max(count, 0);
		if (Styles.Length != count)
		{
			int size = Styles.Length;
			System.Array.Resize(ref Styles, count);
			for (int i = size; i < count; i++)
			{
				Styles[i] = new Style();
			}
		}
	}

	public bool QueryAny()
	{
		for (int i = 0; i < Styles.Length; i++)
		{
			if (Styles[i].Query())
			{
				return true;
			}
		}
		return false;
	}

	public float PoolBias(float[] weights)
	{
		float bias = 0f;
		for (int i = 0; i < weights.Length; i++)
		{
			float _bias = Styles[i].Bias;
			float max = 0f;
			for (int j = 0; j < Styles[i].Multipliers.Length; j++)
			{
				if (Input.IsKeyPressed(Styles[i].Multipliers[j].Key))
				{
					max = Mathf.Max(max, Styles[i].Bias * Styles[i].Multipliers[j].Value);
				}
			}
			for (int j = 0; j < Styles[i].Multipliers.Length; j++)
			{
				if (Input.IsKeyPressed(Styles[i].Multipliers[j].Key))
				{
					_bias = Mathf.Min(max, _bias * Styles[i].Multipliers[j].Value);
				}
			}
			bias += weights[i] * _bias;
		}
		return bias;
	}

	[System.Serializable]
	public partial class Style : Resource
	{
		[Export] public string Name = "Wilkinson";
		[Export] public float Bias = 1f;
		[Export] public float Transition = 0.1f;
		public Key[] Keys = new Key[0];
		public bool[] Negations = new bool[0];

		[Export] public Multiplier[] Multipliers = new Multiplier[0];

		public bool Query()
		{
			if (Keys.Length == 0)
			{
				return false;
			}

			bool active = false;

			for (int i = 0; i < Keys.Length; i++)
			{
				if (!Negations[i])
				{
					if (Keys[i] == Key.None)
					{
						if (!Input.IsAnythingPressed())
						{
							active = true;
						}
					}
					else
					{
						if (Input.IsKeyPressed(Keys[i]))
						{
							active = true;
						}
					}
				}
			}

			for (int i = 0; i < Keys.Length; i++)
			{
				if (Negations[i])
				{
					if (Keys[i] == Key.None)
					{
						if (!Input.IsAnythingPressed())
						{
							active = false;
						}
					}
					else
					{
						if (Input.IsKeyPressed(Keys[i]))
						{
							active = false;
						}
					}
				}
			}

			return active;
		}

		public void SetKeyCount(int count)
		{
			count = Mathf.Max(count, 0);
			if (Keys.Length != count)
			{
				System.Array.Resize(ref Keys, count);
				System.Array.Resize(ref Negations, count);
			}
		}

		public void AddMultiplier()
		{
			ArrayExtensions.Add(ref Multipliers, new Multiplier());
		}

		public void RemoveMultiplier()
		{
			ArrayExtensions.Shrink(ref Multipliers);
		}

		[System.Serializable]
		public partial class Multiplier : Resource
		{
			[Export] public Key Key;
			[Export] public float Value;
		}
	}

#if TOOLS
	public void Inspector()
	{
		// Note: Godot uses a different editor system
		// This would need to be implemented as a custom EditorPlugin
		// or using the built-in inspector with [Export] attributes
		// The GUI drawing code would need to be completely rewritten for Godot's editor
	}
#endif

}