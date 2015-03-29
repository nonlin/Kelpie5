using UnityEngine;
using System.Collections.Generic;
using System;

public class RaycastAllSort: IComparable<RaycastAllSort> { 

	public float distance; 
	public Collider collider;
	public Vector3 point;
	public Vector3 normal; 
	public RaycastHit sortedHit;
	
	public RaycastAllSort(RaycastHit hit)
	{
		distance = hit.distance;
		collider = hit.collider;
		point = hit.point;
		normal = hit.normal;
		sortedHit = hit; 
	}
	
	public int CompareTo(RaycastAllSort other)
	{
		return distance.CompareTo(other.distance);
	}
}

