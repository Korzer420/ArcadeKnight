using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ArcadeKnight;

public abstract class Minigame
{
	#region Methods

	internal abstract MinigameData GetEntry();

	#endregion
}
