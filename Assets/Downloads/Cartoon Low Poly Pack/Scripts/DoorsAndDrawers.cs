using UnityEngine;
using System.Collections;

public class DoorsAndDrawers : MonoBehaviour 
{
	public bool useLocation;
	public bool useRotation;
	public Vector3 startLoc;
	public Vector3 endLoc;

	public Quaternion startRot;
	public Quaternion endRot;

	public float speed = 0.1f;

	private bool open;
	[HideInInspector]public bool activate;

	void Update () 
	{
		if(activate)
		{
			OpenClose();
		}
	}

	public void Activate()
	{
		activate = true;
	}

	public void OpenClose()
	{	
		if(open && useLocation)
		{
			transform.localPosition = Vector3.Lerp(transform.localPosition,startLoc,speed);
			if(transform.localPosition == startLoc)
			{
				activate = false;
				open = false;
			}
		}
		else if(!open && useLocation)
		{
			transform.localPosition = Vector3.Lerp(transform.localPosition,endLoc,speed);
			if(transform.localPosition == endLoc)
			{
				activate = false;
				open = true;
			}
		}
		if(open && useRotation)
		{
			transform.localRotation = Quaternion.Lerp(transform.localRotation,startRot,speed);
			if(transform.localRotation == startRot)
			{
				activate = false;
				open = false;
			}
		}
		else if(!open && useRotation)
		{
			transform.localRotation = Quaternion.Lerp(transform.localRotation,endRot,speed);
			if(transform.localRotation == endRot)
			{
				activate = false;
				open = true;
			}
		}
	}

	public void SaveStartLocation()
	{
		startLoc = transform.localPosition;
	}
	public void SaveEndLocation()
	{
		endLoc = transform.localPosition;
	}
	public void SaveStartRotation()
	{
		startRot = transform.localRotation;
	}
	public void SaveEndRotation()
	{
		endRot = transform.localRotation;
	}
}
