using UnityEngine;
using System.Collections;

public class StreetLight : MonoBehaviour 
{
	public bool lightToggle;
	public GameObject[] lightPoles;

	void Start()
	{
		Toggle();
	}

	public void Toggle()
	{
		if(lightToggle)
		{
			lightPoles[0].SetActive(true);
			lightPoles[1].SetActive(false);
		}
		else
		{
			lightPoles[0].SetActive(false);
			lightPoles[1].SetActive(true);
		}
	}
}
