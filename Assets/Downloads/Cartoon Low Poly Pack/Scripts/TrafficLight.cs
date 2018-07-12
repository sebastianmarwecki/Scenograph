using UnityEngine;
using System.Collections;

public class TrafficLight : MonoBehaviour
{
	public GameObject[] trafficLights;
	public bool startWithRed = true;
	public float redLightTime = 15f;
	public float yellowLightTime = 2f;
	public float greenLightTime = 10f;

	IEnumerator RedStart()
	{
		trafficLights[0].SetActive(true);
		trafficLights[1].SetActive(false);
		trafficLights[2].SetActive(false);
		yield return new WaitForSeconds(redLightTime);
		trafficLights[0].SetActive(false);
		trafficLights[1].SetActive(true);
		trafficLights[2].SetActive(false);
		yield return new WaitForSeconds(yellowLightTime);
		trafficLights[0].SetActive(false);
		trafficLights[1].SetActive(false);
		trafficLights[2].SetActive(true);
		yield return new WaitForSeconds(greenLightTime);
		StartCoroutine(RedStart());

	}
	IEnumerator GreenStart()
	{
		trafficLights[0].SetActive(false);
		trafficLights[1].SetActive(false);
		trafficLights[2].SetActive(true);
		yield return new WaitForSeconds(greenLightTime);
		trafficLights[0].SetActive(true);
		trafficLights[1].SetActive(false);
		trafficLights[2].SetActive(false);
		yield return new WaitForSeconds(redLightTime);
		trafficLights[0].SetActive(false);
		trafficLights[1].SetActive(true);
		trafficLights[2].SetActive(false);
		yield return new WaitForSeconds(yellowLightTime);
		StartCoroutine(GreenStart());
	}

	void Start ()
	{
		Reset();
	}

	public void Reset()
	{
		if(startWithRed)
			StartCoroutine(RedStart());
		else
			StartCoroutine(GreenStart());
	}
}
