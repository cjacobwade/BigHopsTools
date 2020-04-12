using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICustomMesh
{
	event System.Action<ICustomMesh> OnMeshChanged;
}
