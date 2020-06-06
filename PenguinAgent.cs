using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents; // NOTE: new heirarchy compared to (older version of) the tutorial
using Unity.MLAgents.Sensors; // Sensors now necessary for `CollectionObservations` method

public class PenguinAgent : Agent
{
    [Tooltip("How fast the agent moves forward")]
    public float moveSpeed = 5f;

    [Tooltip("How fast the agent turns")]
    public float turnSpeed = 180f;

    [Tooltip("Prefab of the heart that appears when the baby is fed")]
    public GameObject heartPrefab;

    [Tooltip("Prefab of the regurgitated fish that appears when the baby is fed")]
    public GameObject regurgitatedFishPrefab;


    private PenguinArea penguinArea;
    new private Rigidbody rigidbody;
    private GameObject baby;
    private bool isFull; // If true, penguin has a full stomach
    private float feedRadius = 0f;

    // initial setup, called once when the agent is first enabled
    // NOTE: method name changed from `InitializeAgent` in v0.15
    public override void Initialize()
    {
        // NOTE: likewise, the call to the parent class is `Initialize` not `InitializeAgent`
    	base.Initialize();
    	penguinArea = GetComponentInParent<PenguinArea>();
    	baby = penguinArea.penguinBaby;
    	rigidbody = GetComponent<Rigidbody>();
    }

    // perform actions based on a vector of numbers
    // params:
    //		vectorAction: the list of actions to take
    // NOTE: method name changed from `AgentAction` in v0.15
    public override void OnActionReceived(float[] vectorAction) 
    {
    	// convert the first action to forward movement
    	float forwardAmount = vectorAction[0];

    	// convert the second action to turning left or right
    	float turnAmount = 0f;
    	if (vectorAction[1] == 1f)
    	{
    		turnAmount = -1f; // turn left
    	}
    	else if (vectorAction[1] == 2f)
    	{
    		turnAmount = 1f; // turn right
    	}

    	// apply movement
    	rigidbody.MovePosition(transform.position + transform.forward * forwardAmount * moveSpeed * Time.fixedDeltaTime);
    	transform.Rotate(transform.up * turnAmount * turnSpeed * Time.fixedDeltaTime);

    	// apply a tiny negative reward each step to encourage action
        // NOTE: `maxStep` (camelCase) has been changed to `MaxStep` (PascalCase)
    	if (MaxStep > 0)
    	{
    	    AddReward(-1f / MaxStep);
    	}
    }

    // read inputs from the keyboard and convert them into a list of actions.
    // this is called only when the player wants to control the agent and has
    // set Behavior Type to "Heuristic Only" in the Behaviour Parameters inspector.
    // params:
    // 		actionsOut: the array which will be updated with the heuristic actions
    //
    // NOTE: the method signature for `Heuristic` has changed; instead of returning
    //       an action array (float[]), it now makes the changes in-place on the 
    //       `actionsOut` parameter
    public override void Heuristic(float[] actionsOut)
    {
    	float forwardAction = 0f;
    	float turnAction = 0f;
    	if (Input.GetKey(KeyCode.W))
    	{
    		// move forward
    		forwardAction = 1f;
    	}
    	if (Input.GetKey(KeyCode.A))
    	{
    		// turn left
    		turnAction = 1f;
    	}
    	else if (Input.GetKey(KeyCode.D))
    	{
    		// turn right
    		turnAction = 2f;
    	}

    	// put the actions into the array and return
        actionsOut[0] = forwardAction;
        actionsOut[1] = turnAction;
        // NOTE: the return is no longer necessary with the new method signature
    	// return new float[] { forwardAction, turnAction };
    }

    // reset the agent and the area
    // NOTE: method name changed from `AgentReset` in v0.15
    public override void OnEpisodeBegin()
    {
    	isFull = false;
    	penguinArea.ResetArea();
        // NOTE: FloatProperties is no more...
        // feedRadius = Academy.Instance.FloatProperties.GetPropertyWithDefault("feed_radius", 0f);
        feedRadius = Academy.Instance.EnvironmentParameters.GetWithDefault("feed_radius", 0f);
    }

    // collects all non-Raycast observations
    // params:
    //      sensor: a sensor for vector observations
    // NOTE: the requirement for this `sensor` parameter is a new addition, 
    //       and isn't mentioned in the tutorial.
    //       the body of the method features calls to `sensor.AddObservation`
    //       rather than the deprecated `AddVectorObs`
    public override void CollectObservations(VectorSensor sensor)
    {
    	// whether the penguin has eaten a fish (1 float = 1 value)
    	sensor.AddObservation(isFull);

    	// distance to the baby (1 float = 1 value)
    	sensor.AddObservation(Vector3.Distance(baby.transform.position, transform.position));

    	// direction to the baby (1 Vector3 = 3 values)
    	sensor.AddObservation((baby.transform.position - transform.position).normalized);

    	// direction penguin is facing (1 Vector3 = 3 values)
    	sensor.AddObservation(transform.forward);

    	// 1 + 1 + 3 + 3 = 8 values
    }

    // check if the penguin is close enough to the baby and then try to 
    // regurgitate the fish to feed it
    private void FixedUpdate()
    {
        // request a decision every 5 steps. RequestDecision() automatically
        // calls RequestAction(), but for steps in between, we need to call it
        // explicitly to take action using the results of the previous decision
        // NOTE: previously called `GetStepCount`
        if (StepCount % 5 == 0)
        {
            RequestDecision();
        }
        else
        {
            RequestAction();
        }

        // test if the agent is close enough to feed the baby
        if (Vector3.Distance(transform.position, baby.transform.position) < feedRadius)
        {
            // close enough, try to feed the baby
            RegurgitateFish();
        }
    }

    // when the agent collides with something, take action
    // params:
    //      collision: the collision information
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag("fish"))
        {
            // try to eat the fish
            EatFish(collision.gameObject);
        }
        else if (collision.transform.CompareTag("baby"))
        {
            // try to feed the baby
            RegurgitateFish();
        }
    }

    // check if agent is full, if not, eat the fish and get a reward
    // params:
    //      fishObject: the fish to eat
    private void EatFish(GameObject fishObject)
    {
        if (isFull) return;
   
        isFull = true;
        penguinArea.RemoveSpecificFish(fishObject);
        AddReward(1f);
    }

    // check if agent is full, if not, feed the baby
    private void RegurgitateFish()
    {
        if (!isFull) return; // nothing to regurgitate

        isFull = false;

        // spawn regurgitated fish, with an auto-destruct timer
        GameObject regurgitatedFish = Instantiate<GameObject>(regurgitatedFishPrefab);
        regurgitatedFish.transform.parent = transform.parent;
        regurgitatedFish.transform.position = baby.transform.position;
        Destroy(regurgitatedFish, 4f);

        // spawn heart, with an auto-destruct timer
        GameObject heart = Instantiate<GameObject>(heartPrefab);
        heart.transform.parent = transform.parent;
        heart.transform.position = baby.transform.position + Vector3.up;
        Destroy(heart, 4f);

        AddReward(1f);

        if (penguinArea.FishRemaining <= 0)
        {
            // NOTE: method name changed from `Done` in v0.15
            EndEpisode();
        }
    }
}
