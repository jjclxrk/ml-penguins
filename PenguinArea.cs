using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents; // NOTE: new heirarchy compared to (older version of) the tutorial
using TMPro;

// NOTE: the tutorial says this needs to inherit from `Area` but it seems that 
// class no longer exists, so MonoBehaviour is (i think?) fine
public class PenguinArea : MonoBehaviour
{
	[Tooltip("The agent inside the area")]
	public PenguinAgent penguinAgent;

	[Tooltip("The baby penguin inside the area")]
	public GameObject penguinBaby;

	[Tooltip("The TextMeshPro which shows the cumulative reward of the agent")]
	public TextMeshPro cumulativeRewardText;

	[Tooltip("Prefab of a live fish")]
	public Fish fishPrefab;

	// NOTE: PenguinAcademy is no longer necessary (Academy is non-abstract)
	// private PenguinAcademy penguinAcademy;
	private List<GameObject> fishList;

	// reset the area, including fish and penguin placement
	// NOTE: the tutorial has an `override` here but as we aren't inheriting from
	//       `Area` anymore, there is nothing to override.
	public void ResetArea()
	{
		RemoveAllFish();
		PlacePenguin();
		PlaceBaby();
		// NOTE: the following to commented out lines exist in diffferent versions 
		//       of the tutorial, and they are now both outdated; the first because
		//       PenguinAcademy no longer exists and the second becaue FloatProperties
		//       no longer exists
		//   
		// SpawnFish(4, penguinAcademy.FishSpeed);
		// Academy.Instance.FloatProperties.GetPropertyWithDefault("fish_speed", 0.5f));
		SpawnFish(4, Academy.Instance.EnvironmentParameters.GetWithDefault("fish_speed", 0.5f));

	}

	// remove a specific fish from the area when it is eaten
	// params:
	// 		fishObject: the fish to be removed.
	public void RemoveSpecificFish(GameObject fishObject)
	{
		fishList.Remove(fishObject);
		Destroy(fishObject);
	}

	// the number of fish remaining
	public int FishRemaining
	{
		get { return fishList.Count; }
	}

	// choose a random position on the X-Z plane within a partial donut shape
	// params:
	// 		center:		the center of the donut
	// 		minAngle:	minimum angle of the wedge
	//		maxAngle:	maximum angle of the wedge
	//		minRadius:	minimum distance from the center
	//		maxRadius:	maximum distance from the center
	// returns:
	//		a position within the specified region
	public static Vector3 ChooseRandomPosition(Vector3 center, float minAngle, float maxAngle, float minRadius, float maxRadius)
	{
		float radius = minRadius;
		float angle = minAngle;

		if (maxRadius > minRadius)
		{
			// pick a random radius
			radius = UnityEngine.Random.Range(minRadius, maxRadius);
		}

		if (maxAngle > minAngle)
		{
			// pick a random angle
			angle = UnityEngine.Random.Range(minAngle, maxAngle);
		}

		// center position + forward vector rotated around the Y-axis by "angle" degrees, mulitplied by radius
		return center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
	}

	// removes all fish from the area
	private void RemoveAllFish()
	{
		if (fishList != null)
		{
			for (int i = 0; i < fishList.Count; i++)
			{
				if (fishList[i] != null)
				{
					Destroy(fishList[i]);
				}
			}
		}

		fishList = new List<GameObject>();
	}

	// places the penguin in the area
	private void PlacePenguin()
	{
		Rigidbody rigidbody = penguinAgent.GetComponent<Rigidbody>();
		rigidbody.velocity = Vector3.zero;
		rigidbody.angularVelocity = Vector3.zero;
		penguinAgent.transform.position = ChooseRandomPosition(transform.position, 0f, 360f, 0f, 9f) + Vector3.up * .5f;
		penguinAgent.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
	}

	// places the baby in the area
	private void PlaceBaby()
	{
		Rigidbody rigidbody = penguinBaby.GetComponent<Rigidbody>();
		rigidbody.velocity = Vector3.zero;
		rigidbody.angularVelocity = Vector3.zero;
		penguinBaby.transform.position = ChooseRandomPosition(transform.position, -45f, 45f, 4f, 9f) + Vector3.up * .5f;
		penguinBaby.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
	}

	// spawn some number of fish in the area and set their swim speed
	// params:
	// 		count: 		the number fo fish to spawn
	// 		fishSpeed:	the swim speed
	private void SpawnFish(int count, float fishSpeed)
	{
		for (int i = 0; i < count; i++)
		{
			// spawn and place the fish
			GameObject fishObject = Instantiate<GameObject>(fishPrefab.gameObject);
			fishObject.transform.position = ChooseRandomPosition(transform.position, 100f, 260f, 2f, 13f) + Vector3.up * .5f;
			fishObject.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

			// set the fish's parent to this area's transform
			fishObject.transform.SetParent(transform);

			// keep track of the fish
			fishList.Add(fishObject);

			// set the fish speed
			fishObject.GetComponent<Fish>().fishSpeed = fishSpeed;
		}
	}

	// called when the game starts
	private void Start()
	{
		// NOTE: PenguinAcademy no longer exists
		// penguinAcademy = FindObjectOfType<PenguinAcademy>();
		ResetArea();
	}

	// called every frame
	private void Update()
	{
		cumulativeRewardText.text = penguinAgent.GetCumulativeReward().ToString("0.00");
	}

}
